using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
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
internal class LocalPlugin : IAsyncDisposable
{
    /// <summary>
    /// The underlying manifest for this plugin.
    /// </summary>
#pragma warning disable SA1401
    protected LocalPluginManifest manifest;
#pragma warning restore SA1401

    private static readonly ModuleLog Log = ModuleLog.Create<LocalPlugin>();

    private readonly FileInfo manifestFile;
    private readonly FileInfo disabledFile;
    private readonly FileInfo testingFile;

    private readonly SemaphoreSlim pluginLoadStateLock = new(1);

    private PluginLoader? loader;
    private Assembly? pluginAssembly;
    private Type? pluginType;
    private IDalamudPlugin? instance;
    private IServiceScope? serviceScope;
    private DalamudPluginInterface? dalamudInterface;

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

        // Although it is conditionally used here, we need to set the initial value regardless.
        this.manifestFile = LocalPluginManifest.GetManifestFile(this.DllFile);
        this.manifest = manifest;

        this.State = PluginState.Unloaded;

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
        if (this.manifest.WorkingPluginId == Guid.Empty && !this.IsDev)
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
    public DalamudPluginInterface? DalamudInterface => this.dalamudInterface;

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
        Service<ProfileManager>.Get().GetWantStateAsync(this.EffectiveWorkingPluginId, this.Manifest.InternalName, false, false).GetAwaiter().GetResult();

    /// <summary>
    /// Gets a value indicating whether this plugin's API level is out of date.
    /// </summary>
    public bool IsOutdated => this.manifest.EffectiveApiLevel < PluginManager.DalamudApiLevel;

    /// <summary>
    /// Gets a value indicating whether the plugin is for testing use only.
    /// </summary>
    public bool IsTesting => this.manifest.IsTestingExclusive || this.manifest.Testing;

    /// <summary>
    /// Gets a value indicating whether this plugin is orphaned(belongs to a repo) or not.
    /// </summary>
    public bool IsOrphaned => !this.IsDev &&
                              this.GetSourceRepository() == null;

    /// <summary>
    /// Gets a value indicating whether this plugin is serviced(repo still exists, but plugin no longer does).
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
    /// Gets the effective working plugin ID for this plugin.
    /// </summary>
    public virtual Guid EffectiveWorkingPluginId => this.manifest.WorkingPluginId;

    /// <summary>
    /// Gets the service scope for this plugin.
    /// </summary>
    public IServiceScope? ServiceScope => this.serviceScope;

    /// <inheritdoc/>
    public virtual async ValueTask DisposeAsync() =>
        await this.ClearAndDisposeAllResources(PluginLoaderDisposalMode.ImmediateDispose);

