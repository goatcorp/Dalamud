using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Utility;

namespace Dalamud.Plugin.Internal.Profiles;

/// <summary>
/// Class responsible for managing plugin profiles.
/// </summary>
[ServiceManager.BlockingEarlyLoadedService($"Data provider for {nameof(PluginManager)}.")]
internal class ProfileManager : IServiceType
{
    private static readonly ModuleLog Log = new("PROFMAN");
    private readonly DalamudConfiguration config;

    private readonly List<Profile> profiles = new();

    private volatile bool isBusy = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileManager"/> class.
    /// </summary>
    /// <param name="config">Dalamud config.</param>
    [ServiceManager.ServiceConstructor]
    public ProfileManager(DalamudConfiguration config)
    {
        this.config = config;

        this.LoadProfilesFromConfigInitially();
    }

    /// <summary>
    /// Gets the default profile.
    /// </summary>
    public Profile DefaultProfile
    {
        get
        {
            lock (this.profiles)
                return this.profiles.First(x => x.IsDefaultProfile);
        }
    }

    /// <summary>
    /// Gets all profiles, including the default profile.
    /// </summary>
    public IEnumerable<Profile> Profiles => this.profiles;

    /// <summary>
    /// Gets a value indicating whether or not the profile manager is busy enabling/disabling plugins.
    /// </summary>
    public bool IsBusy => this.isBusy;
    
    /// <summary>
    /// Get a disposable that will lock the profile list while it is not disposed.
    /// You must NEVER use this in async code.
    /// </summary>
    /// <returns>The aforementioned disposable.</returns>
    public IDisposable GetSyncScope() => new ScopedSyncRoot(this.profiles);

    /// <summary>
    /// Check if any enabled profile wants a specific plugin enabled.
    /// </summary>
    /// <param name="workingPluginId">The ID of the plugin.</param>
    /// <param name="internalName">The internal name of the plugin, if available.</param>
    /// <param name="defaultState">The state the plugin shall be in, if it needs to be added.</param>
    /// <param name="addIfNotDeclared">Whether or not the plugin should be added to the default preset, if it's not present in any preset.</param>
    /// <returns>Whether or not the plugin shall be enabled.</returns>
    public async Task<bool> GetWantStateAsync(Guid workingPluginId, string? internalName, bool defaultState, bool addIfNotDeclared = true)
    {
        var want = false;
        var wasInAnyProfile = false;
        
        lock (this.profiles)
        {
            foreach (var profile in this.profiles)
            {
                var state = profile.WantsPlugin(workingPluginId);
                if (state.HasValue)
                {
                    want = want || (profile.IsEnabled && state.Value);
                    wasInAnyProfile = true;
                }
            }
        }

        if (!wasInAnyProfile && addIfNotDeclared)
        {
            Log.Warning("'{Guid}'('{InternalName}') was not in any profile, adding to default with {Default}", workingPluginId, internalName, defaultState);
            await this.DefaultProfile.AddOrUpdateAsync(workingPluginId, internalName, defaultState, false);

            return defaultState;
        }

        return want;
    }

    /// <summary>
    /// Check whether a plugin is declared in any profile.
    /// </summary>
    /// <param name="workingPluginId">The ID of the plugin.</param>
    /// <returns>Whether or not the plugin is in any profile.</returns>
    public bool IsInAnyProfile(Guid workingPluginId)
    {
        lock (this.profiles)
            return this.profiles.Any(x => x.WantsPlugin(workingPluginId) != null);
    }

    /// <summary>
    /// Check whether a plugin is only in the default profile.
    /// A plugin can never be in the default profile if it is in any other profile.
    /// </summary>
    /// <param name="workingPluginId">The ID of the plugin.</param>
    /// <returns>Whether or not the plugin is in the default profile.</returns>
    public bool IsInDefaultProfile(Guid workingPluginId)
        => this.DefaultProfile.WantsPlugin(workingPluginId) != null;

