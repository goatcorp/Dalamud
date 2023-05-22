using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal;
using Dalamud.IoC.Internal;
using Dalamud.Logging;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal.Exceptions;
using Dalamud.Plugin.Internal.Loader;
using Dalamud.Utility;

namespace Dalamud.Plugin.Internal.Types;

/// <summary>
/// This class represents a plugin and all facets of its lifecycle.
/// The DLL on disk, dependencies, loaded assembly, etc.
/// </summary>
internal class LocalPlugin : IDisposable
{
    private static readonly ModuleLog Log = new("LOCALPLUGIN");

    private readonly FileInfo manifestFile;
    private readonly FileInfo disabledFile;
    private readonly FileInfo testingFile;

    private readonly SemaphoreSlim pluginLoadStateLock = new(1);

    private PluginLoader? loader;
    private Assembly? pluginAssembly;
    private Type? pluginType;
    private IDalamudPlugin? instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalPlugin"/> class.
    /// </summary>
    /// <param name="dllFile">Path to the DLL file.</param>
    /// <param name="manifest">The plugin manifest.</param>
    public LocalPlugin(FileInfo dllFile, LocalPluginManifest? manifest)
    {
        if (dllFile.Name == "FFXIVClientStructs.Generators.dll")
        {
            // Could this be done another way? Sure. It is an extremely common source
            // of errors in the log through, and should never be loaded as a plugin.
            Log.Error($"Not a plugin: {dllFile.FullName}");
            throw new InvalidPluginException(dllFile);
        }

        this.DllFile = dllFile;
        this.State = PluginState.Unloaded;

        try
        {
            this.loader = PluginLoader.CreateFromAssemblyFile(this.DllFile.FullName, SetupLoaderConfig);
        }
        catch (InvalidOperationException ex)
        {
            Log.Error(ex, "Loader.CreateFromAssemblyFile() failed");
            this.State = PluginState.DependencyResolutionFailed;
            throw;
        }

        try
        {
            this.pluginAssembly = this.loader.LoadDefaultAssembly();
        }
        catch (Exception ex)
        {
            this.pluginAssembly = null;
            this.pluginType = null;
            this.loader.Dispose();

            Log.Error(ex, $"Not a plugin: {this.DllFile.FullName}");
            throw new InvalidPluginException(this.DllFile);
        }

        try
        {
            this.pluginType = this.pluginAssembly.GetTypes().FirstOrDefault(type => type.IsAssignableTo(typeof(IDalamudPlugin)));
        }
        catch (ReflectionTypeLoadException ex)
        {
            Log.Error(ex, $"Could not load one or more types when searching for IDalamudPlugin: {this.DllFile.FullName}");
            // Something blew up when parsing types, but we still want to look for IDalamudPlugin. Let Load() handle the error.
            this.pluginType = ex.Types.FirstOrDefault(type => type != null && type.IsAssignableTo(typeof(IDalamudPlugin)));
        }

        if (this.pluginType == default)
        {
            this.pluginAssembly = null;
            this.pluginType = null;
            this.loader.Dispose();

            Log.Error($"Nothing inherits from IDalamudPlugin: {this.DllFile.FullName}");
            throw new InvalidPluginException(this.DllFile);
        }

        var assemblyVersion = this.pluginAssembly.GetName().Version;

        // Although it is conditionally used here, we need to set the initial value regardless.
        this.manifestFile = LocalPluginManifest.GetManifestFile(this.DllFile);

        // If the parameter manifest was null
        if (manifest == null)
        {
            this.Manifest = new LocalPluginManifest()
            {
                Author = "developer",
                Name = Path.GetFileNameWithoutExtension(this.DllFile.Name),
                InternalName = Path.GetFileNameWithoutExtension(this.DllFile.Name),
                AssemblyVersion = assemblyVersion ?? new Version("1.0.0.0"),
                Description = string.Empty,
                ApplicableVersion = GameVersion.Any,
                DalamudApiLevel = PluginManager.DalamudApiLevel,
                IsHide = false,
            };

            // Save the manifest to disk so there won't be any problems later.
            // We'll update the name property after it can be retrieved from the instance.
            this.Manifest.Save(this.manifestFile);
        }
        else
        {
            this.Manifest = manifest;
        }

        // This converts from the ".disabled" file feature to the manifest instead.
        this.disabledFile = LocalPluginManifest.GetDisabledFile(this.DllFile);
        if (this.disabledFile.Exists)
        {
            this.Manifest.Disabled = true;
            this.disabledFile.Delete();
        }

        // This converts from the ".testing" file feature to the manifest instead.
        this.testingFile = LocalPluginManifest.GetTestingFile(this.DllFile);
        if (this.testingFile.Exists)
        {
            this.Manifest.Testing = true;
            this.testingFile.Delete();
        }

        var pluginManager = Service<PluginManager>.Get();
        this.IsBanned = pluginManager.IsManifestBanned(this.Manifest) && !this.IsDev;
        this.BanReason = pluginManager.GetBanReason(this.Manifest);

        this.SaveManifest();
    }

