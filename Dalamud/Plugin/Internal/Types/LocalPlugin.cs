using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Common.Game;
using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal.Exceptions;
using Dalamud.Plugin.Internal.Loader;
using Dalamud.Plugin.Internal.Profiles;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Utility;

namespace Dalamud.Plugin.Internal.Types;

/// <summary>
/// This class represents a plugin and all facets of its lifecycle.
/// The DLL on disk, dependencies, loaded assembly, etc.
/// </summary>
internal class LocalPlugin : IDisposable
{
    /// <summary>
    /// The underlying manifest for this plugin.
    /// </summary>
#pragma warning disable SA1401
    protected LocalPluginManifest manifest;
#pragma warning restore SA1401
    
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
    public LocalPlugin(FileInfo dllFile, LocalPluginManifest manifest)
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

        // Although it is conditionally used here, we need to set the initial value regardless.
        this.manifestFile = LocalPluginManifest.GetManifestFile(this.DllFile);
        this.manifest = manifest;

        var needsSaveDueToLegacyFiles = false;

        // This converts from the ".disabled" file feature to the manifest instead.
        this.disabledFile = LocalPluginManifest.GetDisabledFile(this.DllFile);
        if (this.disabledFile.Exists)
        {
#pragma warning disable CS0618
            this.manifest.Disabled = true;
#pragma warning restore CS0618
            this.disabledFile.Delete();

            needsSaveDueToLegacyFiles = true;
        }

        // This converts from the ".testing" file feature to the manifest instead.
        this.testingFile = LocalPluginManifest.GetTestingFile(this.DllFile);
        if (this.testingFile.Exists)
        {
            this.manifest.Testing = true;
            this.testingFile.Delete();

            needsSaveDueToLegacyFiles = true;
        }

        // Create an installation instance ID for this plugin, if it doesn't have one yet
        if (this.manifest.WorkingPluginId == Guid.Empty)
        {
            this.manifest.WorkingPluginId = Guid.NewGuid();

            needsSaveDueToLegacyFiles = true;
        }

        var pluginManager = Service<PluginManager>.Get();
        this.IsBanned = pluginManager.IsManifestBanned(this.manifest) && !this.IsDev;
        this.BanReason = pluginManager.GetBanReason(this.manifest);

        if (needsSaveDueToLegacyFiles)
            this.SaveManifest("legacy");
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
    /// Gets the plugin manifest.
    /// </summary>
    public ILocalPluginManifest Manifest => this.manifest;

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
    public string Name => this.manifest.Name;

    /// <summary>
    /// Gets the plugin internal name from the manifest.
    /// </summary>
    public string InternalName => this.manifest.InternalName;

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
    /// Gets a value indicating whether this plugin is wanted active by any profile.
    /// INCLUDES the default profile.
    /// </summary>
    public bool IsWantedByAnyProfile =>
        Service<ProfileManager>.Get().GetWantStateAsync(this.manifest.InternalName, false, false).GetAwaiter().GetResult();

    /// <summary>
    /// Gets a value indicating whether this plugin's API level is out of date.
    /// </summary>
    public bool IsOutdated => this.manifest.DalamudApiLevel < PluginManager.DalamudApiLevel;

    /// <summary>
    /// Gets a value indicating whether the plugin is for testing use only.
    /// </summary>
    public bool IsTesting => this.manifest.IsTestingExclusive || this.manifest.Testing;

    /// <summary>
    /// Gets a value indicating whether or not this plugin is orphaned(belongs to a repo) or not.
    /// </summary>
    public bool IsOrphaned => !this.IsDev &&
                              !this.manifest.InstalledFromUrl.IsNullOrEmpty() && // TODO(api8): Remove this, all plugins will have a proper flag
                              this.GetSourceRepository() == null;

    /// <summary>
    /// Gets a value indicating whether or not this plugin is serviced(repo still exists, but plugin no longer does).
    /// </summary>
    public bool IsDecommissioned => !this.IsDev &&
                                    this.GetSourceRepository()?.State == PluginRepositoryState.Success &&
                                    this.GetSourceRepository()?.PluginMaster?.FirstOrDefault(x => x.InternalName == this.manifest.InternalName) == null;

    /// <summary>
    /// Gets a value indicating whether this plugin has been banned.
    /// </summary>
    public bool IsBanned { get; }

    /// <summary>
    /// Gets a value indicating whether this plugin is dev plugin.
    /// </summary>
    public bool IsDev => this is LocalDevPlugin;

    /// <summary>
    /// Gets a value indicating whether this manifest is associated with a plugin that was installed from a third party
    /// repo.
    /// </summary>
    public bool IsThirdParty => this.manifest.IsThirdParty;

    /// <summary>
    /// Gets a value indicating whether this plugin should be allowed to load.
    /// </summary>
    public bool ApplicableForLoad => !this.IsBanned && !this.IsDecommissioned && !this.IsOrphaned && !this.IsOutdated
                                     && !(!this.IsDev && this.State == PluginState.UnloadError) && this.CheckPolicy();

    /// <summary>
    /// Gets the effective version of this plugin.
    /// </summary>
    public Version EffectiveVersion => this.manifest.EffectiveVersion;

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
            if (this.manifest.CanUnloadAsync || framework == null)
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
        var framework = await Service<Framework>.GetAsync();
        var ioc = await Service<ServiceContainer>.GetAsync();
        var pluginManager = await Service<PluginManager>.GetAsync();
        var dalamud = await Service<Dalamud>.GetAsync();

        if (this.manifest.LoadRequiredState == 0)
            _ = await Service<InterfaceManager.InterfaceManagerWithScene>.GetAsync();

