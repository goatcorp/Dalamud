using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Utility;

namespace Dalamud.Plugin.Internal.Profiles;

/// <summary>
/// Class representing a single runtime profile.
/// </summary>
internal class Profile
{
    private static readonly ModuleLog Log = new("PROFILE");

    private readonly ProfileManager manager;
    private readonly ProfileModelV1 modelV1;

    /// <summary>
    /// Initializes a new instance of the <see cref="Profile"/> class.
    /// </summary>
    /// <param name="manager">The manager this profile belongs to.</param>
    /// <param name="model">The model this profile is tied to.</param>
    /// <param name="isDefaultProfile">Whether or not this profile is the default profile.</param>
    /// <param name="isBoot">Whether or not this profile was initialized during bootup.</param>
    public Profile(ProfileManager manager, ProfileModel model, bool isDefaultProfile, bool isBoot)
    {
        this.manager = manager;
        this.IsDefaultProfile = isDefaultProfile;
        this.modelV1 = model as ProfileModelV1 ??
                       throw new ArgumentException("Model was null or unhandled version");

        // We don't actually enable plugins here, PM will do it on bootup
        if (isDefaultProfile)
        {
            // Default profile cannot be disabled
            this.IsEnabled = this.modelV1.IsEnabled = true;
            this.Name = this.modelV1.Name = "DEFAULT";
        }
        else if (this.modelV1.AlwaysEnableOnBoot && isBoot)
        {
            this.IsEnabled = true;
            Log.Verbose("{Guid} set enabled because bootup", this.modelV1.Guid);
        }
        else if (this.modelV1.IsEnabled)
        {
            this.IsEnabled = true;
            Log.Verbose("{Guid} set enabled because remember", this.modelV1.Guid);
        }
        else
        {
            Log.Verbose("{Guid} not enabled", this.modelV1.Guid);
        }
    }

