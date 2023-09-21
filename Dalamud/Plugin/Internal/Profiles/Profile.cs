using System;
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
    /// <param name="internalName">The internal name of the plugin.</param>
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
    /// <param name="internalName">The internal name of the plugin.</param>
    /// <param name="state">Whether or not the plugin should be enabled.</param>
    /// <param name="apply">Whether or not the current state should immediately be applied.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task AddOrUpdateAsync(Guid workingPluginId, bool state, bool apply = true)
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
                    WorkingPluginId = workingPluginId,
                    IsEnabled = state,
                });
            }
        }

        // We need to remove this plugin from the default profile, if it declares it.
        if (!this.IsDefaultProfile && this.manager.DefaultProfile.WantsPlugin(workingPluginId) != null)
        {
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
    /// <param name="internalName">The internal name of the plugin.</param>
    /// <param name="apply">Whether or not the current state should immediately be applied.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task RemoveAsync(Guid workingPluginId, bool apply = true)
    {
        ProfileModelV1.ProfileModelV1Plugin entry;
        lock (this)
        {
            entry = this.modelV1.Plugins.FirstOrDefault(x => x.WorkingPluginId == workingPluginId);
            if (entry == null)
                throw new ArgumentException($"No plugin \"{workingPluginId}\" in profile \"{this.Guid}\"");

            if (!this.modelV1.Plugins.Remove(entry))
                throw new Exception("Couldn't remove plugin from model collection");
        }

        // We need to add this plugin back to the default profile, if we were the last profile to have it.
        if (!this.manager.IsInAnyProfile(workingPluginId))
        {
            if (!this.IsDefaultProfile)
            {
                await this.manager.DefaultProfile.AddOrUpdateAsync(workingPluginId, this.IsEnabled && entry.IsEnabled, false);
            }
            else
            {
                throw new Exception("Removed plugin from default profile, but wasn't in any other profile");
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