    /// <summary>
    /// Load this plugin.
    /// </summary>
    /// <param name="reason">The reason why this plugin is being loaded.</param>
    /// <param name="reloading">Load while reloading.</param>
    /// <returns>A task.</returns>
    public async Task LoadAsync(PluginLoadReason reason, bool reloading = false)
    {
        var ioc = await Service<ServiceContainer>.GetAsync();
        var pluginManager = await Service<PluginManager>.GetAsync();
        var dalamud = await Service<Dalamud>.GetAsync();

        if (this.manifest.LoadRequiredState == 0)
            _ = await Service<InterfaceManager.InterfaceManagerWithScene>.GetAsync();

        await this.pluginLoadStateLock.WaitAsync();
        try
        {
            if (reloading)
                this.OnPreReload();

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
                    if (this.instance is not null)
                    {
                        throw new InternalPluginStateException(
                            "Plugin should have been unloaded but instance is not cleared");
                    }

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

            // We want to allow loading dev plugins with a lower API level than the current Dalamud API level, for ease of development
            if (!pluginManager.LoadAllApiLevels && !this.IsDev && this.manifest.EffectiveApiLevel < PluginManager.DalamudApiLevel)
                throw new PluginPreconditionFailedException($"Unable to load {this.Name}, incompatible API level {this.manifest.EffectiveApiLevel}");

            // We might want to throw here?
            if (!this.IsWantedByAnyProfile)
                Log.Warning("{Name} is loading, but isn't wanted by any profile", this.Name);

            if (this.IsOrphaned)
                throw new PluginPreconditionFailedException($"Plugin {this.Name} had no associated repo");

            if (!this.CheckPolicy())
                throw new PluginPreconditionFailedException($"Unable to load {this.Name} as a load policy forbids it");

            if (this.Manifest.MinimumDalamudVersion != null && this.Manifest.MinimumDalamudVersion > Versioning.GetAssemblyVersionParsed())
                throw new PluginPreconditionFailedException($"Unable to load {this.Name}, Dalamud version is lower than minimum required version {this.Manifest.MinimumDalamudVersion}");

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
                this.RefreshAssemblyInformation();
            }

            Log.Verbose("{Name} ({Guid}): Have type", this.InternalName, this.EffectiveWorkingPluginId);

            // Check for any loaded plugins with the same assembly name
            var assemblyName = this.pluginAssembly!.GetName().Name;
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
                    Log.Debug("Duplicate assembly: {Name}", this.InternalName);

                    throw new DuplicatePluginException(assemblyName);
                }
            }

            this.dalamudInterface = new(this, reason);

            this.serviceScope = ioc.GetScope();
            this.serviceScope.RegisterPrivateScopes(this); // Add this LocalPlugin as a private scope, so services can get it

            try
            {
                this.instance = await CreatePluginInstance(
                                    this.manifest,
                                    this.serviceScope,
                                    this.pluginType!,
                                    this.dalamudInterface);
                this.State = PluginState.Loaded;
                Log.Information("Finished loading {PluginName}", this.InternalName);

                var manager = Service<PluginManager>.Get();
                manager.NotifyPluginsForStateChange(PluginListInvalidationKind.Loaded, [this.manifest.InternalName]);
            }
            catch (Exception ex)
            {
                this.State = PluginState.LoadError;
                Log.Error(
                    ex,
                    "Error while loading {PluginName}, failed to bind and call the plugin constructor",
                    this.InternalName);
                await this.ClearAndDisposeAllResources(PluginLoaderDisposalMode.ImmediateDispose);
            }
        }
        catch (Exception ex)
        {
            // These are "user errors", we don't want to mark the plugin as failed
            if (ex is not InvalidPluginOperationException)
                this.State = PluginState.LoadError;

            // If a precondition fails, don't record it as an error, as it isn't really.
            if (ex is PluginPreconditionFailedException)
                Log.Warning(ex.Message);
            else
                Log.Error(ex, "Error while loading {PluginName}", this.InternalName);

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
    /// <param name="disposalMode">How to dispose loader.</param>
    /// <returns>The task.</returns>
    public async Task UnloadAsync(PluginLoaderDisposalMode disposalMode = PluginLoaderDisposalMode.WaitBeforeDispose)
    {
        await this.pluginLoadStateLock.WaitAsync();
        try
        {
            switch (this.State)
            {
                case PluginState.Unloaded:
                    throw new InvalidPluginOperationException($"Unable to unload {this.Name}, already unloaded");
                case PluginState.DependencyResolutionFailed:
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
            Log.Information("Unloading {PluginName}", this.InternalName);

            if (await this.ClearAndDisposeAllResources(disposalMode) is { } ex)
            {
                this.State = PluginState.UnloadError;
                throw ex;
            }

            this.State = PluginState.Unloaded;
            Log.Information("Finished unloading {PluginName}", this.InternalName);

            var manager = Service<PluginManager>.Get();
            manager.NotifyPluginsForStateChange(PluginListInvalidationKind.Unloaded, [this.manifest.InternalName]);
        }
        catch (Exception ex)
        {
            // These are "user errors", we don't want to mark the plugin as failed
            if (ex is not InvalidPluginOperationException)
                this.State = PluginState.UnloadError;

            Log.Error(ex, "Error while unloading {PluginName}", this.InternalName);

            throw;
        }
        finally
        {
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
            await this.UnloadAsync(PluginLoaderDisposalMode.None);

        await this.LoadAsync(PluginLoadReason.Reload, true);
    }

    /// <summary>
    /// Check if anything forbids this plugin from loading.
    /// </summary>
    /// <returns>Whether this plugin shouldn't load.</returns>
    public bool CheckPolicy()
    {
        var startInfo = Service<Dalamud>.Get().StartInfo;
        var manager = Service<PluginManager>.Get();

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
    /// Checks whether this plugin loads in the given load context.
    /// </summary>
    /// <param name="context">The load context to check.</param>
    /// <returns>Whether this plugin loads in the given load context.</returns>
    public bool LoadsIn(AssemblyLoadContext context)
        => this.loader?.LoadContext == context;

    /// <summary>
    /// Save this plugin manifest.
    /// </summary>
    /// <param name="reason">Why it should be saved.</param>
    protected void SaveManifest(string reason) => this.manifest.Save(this.manifestFile, reason);

    /// <summary>
    /// Called before a plugin is reloaded.
    /// </summary>
    protected virtual void OnPreReload()
    {
    }

    /// <summary>Creates a new instance of the plugin.</summary>
    /// <param name="manifest">Plugin manifest.</param>
    /// <param name="scope">Service scope.</param>
    /// <param name="type">Type of the plugin main class.</param>
    /// <param name="dalamudInterface">Instance of <see cref="IDalamudPluginInterface"/>.</param>
    /// <returns>A new instance of the plugin.</returns>
    private static async Task<IDalamudPlugin> CreatePluginInstance(
        LocalPluginManifest manifest,
        IServiceScope scope,
        Type type,
        DalamudPluginInterface dalamudInterface)
    {
        var framework = await Service<Framework>.GetAsync();
        var forceFrameworkThread = manifest.LoadSync && manifest.LoadRequiredState is 0 or 1;
        var newInstanceTask = forceFrameworkThread ? framework.RunOnFrameworkThread(Create) : Create();
        return await newInstanceTask.ConfigureAwait(false);

        async Task<IDalamudPlugin> Create() => (IDalamudPlugin)await scope.CreateAsync(type, ObjectInstanceVisibility.ExposedToPlugins, dalamudInterface);
    }

    private static void SetupLoaderConfig(LoaderConfig config)
    {
        config.IsUnloadable = true;
        config.LoadInMemory = true;
        config.PreferSharedTypes = false;

        // Make sure that plugins do not load their own Dalamud assembly.
        // We do not pin this recursively; if a plugin loads its own assembly of Dalamud, it is always wrong,
        // but plugins may load other versions of assemblies that Dalamud depends on.
        config.SharedAssemblies.Add((typeof(EntryPoint).Assembly.GetName(), false));
        config.SharedAssemblies.Add((typeof(Common.DalamudStartInfo).Assembly.GetName(), false));

        // Pin Lumina since we expose it as an API surface. Before anyone removes this again, please see #1598.
        // Changes to Lumina should be upstreamed if feasible, and if there is a desire to re-add unpinned Lumina we
        // will need to put this behind some kind of feature flag somewhere.
        config.SharedAssemblies.Add((typeof(Lumina.GameData).Assembly.GetName(), true));
        config.SharedAssemblies.Add((typeof(Lumina.Excel.Sheets.Addon).Assembly.GetName(), true));
    }

    private void EnsureLoader()
    {
        if (this.loader != null)
            return;

        this.DllFile.Refresh();
        if (!this.DllFile.Exists)
            throw new Exception($"Plugin DLL file at '{this.DllFile.FullName}' did not exist, cannot load.");

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

        this.RefreshAssemblyInformation();
    }

    private void RefreshAssemblyInformation()
    {
        if (this.loader == null)
            throw new InvalidOperationException("No loader available");

        try
        {
            this.pluginAssembly = this.loader.LoadDefaultAssembly();
            this.AssemblyName = this.pluginAssembly.GetName();
        }
        catch (Exception ex)
        {
            this.ResetLoader();
            Log.Error(ex, $"Not a plugin: {this.DllFile.FullName}");
            throw new InvalidPluginException(this.DllFile);
        }

        if (this.pluginAssembly == null)
        {
            this.ResetLoader();
            Log.Error("Plugin assembly is null: {DllFileFullName}", this.DllFile.FullName);
            throw new InvalidPluginException(this.DllFile);
        }

        try
        {
            this.pluginType = this.pluginAssembly.GetTypes().FirstOrDefault(type => type.IsAssignableTo(typeof(IDalamudPlugin)));
        }
        catch (ReflectionTypeLoadException ex)
        {
            this.ResetLoader();
            Log.Error(ex, "Could not load one or more types when searching for IDalamudPlugin: {DllFileFullName}", this.DllFile.FullName);
            throw;
        }

        if (this.pluginType == null)
        {
            this.ResetLoader();
            Log.Error("Nothing inherits from IDalamudPlugin: {DllFileFullName}", this.DllFile.FullName);
            throw new InvalidPluginException(this.DllFile);
        }
    }

    private void ResetLoader()
    {
        this.pluginAssembly = null;
        this.pluginType = null;
        this.loader?.Dispose();
        this.loader = null;
    }

    /// <summary>Clears and disposes all resources associated with the plugin instance.</summary>
    /// <param name="disposalMode">Whether to clear and dispose <see cref="loader"/>.</param>
    /// <returns>Exceptions, if any occurred.</returns>
    private async Task<AggregateException?> ClearAndDisposeAllResources(PluginLoaderDisposalMode disposalMode)
    {
        List<Exception>? exceptions = null;
        Log.Verbose(
            "{name}({id}): {fn}(disposalMode={disposalMode})",
            this.InternalName,
            this.EffectiveWorkingPluginId,
            nameof(this.ClearAndDisposeAllResources),
            disposalMode);

        // Clear the plugin instance first.
        if (!await AttemptCleanup(
            nameof(this.instance),
            Interlocked.Exchange(ref this.instance, null),
            this.manifest,
            static async (inst, manifest) =>
            {
                var framework = Service<Framework>.GetNullable();
                if (manifest.CanUnloadAsync || framework is null)
                    inst.Dispose();
                else
                    await framework.RunOnFrameworkThread(inst.Dispose).ConfigureAwait(false);
            }))
        {
            // Plugin was not loaded; loader is not referenced anyway, so no need to wait.
            disposalMode = PluginLoaderDisposalMode.ImmediateDispose;
        }

        // Fields below are expected to be alive until the plugin is (attempted) disposed.
        // Clear them after this point.
        this.pluginType = null;
        this.pluginAssembly = null;

        await AttemptCleanup(
            nameof(this.serviceScope),
            Interlocked.Exchange(ref this.serviceScope, null),
            0,
            static (x, _) => x.DisposeAsync());

        await AttemptCleanup(
            nameof(this.dalamudInterface),
            Interlocked.Exchange(ref this.dalamudInterface, null),
            0,
            static (x, _) =>
            {
                x.Dispose();
                return ValueTask.CompletedTask;
            });

        if (disposalMode != PluginLoaderDisposalMode.None)
        {
            await AttemptCleanup(
                nameof(this.loader),
                Interlocked.Exchange(ref this.loader, null),
                disposalMode == PluginLoaderDisposalMode.WaitBeforeDispose
                    ? Service<DalamudConfiguration>.Get().PluginWaitBeforeFree ??
                      PluginManager.PluginWaitBeforeFreeDefault
                    : 0,
                static async (ldr, waitBeforeDispose) =>
                {
                    // Just in case plugins still have tasks running that they didn't cancel when they should have,
                    // give them some time to complete it.
                    // This helps avoid plugins being reloaded from conflicting with itself of previous instance.
                    await Task.Delay(waitBeforeDispose);

                    ldr.Dispose();
                });
        }

        return exceptions is not null
                   ? (AggregateException)ExceptionDispatchInfo.SetCurrentStackTrace(new AggregateException(exceptions))
                   : null;

        async ValueTask<bool> AttemptCleanup<T, TContext>(
            string name,
            T? what,
            TContext context,
            Func<T, TContext, ValueTask> cb)
            where T : class
        {
            if (what is null)
                return false;

            try
            {
                await cb.Invoke(what, context);
                Log.Verbose("{name}({id}): {what} disposed", this.InternalName, this.EffectiveWorkingPluginId, name);
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
                Log.Error(
                    ex,
                    "{name}({id}): Failed to dispose {what}",
                    this.InternalName,
                    this.EffectiveWorkingPluginId,
                    name);
            }

            return true;
        }
    }
}
