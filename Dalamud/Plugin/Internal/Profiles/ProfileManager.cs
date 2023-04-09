using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;

namespace Dalamud.Plugin.Internal.Profiles;

/// <summary>
/// Class responsible for managing plugin profiles.
/// </summary>
[ServiceManager.BlockingEarlyLoadedService]
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
    public Profile DefaultProfile => this.profiles.First(x => x.IsDefaultProfile);

    /// <summary>
    /// Gets all profiles, including the default profile.
    /// </summary>
    public IEnumerable<Profile> Profiles => this.profiles;

    /// <summary>
    /// Gets a value indicating whether or not the profile manager is busy enabling/disabling plugins.
    /// </summary>
    public bool IsBusy => this.isBusy;

    /// <summary>
    /// Check if any enabled profile wants a specific plugin enabled.
    /// </summary>
    /// <param name="internalName">The internal name of the plugin.</param>
    /// <param name="isBoot">Whether this method is called during bootup plugin load.</param>
    /// <returns>Whether or not the plugin shall be enabled.</returns>
    /// <exception cref="InvalidOperationException">Thrown when something odd has happened.</exception>
    public bool GetWantState(string internalName, bool isBoot)
    {
        var want = false;
        var wasInAnyProfile = false;

        foreach (var profile in this.profiles.Where(x => x.IsEnabled))
        {
            var state = profile.WantsPlugin(internalName);
            Log.Verbose("Checking {Name} in {Profile}: {State}", internalName, profile.Guid, state == null ? "null" : state);

            if (state.HasValue)
            {
                want = want || state.Value;
                wasInAnyProfile = true;
            }
        }

        // Can we just do migration here?
        if (!wasInAnyProfile && isBoot)
        {
            Log.Warning("{Name} was not in any profile during boot, adding to default", internalName);
            this.DefaultProfile.AddOrUpdate(internalName, false, false);
            return false;
        }

        return want;
    }

    /// <summary>
    /// Check whether a plugin is declared in any profile.
    /// </summary>
    /// <param name="internalName">The internal name of the plugin.</param>
    /// <returns>Whether or not the plugin is in any profile.</returns>
    public bool IsInAnyProfile(string internalName)
        => this.profiles.Any(x => x.WantsPlugin(internalName) != null);

    /// <summary>
    /// Check whether a plugin is only in the default profile.
    /// A plugin can never be in the default profile if it is in any other profile.
    /// </summary>
    /// <param name="internalName">The internal name of the plugin.</param>
    /// <returns>Whether or not the plugin is in the default profile.</returns>
    public bool IsInDefaultProfile(string internalName)
        => this.DefaultProfile.WantsPlugin(internalName) != null;

    /// <summary>
    /// Add a new profile.
    /// </summary>
    /// <returns>The added profile.</returns>
    public Profile AddNewProfile()
    {
        var model = new ProfileModelV1
        {
            Guid = Guid.NewGuid(),
            Name = this.GenerateUniqueProfileName(Loc.Localize("PluginProfilesNewProfile", "New Profile")),
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
        var newProfile = this.ImportProfile(toClone.Model.Serialize());
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
        newModel.Name = this.GenerateUniqueProfileName(newModel.Name.IsNullOrEmpty() ? "Unknown Profile" : newModel.Name);
        if (newModel is ProfileModelV1 modelV1)
            modelV1.IsEnabled = false;

        this.config.SavedProfiles!.Add(newModel);
        this.config.QueueSave();

        return new Profile(this, newModel, false, false);
    }

    /// <summary>
    /// Go through all profiles and plugins, and enable/disable plugins they want active.
    /// This will block until all plugins have been loaded/reloaded!
    /// </summary>
    public void ApplyAllWantStates()
    {
        this.isBusy = true;
        Log.Information("Getting want states...");

        var wantActive = this.profiles
                             .Where(x => x.IsEnabled)
                             .SelectMany(profile => profile.Plugins.Where(plugin => plugin.IsEnabled)
                                                           .Select(plugin => plugin.InternalName))
                             .Distinct().ToList();

        foreach (var internalName in wantActive)
        {
            Log.Information("\t=> Want {Name}", internalName);
        }

        Log.Information("Applying want states...");

        var pm = Service<PluginManager>.Get();
        var tasks = new List<Task>();

        foreach (var installedPlugin in pm.InstalledPlugins)
        {
            if (installedPlugin.IsDev)
                continue;

            var wantThis = wantActive.Contains(installedPlugin.Manifest.InternalName);
            switch (wantThis)
            {
                case true when !installedPlugin.IsLoaded:
                    Log.Information("\t=> Enabling {Name}", installedPlugin.Manifest.InternalName);
                    tasks.Add(installedPlugin.LoadAsync(PluginLoadReason.Installer));
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
            Task.WaitAll(tasks.ToArray());
        }
        catch (Exception e)
        {
            Log.Error(e, "Couldn't apply state for one or more plugins");
        }

        Log.Information("Applied!");
        this.isBusy = false;
    }

    private string GenerateUniqueProfileName(string startingWith)
    {
        if (this.profiles.All(x => x.Name != startingWith))
            return startingWith;

        while (true)
        {
            var newName = $"{startingWith} ({Util.GetRandomName()} Mix)";

            if (this.profiles.All(x => x.Name != newName))
                return startingWith;
        }
    }

    private void LoadProfilesFromConfigInitially()
    {
        var needMigration = false;
        if (this.config.DefaultProfile == null)
        {
            this.config.DefaultProfile = new ProfileModelV1();
            needMigration = true;
        }

        this.profiles.Add(new Profile(this, this.config.DefaultProfile, true, true));

        if (needMigration)
        {
            this.MigratePluginsIntoDefaultProfile();
        }

        this.config.SavedProfiles ??= new List<ProfileModel>();
        foreach (var profileModel in this.config.SavedProfiles)
        {
            this.profiles.Add(new Profile(this, profileModel, false, true));
        }

        this.config.QueueSave();
    }

    // This duplicates some of the original handling in PM; don't care though
    private void MigratePluginsIntoDefaultProfile()
    {
        var pluginDirectory = new DirectoryInfo(Service<DalamudStartInfo>.Get().PluginDirectory!);
        var pluginDefs = new List<PluginDef>();

        Log.Information($"Now migrating plugins at {pluginDirectory} into profiles");

        // Nothing to migrate
        if (!pluginDirectory.Exists)
        {
            Log.Information("\t=> Plugin directory didn't exist, nothing to migrate");
            return;
        }

        // Add installed plugins. These are expected to be in a specific format so we can look for exactly that.
        foreach (var pluginDir in pluginDirectory.GetDirectories())
        {
            var versionsDefs = new List<PluginDef>();
            foreach (var versionDir in pluginDir.GetDirectories())
            {
                try
                {
                    var dllFile = new FileInfo(Path.Combine(versionDir.FullName, $"{pluginDir.Name}.dll"));
                    var manifestFile = LocalPluginManifest.GetManifestFile(dllFile);

                    if (!manifestFile.Exists)
                        continue;

                    var manifest = LocalPluginManifest.Load(manifestFile);
                    versionsDefs.Add(new PluginDef(dllFile, manifest, false));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Could not load manifest for installed at {Directory}", versionDir.FullName);
                }
            }

            try
            {
                pluginDefs.Add(versionsDefs.MaxBy(x => x.Manifest!.EffectiveVersion));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Couldn't choose best version for plugin: {Name}", pluginDir.Name);
            }
        }

        var defaultProfile = this.DefaultProfile;
        foreach (var pluginDef in pluginDefs)
        {
            if (pluginDef.Manifest == null)
            {
                Log.Information($"\t=> Skipping DLL at {pluginDef.DllFile.FullName}, no valid manifest");
                continue;
            }

            // OK for migration code
#pragma warning disable CS0618
            defaultProfile.AddOrUpdate(pluginDef.Manifest.InternalName, !pluginDef.Manifest.Disabled, false);
            Log.Information(
                $"\t=> Added {pluginDef.Manifest.InternalName} to default profile with {!pluginDef.Manifest.Disabled}");
#pragma warning restore CS0618
        }
    }
}
