using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Configuration;
using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.IoC;
using Dalamud.Logging.Internal;
using Dalamud.Networking.Http;
using Dalamud.Plugin.Internal.Exceptions;
using Dalamud.Plugin.Internal.Profiles;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Plugin.Ipc.Internal;
using Dalamud.Support;
using Dalamud.Utility;
using Dalamud.Utility.Timing;
using Newtonsoft.Json;

namespace Dalamud.Plugin.Internal;

/// <summary>
/// Class responsible for loading and unloading plugins.
/// NOTE: ALL plugin exposed services are marked as dependencies for <see cref="PluginManager"/>
/// from <see cref="ResolvePossiblePluginDependencyServices"/>.
/// </summary>
[ServiceManager.BlockingEarlyLoadedService("Accommodation of plugins that blocks the game startup.")]
internal class PluginManager : IInternalDisposableService
{
    /// <summary>
    /// Default time to wait between plugin unload and plugin assembly unload.
    /// </summary>
    public const int PluginWaitBeforeFreeDefault = 1000; // upped from 500ms, seems more stable

    private static readonly ModuleLog Log = new("PLUGINM");

    private readonly object pluginListLock = new();
    private readonly DirectoryInfo pluginDirectory;
    private readonly BannedPlugin[]? bannedPlugins;
    
    private readonly List<LocalPlugin> installedPluginsList = new();
    private readonly List<RemotePluginManifest> availablePluginsList = new();
    private readonly List<AvailablePluginUpdate> updatablePluginsList = new();

    private readonly Task<DalamudLinkPayload> openInstallerWindowPluginChangelogsLink;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    [ServiceManager.ServiceDependency]
    private readonly Dalamud dalamud = Service<Dalamud>.Get();

    [ServiceManager.ServiceDependency]
    private readonly ProfileManager profileManager = Service<ProfileManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly HappyHttpClient happyHttpClient = Service<HappyHttpClient>.Get();

    static PluginManager()
    {
        DalamudApiLevel = typeof(PluginManager).Assembly.GetName().Version!.Major;
    }