    /// <summary>
    /// Gets or sets this profile's name.
    /// </summary>
    public string Name
    {
        get => this.modelV1.Name;
        set
        {
            this.modelV1.Name = value;
            Service<DalamudConfiguration>.Get().QueueSave();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this profile shall always be enabled at boot.
    /// </summary>
    public bool AlwaysEnableAtBoot
    {
        get => this.modelV1.AlwaysEnableOnBoot;
        set
        {
            this.modelV1.AlwaysEnableOnBoot = value;
            Service<DalamudConfiguration>.Get().QueueSave();
        }
    }

    /// <summary>
    /// Gets this profile's guid.
    /// </summary>
    public Guid Guid => this.modelV1.Guid;

    /// <summary>
    /// Gets a value indicating whether or not this profile is currently enabled.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether or not this profile is the default profile.
    /// </summary>
    public bool IsDefaultProfile { get; }

    /// <summary>
    /// Gets all plugins declared in this profile.
    /// </summary>
    public IEnumerable<ProfilePluginEntry> Plugins =>
        this.modelV1.Plugins.Select(x => new ProfilePluginEntry(x.InternalName, x.WorkingPluginId, x.IsEnabled));

    /// <summary>
    /// Gets this profile's underlying model.
    /// </summary>
    public ProfileModel Model => this.modelV1;

    /// <summary>
    /// Get a disposable that will lock the plugin list while it is not disposed.
    /// You must NEVER use this in async code.
    /// </summary>
    /// <returns>The aforementioned disposable.</returns>
    public IDisposable GetSyncScope() => new ScopedSyncRoot(this);

    /// <summary>
    /// Set this profile's state. This cannot be called for the default profile.
    /// This will block until all states have been applied.
    /// </summary>
    /// <param name="enabled">Whether or not the profile is enabled.</param>
    /// <param name="apply">Whether or not the current state should immediately be applied.</param>
    /// <exception cref="InvalidOperationException">Thrown when an untoggleable profile is toggled.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SetStateAsync(bool enabled, bool apply = true)
    {
        if (this.IsDefaultProfile)
            throw new InvalidOperationException("Cannot set state of default profile");

        Debug.Assert(this.IsEnabled != enabled, "Trying to set state of a profile to the same state");
        this.IsEnabled = this.modelV1.IsEnabled = enabled;
        Log.Verbose("Set state {State} for {Guid}", enabled, this.modelV1.Guid);

        Service<DalamudConfiguration>.Get().QueueSave();

        if (apply)
            await this.manager.ApplyAllWantStatesAsync();
    }

    /// <summary>
    /// Check if this profile contains a specific plugin, and if it is enabled.
    /// </summary>
    /// <param name="workingPluginId">The ID of the plugin.</param>
    /// <returns>Null if this profile does not declare the plugin, true if the profile declares the plugin and wants it enabled, false if the profile declares the plugin and does not want it enabled.</returns>
    public bool? WantsPlugin(Guid workingPluginId)
    {
        lock (this)
        {
            var entry = this.modelV1.Plugins.FirstOrDefault(x => x.WorkingPluginId == workingPluginId);
            return entry?.IsEnabled;
        }
    }

    /// <summary>
    /// Add a plugin to this profile with the desired state, or change the state of a plugin in this profile.
    /// This will block until all states have been applied.
    /// </summary>
    /// <param name="workingPluginId">The ID of the plugin.</param>
    /// <param name="internalName">The internal name of the plugin, if available.</param>
    /// <param name="state">Whether or not the plugin should be enabled.</param>
    /// <param name="apply">Whether or not the current state should immediately be applied.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task AddOrUpdateAsync(Guid workingPluginId, string? internalName, bool state, bool apply = true)
    {
        Debug.Assert(workingPluginId != Guid.Empty, "Trying to add plugin with empty guid");
        
        lock (this)
        {
            var existing = this.modelV1.Plugins.FirstOrDefault(x => x.WorkingPluginId == workingPluginId);
            if (existing != null)
            {
                existing.IsEnabled = state;
            }
            else
            {
                this.modelV1.Plugins.Add(new ProfileModelV1.ProfileModelV1Plugin
                {
                    InternalName = internalName,
                    WorkingPluginId = workingPluginId,
                    IsEnabled = state,
                });
            }
        }
        
        Log.Information("Adding plugin {Plugin}({Guid}) to profile {Profile} with state {State}", internalName, workingPluginId, this.Guid, state);
        
        // We need to remove this plugin from the default profile, if it declares it.
        if (!this.IsDefaultProfile && this.manager.DefaultProfile.WantsPlugin(workingPluginId) != null)
        {
            Log.Information("=> Removing plugin {Plugin}({Guid}) from default profile", internalName, workingPluginId);
            await this.manager.DefaultProfile.RemoveAsync(workingPluginId, false);
        }

        Service<DalamudConfiguration>.Get().QueueSave();

        if (apply)
            await this.manager.ApplyAllWantStatesAsync();
    }

    /// <summary>
    /// Remove a plugin from this profile.
    /// This will block until all states have been applied.
    /// </summary>
    /// <param name="workingPluginId">The ID of the plugin.</param>
    /// <param name="apply">Whether or not the current state should immediately be applied.</param>
    /// <param name="checkDefault">
    /// Whether or not to throw when a plugin is removed from the default profile, without being in another profile.
    /// Used to prevent orphan plugins, but can be ignored when cleaning up old entries.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task RemoveAsync(Guid workingPluginId, bool apply = true, bool checkDefault = true)
    {
        ProfileModelV1.ProfileModelV1Plugin entry;
        lock (this)
        {
            entry = this.modelV1.Plugins.FirstOrDefault(x => x.WorkingPluginId == workingPluginId);
            if (entry == null)
                throw new PluginNotFoundException(workingPluginId);

            if (!this.modelV1.Plugins.Remove(entry))
                throw new Exception("Couldn't remove plugin from model collection");
        }
        
        Log.Information("Removing plugin {Plugin}({Guid}) from profile {Profile}", entry.InternalName, entry.WorkingPluginId, this.Guid);

        // We need to add this plugin back to the default profile, if we were the last profile to have it.
        if (!this.manager.IsInAnyProfile(workingPluginId))
        {
            if (!this.IsDefaultProfile)
            {
                Log.Information("=> Adding plugin {Plugin}({Guid}) back to default profile", entry.InternalName, entry.WorkingPluginId);
                await this.manager.DefaultProfile.AddOrUpdateAsync(workingPluginId, entry.InternalName, this.IsEnabled && entry.IsEnabled, false);
            }
            else if (checkDefault)
            {
                throw new PluginNotInDefaultProfileException(workingPluginId.ToString());
            }
        }

        Service<DalamudConfiguration>.Get().QueueSave();

        if (apply)
            await this.manager.ApplyAllWantStatesAsync();
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
        lock (this)
        {
            foreach (var plugin in this.modelV1.Plugins)
            {
                // TODO: What should happen if a profile has a GUID locked in, but the plugin
                // is not installed anymore? That probably means that the user uninstalled the plugin
                // and is now reinstalling it. We should still satisfy that and update the ID.
                
                if (plugin.InternalName == internalName && plugin.WorkingPluginId == Guid.Empty)
                {
                    plugin.WorkingPluginId = newGuid;
                    Log.Information("Migrated profile {Profile} plugin {Name} to guid {Guid}", this, internalName, newGuid);
                }
            }
        }
        
        Service<DalamudConfiguration>.Get().QueueSave();
    }

    /// <inheritdoc/>
    public override string ToString() => $"{this.Guid} ({this.Name})";
}

/// <summary>
/// Exception indicating an issue during a profile operation.
/// </summary>
internal abstract class ProfileOperationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileOperationException"/> class.
    /// </summary>
    /// <param name="message">Message to pass on.</param>
    protected ProfileOperationException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Exception indicating that a plugin was not found in the default profile.
/// </summary>
internal sealed class PluginNotInDefaultProfileException : ProfileOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginNotInDefaultProfileException"/> class.
    /// </summary>
    /// <param name="internalName">The internal name of the plugin causing the error.</param>
    public PluginNotInDefaultProfileException(string internalName)
        : base($"The plugin '{internalName}' is not in the default profile, and cannot be removed")
    {
    }
}

/// <summary>
/// Exception indicating that the plugin was not found.
/// </summary>
internal sealed class PluginNotFoundException : ProfileOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginNotFoundException"/> class.
    /// </summary>
    /// <param name="internalName">The internal name of the plugin causing the error.</param>
    public PluginNotFoundException(string internalName)
        : base($"The plugin '{internalName}' was not found in the profile")
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginNotFoundException"/> class.
    /// </summary>
    /// <param name="workingPluginId">The ID of the plugin causing the error.</param>
    public PluginNotFoundException(Guid workingPluginId)
        : base($"The plugin '{workingPluginId}' was not found in the profile")
    {
    }
}