    /// <summary>
    /// Add a new profile.
    /// </summary>
    /// <returns>The added profile.</returns>
    public Profile AddNewProfile()
    {
        var model = new ProfileModelV1
        {
            Guid = Guid.NewGuid(),
            Name = this.GenerateUniqueProfileName(Loc.Localize("PluginProfilesNewProfile", "New Collection")),
            IsEnabled = false,
        };

        this.config.SavedProfiles!.Add(model);
        this.config.QueueSave();

        var profile = new Profile(this, model, false, false);
        this.profiles.Add(profile);

        return profile;
    }

    /// <summary>
    /// Clone a specified profile.
    /// </summary>
    /// <param name="toClone">The profile to clone.</param>
    /// <returns>The newly cloned profile.</returns>
    public Profile CloneProfile(Profile toClone)
    {
        var newProfile = this.ImportProfile(toClone.Model.SerializeForShare());
        if (newProfile == null)
            throw new Exception("New profile was null while cloning");

        return newProfile;
    }

    /// <summary>
    /// Import a profile with a sharing string.
    /// </summary>
    /// <param name="data">The sharing string to import.</param>
    /// <returns>The imported profile, or null, if the string was invalid.</returns>
    public Profile? ImportProfile(string data)
    {
        var newModel = ProfileModel.Deserialize(data);
        if (newModel == null)
            return null;

        newModel.Guid = Guid.NewGuid();
        newModel.Name = this.GenerateUniqueProfileName(newModel.Name.IsNullOrEmpty() ? "Unknown Collection" : newModel.Name);
        if (newModel is ProfileModelV1 modelV1)
        {
            // Disable it
            modelV1.IsEnabled = false;
            
            // Try to find matching plugins for all plugins in the profile
            var pm = Service<PluginManager>.Get();
            foreach (var plugin in modelV1.Plugins)
            {
                var installedPlugin = pm.InstalledPlugins.FirstOrDefault(x => x.Manifest.InternalName == plugin.InternalName);
                if (installedPlugin != null)
                {
                    Log.Information("Satisfying plugin {InternalName} for profile {Name} with {Guid}", plugin.InternalName, newModel.Name, installedPlugin.EffectiveWorkingPluginId);
                    plugin.WorkingPluginId = installedPlugin.EffectiveWorkingPluginId;
                }
                else
                {
                    Log.Warning("Couldn't find plugin {InternalName} for profile {Name}", plugin.InternalName, newModel.Name);
                    plugin.WorkingPluginId = Guid.Empty;
                }
            }
        }

        this.config.SavedProfiles!.Add(newModel);
        this.config.QueueSave();

        var profile = new Profile(this, newModel, false, false);
        this.profiles.Add(profile);

        return profile;
    }

    /// <summary>
    /// Go through all profiles and plugins, and enable/disable plugins they want active.
    /// This will block until all plugins have been loaded/reloaded.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ApplyAllWantStatesAsync()
    {
        if (this.isBusy)
            throw new Exception("Already busy, this must not run in parallel. Check before starting another apply!");

        this.isBusy = true;
        Log.Information("Getting want states...");

        List<ProfilePluginEntry> wantActive;
        lock (this.profiles)
        {
            wantActive = this.profiles
                             .Where(x => x.IsEnabled)
                             .SelectMany(profile => profile.Plugins.Where(plugin => plugin.IsEnabled))
                             .Distinct().ToList();
        }

        foreach (var profilePluginEntry in wantActive)
        {
            Log.Information("\t=> Want {Name}({WorkingPluginId})", profilePluginEntry.InternalName, profilePluginEntry.WorkingPluginId);
        }

        Log.Information("Applying want states...");

        var tasks = new List<Task>();

        var pm = Service<PluginManager>.Get();
        foreach (var installedPlugin in pm.InstalledPlugins)
        {
            var wantThis = wantActive.Any(x => x.WorkingPluginId == installedPlugin.EffectiveWorkingPluginId);
            switch (wantThis)
            {
                case true when !installedPlugin.IsLoaded:
                    if (installedPlugin.ApplicableForLoad)
                    {
                        Log.Information("\t=> Enabling {Name}", installedPlugin.Manifest.InternalName);
                        tasks.Add(installedPlugin.LoadAsync(PluginLoadReason.Installer));
                    }
                    else
                    {
                        Log.Warning("\t=> {Name} wanted active, but not applicable", installedPlugin.Manifest.InternalName);
                    }

                    break;
                case false when installedPlugin.IsLoaded:
                    Log.Information("\t=> Disabling {Name}", installedPlugin.Manifest.InternalName);
                    tasks.Add(installedPlugin.UnloadAsync());
                    break;
            }
        }

        // This is probably not ideal... Might need to rethink the error handling strategy for this.
        try
        {
            await Task.WhenAll(tasks.ToArray());
        }
        catch (Exception e)
        {
            Log.Error(e, "Couldn't apply state for one or more plugins");
        }

        Log.Information("Applied!");
        this.isBusy = false;
    }