    [ServiceManager.ServiceConstructor]
    private PluginManager(
        ServiceManager.RegisterStartupBlockerDelegate registerStartupBlocker,
        ServiceManager.RegisterUnloadAfterDelegate registerUnloadAfter)
    {
        this.pluginDirectory = new DirectoryInfo(this.dalamud.StartInfo.PluginDirectory!);

        if (!this.pluginDirectory.Exists)
            this.pluginDirectory.Create();

        this.SafeMode = EnvironmentConfiguration.DalamudNoPlugins || this.configuration.PluginSafeMode || this.dalamud.StartInfo.NoLoadPlugins;

        try
        {
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var safeModeFile = Path.Combine(appdata, "XIVLauncher", ".dalamud_safemode");

            if (File.Exists(safeModeFile))
            {
                this.SafeMode = true;
                File.Delete(safeModeFile);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Couldn't check safe mode file");
        }

        if (this.SafeMode)
        {
            this.configuration.PluginSafeMode = false;
            this.configuration.QueueSave();
        }

        this.PluginConfigs = new PluginConfigurations(Path.Combine(Path.GetDirectoryName(this.dalamud.StartInfo.ConfigurationPath) ?? string.Empty, "pluginConfigs"));

        var bannedPluginsJson = File.ReadAllText(Path.Combine(this.dalamud.StartInfo.AssetDirectory!, "UIRes", "bannedplugin.json"));
        this.bannedPlugins = JsonConvert.DeserializeObject<BannedPlugin[]>(bannedPluginsJson);
        if (this.bannedPlugins == null)
        {
            throw new InvalidDataException("Couldn't deserialize banned plugins manifest.");
        }

        this.openInstallerWindowPluginChangelogsLink =
            Service<ChatGui>.GetAsync().ContinueWith(
                chatGuiTask => chatGuiTask.Result.AddChatLinkHandler(
                    "Dalamud",
                    1003,
                    (_, _) =>
                    {
                        Service<DalamudInterface>.GetNullable()?.OpenPluginInstallerTo(
                            PluginInstallerOpenKind.Changelogs);
                    }));

        this.configuration.PluginTestingOptIns ??= new();
        this.MainRepo = PluginRepository.CreateMainRepo(this.happyHttpClient);

        // NET8 CHORE
        // this.ApplyPatches();

        registerStartupBlocker(
            Task.Run(this.LoadAndStartLoadSyncPlugins),
            "Waiting for plugins that asked to be loaded before the game.");

        registerUnloadAfter(
            ResolvePossiblePluginDependencyServices(),
            "See the attached comment for the called function.");
    }

    /// <summary>
    /// An event that fires when the installed plugins have changed.
    /// </summary>
    public event Action? OnInstalledPluginsChanged;

    /// <summary>
    /// An event that fires when the available plugins have changed.
    /// </summary>
    public event Action? OnAvailablePluginsChanged;

    /// <summary>
    /// Gets the current Dalamud API level, used to handle breaking changes. Only plugins with this level will be loaded.
    /// As of Dalamud 9.x, this always matches the major version number of Dalamud.
    /// </summary>
    public static int DalamudApiLevel { get; private set; }

    /// <summary>
    /// Gets a copy of the list of all loaded plugins.
    /// </summary>
    public IEnumerable<LocalPlugin> InstalledPlugins
    {
        get
        {
            lock (this.pluginListLock)
            {
                return this.installedPluginsList.ToList();
            }
        }
    }

    /// <summary>
    /// Gets a copy of the list of all available plugins.
    /// </summary>
    public IEnumerable<RemotePluginManifest> AvailablePlugins
    {
        get
        {
            lock (this.pluginListLock)
            {
                return this.availablePluginsList.ToList();
            }
        }
    }
    
    /// <summary>
    /// Gets a copy of the list of all plugins with an available update.
    /// </summary>
    public IEnumerable<AvailablePluginUpdate> UpdatablePlugins
    {
        get
        {
            lock (this.pluginListLock)
            {
                return this.updatablePluginsList.ToList();
            }
        }
    }

    /// <summary>
    /// Gets the main repository.
    /// </summary>
    public PluginRepository MainRepo { get; }

    /// <summary>
    /// Gets a list of all plugin repositories. The main repo should always be first.
    /// </summary>
    public List<PluginRepository> Repos { get; private set; } = new();

    /// <summary>
    /// Gets a value indicating whether plugins are not still loading from boot.
    /// </summary>
    public bool PluginsReady { get; private set; }

    /// <summary>
    /// Gets a value indicating whether all added repos are not in progress.
    /// </summary>
    public bool ReposReady { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the plugin manager started in safe mode.
    /// </summary>
    public bool SafeMode { get; init; }

    /// <summary>
    /// Gets the <see cref="PluginConfigurations"/> object used when initializing plugins.
    /// </summary>
    public PluginConfigurations PluginConfigs { get; }

    /// <summary>
    /// Gets or sets a value indicating whether plugins of all API levels will be loaded.
    /// </summary>
    public bool LoadAllApiLevels { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether banned plugins will be loaded.
    /// </summary>
    public bool LoadBannedPlugins { get; set; }
    
    /// <summary>
    /// Gets a tracker for plugins that are loading at startup, used to display information to the user.
    /// </summary>
    public StartupLoadTracker? StartupLoadTracking { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the given repo manifest should be visible to the user.
    /// </summary>
    /// <param name="manifest">Repo manifest.</param>
    /// <returns>If the manifest is visible.</returns>
    public static bool IsManifestVisible(RemotePluginManifest manifest)
    {
        // Hidden by manifest
        return !manifest.IsHide;
    }

    /// <summary>
    /// Check if a manifest even has an available testing version.
    /// </summary>
    /// <param name="manifest">The manifest to test.</param>
    /// <returns>Whether or not a testing version is available.</returns>
    public static bool HasTestingVersion(IPluginManifest manifest)
    {
        var av = manifest.AssemblyVersion;
        var tv = manifest.TestingAssemblyVersion;
        var hasTv = tv != null;

        if (hasTv)
        {
            return tv > av &&
                   manifest.TestingDalamudApiLevel == DalamudApiLevel;
        }

        return false;
    }

    /// <summary>
    /// Get a disposable that will lock plugin lists while it is not disposed.
    /// You must NEVER use this in async code.
    /// </summary>
    /// <returns>The aforementioned disposable.</returns>
    public IDisposable GetSyncScope() => new ScopedSyncRoot(this.pluginListLock);

    /// <summary>
    /// Print to chat any plugin updates and whether they were successful.
    /// </summary>
    /// <param name="updateMetadata">The list of updated plugin metadata.</param>
    /// <param name="header">The header text to send to chat prior to any update info.</param>
    public void PrintUpdatedPlugins(List<PluginUpdateStatus>? updateMetadata, string header)
        => Service<ChatGui>.GetAsync().ContinueWith(
            chatGuiTask =>
            {
                if (!chatGuiTask.IsCompletedSuccessfully)
                    return;

                var chatGui = chatGuiTask.Result;
                if (updateMetadata is { Count: > 0 })
                {
                    chatGui.Print(
                        new XivChatEntry
                        {
                            Message = new SeString(
                                new List<Payload>()
                                {
                                    new TextPayload(header),
                                    new TextPayload("  ["),
                                    new UIForegroundPayload(500),
                                    this.openInstallerWindowPluginChangelogsLink.Result,
                                    new TextPayload(
                                        Loc.Localize("DalamudInstallerPluginChangelogHelp", "Open plugin changelogs")),
                                    RawPayload.LinkTerminator,
                                    new UIForegroundPayload(0),
                                    new TextPayload("]"),
                                }),
                        });

                    foreach (var metadata in updateMetadata)
                    {
                        if (metadata.Status == PluginUpdateStatus.StatusKind.Success)
                        {
                            chatGui.Print(Locs.DalamudPluginUpdateSuccessful(metadata.Name, metadata.Version));
                        }
                        else
                        {
                            chatGui.Print(
                                new XivChatEntry
                                {
                                    Message = Locs.DalamudPluginUpdateFailed(
                                        metadata.Name,
                                        metadata.Version,
                                        PluginUpdateStatus.LocalizeUpdateStatusKind(metadata.Status)),
                                    Type = XivChatType.Urgent,
                                });
                        }
                    }
                }
            });

    /// <summary>
    /// For a given manifest, determine if the user opted into testing this plugin.
    /// </summary>
    /// <param name="manifest">Manifest to check.</param>
    /// <returns>A value indicating whether testing should be used.</returns>
    public bool HasTestingOptIn(IPluginManifest manifest)
    {
        return this.configuration.PluginTestingOptIns!.Any(x => x.InternalName == manifest.InternalName);
    }

    /// <summary>
    /// For a given manifest, determine if the testing version should be used over the normal version.
    /// The higher of the two versions is calculated after checking other settings.
    /// </summary>
    /// <param name="manifest">Manifest to check.</param>
    /// <returns>A value indicating whether testing should be used.</returns>
    public bool UseTesting(IPluginManifest manifest)
    {
        if (!this.configuration.DoPluginTest)
            return false;

        if (!this.HasTestingOptIn(manifest))
            return false;

        if (manifest.IsTestingExclusive)
            return true;

        return HasTestingVersion(manifest);
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        var disposablePlugins =
            this.installedPluginsList.Where(plugin => plugin.State is PluginState.Loaded or PluginState.LoadError).ToArray();
        if (disposablePlugins.Any())
        {
            // Unload them first, just in case some of plugin codes are still running via callbacks initiated externally.
            foreach (var plugin in disposablePlugins.Where(plugin => !plugin.Manifest.CanUnloadAsync))
            {
                try
                {
                    plugin.UnloadAsync(true, false).Wait();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error unloading {plugin.Name}");
                }
            }

            Task.WaitAll(disposablePlugins
                             .Where(plugin => plugin.Manifest.CanUnloadAsync)
                             .Select(plugin => Task.Run(async () =>
                             {
                                 try
                                 {
                                     await plugin.UnloadAsync(true, false);
                                 }
                                 catch (Exception ex)
                                 {
                                     Log.Error(ex, $"Error unloading {plugin.Name}");
                                 }
                             })).ToArray());

            // Just in case plugins still have tasks running that they didn't cancel when they should have,
            // give them some time to complete it.
            Thread.Sleep(this.configuration.PluginWaitBeforeFree ?? PluginWaitBeforeFreeDefault);

            // Now that we've waited enough, dispose the whole plugin.
            // Since plugins should have been unloaded above, this should be done quickly.
            foreach (var plugin in disposablePlugins)
            {
                try
                {
                    plugin.Dispose();
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Error disposing {plugin.Name}");
                }
            }
        }

        // NET8 CHORE
        // this.assemblyLocationMonoHook?.Dispose();
        // this.assemblyCodeBaseMonoHook?.Dispose();
    }

    /// <summary>
    /// Set the list of repositories to use and downloads their contents.
    /// Should be called when the Settings window has been updated or at instantiation.
    /// </summary>
    /// <param name="notify">Whether the available plugins changed event should be sent after.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SetPluginReposFromConfigAsync(bool notify)
    {
        var repos = new List<PluginRepository> { this.MainRepo };
        repos.AddRange(this.configuration.ThirdRepoList
                           .Where(repo => repo.IsEnabled)
                           .Select(repo => new PluginRepository(this.happyHttpClient, repo.Url, repo.IsEnabled)));

        this.Repos = repos;
        await this.ReloadPluginMastersAsync(notify);
    }

    /// <summary>
    /// Load all plugins, sorted by priority. Any plugins with no explicit definition file or a negative priority
    /// are loaded asynchronously.
    /// </summary>
    /// <remarks>
    /// This should only be called during Dalamud startup.
    /// </remarks>
    /// <returns>The task.</returns>
    public async Task LoadAllPlugins()
    {
        var pluginDefs = new List<PluginDef>();
        var devPluginDefs = new List<PluginDef>();

        if (!this.pluginDirectory.Exists)
            this.pluginDirectory.Create();

        // Add installed plugins. These are expected to be in a specific format so we can look for exactly that.
        foreach (var pluginDir in this.pluginDirectory.GetDirectories())
        {
            var versionsDefs = new List<PluginDef>();
            foreach (var versionDir in pluginDir.GetDirectories())
            {
                try
                {
                    var dllFile = new FileInfo(Path.Combine(versionDir.FullName, $"{pluginDir.Name}.dll"));
                    if (!dllFile.Exists)
                    {
                        Log.Error("No DLL found for plugin at {Path}", versionDir.FullName);
                        continue;
                    }
                    
                    var manifestFile = LocalPluginManifest.GetManifestFile(dllFile);
                    if (!manifestFile.Exists)
                    {
                        Log.Error("No manifest for plugin at {Path}", dllFile.FullName);
                        continue;
                    }

                    var manifest = LocalPluginManifest.Load(manifestFile);
                    if (manifest == null)
                    {
                        Log.Error("Manifest for plugin at {Path} was null", dllFile.FullName);
                        continue;
                    }

                    if (manifest.IsTestingExclusive && this.configuration.PluginTestingOptIns!.All(x => x.InternalName != manifest.InternalName))
                        this.configuration.PluginTestingOptIns.Add(new PluginTestingOptIn(manifest.InternalName));

                    versionsDefs.Add(new PluginDef(dllFile, manifest, false));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Could not load manifest for installed at {Directory}", versionDir.FullName);
                }
            }

            this.configuration.QueueSave();
            
            if (versionsDefs.Count == 0)
            {
                Log.Verbose("No versions found for plugin: {Name}", pluginDir.Name);
                continue;
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

        // devPlugins are more freeform. Look for any dll and hope to get lucky.
        var devDllFiles = new List<FileInfo>();

        foreach (var setting in this.configuration.DevPluginLoadLocations)
        {
            if (!setting.IsEnabled)
                continue;

            if (Directory.Exists(setting.Path))
            {
                devDllFiles.AddRange(new DirectoryInfo(setting.Path).GetFiles("*.dll", SearchOption.AllDirectories));
            }
            else if (File.Exists(setting.Path))
            {
                devDllFiles.Add(new FileInfo(setting.Path));
            }
        }

        foreach (var dllFile in devDllFiles)
        {
            try
            {
                // Manifests are now required for devPlugins
                var manifestFile = LocalPluginManifest.GetManifestFile(dllFile);
                if (!manifestFile.Exists)
                {
                    Log.Error("DLL at {DllPath} has no manifest, this is no longer valid", dllFile.FullName);
                    continue;
                }
            
                var manifest = LocalPluginManifest.Load(manifestFile);
                if (manifest == null)
                {
                    Log.Error("Could not deserialize manifest for DLL at {DllPath}", dllFile.FullName);
                    continue;
                }

                if (manifest != null && manifest.InternalName.IsNullOrEmpty())
                {
                    Log.Error("InternalName for dll at {Path} was null", manifestFile.FullName);
                    continue;
                }

                devPluginDefs.Add(new PluginDef(dllFile, manifest, true));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not load manifest for dev at {Directory}", dllFile.FullName);
            }
        }

        // Sort for load order - unloaded definitions have default priority of 0
        pluginDefs.Sort(PluginDef.Sorter);
        devPluginDefs.Sort(PluginDef.Sorter);

        // Dev plugins should load first.
        pluginDefs.InsertRange(0, devPluginDefs);

        async Task LoadPluginOnBoot(string logPrefix, PluginDef pluginDef, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            using (Timings.Start($"{pluginDef.DllFile.Name}: {logPrefix}Boot"))
            {
                try
                {
                    await this.LoadPluginAsync(
                        pluginDef.DllFile,
                        pluginDef.Manifest,
                        PluginLoadReason.Boot,
                        pluginDef.IsDev,
                        isBoot: true);
                }
                catch (InvalidPluginException)
                {
                    // Not a plugin
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{0}: During boot plugin load, an unexpected error occurred", logPrefix);
                }
            }
        }

        async Task LoadPluginsSync(string logPrefix, IEnumerable<PluginDef> pluginDefsList, CancellationToken token)
        {
            Log.Information($"============= LoadPluginsSync({logPrefix}) START =============");

            foreach (var pluginDef in pluginDefsList)
                await LoadPluginOnBoot(logPrefix, pluginDef, token).ConfigureAwait(false);

            Log.Information($"============= LoadPluginsSync({logPrefix}) END =============");
        }

        async Task LoadPluginsAsync(string logPrefix, IEnumerable<PluginDef> pluginDefsList, CancellationToken token)
        {
            Log.Information($"============= LoadPluginsAsync({logPrefix}) START =============");

            await Task.WhenAll(
                pluginDefsList
                    .Select(pluginDef =>
                                Task.Run(
                                    Timings.AttachTimingHandle(
                                             () => LoadPluginOnBoot(logPrefix, pluginDef, token)),
                                    token))
                    .ToArray()).ConfigureAwait(false);

            Log.Information($"============= LoadPluginsAsync({logPrefix}) END =============");
        }

        // Initialize the startup load tracker for all LoadSync plugins
        {
            this.StartupLoadTracking = new();
            foreach (var pluginDef in pluginDefs.Where(x => x.Manifest.LoadSync))
            {
                this.StartupLoadTracking.Add(pluginDef.Manifest!.InternalName, pluginDef.Manifest.Name);
            }
        }

        var syncPlugins = pluginDefs.Where(def => def.Manifest?.LoadSync == true).ToList();
        var asyncPlugins = pluginDefs.Where(def => def.Manifest?.LoadSync != true).ToList();
        var loadTasks = new List<Task>();

        var tokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        // Load plugins that can be loaded anytime
        await LoadPluginsSync(
            "AnytimeSync",
            syncPlugins.Where(def => def.Manifest?.LoadRequiredState == 2),
            tokenSource.Token);
        loadTasks.Add(LoadPluginsAsync(
                          "AnytimeAsync",
                          asyncPlugins.Where(def => def.Manifest?.LoadRequiredState == 2),
                          tokenSource.Token));

        // Pass the rest of plugin loading to another thread(task)
        _ = Task.Run(
            async () =>
            {
                // Load plugins that want to be loaded during Framework.Tick
                var framework = await Service<Framework>.GetAsync().ConfigureAwait(false);
                await framework.RunOnTick(
                    () => LoadPluginsSync(
                        "FrameworkTickSync",
                        syncPlugins.Where(def => def.Manifest?.LoadRequiredState == 1),
                        tokenSource.Token),
                    cancellationToken: tokenSource.Token).ConfigureAwait(false);
                loadTasks.Add(LoadPluginsAsync(
                                  "FrameworkTickAsync",
                                  asyncPlugins.Where(def => def.Manifest?.LoadRequiredState == 1),
                                  tokenSource.Token));

                // Load plugins that want to be loaded during Framework.Tick, when drawing facilities are available
                _ = await Service<InterfaceManager.InterfaceManagerWithScene>.GetAsync().ConfigureAwait(false);
                await framework.RunOnTick(
                    () => LoadPluginsSync(
                        "DrawAvailableSync",
                        syncPlugins.Where(def => def.Manifest?.LoadRequiredState is 0 or null),
                        tokenSource.Token),
                    cancellationToken: tokenSource.Token);
                loadTasks.Add(LoadPluginsAsync(
                                  "DrawAvailableAsync",
                                  asyncPlugins.Where(def => def.Manifest?.LoadRequiredState is 0 or null),
                                  tokenSource.Token));

                // Save signatures when all plugins are done loading, successful or not.
                try
                {
                    await Task.WhenAll(loadTasks).ConfigureAwait(false);
                    Log.Information("Loaded plugins on boot");
                }
                catch (Exception e)
                {
                    Log.Error(e, "Failed to load at least one plugin");
                }

                var sigScanner = await Service<TargetSigScanner>.GetAsync().ConfigureAwait(false);
                this.PluginsReady = true;
                this.NotifyinstalledPluginsListChanged();
                sigScanner.Save();

                try
                {
                    this.ParanoiaValidatePluginsAndProfiles();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Plugin and profile validation failed!");
                }

                this.StartupLoadTracking = null;
            },
            tokenSource.Token);
    }

    /// <summary>
    /// Reload the PluginMaster for each repo, filter, and event that the list has updated.
    /// </summary>
    /// <param name="notify">Whether to notify that available plugins have changed afterwards.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ReloadPluginMastersAsync(bool notify = true)
    {
        Log.Information("Now reloading all PluginMasters...");
        this.ReposReady = false;

        try
        {
            Debug.Assert(!this.Repos.First().IsThirdParty, "First repository should be main repository");
            await this.Repos.First().ReloadPluginMasterAsync(); // Load official repo first

            await Task.WhenAll(this.Repos.Skip(1).Select(repo => repo.ReloadPluginMasterAsync()));

            Log.Information("PluginMasters reloaded, now refiltering...");

            this.RefilterPluginMasters(notify);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not reload plugin repositories");
        }
        finally
        {
            this.ReposReady = true;
        }
    }

    /// <summary>
    /// Apply visibility and eligibility filters to the available plugins, then event that the list has updated.
    /// </summary>
    /// <param name="notify">Whether to notify that available plugins have changed afterwards.</param>
    public void RefilterPluginMasters(bool notify = true)
    {
        lock (this.pluginListLock)
        {
            this.availablePluginsList.Clear();
            this.availablePluginsList.AddRange(this.Repos
                                                   .SelectMany(repo => repo.PluginMaster)
                                                   .Where(this.IsManifestEligible)
                                                   .Where(IsManifestVisible));
            
            if (notify)
            {
                this.NotifyAvailablePluginsChanged();
            }
        }
    }

    /// <summary>
    /// Scan the devPlugins folder for new DLL files that are not already loaded into the manager. They are not loaded,
    /// only shown as disabled in the installed plugins window. This is a modified version of LoadAllPlugins that works
    /// a little differently.
    /// </summary>
    public void ScanDevPlugins()
    {
        // devPlugins are more freeform. Look for any dll and hope to get lucky.
        var devDllFiles = new List<FileInfo>();

        foreach (var setting in this.configuration.DevPluginLoadLocations)
        {
            if (!setting.IsEnabled)
                continue;
            
            Log.Verbose("Scanning dev plugins at {Path}", setting.Path);

            if (Directory.Exists(setting.Path))
            {
                devDllFiles.AddRange(new DirectoryInfo(setting.Path).GetFiles("*.dll", SearchOption.AllDirectories));
            }
            else if (File.Exists(setting.Path))
            {
                devDllFiles.Add(new FileInfo(setting.Path));
            }
        }

        var listChanged = false;

        foreach (var dllFile in devDllFiles)
        {
            // This file is already known to us
            lock (this.pluginListLock)
            {
                if (this.installedPluginsList.Any(lp => lp.DllFile.FullName == dllFile.FullName))
                    continue;
            }

            // Manifests are now required for devPlugins
            var manifestFile = LocalPluginManifest.GetManifestFile(dllFile);
            if (!manifestFile.Exists)
            {
                Log.Error("DLL at {DllPath} has no manifest, this is no longer valid", dllFile.FullName);
                continue;
            }
            
            var manifest = LocalPluginManifest.Load(manifestFile);
            if (manifest == null)
            {
                Log.Error("Could not deserialize manifest for DLL at {DllPath}", dllFile.FullName);
                continue;
            }

            try
            {
                // Add them to the list and let the user decide, nothing is auto-loaded.
                this.LoadPluginAsync(dllFile, manifest, PluginLoadReason.Installer, isDev: true, doNotLoad: true)
                    .Wait();
                listChanged = true;
            }
            catch (InvalidPluginException)
            {
                // Not a plugin
            }
            catch (Exception ex)
            {
                Log.Error(ex, "During devPlugin scan, an unexpected error occurred");
            }
        }

        if (listChanged)
            this.NotifyinstalledPluginsListChanged();
    }

    /// <summary>
    /// Install a plugin from a repository and load it.
    /// </summary>
    /// <param name="repoManifest">The plugin definition.</param>
    /// <param name="useTesting">If the testing version should be used.</param>
    /// <param name="reason">The reason this plugin was loaded.</param>
    /// <param name="inheritedWorkingPluginId">WorkingPluginId this plugin should inherit.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<LocalPlugin> InstallPluginAsync(
        RemotePluginManifest repoManifest, bool useTesting, PluginLoadReason reason,
        Guid? inheritedWorkingPluginId = null)
    {
        var stream = await this.DownloadPluginAsync(repoManifest, useTesting);
        return await this.InstallPluginInternalAsync(repoManifest, useTesting, reason, stream, inheritedWorkingPluginId);
    }
    
    /// <summary>
    /// Remove a plugin.
    /// </summary>
    /// <param name="plugin">Plugin to remove.</param>
    public void RemovePlugin(LocalPlugin plugin)
    {
        if (plugin.State != PluginState.Unloaded && plugin.HasEverStartedLoad)
            throw new InvalidPluginOperationException($"Unable to remove {plugin.Name}, not unloaded and had loaded before");

        lock (this.pluginListLock)
        {
            this.installedPluginsList.Remove(plugin);
        }

        // NET8 CHORE
        // PluginLocations.Remove(plugin.AssemblyName?.FullName ?? string.Empty, out _);

        this.NotifyinstalledPluginsListChanged();
        this.NotifyAvailablePluginsChanged();
    }

    /// <summary>
    /// Cleanup disabled plugins. Does not target devPlugins.
    /// </summary>
    public void CleanupPlugins()
    {
        foreach (var pluginDir in this.pluginDirectory.GetDirectories())
        {
            try
            {
                var versionDirs = pluginDir.GetDirectories();

                versionDirs = versionDirs
                              .OrderByDescending(dir =>
                              {
                                  var isVersion = Version.TryParse(dir.Name, out var version);

                                  if (!isVersion)
                                  {
                                      Log.Debug($"Not a version, cleaning up {dir.FullName}");
                                      dir.Delete(true);
                                  }

                                  return version;
                              })
                              .ToArray();

                if (versionDirs.Length == 0)
                {
                    Log.Information($"No versions: cleaning up {pluginDir.FullName}");
                    pluginDir.Delete(true);
                }
                else
                {
                    for (var i = 0; i < versionDirs.Length; i++)
                    {
                        var versionDir = versionDirs[i];
                        try
                        {
                            if (i != 0)
                            {
                                Log.Information($"Old version: cleaning up {versionDir.FullName}");
                                versionDir.Delete(true);
                                continue;
                            }

                            var dllFile = new FileInfo(Path.Combine(versionDir.FullName, $"{pluginDir.Name}.dll"));
                            if (!dllFile.Exists)
                            {
                                Log.Information($"Missing dll: cleaning up {versionDir.FullName}");
                                versionDir.Delete(true);
                                continue;
                            }

                            var manifestFile = LocalPluginManifest.GetManifestFile(dllFile);
                            if (!manifestFile.Exists)
                            {
                                Log.Information($"Missing manifest: cleaning up {versionDir.FullName}");
                                versionDir.Delete(true);
                                continue;
                            }

                            if (manifestFile.Length == 0)
                            {
                                Log.Information($"Manifest empty: cleaning up {versionDir.FullName}");
                                versionDir.Delete(true);
                                continue;
                            }

                            var manifest = LocalPluginManifest.Load(manifestFile);
                            if (manifest.ScheduledForDeletion)
                            {
                                Log.Information($"Scheduled deletion: cleaning up {versionDir.FullName}");
                                versionDir.Delete(true);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"Could not clean up {versionDir.FullName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Could not clean up {pluginDir.FullName}");
            }
        }
    }

    /// <summary>
    /// Update all non-dev plugins.
    /// </summary>
    /// <param name="toUpdate">List of plugins to update.</param>
    /// <param name="dryRun">Perform a dry run, don't install anything.</param>
    /// <param name="autoUpdate">If this action was performed as part of an auto-update.</param>
    /// <param name="progress">An <see cref="IProgress{T}"/> implementation to receive progress updates about the installation status.</param>
    /// <returns>Success or failure and a list of updated plugin metadata.</returns>
    public async Task<IEnumerable<PluginUpdateStatus>> UpdatePluginsAsync(
        ICollection<AvailablePluginUpdate> toUpdate,
        bool dryRun,
        bool autoUpdate = false,
        IProgress<PluginUpdateProgress>? progress = null)
    {
        Log.Information("Starting plugin update");

        var updateTasks = new List<Task<PluginUpdateStatus>>();
        var totalPlugins = toUpdate.Count;
        var processedPlugins = 0;

        foreach (var plugin in toUpdate)
        {
            // Can't update that!
            if (plugin.InstalledPlugin.IsDev)
                continue;

            if (plugin.InstalledPlugin.Manifest.ScheduledForDeletion)
                continue;

            updateTasks.Add(UpdateSinglePluginWithProgressAsync(plugin));
        }

        var updatedList = await Task.WhenAll(updateTasks);

        this.NotifyinstalledPluginsListChanged();
        this.NotifyPluginsForStateChange(
            autoUpdate ? PluginListInvalidationKind.AutoUpdate : PluginListInvalidationKind.Update,
            updatedList.Select(x => x.InternalName));

        Log.Information("Plugin update OK. {UpdateCount} plugins updated", updatedList.Length);

        return updatedList;

        async Task<PluginUpdateStatus> UpdateSinglePluginWithProgressAsync(AvailablePluginUpdate plugin)
        {
            var result = await this.UpdateSinglePluginAsync(plugin, false, dryRun);

            // Update the progress
            if (progress != null)
            {
                var newProcessedAmount = Interlocked.Increment(ref processedPlugins);
                progress.Report(new PluginUpdateProgress(
                                    newProcessedAmount,
                                    totalPlugins,
                                    plugin.InstalledPlugin.Manifest));
            }

            return result;
        }
    }

    /// <summary>
    /// Update a single plugin, provided a valid <see cref="AvailablePluginUpdate"/>.
    /// </summary>
    /// <param name="metadata">The available plugin update.</param>
    /// <param name="notify">Whether to notify that installed plugins have changed afterwards.</param>
    /// <param name="dryRun">Whether or not to actually perform the update, or just indicate success.</param>
    /// <returns>The status of the update.</returns>
    public async Task<PluginUpdateStatus> UpdateSinglePluginAsync(AvailablePluginUpdate metadata, bool notify, bool dryRun)
    {
        var plugin = metadata.InstalledPlugin;

        var workingPluginId = metadata.InstalledPlugin.EffectiveWorkingPluginId;
        if (workingPluginId == Guid.Empty)
            throw new Exception("Existing plugin had no WorkingPluginId");

        var updateStatus = new PluginUpdateStatus
        {
            InternalName = plugin.Manifest.InternalName,
            Name = plugin.Manifest.Name,
            Version = (metadata.UseTesting
                           ? metadata.UpdateManifest.TestingAssemblyVersion
                           : metadata.UpdateManifest.AssemblyVersion)!,
            Status = PluginUpdateStatus.StatusKind.Success,
            HasChangelog = !metadata.UpdateManifest.Changelog.IsNullOrWhitespace(),
        };
        
        // Check if this plugin is already up to date (=> AvailablePluginUpdate was stale)
        lock (this.installedPluginsList)
        {
            var matchedPlugin = this.installedPluginsList.FirstOrDefault(x => x.EffectiveWorkingPluginId == workingPluginId);
            if (matchedPlugin?.EffectiveVersion == metadata.EffectiveVersion)
            {
                Log.Information("Plugin {Name} is already up to date", plugin.Manifest.Name);
                updateStatus.Status = PluginUpdateStatus.StatusKind.AlreadyUpToDate;
                return updateStatus;
            }
        }

        if (!dryRun)
        {
            // Download the update before unloading
            Stream updateStream;
            try
            {
                updateStream = await this.DownloadPluginAsync(metadata.UpdateManifest, metadata.UseTesting);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during download (update)");
                updateStatus.Status = PluginUpdateStatus.StatusKind.FailedDownload;
                return updateStatus;
            }
            
            // Unload if loaded
            if (plugin.State is PluginState.Loaded or PluginState.LoadError or PluginState.DependencyResolutionFailed)
            {
                try
                {
                    await plugin.UnloadAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during unload (update)");
                    updateStatus.Status = PluginUpdateStatus.StatusKind.FailedUnload;
                    return updateStatus;
                }
            }

            if (plugin.IsDev)
            {
                throw new Exception("We should never update a dev plugin");
            }

            try
            {
                // TODO: Why were we ever doing this? We should never be loading the old version in the first place
                /*
                    if (!plugin.IsDisabled)
                        plugin.Disable();
                        */

                lock (this.pluginListLock)
                {
                    this.installedPluginsList.Remove(plugin);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during remove from plugin list (update)");
                updateStatus.Status = PluginUpdateStatus.StatusKind.FailedUnload;
                return updateStatus;
            }

            try
            {
                await this.InstallPluginInternalAsync(metadata.UpdateManifest, metadata.UseTesting, PluginLoadReason.Update, updateStream, workingPluginId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during install (update)");
                updateStatus.Status = PluginUpdateStatus.StatusKind.FailedLoad;
                return updateStatus;
            }
        }

        if (notify && updateStatus.Status == PluginUpdateStatus.StatusKind.Success)
            this.NotifyinstalledPluginsListChanged();

        return updateStatus;
    }

    /// <summary>
    /// Delete the plugin configuration, unload/reload it if loaded.
    /// </summary>
    /// <param name="plugin">The plugin.</param>
    /// <exception cref="Exception">Throws if the plugin is still loading/unloading.</exception>
    /// <returns>The task.</returns>
    public async Task DeleteConfigurationAsync(LocalPlugin plugin)
    {
        if (plugin.State is PluginState.Loading or PluginState.Unloading)
            throw new Exception("Cannot delete configuration for a loading/unloading plugin");

        var isReloading = plugin.IsLoaded;
        if (isReloading)
            await plugin.UnloadAsync();

        for (var waitUntil = Environment.TickCount64 + 1000; Environment.TickCount64 < waitUntil;)
        {
            try
            {
                this.PluginConfigs.Delete(plugin.Manifest.InternalName);
                break;
            }
            catch (IOException)
            {
                await Task.Delay(100);
            }
        }

        if (isReloading)
        {
            // Let's indicate "installer" here since this is supposed to be a fresh install
            await plugin.LoadAsync(PluginLoadReason.Installer);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the given manifest is eligible for ANYTHING. These are hard
    /// checks that should not allow installation or loading.
    /// </summary>
    /// <param name="manifest">Plugin manifest.</param>
    /// <returns>If the manifest is eligible.</returns>
    public bool IsManifestEligible(PluginManifest manifest)
    {
        // Testing exclusive
        if (manifest.IsTestingExclusive && !this.configuration.DoPluginTest)
        {
            Log.Verbose($"Testing exclusivity: {manifest.InternalName} - {manifest.AssemblyVersion} - {manifest.TestingAssemblyVersion}");
            return false;
        }

        // Applicable version
        if (manifest.ApplicableVersion < this.dalamud.StartInfo.GameVersion)
        {
            Log.Verbose($"Game version: {manifest.InternalName} - {manifest.AssemblyVersion} - {manifest.TestingAssemblyVersion}");
            return false;
        }

        // API level - we keep the API before this in the installer to show as "outdated"
        var effectiveApiLevel = this.UseTesting(manifest) && manifest.TestingDalamudApiLevel != null ? manifest.TestingDalamudApiLevel.Value : manifest.DalamudApiLevel;
        if (effectiveApiLevel < DalamudApiLevel - 1 && !this.LoadAllApiLevels)
        {
            Log.Verbose($"API Level: {manifest.InternalName} - {manifest.AssemblyVersion} - {manifest.TestingAssemblyVersion}");
            return false;
        }

        // Banned
        if (this.IsManifestBanned(manifest))
        {
            Log.Verbose($"Banned: {manifest.InternalName} - {manifest.AssemblyVersion} - {manifest.TestingAssemblyVersion}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Determine if a plugin has been banned by inspecting the manifest.
    /// </summary>
    /// <param name="manifest">Manifest to inspect.</param>
    /// <returns>A value indicating whether the plugin/manifest has been banned.</returns>
    public bool IsManifestBanned(PluginManifest manifest)
    {
        Debug.Assert(this.bannedPlugins != null, "this.bannedPlugins != null");

        if (this.LoadBannedPlugins)
            return false;

        var config = Service<DalamudConfiguration>.Get();

        var versionToCheck = manifest.AssemblyVersion;
        if (config.DoPluginTest && manifest.TestingAssemblyVersion > manifest.AssemblyVersion)
        {
            versionToCheck = manifest.TestingAssemblyVersion;
        }

        return this.bannedPlugins.Any(ban => (ban.Name == manifest.InternalName || ban.Name == Hash.GetStringSha256Hash(manifest.InternalName))
                                                                        && ban.AssemblyVersion >= versionToCheck);
    }

    /// <summary>
    /// Get the reason of a banned plugin by inspecting the manifest.
    /// </summary>
    /// <param name="manifest">Manifest to inspect.</param>
    /// <returns>The reason of the ban, if any.</returns>
    public string GetBanReason(PluginManifest manifest)
    {
        Debug.Assert(this.bannedPlugins != null, "this.bannedPlugins != null");

        return this.bannedPlugins.LastOrDefault(ban => ban.Name == manifest.InternalName).Reason;
    }

    /// <summary>
    /// Get the plugin that called this method by walking the provided stack trace,
    /// or null, if it cannot be determined.
    /// At the time, this is naive and shouldn't be used for security-critical checks.
    /// </summary>
    /// <param name="trace">The trace to walk.</param>
    /// <returns>The calling plugin, or null.</returns>
    public LocalPlugin? FindCallingPlugin(StackTrace trace)
    {
        foreach (var frame in trace.GetFrames())
        {
            var declaringType = frame.GetMethod()?.DeclaringType;
            if (declaringType == null)
                continue;

            lock (this.pluginListLock)
            {
                foreach (var plugin in this.installedPluginsList)
                {
                    if (plugin.AssemblyName != null &&
                        plugin.AssemblyName.FullName == declaringType.Assembly.GetName().FullName)
                        return plugin;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Get the plugin that called this method by walking the stack,
    /// or null, if it cannot be determined.
    /// At the time, this is naive and shouldn't be used for security-critical checks.
    /// </summary>
    /// <returns>The calling plugin, or null.</returns>
    public LocalPlugin? FindCallingPlugin() => this.FindCallingPlugin(new StackTrace());

    /// <summary>
    /// Resolves the services that a plugin may have a dependency on.<br />
    /// This is required, as the lifetime of a plugin cannot be longer than PluginManager,
    /// and we want to ensure that dependency services to be kept alive at least until all the plugins, and thus
    /// PluginManager to be gone.
    /// </summary>
    /// <returns>The dependency services.</returns>
    private static IEnumerable<Type> ResolvePossiblePluginDependencyServices()
    {
        // DalamudPluginInterface hands out a reference to this, so we have to keep it around.
        // TODO api9: make it a service
        yield return typeof(DataShare);

        // DalamudTextureWrap registers textures to dispose with IM.
        yield return typeof(InterfaceManager);

        // Note: LocalPlugin uses ServiceContainer to create scopes, but it is done outside PM ctor.
        // This is not required: yield return typeof(ServiceContainer);

        foreach (var serviceType in ServiceManager.GetConcreteServiceTypes())
        {
            if (serviceType == typeof(PluginManager))
                continue;
                
            // Scoped plugin services lifetime is tied to their scopes. They go away when LocalPlugin goes away.
            // Nonetheless, their direct dependencies must be considered.
            if (serviceType.GetServiceKind() == ServiceManager.ServiceKind.ScopedService)
            {
                var typeAsServiceT = ServiceHelpers.GetAsService(serviceType);
                var dependencies = ServiceHelpers.GetDependencies(typeAsServiceT, false);
                ServiceManager.Log.Verbose("Found dependencies of scoped plugin service {Type} ({Cnt})", serviceType.FullName!, dependencies!.Count);
                    
                foreach (var scopedDep in dependencies)
                {
                    if (scopedDep == typeof(PluginManager))
                        throw new Exception("Scoped plugin services cannot depend on PluginManager.");
                        
                    ServiceManager.Log.Verbose("PluginManager MUST depend on {Type} via {BaseType}", scopedDep.FullName!, serviceType.FullName!);
                    yield return scopedDep;
                }

                continue;
            }
                
            var pluginInterfaceAttribute = serviceType.GetCustomAttribute<PluginInterfaceAttribute>(true);
            if (pluginInterfaceAttribute == null)
                continue;

            ServiceManager.Log.Verbose("PluginManager MUST depend on {Type}", serviceType.FullName!);
            yield return serviceType;
        }
    }

    /// <summary>
    /// Check if there are any inconsistencies with our plugins, their IDs, and our profiles. 
    /// </summary>
    private void ParanoiaValidatePluginsAndProfiles()
    {
        var seenIds = new List<Guid>();
        
        foreach (var installedPlugin in this.InstalledPlugins)
        {
            if (installedPlugin.EffectiveWorkingPluginId == Guid.Empty)
                throw new Exception($"{(installedPlugin is LocalDevPlugin ? "DevPlugin" : "Plugin")} '{installedPlugin.Manifest.InternalName}' has an empty WorkingPluginId.");

            if (seenIds.Contains(installedPlugin.EffectiveWorkingPluginId))
            {
                throw new Exception(
                    $"{(installedPlugin is LocalDevPlugin ? "DevPlugin" : "Plugin")} '{installedPlugin.Manifest.InternalName}' has a duplicate WorkingPluginId '{installedPlugin.EffectiveWorkingPluginId}'");
            }
            
            seenIds.Add(installedPlugin.EffectiveWorkingPluginId);
        }
        
        this.profileManager.ParanoiaValidateProfiles();
    }
    
    private async Task<Stream> DownloadPluginAsync(RemotePluginManifest repoManifest, bool useTesting)
    {
        var downloadUrl = useTesting ? repoManifest.DownloadLinkTesting : repoManifest.DownloadLinkInstall;
        var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl)
        {
            Headers =
            {
                Accept =
                {
                    new MediaTypeWithQualityHeaderValue("application/zip"),
                },
            },
        };
        var response = await this.happyHttpClient.SharedHttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync();
    }

    /// <summary>
    /// Install a plugin from a repository and load it.
    /// </summary>
    /// <param name="repoManifest">The plugin definition.</param>
    /// <param name="useTesting">If the testing version should be used.</param>
    /// <param name="reason">The reason this plugin was loaded.</param>
    /// <param name="zipStream">Stream of the ZIP archive containing the plugin that is about to be installed.</param>
    /// <param name="inheritedWorkingPluginId">WorkingPluginId this plugin should inherit.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task<LocalPlugin> InstallPluginInternalAsync(RemotePluginManifest repoManifest, bool useTesting, PluginLoadReason reason, Stream zipStream, Guid? inheritedWorkingPluginId = null)
    {
        var version = useTesting ? repoManifest.TestingAssemblyVersion : repoManifest.AssemblyVersion;
        Log.Debug($"Installing plugin {repoManifest.Name} (testing={useTesting}, version={version}, reason={reason})");
        
        // If this plugin is in the default profile for whatever reason, delete the state
        // If it was in multiple profiles and is still, the user uninstalled it and chose to keep it in there,
        // or the user removed the plugin manually in which case we don't care
        if (reason == PluginLoadReason.Installer)
        {
            try
            {
                // Only remove entries from the default profile that are NOT currently tied to an active LocalPlugin
                var guidsToRemove = this.profileManager.DefaultProfile.Plugins
                                        .Where(x => this.InstalledPlugins.All(y => y.EffectiveWorkingPluginId != x.WorkingPluginId))
                                        .Select(x => x.WorkingPluginId)
                                        .ToArray();

                if (guidsToRemove.Length != 0)
                {
                    Log.Verbose("Removing {Cnt} orphaned entries from default profile", guidsToRemove.Length);
                    foreach (var guid in guidsToRemove)
                    {
                        // We don't need to apply, it doesn't matter
                        await this.profileManager.DefaultProfile.RemoveAsync(guid, false, false);
                    }
                }
            }
            catch (ProfileOperationException ex)
            {
                Log.Error(ex, "Error during default profile cleanup");
            }
        }
        else
        {
            // If we are doing anything other than a fresh install, not having a workingPluginId is an error that must be fixed
            Debug.Assert(inheritedWorkingPluginId != null, "inheritedWorkingPluginId != null");
        }
        
        // Ensure that we have a testing opt-in for this plugin if we are installing a testing version
        if (useTesting && this.configuration.PluginTestingOptIns!.All(x => x.InternalName != repoManifest.InternalName))
        {
            // TODO: this isn't safe
            this.configuration.PluginTestingOptIns.Add(new PluginTestingOptIn(repoManifest.InternalName));
            this.configuration.QueueSave();
        }

        var outputDir = new DirectoryInfo(Path.Combine(this.pluginDirectory.FullName, repoManifest.InternalName, version?.ToString() ?? string.Empty));

        try
        {
            if (outputDir.Exists)
                outputDir.Delete(true);

            outputDir.Create();
        }
        catch
        {
            // ignored, since the plugin may be loaded already
        }

        Log.Debug("Extracting to {OutputDir}", outputDir);

        using (var archive = new ZipArchive(zipStream))
        {
            foreach (var zipFile in archive.Entries)
            {
                var outputFile = new FileInfo(Path.GetFullPath(Path.Combine(outputDir.FullName, zipFile.FullName)));

                if (!outputFile.FullName.StartsWith(outputDir.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException("Trying to extract file outside of destination directory. See this link for more info: https://snyk.io/research/zip-slip-vulnerability");
                }

                if (outputFile.Directory == null)
                {
                    throw new IOException("Output directory invalid.");
                }

                if (zipFile.Name.IsNullOrEmpty())
                {
                    // Assuming Empty for Directory
                    Log.Verbose($"ZipFile name is null or empty, treating as a directory: {outputFile.Directory.FullName}");
                    Directory.CreateDirectory(outputFile.Directory.FullName);
                    continue;
                }

                // Ensure directory is created
                Directory.CreateDirectory(outputFile.Directory.FullName);

                try
                {
                    zipFile.ExtractToFile(outputFile.FullName, true);
                }
                catch (Exception ex)
                {
                    if (outputFile.Extension.EndsWith("dll"))
                    {
                        throw new IOException($"Could not overwrite {zipFile.Name}: {ex.Message}");
                    }

                    Log.Error($"Could not overwrite {zipFile.Name}: {ex.Message}");
                }
            }
        }

        var dllFile = LocalPluginManifest.GetPluginFile(outputDir, repoManifest);
        var manifestFile = LocalPluginManifest.GetManifestFile(dllFile);

        // We need to save the repoManifest due to how the repo fills in some fields that authors are not expected to use.
        Util.WriteAllTextSafe(manifestFile.FullName, JsonConvert.SerializeObject(repoManifest, Formatting.Indented));

        // Reload as a local manifest, add some attributes, and save again.
        var manifest = LocalPluginManifest.Load(manifestFile);

        if (manifest == null)
            throw new Exception("Plugin had no valid manifest");

        if (manifest.InternalName != repoManifest.InternalName)
        {
            Directory.Delete(outputDir.FullName, true);
            throw new Exception(
                $"Distributed internal name does not match repo internal name: {manifest.InternalName} - {repoManifest.InternalName}");
        }

        if (manifest.WorkingPluginId != Guid.Empty)
            throw new Exception("Plugin shall not specify a WorkingPluginId");

        manifest.WorkingPluginId = inheritedWorkingPluginId ?? Guid.NewGuid();

        if (useTesting)
        {
            manifest.Testing = true;
        }

        // Document the url the plugin was installed from
        manifest.InstalledFromUrl = repoManifest.SourceRepo.IsThirdParty ? repoManifest.SourceRepo.PluginMasterUrl : SpecialPluginSource.MainRepo;

        manifest.Save(manifestFile, "installation");

        Log.Information($"Installed plugin {manifest.Name} (testing={useTesting})");

        var plugin = await this.LoadPluginAsync(dllFile, manifest, reason);

        this.NotifyinstalledPluginsListChanged();
        return plugin;
    }
    
    /// <summary>
    /// Load a plugin.
    /// </summary>
    /// <param name="dllFile">The <see cref="FileInfo"/> associated with the main assembly of this plugin.</param>
    /// <param name="manifest">The already loaded definition, if available.</param>
    /// <param name="reason">The reason this plugin was loaded.</param>
    /// <param name="isDev">If this plugin should support development features.</param>
    /// <param name="isBoot">If this plugin is being loaded at boot.</param>
    /// <param name="doNotLoad">Don't load the plugin, just don't do it.</param>
    /// <returns>The loaded plugin.</returns>
    private async Task<LocalPlugin> LoadPluginAsync(FileInfo dllFile, LocalPluginManifest manifest, PluginLoadReason reason, bool isDev = false, bool isBoot = false, bool doNotLoad = false)
    {
        var name = manifest?.Name ?? dllFile.Name;
        var loadPlugin = !doNotLoad;

        LocalPlugin? plugin;

        if (manifest != null && (manifest.InternalName == null || manifest.Name == null))
        {
            Log.Error("{FileName}: Your manifest has no internal name or name set! Can't load this.", dllFile.FullName);
            throw new Exception("No internal name");
        }

        if (isDev)
        {
            Log.Information($"Loading dev plugin {name}");
            plugin = new LocalDevPlugin(dllFile, manifest);
        }
        else
        {
            Log.Information($"Loading plugin {name}");
            plugin = new LocalPlugin(dllFile, manifest);
        }
        
        // Perform a migration from InternalName to GUIDs. The plugin should definitely have a GUID here.
        // This will also happen if you are installing a plugin with the installer, and that's intended!
        // It means that, if you have a profile which has unsatisfied plugins, installing a matching plugin will
        // enter it into the profiles it can match.
        if (plugin.EffectiveWorkingPluginId == Guid.Empty)
            throw new Exception("Plugin should have a WorkingPluginId at this point");
        this.profileManager.MigrateProfilesToGuidsForPlugin(plugin.Manifest.InternalName, plugin.EffectiveWorkingPluginId);
        
        var wantedByAnyProfile = false;

        // Now, if this is a devPlugin, figure out if we want to load it
        if (isDev)
        {
            var devPlugin = (LocalDevPlugin)plugin;
            loadPlugin &= !isBoot;

            var wantsInDefaultProfile =
                this.profileManager.DefaultProfile.WantsPlugin(plugin.EffectiveWorkingPluginId);
            if (wantsInDefaultProfile == null)
            {
                // We don't know about this plugin, so we don't want to do anything here.
                // The code below will take care of it and add it with the default value.
                Log.Verbose("DevPlugin {Name} not wanted in default plugin", plugin.Manifest.InternalName);
                
                // Check if any profile wants this plugin. We need to do this here, since we want to allow loading a dev plugin if a non-default profile wants it active.
                // Note that this will not add the plugin to the default profile. That's done below in any other case.
                wantedByAnyProfile = await this.profileManager.GetWantStateAsync(plugin.EffectiveWorkingPluginId, plugin.Manifest.InternalName, false, false);
                
                // If it is wanted by any other profile, we do want to load it.
                if (wantedByAnyProfile)
                    loadPlugin = true;
            }
            else if (wantsInDefaultProfile == false && devPlugin.StartOnBoot)
            {
                // We didn't want this plugin, and StartOnBoot is on. That means we don't want it and it should stay off until manually enabled.
                Log.Verbose("DevPlugin {Name} disabled and StartOnBoot => disable", plugin.Manifest.InternalName);
                await this.profileManager.DefaultProfile.AddOrUpdateAsync(plugin.EffectiveWorkingPluginId, plugin.Manifest.InternalName, false, false);
                loadPlugin = false;
            }
            else if (wantsInDefaultProfile == true && devPlugin.StartOnBoot)
            {
                // We wanted this plugin, and StartOnBoot is on. That means we actually do want it.
                Log.Verbose("DevPlugin {Name} enabled and StartOnBoot => enable", plugin.Manifest.InternalName);
                await this.profileManager.DefaultProfile.AddOrUpdateAsync(plugin.EffectiveWorkingPluginId, plugin.Manifest.InternalName, true, false);
                loadPlugin = !doNotLoad;
            }
            else if (wantsInDefaultProfile == true && !devPlugin.StartOnBoot)
            {
                // We wanted this plugin, but StartOnBoot is off. This means we don't want it anymore.
                Log.Verbose("DevPlugin {Name} enabled and !StartOnBoot => disable", plugin.Manifest.InternalName);
                await this.profileManager.DefaultProfile.AddOrUpdateAsync(plugin.EffectiveWorkingPluginId, plugin.Manifest.InternalName, false, false);
                loadPlugin = false;
            }
            else if (wantsInDefaultProfile == false && !devPlugin.StartOnBoot)
            {
                // We didn't want this plugin, and StartOnBoot is off. We don't want it.
                Log.Verbose("DevPlugin {Name} disabled and !StartOnBoot => disable", plugin.Manifest.InternalName);
                await this.profileManager.DefaultProfile.AddOrUpdateAsync(plugin.EffectiveWorkingPluginId, plugin.Manifest.InternalName, false, false);
                loadPlugin = false;
            }

            // Never automatically load outdated dev plugins.
            if (devPlugin.IsOutdated)
            {
                loadPlugin = false;
                Log.Warning("DevPlugin {Name} is outdated, not loading automatically - update DalamudPackager or SDK!", plugin.Manifest.InternalName);
            }

            plugin = devPlugin;
        }

#pragma warning disable CS0618
        var defaultState = manifest?.Disabled != true && loadPlugin;
#pragma warning restore CS0618
        
        // Plugins that aren't in any profile will be added to the default profile with this call.
        // We are skipping a double-lookup for dev plugins that are wanted by non-default profiles, as noted above.
        wantedByAnyProfile = wantedByAnyProfile || await this.profileManager.GetWantStateAsync(plugin.EffectiveWorkingPluginId, plugin.Manifest.InternalName, defaultState);
        Log.Information("{Name} defaultState: {State} wantedByAnyProfile: {WantedByAny} loadPlugin: {LoadPlugin}", plugin.Manifest.InternalName, defaultState, wantedByAnyProfile, loadPlugin);
        
        if (loadPlugin)
        {
            try
            {
                if (wantedByAnyProfile && !plugin.IsOrphaned)
                {
                    await plugin.LoadAsync(reason);
                }
                else
                {
                    Log.Verbose($"{name} not loaded, wantToLoad:{wantedByAnyProfile} orphaned:{plugin.IsOrphaned}");
                }
            }
            catch (InvalidPluginException)
            {
                // NET8 CHORE
                // PluginLocations.Remove(plugin.AssemblyName?.FullName ?? string.Empty, out _);
                throw;
            }
            catch (BannedPluginException)
            {
                // Out of date plugins get added so they can be updated.
                Log.Information($"Plugin was banned, adding anyways: {dllFile.Name}");
            }
            catch (Exception ex)
            {
                if (plugin.IsDev)
                {
                    // Dev plugins always get added to the list so they can be fiddled with in the UI
                    Log.Information(ex, $"Dev plugin failed to load, adding anyways: {dllFile.Name}");

                    // NOTE(goat): This can't work - plugins don't "unload" if they fail to load.
                    // plugin.Disable(); // Disable here, otherwise you can't enable+load later
                }
                else if (plugin.IsOutdated)
                {
                    // Out of date plugins get added, so they can be updated.
                    Log.Information(ex, $"Plugin was outdated, adding anyways: {dllFile.Name}");
                }
                else if (plugin.IsOrphaned)
                {
                    // Orphaned plugins get added, so that users aren't confused.
                    Log.Information(ex, $"Plugin was orphaned, adding anyways: {dllFile.Name}");
                }
                else if (isBoot)
                {
                    // During boot load, plugins always get added to the list so they can be fiddled with in the UI
                    Log.Information(ex, $"Regular plugin failed to load, adding anyways: {dllFile.Name}");

                    // NOTE(goat): This can't work - plugins don't "unload" if they fail to load.
                    // plugin.Disable(); // Disable here, otherwise you can't enable+load later
                }
                else if (!plugin.CheckPolicy())
                {
                    // During boot load, plugins always get added to the list so they can be fiddled with in the UI
                    Log.Information(ex, $"Plugin not loaded due to policy, adding anyways: {dllFile.Name}");

                    // NOTE(goat): This can't work - plugins don't "unload" if they fail to load.
                    // plugin.Disable(); // Disable here, otherwise you can't enable+load later
                }
                else
                {
                    // NET8 CHORE
                    // PluginLocations.Remove(plugin.AssemblyName?.FullName ?? string.Empty, out _);
                    throw;
                }
            }
        }

        if (plugin == null)
            throw new Exception("Plugin was null when adding to list");

        lock (this.pluginListLock)
        {
            this.installedPluginsList.Add(plugin);
        }

        // Mark as finished loading
        if (manifest.LoadSync)
            this.StartupLoadTracking?.Finish(manifest.InternalName);

        return plugin;
    }

    private void DetectAvailablePluginUpdates()
    {
        Log.Debug("Starting plugin update check...");
        
        lock (this.pluginListLock)
        {
            this.updatablePluginsList.Clear();
            
            foreach (var plugin in this.installedPluginsList)
            {
                var installedVersion = plugin.IsTesting
                                           ? plugin.Manifest.TestingAssemblyVersion
                                           : plugin.Manifest.AssemblyVersion;

                var updates = this.AvailablePlugins
                                  .Where(remoteManifest => plugin.Manifest.InternalName == remoteManifest.InternalName)
                                  .Where(remoteManifest => plugin.Manifest.InstalledFromUrl == remoteManifest.SourceRepo.PluginMasterUrl || !remoteManifest.SourceRepo.IsThirdParty)
                                  .Where(remoteManifest =>
                                  {
                                      var useTesting = this.UseTesting(remoteManifest);
                                      var candidateApiLevel = useTesting && remoteManifest.TestingDalamudApiLevel != null
                                                                  ? remoteManifest.TestingDalamudApiLevel.Value
                                                                  : remoteManifest.DalamudApiLevel;

                                      return candidateApiLevel == DalamudApiLevel;
                                  })
                                  .Select(remoteManifest =>
                                  {
                                      var useTesting = this.UseTesting(remoteManifest);
                                      var candidateVersion = useTesting
                                                                 ? remoteManifest.TestingAssemblyVersion
                                                                 : remoteManifest.AssemblyVersion;
                                      var isUpdate = candidateVersion > installedVersion;

                                      return (isUpdate, useTesting, candidateVersion, remoteManifest);
                                  })
                                  .Where(tpl => tpl.isUpdate)
                                  .ToList();

                if (updates.Count > 0)
                {
                    var update = updates.Aggregate((t1, t2) => t1.candidateVersion > t2.candidateVersion ? t1 : t2);
                    this.updatablePluginsList.Add(new AvailablePluginUpdate(plugin, update.remoteManifest, update.useTesting));
                }
            }
        }
        
        Log.Debug("Update check found {updateCount} available updates.", this.updatablePluginsList.Count);
    }

    private void NotifyAvailablePluginsChanged()
    { 
        this.DetectAvailablePluginUpdates();

        this.OnAvailablePluginsChanged?.InvokeSafely();
    }

    private void NotifyinstalledPluginsListChanged()
    {
        this.DetectAvailablePluginUpdates();

        this.OnInstalledPluginsChanged?.InvokeSafely();
    }

    private void NotifyPluginsForStateChange(PluginListInvalidationKind kind, IEnumerable<string> affectedInternalNames)
    {
        foreach (var installedPlugin in this.installedPluginsList)
        {
            if (!installedPlugin.IsLoaded || installedPlugin.DalamudInterface == null)
                continue;

            installedPlugin.DalamudInterface.NotifyActivePluginsChanged(
                kind,
                // ReSharper disable once PossibleMultipleEnumeration
                affectedInternalNames.Contains(installedPlugin.Manifest.InternalName));
        }
    }

    private void LoadAndStartLoadSyncPlugins()
    {
        try
        {
            using (Timings.Start("PM Load Plugin Repos"))
            {
                _ = this.SetPluginReposFromConfigAsync(false);
                this.OnInstalledPluginsChanged += () => Task.Run(Troubleshooting.LogTroubleshooting);

                Log.Information("[T3] PM repos OK!");
            }

            using (Timings.Start("PM Cleanup Plugins"))
            {
                this.CleanupPlugins();
                Log.Information("[T3] PMC OK!");
            }

            using (Timings.Start("PM Load Sync Plugins"))
            {
                var loadAllPlugins = Task.Run(this.LoadAllPlugins);
                
                // We wait for all blocking services and tasks to finish before kicking off the main thread in any mode.
                // This means that we don't want to block here if this stupid thing isn't enabled.
                if (this.configuration.IsResumeGameAfterPluginLoad)
                {
                    Log.Verbose("Waiting for all plugins to load before resuming game");
                    loadAllPlugins.Wait();
                }

                Log.Information("[T3] PML OK!");
            }

            _ = Task.Run(Troubleshooting.LogTroubleshooting);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Plugin load failed");
        }
    }
    
    /// <summary>
    /// Class representing progress of an update operation.
    /// </summary>
    public record PluginUpdateProgress(int PluginsProcessed, int TotalPlugins, IPluginManifest CurrentPluginManifest);
    
    /// <summary>
    /// Simple class that tracks the internal names and public names of plugins that we are planning to load at startup,
    /// and are still actively loading.
    /// </summary>
    public class StartupLoadTracker
    {
        private readonly Dictionary<string, string> internalToPublic = new();
        private readonly ConcurrentBag<string> allInternalNames = new();
        private readonly ConcurrentBag<string> finishedInternalNames = new();
        
        /// <summary>
        /// Gets a value indicating the total load progress.
        /// </summary>
        public float Progress => (float)this.finishedInternalNames.Count / this.allInternalNames.Count;
        
        /// <summary>
        /// Calculate a set of internal names that are still pending.
        /// </summary>
        /// <returns>Set of pending InternalNames.</returns>
        public IReadOnlySet<string> GetPendingInternalNames()
        {
            var pending = new HashSet<string>(this.allInternalNames);
            pending.ExceptWith(this.finishedInternalNames);
            return pending;
        }
        
        /// <summary>
        /// Track a new plugin.
        /// </summary>
        /// <param name="internalName">The plugin's internal name.</param>
        /// <param name="publicName">The plugin's public name.</param>
        public void Add(string internalName, string publicName)
        {
            this.internalToPublic[internalName] = publicName;
            this.allInternalNames.Add(internalName);
        }
    
        /// <summary>
        /// Mark a plugin as finished loading.
        /// </summary>
        /// <param name="internalName">The internal name of the plugin.</param>
        public void Finish(string internalName)
        {
            this.finishedInternalNames.Add(internalName);
        }
        
        /// <summary>
        /// Get the public name for a given internal name.
        /// </summary>
        /// <param name="internalName">The internal name to look up.</param>
        /// <returns>The public name.</returns>
        public string? GetPublicName(string internalName)
        {
            return this.internalToPublic.TryGetValue(internalName, out var publicName) ? publicName : null;
        }
    }

    private static class Locs
    {
        public static string DalamudPluginUpdateSuccessful(string name, Version version) => Loc.Localize("DalamudPluginUpdateSuccessful", "    》 {0} updated to v{1}.").Format(name, version);

        public static string DalamudPluginUpdateFailed(string name, Version version, string why) => Loc.Localize("DalamudPluginUpdateFailed", "    》 {0} update to v{1} failed ({2}).").Format(name, version, why);
    }
}

// NET8 CHORE
/*
/// <summary>
/// Class responsible for loading and unloading plugins.
/// This contains the assembly patching functionality to resolve assembly locations.
/// </summary>
internal partial class PluginManager
{
    /// <summary>
    /// A mapping of plugin assembly name to patch data. Used to fill in missing data due to loading
    /// plugins via byte[].
    /// </summary>
    internal static readonly ConcurrentDictionary<string, PluginPatchData> PluginLocations = new();

    private MonoMod.RuntimeDetour.Hook? assemblyLocationMonoHook;
    private MonoMod.RuntimeDetour.Hook? assemblyCodeBaseMonoHook;

    /// <summary>
    /// Patch method for internal class RuntimeAssembly.Location, also known as Assembly.Location.
    /// This patch facilitates resolving the assembly location for plugins that are loaded via byte[].
    /// It should never be called manually.
    /// </summary>
    /// <param name="orig">A delegate that acts as the original method.</param>
    /// <param name="self">The equivalent of `this`.</param>
    /// <returns>The plugin location, or the result from the original method.</returns>
    private static string AssemblyLocationPatch(Func<Assembly, string?> orig, Assembly self)
    {
        var result = orig(self);

        if (string.IsNullOrEmpty(result))
        {
            foreach (var assemblyName in GetStackFrameAssemblyNames())
            {
                if (PluginLocations.TryGetValue(assemblyName, out var data))
                {
                    result = data.Location;
                    break;
                }
            }
        }

        result ??= string.Empty;

        Log.Verbose($"Assembly.Location // {self.FullName} // {result}");
        return result;
    }

    /// <summary>
    /// Patch method for internal class RuntimeAssembly.CodeBase, also known as Assembly.CodeBase.
    /// This patch facilitates resolving the assembly location for plugins that are loaded via byte[].
    /// It should never be called manually.
    /// </summary>
    /// <param name="orig">A delegate that acts as the original method.</param>
    /// <param name="self">The equivalent of `this`.</param>
    /// <returns>The plugin code base, or the result from the original method.</returns>
    private static string AssemblyCodeBasePatch(Func<Assembly, string?> orig, Assembly self)
    {
        var result = orig(self);

        if (string.IsNullOrEmpty(result))
        {
            foreach (var assemblyName in GetStackFrameAssemblyNames())
            {
                if (PluginLocations.TryGetValue(assemblyName, out var data))
                {
                    result = data.CodeBase;
                    break;
                }
            }
        }

        result ??= string.Empty;

        Log.Verbose($"Assembly.CodeBase // {self.FullName} // {result}");
        return result;
    }

    private static IEnumerable<string> GetStackFrameAssemblyNames()
    {
        var stackTrace = new StackTrace();
        var stackFrames = stackTrace.GetFrames();

        foreach (var stackFrame in stackFrames)
        {
            var methodBase = stackFrame.GetMethod();
            if (methodBase == null)
                continue;

            yield return methodBase.Module.Assembly.FullName!;
        }
    }

    private void ApplyPatches()
    {
        var targetType = typeof(PluginManager).Assembly.GetType();

        var locationTarget = targetType.GetProperty(nameof(Assembly.Location))!.GetGetMethod();
        var locationPatch = typeof(PluginManager).GetMethod(nameof(AssemblyLocationPatch), BindingFlags.NonPublic | BindingFlags.Static);
        this.assemblyLocationMonoHook = new MonoMod.RuntimeDetour.Hook(locationTarget, locationPatch);

#pragma warning disable CS0618
#pragma warning disable SYSLIB0012
        var codebaseTarget = targetType.GetProperty(nameof(Assembly.CodeBase))?.GetGetMethod();
#pragma warning restore SYSLIB0012
#pragma warning restore CS0618
        var codebasePatch = typeof(PluginManager).GetMethod(nameof(AssemblyCodeBasePatch), BindingFlags.NonPublic | BindingFlags.Static);
        this.assemblyCodeBaseMonoHook = new MonoMod.RuntimeDetour.Hook(codebaseTarget, codebasePatch);
    }
}
*/