    /// <summary>
    /// Gets the <see cref="DalamudPluginInterface"/> associated with this plugin.
    /// </summary>
    public DalamudPluginInterface? DalamudInterface { get; private set; }

    /// <summary>
    /// Gets the path to the plugin DLL.
    /// </summary>
    public FileInfo DllFile { get; }

    /// <summary>
    /// Gets the plugin manifest, if one exists.
    /// </summary>
    public LocalPluginManifest Manifest { get; private set; }

    /// <summary>
    /// Gets or sets the current state of the plugin.
    /// </summary>
    public PluginState State { get; protected set; }

    /// <summary>
    /// Gets the AssemblyName plugin, populated during <see cref="LoadAsync"/>.
    /// </summary>
    /// <returns>Plugin type.</returns>
    public AssemblyName? AssemblyName { get; private set; }

    /// <summary>
    /// Gets the plugin name from the manifest.
    /// </summary>
    public string Name => this.Manifest.Name;

    /// <summary>
    /// Gets the plugin internal name from the manifest.
    /// </summary>
    public string InternalName => this.Manifest.InternalName;

    /// <summary>
    /// Gets an optional reason, if the plugin is banned.
    /// </summary>
    public string BanReason { get; }

    /// <summary>
    /// Gets a value indicating whether the plugin has ever started to load.
    /// </summary>
    public bool HasEverStartedLoad { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the plugin is loaded and running.
    /// </summary>
    public bool IsLoaded => this.State == PluginState.Loaded;

    /// <summary>
    /// Gets a value indicating whether the plugin is disabled.
    /// </summary>
    public bool IsDisabled => this.Manifest.Disabled;

    /// <summary>
    /// Gets a value indicating whether this plugin's API level is out of date.
    /// </summary>
    public bool IsOutdated => this.Manifest.DalamudApiLevel < PluginManager.DalamudApiLevel;

    /// <summary>
    /// Gets a value indicating whether the plugin is for testing use only.
    /// </summary>
    public bool IsTesting => this.Manifest.IsTestingExclusive || this.Manifest.Testing;

    /// <summary>
    /// Gets a value indicating whether or not this plugin is orphaned(belongs to a repo) or not.
    /// </summary>
    public bool IsOrphaned => !this.IsDev &&
                              !this.Manifest.InstalledFromUrl.IsNullOrEmpty() && // TODO(api8): Remove this, all plugins will have a proper flag
                              this.GetSourceRepository() == null;

    /// <summary>
    /// Gets a value indicating whether or not this plugin is serviced(repo still exists, but plugin no longer does).
    /// </summary>
    public bool IsDecommissioned => !this.IsDev &&
                                    this.GetSourceRepository()?.State == PluginRepositoryState.Success &&
                                    this.GetSourceRepository()?.PluginMaster?.FirstOrDefault(x => x.InternalName == this.Manifest.InternalName) == null;

    /// <summary>
    /// Gets a value indicating whether this plugin has been banned.
    /// </summary>
    public bool IsBanned { get; }

    /// <summary>
    /// Gets a value indicating whether this plugin is dev plugin.
    /// </summary>
    public bool IsDev => this is LocalDevPlugin;

    /// <summary>
    /// Gets the service scope for this plugin.
    /// </summary>
    public IServiceScope? ServiceScope { get; private set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        var framework = Service<Framework>.GetNullable();
        var configuration = Service<DalamudConfiguration>.Get();

        var didPluginDispose = false;
        if (this.instance != null)
        {
            didPluginDispose = true;
            if (this.Manifest.CanUnloadAsync || framework == null)
                this.instance.Dispose();
            else
                framework.RunOnFrameworkThread(() => this.instance.Dispose()).Wait();

            this.instance = null;
        }

        this.DalamudInterface?.ExplicitDispose();
        this.DalamudInterface = null;

        this.ServiceScope?.Dispose();
        this.ServiceScope = null;

        this.pluginType = null;
        this.pluginAssembly = null;

        if (this.loader != null && didPluginDispose)
            Thread.Sleep(configuration.PluginWaitBeforeFree ?? PluginManager.PluginWaitBeforeFreeDefault);
        this.loader?.Dispose();
    }

    /// <summary>
    /// Load this plugin.
    /// </summary>
    /// <param name="reason">The reason why this plugin is being loaded.</param>
    /// <param name="reloading">Load while reloading.</param>
    /// <returns>A task.</returns>
    public async Task LoadAsync(PluginLoadReason reason, bool reloading = false)
    {
        var configuration = await Service<DalamudConfiguration>.GetAsync();
        var framework = await Service<Framework>.GetAsync();
        var ioc = await Service<ServiceContainer>.GetAsync();
        var pluginManager = await Service<PluginManager>.GetAsync();
        var startInfo = await Service<DalamudStartInfo>.GetAsync();

        // UiBuilder constructor requires the following two.
        await Service<InterfaceManager>.GetAsync();
        await Service<GameFontManager>.GetAsync();

        if (this.Manifest.LoadRequiredState == 0)
            _ = await Service<InterfaceManager.InterfaceManagerWithScene>.GetAsync();

        await this.pluginLoadStateLock.WaitAsync();
        try
        {
            if (reloading && this.IsDev)
            {
                // Reload the manifest in-case there were changes here too.
                this.ReloadManifest();
            }

            switch (this.State)
            {
                case PluginState.Loaded:
                    throw new InvalidPluginOperationException($"Unable to load {this.Name}, already loaded");
                case PluginState.LoadError:
                    if (!this.IsDev)
                    {
                        throw new InvalidPluginOperationException(
                            $"Unable to load {this.Name}, load previously faulted, unload first");
                    }

                    break;
                case PluginState.UnloadError:
                    if (!this.IsDev)
                    {
                        throw new InvalidPluginOperationException(
                        $"Unable to load {this.Name}, unload previously faulted, restart Dalamud");
                    }

                    break;
                case PluginState.Unloaded:
                    break;
                case PluginState.Loading:
                case PluginState.Unloading:
                default:
                    throw new ArgumentOutOfRangeException(this.State.ToString());
            }

            if (pluginManager.IsManifestBanned(this.Manifest) && !this.IsDev)
                throw new BannedPluginException($"Unable to load {this.Name}, banned");

            if (this.Manifest.ApplicableVersion < startInfo.GameVersion)
                throw new InvalidPluginOperationException($"Unable to load {this.Name}, no applicable version");

            if (this.Manifest.DalamudApiLevel < PluginManager.DalamudApiLevel && !pluginManager.LoadAllApiLevels)
                throw new InvalidPluginOperationException($"Unable to load {this.Name}, incompatible API level");

            if (this.Manifest.Disabled)
                throw new InvalidPluginOperationException($"Unable to load {this.Name}, disabled");

            if (this.IsOrphaned)
                throw new InvalidPluginOperationException($"Plugin {this.Name} had no associated repo.");

            if (!this.CheckPolicy())
                throw new InvalidPluginOperationException("Plugin was not loaded as per policy");

            this.State = PluginState.Loading;
            Log.Information($"Loading {this.DllFile.Name}");

            if (this.DllFile.DirectoryName != null &&
                File.Exists(Path.Combine(this.DllFile.DirectoryName, "Dalamud.dll")))
            {
                Log.Error(
                    "==== IMPORTANT MESSAGE TO {0}, THE DEVELOPER OF {1} ====",
                    this.Manifest.Author!,
                    this.Manifest.InternalName);
                Log.Error(
                    "YOU ARE INCLUDING DALAMUD DEPENDENCIES IN YOUR BUILDS!!!");
                Log.Error(
                    "You may not be able to load your plugin. \"<Private>False</Private>\" needs to be set in your csproj.");
                Log.Error(
                    "If you are using ILMerge, do not merge anything other than your direct dependencies.");
                Log.Error("Do not merge FFXIVClientStructs.Generators.dll.");
                Log.Error(
                    "Please refer to https://github.com/goatcorp/Dalamud/discussions/603 for more information.");
            }

            this.HasEverStartedLoad = true;

            this.loader ??= PluginLoader.CreateFromAssemblyFile(this.DllFile.FullName, SetupLoaderConfig);

            if (reloading || this.IsDev)
            {
                if (this.IsDev)
                {
                    // If a dev plugin is set to not autoload on boot, but we want to reload it at the arbitrary load
                    // time, we need to essentially "Unload" the plugin, but we can't call plugin.Unload because of the
                    // load state checks. Null any references to the assembly and types, then proceed with regular reload
                    // operations.
                    this.pluginAssembly = null;
                    this.pluginType = null;
                }

                this.loader.Reload();
            }

            // Load the assembly
            this.pluginAssembly ??= this.loader.LoadDefaultAssembly();

            this.AssemblyName = this.pluginAssembly.GetName();

            // Find the plugin interface implementation. It is guaranteed to exist after checking in the ctor.
            this.pluginType ??= this.pluginAssembly.GetTypes()
                                    .First(type => type.IsAssignableTo(typeof(IDalamudPlugin)));

            // Check for any loaded plugins with the same assembly name
            var assemblyName = this.pluginAssembly.GetName().Name;
            foreach (var otherPlugin in pluginManager.InstalledPlugins)
            {
                // During hot-reloading, this plugin will be in the plugin list, and the instance will have been disposed
                if (otherPlugin == this || otherPlugin.instance == null)
                    continue;

                var otherPluginAssemblyName =
                    otherPlugin.instance.GetType().Assembly.GetName().Name;
                if (otherPluginAssemblyName == assemblyName && otherPluginAssemblyName != null)
                {
                    this.State = PluginState.Unloaded;
                    Log.Debug($"Duplicate assembly: {this.Name}");

                    throw new DuplicatePluginException(assemblyName);
                }
            }

            // Update the location for the Location and CodeBase patches
            PluginManager.PluginLocations[this.pluginType.Assembly.FullName] = new PluginPatchData(this.DllFile);

            this.DalamudInterface =
                new DalamudPluginInterface(this, reason);

            this.ServiceScope = ioc.GetScope();
            this.ServiceScope.RegisterPrivateScopes(this); // Add this LocalPlugin as a private scope, so services can get it

            if (this.Manifest.LoadSync && this.Manifest.LoadRequiredState is 0 or 1)
            {
                this.instance = await framework.RunOnFrameworkThread(
                                    () => this.ServiceScope.CreateAsync(this.pluginType!, this.DalamudInterface!)) as IDalamudPlugin;
            }
            else
            {
                this.instance =
                    await this.ServiceScope.CreateAsync(this.pluginType!, this.DalamudInterface!) as IDalamudPlugin;
            }

            if (this.instance == null)
            {
                this.State = PluginState.LoadError;
                this.DalamudInterface.ExplicitDispose();
                Log.Error(
                    $"Error while loading {this.Name}, failed to bind and call the plugin constructor");
                return;
            }

            // In-case the manifest name was a placeholder. Can occur when no manifest was included.
            if (this.Manifest.Name.IsNullOrEmpty())
            {
                this.Manifest.Name = this.instance.Name;
                this.Manifest.Save(this.manifestFile);
            }

            this.State = PluginState.Loaded;
            Log.Information($"Finished loading {this.DllFile.Name}");
        }
        catch (Exception ex)
        {
            this.State = PluginState.LoadError;
            Log.Error(ex, $"Error while loading {this.Name}");

            throw;
        }
        finally
        {
            this.pluginLoadStateLock.Release();
        }
    }

    /// <summary>
    /// Unload this plugin. This is the same as dispose, but without the "disposed" connotations. This object should stay
    /// in the plugin list until it has been actually disposed.
    /// </summary>
    /// <param name="reloading">Unload while reloading.</param>
    /// <param name="waitBeforeLoaderDispose">Wait before disposing loader.</param>
    /// <returns>The task.</returns>
    public async Task UnloadAsync(bool reloading = false, bool waitBeforeLoaderDispose = true)
    {
        var configuration = Service<DalamudConfiguration>.Get();
        var framework = Service<Framework>.GetNullable();
        var ioc = await Service<ServiceContainer>.GetAsync();

        await this.pluginLoadStateLock.WaitAsync();
        try
        {
            switch (this.State)
            {
                case PluginState.Unloaded:
                    throw new InvalidPluginOperationException($"Unable to unload {this.Name}, already unloaded");
                case PluginState.UnloadError:
                    if (!this.IsDev)
                    {
                        throw new InvalidPluginOperationException(
                            $"Unable to unload {this.Name}, unload previously faulted, restart Dalamud");
                    }

                    break;
                case PluginState.Loaded:
                case PluginState.LoadError:
                    break;
                case PluginState.Loading:
                case PluginState.Unloading:
                default:
                    throw new ArgumentOutOfRangeException(this.State.ToString());
            }

            this.State = PluginState.Unloading;
            Log.Information($"Unloading {this.DllFile.Name}");

            if (this.Manifest.CanUnloadAsync || framework == null)
                this.instance?.Dispose();
            else
                await framework.RunOnFrameworkThread(() => this.instance?.Dispose());

            this.instance = null;

            this.DalamudInterface?.ExplicitDispose();
            this.DalamudInterface = null;

            this.ServiceScope?.Dispose();
            this.ServiceScope = null;

            this.pluginType = null;
            this.pluginAssembly = null;

            if (!reloading)
            {
                if (waitBeforeLoaderDispose && this.loader != null)
                    await Task.Delay(configuration.PluginWaitBeforeFree ?? PluginManager.PluginWaitBeforeFreeDefault);
                this.loader?.Dispose();
                this.loader = null;
            }

            this.State = PluginState.Unloaded;
            Log.Information($"Finished unloading {this.DllFile.Name}");
        }
        catch (Exception ex)
        {
            this.State = PluginState.UnloadError;
            Log.Error(ex, $"Error while unloading {this.Name}");

            throw;
        }
        finally
        {
            // We need to handle removed DTR nodes here, as otherwise, plugins will not be able to re-add their bar entries after updates.
            Service<DtrBar>.GetNullable()?.HandleRemovedNodes();

            this.pluginLoadStateLock.Release();
        }
    }

    /// <summary>
    /// Reload this plugin.
    /// </summary>
    /// <returns>A task.</returns>
    public async Task ReloadAsync()
    {
        // Don't unload if we're a dev plugin and have an unload error, this is a bad idea but whatever
        if (this.IsDev && this.State != PluginState.UnloadError)
            await this.UnloadAsync(true);

        await this.LoadAsync(PluginLoadReason.Reload, true);
    }

    /// <summary>
    /// Revert a disable. Must be unloaded first, does not load.
    /// </summary>
    public void Enable()
    {
        // Allowed: Unloaded, UnloadError
        switch (this.State)
        {
            case PluginState.Loading:
            case PluginState.Unloading:
            case PluginState.Loaded:
            case PluginState.LoadError:
                if (!this.IsDev)
                    throw new InvalidPluginOperationException($"Unable to enable {this.Name}, still loaded");
                break;
            case PluginState.Unloaded:
                break;
            case PluginState.UnloadError:
                break;
            case PluginState.DependencyResolutionFailed:
                throw new InvalidPluginOperationException($"Unable to enable {this.Name}, dependency resolution failed");
            default:
                throw new ArgumentOutOfRangeException(this.State.ToString());
        }

        // NOTE(goat): This is inconsequential, and we do have situations where a plugin can end up enabled but not loaded:
        // Orphaned plugins can have their repo added back, but may not have been loaded at boot and may still be enabled.
        // We don't want to disable orphaned plugins when they are orphaned so this is how it's going to be.
        // if (!this.Manifest.Disabled)
        //    throw new InvalidPluginOperationException($"Unable to enable {this.Name}, not disabled");

        this.Manifest.Disabled = false;
        this.Manifest.ScheduledForDeletion = false;
        this.SaveManifest();
    }

    /// <summary>
    /// Check if anything forbids this plugin from loading.
    /// </summary>
    /// <returns>Whether or not this plugin shouldn't load.</returns>
    public bool CheckPolicy()
    {
        var startInfo = Service<DalamudStartInfo>.Get();
        var manager = Service<PluginManager>.Get();

        if (startInfo.NoLoadPlugins)
            return false;

        if (startInfo.NoLoadThirdPartyPlugins && this.Manifest.IsThirdParty)
            return false;

        if (manager.SafeMode)
            return false;

        return true;
    }

    /// <summary>
    /// Disable this plugin, must be unloaded first.
    /// </summary>
    public void Disable()
    {
        // Allowed: Unloaded, UnloadError
        switch (this.State)
        {
            case PluginState.Loading:
            case PluginState.Unloading:
            case PluginState.Loaded:
            case PluginState.LoadError:
                throw new InvalidPluginOperationException($"Unable to disable {this.Name}, still loaded");
            case PluginState.Unloaded:
                break;
            case PluginState.UnloadError:
                break;
            case PluginState.DependencyResolutionFailed:
                return; // This is a no-op.
            default:
                throw new ArgumentOutOfRangeException(this.State.ToString());
        }

        if (this.Manifest.Disabled)
            throw new InvalidPluginOperationException($"Unable to disable {this.Name}, already disabled");

        this.Manifest.Disabled = true;
        this.SaveManifest();
    }

    /// <summary>
    /// Schedule the deletion of this plugin on next cleanup.
    /// </summary>
    /// <param name="status">Schedule or cancel the deletion.</param>
    public void ScheduleDeletion(bool status = true)
    {
        this.Manifest.ScheduledForDeletion = status;
        this.SaveManifest();
    }

    /// <summary>
    /// Reload the manifest if it exists, preserve the internal Disabled state.
    /// </summary>
    public void ReloadManifest()
    {
        var manifest = LocalPluginManifest.GetManifestFile(this.DllFile);
        if (manifest.Exists)
        {
            var isDisabled = this.IsDisabled; // saving the internal state because it could have been deleted
            this.Manifest = LocalPluginManifest.Load(manifest);
            this.Manifest.Disabled = isDisabled;

            this.SaveManifest();
        }
    }

    /// <summary>
    /// Get the repository this plugin was installed from.
    /// </summary>
    /// <returns>The plugin repository this plugin was installed from, or null if it is no longer there or if the plugin is a dev plugin.</returns>
    public PluginRepository? GetSourceRepository()
    {
        if (this.IsDev)
            return null;

        var repos = Service<PluginManager>.Get().Repos;
        return repos.FirstOrDefault(x =>
        {
            if (!x.IsThirdParty && !this.Manifest.IsThirdParty)
                return true;

            return x.PluginMasterUrl == this.Manifest.InstalledFromUrl;
        });
    }

    private static void SetupLoaderConfig(LoaderConfig config)
    {
        config.IsUnloadable = true;
        config.LoadInMemory = true;
        config.PreferSharedTypes = false;
        config.SharedAssemblies.Add(typeof(Lumina.GameData).Assembly.GetName());
        config.SharedAssemblies.Add(typeof(Lumina.Excel.ExcelSheetImpl).Assembly.GetName());
    }

    private void SaveManifest() => this.Manifest.Save(this.manifestFile);
}