    /// <summary>
    /// Delete a profile.
    /// </summary>
    /// <remarks>
    /// You should definitely apply states after this. It doesn't do it for you.
    /// </remarks>
    /// <param name="profile">The profile to delete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task DeleteProfileAsync(Profile profile)
    {
        // We need to remove all plugins from the profile first, so that they are re-added to the default profile if needed
        foreach (var plugin in profile.Plugins.ToArray())
        {
            await profile.RemoveAsync(plugin.WorkingPluginId, false);
        }

        if (!this.config.SavedProfiles!.Remove(profile.Model))
            throw new Exception("Couldn't remove profile from models");

        if (!this.profiles.Remove(profile))
            throw new Exception("Couldn't remove runtime profile");

        this.config.QueueSave();
    }

    /// <summary>
    /// This function tries to migrate all plugins with this internalName which do not have
    /// a GUID to the specified GUID.
    /// This is best-effort and will probably work well for anyone that only uses regular plugins.
    /// </summary>
    /// <param name="internalName">InternalName of the plugin to migrate.</param>
    /// <param name="newGuid">Guid to use.</param>
    public void MigrateProfilesToGuidsForPlugin(string internalName, Guid newGuid)
    {
        lock (this.profiles)
        {
            foreach (var profile in this.profiles)
                profile.MigrateProfilesToGuidsForPlugin(internalName, newGuid);
        }
    }
    
    /// <summary>
    /// Validate profiles for errors.
    /// </summary>
    /// <exception cref="Exception">Thrown when a profile is not sane.</exception>
    public void ParanoiaValidateProfiles()
    {
        foreach (var profile in this.profiles)
        {
            var seenIds = new List<Guid>();

            foreach (var pluginEntry in profile.Plugins)
            {
                if (seenIds.Contains(pluginEntry.WorkingPluginId))
                    throw new Exception($"Plugin '{pluginEntry.WorkingPluginId}'('{pluginEntry.InternalName}') is twice in profile '{profile.Guid}'('{profile.Name}')");
                
                seenIds.Add(pluginEntry.WorkingPluginId);
            }
        }
    }

    private string GenerateUniqueProfileName(string startingWith)
    {
        if (this.profiles.All(x => x.Name != startingWith))
            return startingWith;

        startingWith = Regex.Replace(startingWith, @" \(.* Mix\)", string.Empty);

        while (true)
        {
            var newName = $"{startingWith} ({CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Util.GetRandomName())} Mix)";

            if (this.profiles.All(x => x.Name != newName))
                return newName;
        }
    }

    private void LoadProfilesFromConfigInitially()
    {
        this.config.DefaultProfile ??= new ProfileModelV1();
        this.profiles.Add(new Profile(this, this.config.DefaultProfile, true, true));

        this.config.SavedProfiles ??= new List<ProfileModel>();
        foreach (var profileModel in this.config.SavedProfiles)
        {
            this.profiles.Add(new Profile(this, profileModel, false, true));
        }

        this.config.QueueSave();
    }
}
