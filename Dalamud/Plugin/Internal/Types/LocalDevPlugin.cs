using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal.Types.Manifest;

namespace Dalamud.Plugin.Internal.Types;

/// <summary>
/// This class represents a dev plugin and all facets of its lifecycle.
/// The DLL on disk, dependencies, loaded assembly, etc.
/// </summary>
internal class LocalDevPlugin : LocalPlugin, IDisposable
{
    private static readonly ModuleLog Log = new("PLUGIN");

    // Ref to Dalamud.Configuration.DevPluginSettings
    private readonly DevPluginSettings devSettings;

    private FileSystemWatcher? fileWatcher;
    private CancellationTokenSource fileWatcherTokenSource = new();
    private int reloadCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDevPlugin"/> class.
    /// </summary>
    /// <param name="dllFile">Path to the DLL file.</param>
    /// <param name="manifest">The plugin manifest.</param>
    public LocalDevPlugin(FileInfo dllFile, LocalPluginManifest? manifest)
        : base(dllFile, manifest)
    {
        var configuration = Service<DalamudConfiguration>.Get();

        if (!configuration.DevPluginSettings.TryGetValue(dllFile.FullName, out this.devSettings))
        {
            configuration.DevPluginSettings[dllFile.FullName] = this.devSettings = new DevPluginSettings();
            configuration.QueueSave();
        }
        
        // Legacy dev plugins might not have this!
        if (this.devSettings.WorkingPluginId == Guid.Empty)
        {
            this.devSettings.WorkingPluginId = Guid.NewGuid();
            Log.Verbose("{InternalName} was assigned new devPlugin GUID {Guid}", this.InternalName, this.devSettings.WorkingPluginId);
            configuration.QueueSave();
        }

        if (this.AutomaticReload)
        {
            this.EnableReloading();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this dev plugin should start on boot.
    /// </summary>
    public bool StartOnBoot
    {
        get => this.devSettings.StartOnBoot;
        set => this.devSettings.StartOnBoot = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this dev plugin should reload on change.
    /// </summary>
    public bool AutomaticReload
    {
        get => this.devSettings.AutomaticReloading;
        set
        {
            this.devSettings.AutomaticReloading = value;

            if (this.devSettings.AutomaticReloading)
            {
                this.EnableReloading();
            }
            else
            {
                this.DisableReloading();
            }
        }
    }
    
    /// <summary>
    /// Gets an ID uniquely identifying this specific instance of a devPlugin.
    /// </summary>
    public Guid DevImposedWorkingPluginId => this.devSettings.WorkingPluginId;

    /// <inheritdoc/>
    public override Guid EffectiveWorkingPluginId => this.DevImposedWorkingPluginId;

    /// <summary>
    /// Gets a list of validation problems that have been dismissed by the user.
    /// </summary>
    public List<string> DismissedValidationProblems => this.devSettings.DismissedValidationProblems;

    /// <inheritdoc/>
    public new void Dispose()
    {
        if (this.fileWatcher != null)
        {
            this.fileWatcher.Changed -= this.OnFileChanged;
            this.fileWatcherTokenSource.Cancel();
            this.fileWatcher.Dispose();
        }

        base.Dispose();
    }

    /// <summary>
    /// Configure this plugin for automatic reloading and enable it.
    /// </summary>
    public void EnableReloading()
    {
        if (this.fileWatcher == null && this.DllFile.DirectoryName != null)
        {
            this.fileWatcherTokenSource = new CancellationTokenSource();
            this.fileWatcher = new FileSystemWatcher(this.DllFile.DirectoryName);
            this.fileWatcher.Changed += this.OnFileChanged;
            this.fileWatcher.Filter = this.DllFile.Name;
            this.fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            this.fileWatcher.EnableRaisingEvents = true;
        }
    }

    /// <summary>
    /// Disable automatic reloading for this plugin.
    /// </summary>
    public void DisableReloading()
    {
        if (this.fileWatcher != null)
        {
            this.fileWatcherTokenSource.Cancel();
            this.fileWatcher.Changed -= this.OnFileChanged;
            this.fileWatcher.Dispose();
            this.fileWatcher = null;
        }
    }

    /// <summary>
    /// Reload the manifest if it exists, to update possible changes.
    /// </summary>
    /// <exception cref="Exception">Thrown if the manifest could not be loaded.</exception>
    public void ReloadManifest()
    {
        var manifestPath = LocalPluginManifest.GetManifestFile(this.DllFile);
        if (manifestPath.Exists)
            this.manifest = LocalPluginManifest.Load(manifestPath) ?? throw new Exception("Could not reload manifest.");
    }
    
    /// <inheritdoc/>
    protected override void OnPreReload()
    {
        this.ReloadManifest();
    }

    private void OnFileChanged(object sender, FileSystemEventArgs args)
    {
        var current = Interlocked.Increment(ref this.reloadCounter);

        Task.Delay(500).ContinueWith(
            async _ =>
            {
                if (this.fileWatcherTokenSource.IsCancellationRequested)
                {
                    Log.Debug($"Skipping reload of {this.Name}, file watcher was cancelled.");
                    return;
                }

                if (current != this.reloadCounter)
                {
                    Log.Debug($"Skipping reload of {this.Name}, file has changed again.");
                    return;
                }

                if (this.State != PluginState.Loaded && this.State != PluginState.LoadError && this.State != PluginState.UnloadError)
                {
                    Log.Debug($"Skipping reload of {this.Name}, state ({this.State}) is not {PluginState.Loaded}, {PluginState.LoadError} or {PluginState.UnloadError}.");
                    return;
                }

                var notificationManager = Service<NotificationManager>.Get();

                try
                {
                    if (this.State == PluginState.UnloadError)
                    {
                        Log.Warning($"{this.Manifest.Author}: TAKE CARE!!! You need to fix your unload error, and restart the game - your plugin might be in an inconsistent state.");
                        Log.Warning("Reloading anyway, as this is a dev plugin, but you might encounter unexpected results.");
                    }

                    await this.ReloadAsync();
                    notificationManager.AddNotification($"The DevPlugin '{this.Name} was reloaded successfully.", "Plugin reloaded!", NotificationType.Success);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "DevPlugin reload failed.");
                    notificationManager.AddNotification($"The DevPlugin '{this.Name} could not be reloaded.", "Plugin reload failed!", NotificationType.Error);
                }
            },
            this.fileWatcherTokenSource.Token);
    }
}