        await this.pluginLoadStateLock.WaitAsync();
        try
        {
            if (reloading && this.IsDev)
            {
                // Reload the manifest in-case there were changes here too.
                this.ReloadManifest();
            }

            // If we reload a plugin we don't want to delete it. Makes sense, right?
            if (this.manifest.ScheduledForDeletion)
            {
                this.manifest.ScheduledForDeletion = false;
                this.SaveManifest("Scheduled for deletion, but loading");
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

            if (pluginManager.IsManifestBanned(this.manifest) && !this.IsDev)
                throw new BannedPluginException($"Unable to load {this.Name} as it was banned");

            if (this.manifest.ApplicableVersion < dalamud.StartInfo.GameVersion)
                throw new PluginPreconditionFailedException($"Unable to load {this.Name}, game is newer than applicable version {this.manifest.ApplicableVersion}");

            if (this.manifest.DalamudApiLevel < PluginManager.DalamudApiLevel && !pluginManager.LoadAllApiLevels)
                throw new PluginPreconditionFailedException($"Unable to load {this.Name}, incompatible API level {this.manifest.DalamudApiLevel}");

            // We might want to throw here?
            if (!this.IsWantedByAnyProfile)
                Log.Warning("{Name} is loading, but isn't wanted by any profile", this.Name);

            if (this.IsOrphaned)
                throw new PluginPreconditionFailedException($"Plugin {this.Name} had no associated repo");

            if (!this.CheckPolicy())
                throw new PluginPreconditionFailedException($"Unable to load {this.Name} as a load policy forbids it");

            this.State = PluginState.Loading;
            Log.Information($"Loading {this.DllFile.Name}");
            
            this.EnsureLoader();

            if (this.DllFile.DirectoryName != null &&
                File.Exists(Path.Combine(this.DllFile.DirectoryName, "Dalamud.dll")))
            {
                Log.Error(
                    "==== IMPORTANT MESSAGE TO {0}, THE DEVELOPER OF {1} ====",
                    this.manifest.Author!,
                    this.manifest.InternalName);
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

            if (this.manifest.LoadSync && this.manifest.LoadRequiredState is 0 or 1)
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

            this.State = PluginState.Loaded;
            Log.Information($"Finished loading {this.DllFile.Name}");
        }
        catch (Exception ex)
        {
            this.State = PluginState.LoadError;

            // If a precondition fails, don't record it as an error, as it isn't really. 
            if (ex is PluginPreconditionFailedException)
                Log.Warning(ex.Message);
            else
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

            if (this.manifest.CanUnloadAsync || framework == null)
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
    /// Check if anything forbids this plugin from loading.
    /// </summary>
    /// <returns>Whether or not this plugin shouldn't load.</returns>
    public bool CheckPolicy()
    {
        var startInfo = Service<Dalamud>.Get().StartInfo;
        var manager = Service<PluginManager>.Get();

        if (startInfo.NoLoadPlugins)
            return false;

        if (startInfo.NoLoadThirdPartyPlugins && this.manifest.IsThirdParty)
            return false;

        if (manager.SafeMode)
            return false;

        return true;
    }

    /// <summary>
    /// Schedule the deletion of this plugin on next cleanup.
    /// </summary>
    /// <param name="status">Schedule or cancel the deletion.</param>
    public void ScheduleDeletion(bool status = true)
    {
        this.manifest.ScheduledForDeletion = status;
        this.SaveManifest("scheduling for deletion");
    }

    /// <summary>
    /// Reload the manifest if it exists, preserve the internal Disabled state.
    /// </summary>
    public void ReloadManifest()
    {
        var manifestPath = LocalPluginManifest.GetManifestFile(this.DllFile);
        if (manifestPath.Exists)
        {
            // Save some state that we do actually want to carry over
            var guid = this.manifest.WorkingPluginId;
            
            this.manifest = LocalPluginManifest.Load(manifestPath) ?? throw new Exception("Could not reload manifest.");
            this.manifest.WorkingPluginId = guid;

            this.SaveManifest("dev reload");
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
            if (!x.IsThirdParty && !this.manifest.IsThirdParty)
                return true;

            return x.PluginMasterUrl == this.manifest.InstalledFromUrl;
        });
    }

    /// <summary>
    /// Save this plugin manifest.
    /// </summary>
    /// <param name="reason">Why it should be saved.</param>
    protected void SaveManifest(string reason) => this.manifest.Save(this.manifestFile, reason);

    private static void SetupLoaderConfig(LoaderConfig config)
    {
        config.IsUnloadable = true;
        config.LoadInMemory = true;
        config.PreferSharedTypes = false;

        // Pin Lumina and its dependencies recursively (compatibility behavior).
        // It currently only pulls in System.* anyway.
        // TODO(api10): Remove this. We don't want to pin Lumina anymore, plugins should be able to provide their own.
        config.SharedAssemblies.Add((typeof(Lumina.GameData).Assembly.GetName(), true));
        config.SharedAssemblies.Add((typeof(Lumina.Excel.ExcelSheetImpl).Assembly.GetName(), true));

        // Make sure that plugins do not load their own Dalamud assembly.
        // We do not pin this recursively; if a plugin loads its own assembly of Dalamud, it is always wrong,
        // but plugins may load other versions of assemblies that Dalamud depends on.
        config.SharedAssemblies.Add((typeof(EntryPoint).Assembly.GetName(), false));
        config.SharedAssemblies.Add((typeof(Common.DalamudStartInfo).Assembly.GetName(), false));
    }

    private void EnsureLoader()
    {
        if (this.loader != null)
            return;
        
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
    }
}
