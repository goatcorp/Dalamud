using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Configuration;
using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Internal;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Networking.Http;
using Dalamud.Plugin.Internal.Exceptions;
using Dalamud.Plugin.Internal.Profiles;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Plugin.Ipc.Internal;
using Dalamud.Utility;
using Dalamud.Utility.Timing;
using Newtonsoft.Json;

namespace Dalamud.Plugin.Internal;

/// <summary>
/// Class responsible for loading and unloading plugins.
/// NOTE: ALL plugin exposed services are marked as dependencies for PluginManager in Service{T}.
/// </summary>
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015

// DalamudTextureWrap registers textures to dispose with IM
[InherentDependency<InterfaceManager>]

// LocalPlugin uses ServiceContainer to create scopes
[InherentDependency<ServiceContainer>]

// DalamudPluginInterface hands out a reference to this, so we have to keep it around
// TODO api9: make it a service 
[InherentDependency<DataShare>]

#pragma warning restore SA1015
internal partial class PluginManager : IDisposable, IServiceType
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

    private readonly DalamudLinkPayload openInstallerWindowPluginChangelogsLink;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DalamudStartInfo startInfo = Service<DalamudStartInfo>.Get();

    [ServiceManager.ServiceDependency]
    private readonly ProfileManager profileManager = Service<ProfileManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly HappyHttpClient happyHttpClient = Service<HappyHttpClient>.Get();

    static PluginManager()
    {
        DalamudApiLevel = typeof(PluginManager).Assembly.GetName().Version!.Major;
    }

    [ServiceManager.ServiceConstructor]
    private PluginManager()
    {
        this.pluginDirectory = new DirectoryInfo(this.startInfo.PluginDirectory!);

        if (!this.pluginDirectory.Exists)
            this.pluginDirectory.Create();

        this.SafeMode = EnvironmentConfiguration.DalamudNoPlugins || this.configuration.PluginSafeMode || this.startInfo.NoLoadPlugins;

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

        this.PluginConfigs = new PluginConfigurations(Path.Combine(Path.GetDirectoryName(this.startInfo.ConfigurationPath) ?? string.Empty, "pluginConfigs"));

        var bannedPluginsJson = File.ReadAllText(Path.Combine(this.startInfo.AssetDirectory!, "UIRes", "bannedplugin.json"));
        this.bannedPlugins = JsonConvert.DeserializeObject<BannedPlugin[]>(bannedPluginsJson);
        if (this.bannedPlugins == null)
        {
            throw new InvalidDataException("Couldn't deserialize banned plugins manifest.");
        }

        this.openInstallerWindowPluginChangelogsLink = Service<ChatGui>.Get().AddChatLinkHandler("Dalamud", 1003, (_, _) =>
        {
            Service<DalamudInterface>.GetNullable()?.OpenPluginInstallerPluginChangelogs();
        });

        this.configuration.PluginTestingOptIns ??= new List<PluginTestingOptIn>();

        this.ApplyPatches();
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
    /// Gets a value indicating whether the given repo manifest should be visible to the user.
    /// </summary>
    /// <param name="manifest">Repo manifest.</param>
    /// <returns>If the manifest is visible.</returns>
    public static bool IsManifestVisible(RemotePluginManifest manifest)
    {
        var configuration = Service<DalamudConfiguration>.Get();

        // Hidden by user
        if (configuration.HiddenPluginInternalName.Contains(manifest.InternalName))
            return false;

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
            return tv > av;
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
    {
        var chatGui = Service<ChatGui>.Get();

        if (updateMetadata is { Count: > 0 })
        {
            chatGui.Print(new XivChatEntry
            {
                Message = new SeString(new List<Payload>()
                {
                    new TextPayload(header),
                    new TextPayload("  ["),
                    new UIForegroundPayload(500),
                    this.openInstallerWindowPluginChangelogsLink,
                    new TextPayload(Loc.Localize("DalamudInstallerPluginChangelogHelp", "Open plugin changelogs")),
                    RawPayload.LinkTerminator,
                    new UIForegroundPayload(0),
                    new TextPayload("]"),
                }),
            });

            foreach (var metadata in updateMetadata)
            {
                if (metadata.WasUpdated)
                {
                    chatGui.Print(Locs.DalamudPluginUpdateSuccessful(metadata.Name, metadata.Version));
                }
                else
                {
                    chatGui.Print(new XivChatEntry
                    {
                        Message = Locs.DalamudPluginUpdateFailed(metadata.Name, metadata.Version),
                        Type = XivChatType.Urgent,
                    });
                }
            }
        }
    }

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
    public void Dispose()
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
                plugin.ExplicitDisposeIgnoreExceptions($"Error disposing {plugin.Name}", Log);
        }

        this.assemblyLocationMonoHook?.Dispose();
        this.assemblyCodeBaseMonoHook?.Dispose();
    }

    /// <summary>
    /// Set the list of repositories to use and downloads their contents.
    /// Should be called when the Settings window has been updated or at instantiation.
    /// </summary>
    /// <param name="notify">Whether the available plugins changed event should be sent after.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SetPluginReposFromConfigAsync(bool notify)
    {
        var repos = new List<PluginRepository>() { PluginRepository.MainRepo };
        repos.AddRange(this.configuration.ThirdRepoList
                           .Where(repo => repo.IsEnabled)
                           .Select(repo => new PluginRepository(repo.Url, repo.IsEnabled)));

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
                    var manifestFile = LocalPluginManifest.GetManifestFile(dllFile);

                    if (!manifestFile.Exists)
                        continue;

                    var manifest = LocalPluginManifest.Load(manifestFile);

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
                // Manifests are not required for devPlugins. the Plugin type will handle any null manifests.
                var manifestFile = LocalPluginManifest.GetManifestFile(dllFile);
                var manifest = manifestFile.Exists ? LocalPluginManifest.Load(manifestFile) : null;

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

            // Manifests are not required for devPlugins. the Plugin type will handle any null manifests.
            var manifestFile = LocalPluginManifest.GetManifestFile(dllFile);
            var manifest = manifestFile.Exists ? LocalPluginManifest.Load(manifestFile) : null;

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
                Log.Error(ex, $"During devPlugin scan, an unexpected error occurred");
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
    public async Task<LocalPlugin> InstallPluginAsync(RemotePluginManifest repoManifest, bool useTesting, PluginLoadReason reason, Guid? inheritedWorkingPluginId = null)
    {
        Log.Debug($"Installing plugin {repoManifest.Name} (testing={useTesting})");

        // Ensure that we have a testing opt-in for this plugin if we are installing a testing version
        if (useTesting && this.configuration.PluginTestingOptIns!.All(x => x.InternalName != repoManifest.InternalName))
        {
            // TODO: this isn't safe
            this.configuration.PluginTestingOptIns.Add(new PluginTestingOptIn(repoManifest.InternalName));
            this.configuration.QueueSave();
        }

        var downloadUrl = useTesting ? repoManifest.DownloadLinkTesting : repoManifest.DownloadLinkInstall;
        var version = useTesting ? repoManifest.TestingAssemblyVersion : repoManifest.AssemblyVersion;

        var response = await this.happyHttpClient.SharedHttpClient.GetAsync(downloadUrl);
        response.EnsureSuccessStatusCode();

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

        Log.Debug($"Extracting to {outputDir}");
        // This throws an error, even with overwrite=false
        // ZipFile.ExtractToDirectory(tempZip.FullName, outputDir.FullName, false);
        using (var archive = new ZipArchive(await response.Content.ReadAsStreamAsync()))
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

        PluginLocations.Remove(plugin.AssemblyName?.FullName ?? string.Empty, out _);

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
    /// <param name="ignoreDisabled">Ignore disabled plugins.</param>
    /// <param name="dryRun">Perform a dry run, don't install anything.</param>
    /// <param name="autoUpdate">If this action was performed as part of an auto-update.</param>
    /// <returns>Success or failure and a list of updated plugin metadata.</returns>
    public async Task<IEnumerable<PluginUpdateStatus>> UpdatePluginsAsync(bool ignoreDisabled, bool dryRun, bool autoUpdate = false)
    {
        Log.Information("Starting plugin update");

        var updateTasks = new List<Task<PluginUpdateStatus>>();

        // Prevent collection was modified errors
        lock (this.pluginListLock)
        {
            foreach (var plugin in this.updatablePluginsList)
            {
                // Can't update that!
                if (plugin.InstalledPlugin.IsDev)
                    continue;

                if (!plugin.InstalledPlugin.IsWantedByAnyProfile && ignoreDisabled)
                    continue;

                if (plugin.InstalledPlugin.Manifest.ScheduledForDeletion)
                    continue;

                updateTasks.Add(this.UpdateSinglePluginAsync(plugin, false, dryRun));
            }
        }

        var updatedList = await Task.WhenAll(updateTasks);

        this.NotifyinstalledPluginsListChanged();
        this.NotifyPluginsForStateChange(
            autoUpdate ? PluginListInvalidationKind.AutoUpdate : PluginListInvalidationKind.Update,
            updatedList.Select(x => x.InternalName));

        Log.Information("Plugin update OK.");

        return updatedList;
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

        var workingPluginId = metadata.InstalledPlugin.Manifest.WorkingPluginId;
        if (workingPluginId == Guid.Empty)
            throw new Exception("Existing plugin had no WorkingPluginId");

        var updateStatus = new PluginUpdateStatus
        {
            InternalName = plugin.Manifest.InternalName,
            Name = plugin.Manifest.Name,
            Version = (metadata.UseTesting
                           ? metadata.UpdateManifest.TestingAssemblyVersion
                           : metadata.UpdateManifest.AssemblyVersion)!,
            WasUpdated = true,
            HasChangelog = !metadata.UpdateManifest.Changelog.IsNullOrWhitespace(),
        };

        if (!dryRun)
        {
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
                    updateStatus.WasUpdated = false;
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
                Log.Error(ex, "Error during disable (update)");
                updateStatus.WasUpdated = false;
                return updateStatus;
            }

            // We need to handle removed DTR nodes here, as otherwise, plugins will not be able to re-add their bar entries after updates.
            var dtr = Service<DtrBar>.Get();
            dtr.HandleRemovedNodes();

            try
            {
                await this.InstallPluginAsync(metadata.UpdateManifest, metadata.UseTesting, PluginLoadReason.Update, workingPluginId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during install (update)");
                updateStatus.WasUpdated = false;
                return updateStatus;
            }
        }

        if (notify && updateStatus.WasUpdated)
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
                this.PluginConfigs.Delete(plugin.Name);
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
        if (manifest.ApplicableVersion < this.startInfo.GameVersion)
        {
            Log.Verbose($"Game version: {manifest.InternalName} - {manifest.AssemblyVersion} - {manifest.TestingAssemblyVersion}");
            return false;
        }

        // API level - we keep the API before this in the installer to show as "outdated"
        if (manifest.DalamudApiLevel < DalamudApiLevel - 1 && !this.LoadAllApiLevels)
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
    /// Load a plugin.
    /// </summary>
    /// <param name="dllFile">The <see cref="FileInfo"/> associated with the main assembly of this plugin.</param>
    /// <param name="manifest">The already loaded definition, if available.</param>
    /// <param name="reason">The reason this plugin was loaded.</param>
    /// <param name="isDev">If this plugin should support development features.</param>
    /// <param name="isBoot">If this plugin is being loaded at boot.</param>
    /// <param name="doNotLoad">Don't load the plugin, just don't do it.</param>
    /// <returns>The loaded plugin.</returns>
    private async Task<LocalPlugin> LoadPluginAsync(FileInfo dllFile, LocalPluginManifest? manifest, PluginLoadReason reason, bool isDev = false, bool isBoot = false, bool doNotLoad = false)
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
            var devPlugin = new LocalDevPlugin(dllFile, manifest);
            loadPlugin &= !isBoot;

            var probablyInternalNameForThisPurpose = manifest?.InternalName ?? dllFile.Name;

            var wantsInDefaultProfile =
                this.profileManager.DefaultProfile.WantsPlugin(probablyInternalNameForThisPurpose);
            if (wantsInDefaultProfile == null)
            {
                // We don't know about this plugin, so we don't want to do anything here.
                // The code below will take care of it and add it with the default value.
            }
            else if (wantsInDefaultProfile == false && devPlugin.StartOnBoot)
            {
                // We didn't want this plugin, and StartOnBoot is on. That means we don't want it and it should stay off until manually enabled.
                Log.Verbose("DevPlugin {Name} disabled and StartOnBoot => disable", probablyInternalNameForThisPurpose);
                await this.profileManager.DefaultProfile.AddOrUpdateAsync(probablyInternalNameForThisPurpose, false, false);
                loadPlugin = false;
            }
            else if (wantsInDefaultProfile == true && devPlugin.StartOnBoot)
            {
                // We wanted this plugin, and StartOnBoot is on. That means we actually do want it.
                Log.Verbose("DevPlugin {Name} enabled and StartOnBoot => enable", probablyInternalNameForThisPurpose);
                await this.profileManager.DefaultProfile.AddOrUpdateAsync(probablyInternalNameForThisPurpose, true, false);
                loadPlugin = !doNotLoad;
            }
            else if (wantsInDefaultProfile == true && !devPlugin.StartOnBoot)
            {
                // We wanted this plugin, but StartOnBoot is off. This means we don't want it anymore.
                Log.Verbose("DevPlugin {Name} enabled and !StartOnBoot => disable", probablyInternalNameForThisPurpose);
                await this.profileManager.DefaultProfile.AddOrUpdateAsync(probablyInternalNameForThisPurpose, false, false);
                loadPlugin = false;
            }
            else if (wantsInDefaultProfile == false && !devPlugin.StartOnBoot)
            {
                // We didn't want this plugin, and StartOnBoot is off. We don't want it.
                Log.Verbose("DevPlugin {Name} disabled and !StartOnBoot => disable", probablyInternalNameForThisPurpose);
                await this.profileManager.DefaultProfile.AddOrUpdateAsync(probablyInternalNameForThisPurpose, false, false);
                loadPlugin = false;
            }

            plugin = devPlugin;
        }
        else
        {
            Log.Information($"Loading plugin {name}");
            plugin = new LocalPlugin(dllFile, manifest);
        }

#pragma warning disable CS0618
        var defaultState = manifest?.Disabled != true && loadPlugin;
#pragma warning restore CS0618

        // Need to do this here, so plugins that don't load are still added to the default profile
        var wantToLoad = await this.profileManager.GetWantStateAsync(plugin.Manifest.InternalName, defaultState);

        if (loadPlugin)
        {
            try
            {
                if (wantToLoad && !plugin.IsOrphaned)
                {
                    await plugin.LoadAsync(reason);
                }
                else
                {
                    Log.Verbose($"{name} not loaded, wantToLoad:{wantToLoad} orphaned:{plugin.IsOrphaned}");
                }
            }
            catch (InvalidPluginException)
            {
                PluginLocations.Remove(plugin.AssemblyName?.FullName ?? string.Empty, out _);
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
                    PluginLocations.Remove(plugin.AssemblyName?.FullName ?? string.Empty, out _);
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

        return plugin;
    }

    private void DetectAvailablePluginUpdates()
    {
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
                                  .Where(remoteManifest => remoteManifest.DalamudApiLevel == DalamudApiLevel)
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

    private static class Locs
    {
        public static string DalamudPluginUpdateSuccessful(string name, Version version) => Loc.Localize("DalamudPluginUpdateSuccessful", "    》 {0} updated to v{1}.").Format(name, version);

        public static string DalamudPluginUpdateFailed(string name, Version version) => Loc.Localize("DalamudPluginUpdateFailed", "    》 {0} update to v{1} failed.").Format(name, version);
    }
}

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
