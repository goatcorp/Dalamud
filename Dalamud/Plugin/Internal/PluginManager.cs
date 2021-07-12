using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Configuration;
using Dalamud.Game.Text;
using Dalamud.Plugin.Internal.Exceptions;
using Dalamud.Plugin.Internal.Types;
using HarmonyLib;
using Newtonsoft.Json;

namespace Dalamud.Plugin.Internal
{
    /// <summary>
    /// Class responsible for loading and unloading plugins.
    /// </summary>
    internal partial class PluginManager : IDisposable
    {
        /// <summary>
        /// The current Dalamud API level, used to handle breaking changes. Only plugins with this level will be loaded.
        /// </summary>
        public const int DalamudApiLevel = 4;

        private static readonly ModuleLog Log = new("PLUGINM");

        private readonly Dalamud dalamud;
        private readonly DirectoryInfo pluginDirectory;
        private readonly DirectoryInfo devPluginDirectory;
        private readonly BannedPlugin[] bannedPlugins;

        private readonly List<LocalPlugin> installedPlugins = new();
        private List<RemotePluginManifest> availablePlugins = new();
        private List<AvailablePluginUpdate> updatablePlugins = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginManager"/> class.
        /// </summary>
        /// <param name="dalamud">The <see cref="Dalamud"/> instance to load plugins with.</param>
        public PluginManager(Dalamud dalamud)
        {
            this.dalamud = dalamud;
            this.pluginDirectory = new DirectoryInfo(dalamud.StartInfo.PluginDirectory);
            this.devPluginDirectory = new DirectoryInfo(dalamud.StartInfo.DefaultPluginDirectory);

            if (!this.pluginDirectory.Exists)
                this.pluginDirectory.Create();

            if (!this.devPluginDirectory.Exists)
                this.devPluginDirectory.Create();

            this.PluginConfigs = new PluginConfigurations(Path.Combine(Path.GetDirectoryName(dalamud.StartInfo.ConfigurationPath), "pluginConfigs"));

            var bannedPluginsJson = File.ReadAllText(Path.Combine(this.dalamud.StartInfo.AssetDirectory, "UIRes", "bannedplugin.json"));
            this.bannedPlugins = JsonConvert.DeserializeObject<BannedPlugin[]>(bannedPluginsJson);

            this.Repos.Add(PluginRepository.MainRepo);
            this.Repos.AddRange(this.dalamud.Configuration.ThirdRepoList
                .Select(repo => new PluginRepository(repo.Url, repo.IsEnabled)));

            this.ApplyPatches();
        }

        /// <summary>
        /// An event that fires when the installed plugins have changed.
        /// </summary>
        public event Action OnInstalledPluginsChanged;

        /// <summary>
        /// An event that fires when the available plugins have changed.
        /// </summary>
        public event Action OnAvailablePluginsChanged;

        /// <summary>
        /// Gets a list of all loaded plugins.
        /// </summary>
        public ImmutableList<LocalPlugin> InstalledPlugins { get; private set; } = ImmutableList.Create<LocalPlugin>();

        /// <summary>
        /// Gets a list of all available plugins.
        /// </summary>
        public ImmutableList<RemotePluginManifest> AvailablePlugins { get; private set; } = ImmutableList.Create<RemotePluginManifest>();

        /// <summary>
        /// Gets a list of all plugins with an available update.
        /// </summary>
        public ImmutableList<AvailablePluginUpdate> UpdatablePlugins { get; private set; } = ImmutableList.Create<AvailablePluginUpdate>();

        /// <summary>
        /// Gets a list of all plugin repositories. The main repo should always be first.
        /// </summary>
        public List<PluginRepository> Repos { get; } = new();

        /// <summary>
        /// Gets a value indicating whether plugins are not still loading from boot.
        /// </summary>
        public bool PluginsReady { get; private set; } = false;

        /// <summary>
        /// Gets a value indicating whether all added repos are not in progress.
        /// </summary>
        public bool ReposReady => this.Repos.All(repo => repo.State != PluginRepositoryState.InProgress);

        /// <summary>
        /// Gets a list of all IPC subscriptions.
        /// </summary>
        public List<IpcSubscription> IpcSubscriptions { get; } = new();

        /// <summary>
        /// Gets the <see cref="PluginConfigurations"/> object used when initializing plugins.
        /// </summary>
        public PluginConfigurations PluginConfigs { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var plugin in this.installedPlugins.ToArray())
            {
                try
                {
                    plugin.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error disposing {plugin.Name}");
                }
            }
        }

        /// <summary>
        /// Load all plugins, sorted by priority. Any plugins with no explicit definition file or a negative priority
        /// are loaded asynchronously. Should only be called during Dalamud startup.
        /// </summary>
        public void LoadAllPlugins()
        {
            var pluginDefs = new List<PluginDef>();
            var devPluginDefs = new List<PluginDef>();

            if (!this.pluginDirectory.Exists)
                this.pluginDirectory.Create();

            if (!this.devPluginDirectory.Exists)
                this.devPluginDirectory.Create();

            // Add installed plugins. These are expected to be in a specific format so we can look for exactly that.
            foreach (var pluginDir in this.pluginDirectory.GetDirectories())
            {
                foreach (var versionDir in pluginDir.GetDirectories())
                {
                    var dllFile = new FileInfo(Path.Combine(versionDir.FullName, $"{pluginDir.Name}.dll"));
                    var manifestFile = LocalPluginManifest.GetManifestFile(dllFile);

                    if (!manifestFile.Exists)
                        continue;

                    var manifest = LocalPluginManifest.Load(manifestFile);

                    pluginDefs.Add(new(dllFile, manifest, false));
                }
            }

            // devPlugins are more freeform. Look for any dll and hope to get lucky.
            var devDllFiles = this.devPluginDirectory.GetFiles("*.dll", SearchOption.AllDirectories);

            foreach (var dllFile in devDllFiles)
            {
                // Manifests are not required for devPlugins. the Plugin type will handle any null manifests.
                var manifestFile = LocalPluginManifest.GetManifestFile(dllFile);
                var manifest = manifestFile.Exists ? LocalPluginManifest.Load(manifestFile) : null;
                devPluginDefs.Add(new(dllFile, manifest, true));
            }

            // Sort for load order - unloaded definitions have default priority of 0
            pluginDefs.Sort(PluginDef.Sorter);
            devPluginDefs.Sort(PluginDef.Sorter);

            // Dev plugins should load first.
            pluginDefs.InsertRange(0, devPluginDefs);

            void LoadPlugins(IEnumerable<PluginDef> pluginDefs)
            {
                foreach (var pluginDef in pluginDefs)
                {
                    try
                    {
                        this.LoadPlugin(pluginDef.DllFile, pluginDef.Manifest, PluginLoadReason.Boot, pluginDef.IsDev, isBoot: true);
                    }
                    catch (InvalidPluginException)
                    {
                        // Not a plugin
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "During boot plugin load, an unexpected error occurred");
                    }
                }
            }

            // Load sync plugins
            var syncPlugins = pluginDefs.Where(def => def.Manifest?.LoadPriority > 0);
            LoadPlugins(syncPlugins);

            var asyncPlugins = pluginDefs.Where(def => def.Manifest == null || def.Manifest.LoadPriority <= 0);
            Task.Run(() => LoadPlugins(asyncPlugins))
                .ContinueWith(task => this.PluginsReady = true)
                .ContinueWith(task => this.NotifyInstalledPluginsChanged());
        }

        /// <summary>
        /// Reload all loaded plugins.
        /// </summary>
        public void ReloadAllPlugins()
        {
            var aggregate = new List<Exception>();

            for (var i = 0; i < this.installedPlugins.Count; i++)
            {
                var plugin = this.installedPlugins[i];

                if (plugin.IsLoaded)
                {
                    try
                    {
                        plugin.Reload();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error during reload all");

                        aggregate.Add(ex);
                    }
                }
            }

            if (aggregate.Any())
            {
                throw new AggregateException(aggregate);
            }
        }

        /// <summary>
        /// Reload the PluginMaster for each repo, filter, and event that the list has updated.
        /// </summary>
        public void ReloadPluginMasters()
        {
            Task.WhenAll(this.Repos.Select(repo => repo.ReloadPluginMasterAsync()))
                .ContinueWith(task => this.RefilterPluginMasters())
                .Wait();
        }

        /// <summary>
        /// Apply visibility and eligibility filters to the available plugins, then event that the list has updated.
        /// </summary>
        public void RefilterPluginMasters()
        {
            this.availablePlugins = this.dalamud.PluginManager.Repos
                .SelectMany(repo => repo.PluginMaster)
                .Where(this.IsManifestEligible)
                .Where(this.IsManifestVisible)
                .ToList();

            this.NotifyAvailablePluginsChanged();
        }

        /// <summary>
        /// Scan the devPlugins folder for new DLL files that are not already loaded into the manager. They are not loaded,
        /// only shown as disabled in the installed plugins window. This is a modified version of LoadAllPlugins that works
        /// a little differently.
        /// </summary>
        public void ScanDevPlugins()
        {
            if (!this.devPluginDirectory.Exists)
                this.devPluginDirectory.Create();

            // devPlugins are more freeform. Look for any dll and hope to get lucky.
            var devDllFiles = this.devPluginDirectory.GetFiles("*.dll", SearchOption.AllDirectories);

            var listChanged = false;

            foreach (var dllFile in devDllFiles)
            {
                // This file is already known to us
                if (this.InstalledPlugins.Any(lp => lp.DllFile.FullName == dllFile.FullName))
                    continue;

                // Manifests are not required for devPlugins. the Plugin type will handle any null manifests.
                var manifestFile = LocalPluginManifest.GetManifestFile(dllFile);
                var manifest = manifestFile.Exists ? LocalPluginManifest.Load(manifestFile) : null;

                try
                {
                    // Add them to the list and let the user decide, nothing is auto-loaded.
                    this.LoadPlugin(dllFile, manifest, PluginLoadReason.Installer, isDev: true, doNotLoad: true);
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
                this.NotifyInstalledPluginsChanged();
        }

        /// <summary>
        /// Install a plugin from a repository and load it.
        /// </summary>
        /// <param name="repoManifest">The plugin definition.</param>
        /// <param name="useTesting">If the testing version should be used.</param>
        /// <param name="reason">The reason this plugin was loaded.</param>
        public void InstallPlugin(RemotePluginManifest repoManifest, bool useTesting, PluginLoadReason reason)
        {
            Log.Debug($"Installing plugin {repoManifest.Name} (testing={useTesting})");

            var downloadUrl = useTesting ? repoManifest.DownloadLinkTesting : repoManifest.DownloadLinkInstall;
            var version = useTesting ? repoManifest.TestingAssemblyVersion : repoManifest.AssemblyVersion;

            var outputDir = new DirectoryInfo(Path.Combine(this.pluginDirectory.FullName, repoManifest.InternalName, version.ToString()));

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

            using var client = new WebClient();

            var tempZip = new FileInfo(Path.GetTempFileName());

            try
            {
                Log.Debug($"Downloading plugin to {tempZip} from {downloadUrl}");
                client.DownloadFile(downloadUrl, tempZip.FullName);
            }
            catch (WebException ex)
            {
                Log.Error(ex, $"Download of plugin {repoManifest.Name} failed unexpectedly.");
                throw;
            }

            Log.Debug($"Extracting to {outputDir}");
            // This throws an error, even with overwrite=false
            // ZipFile.ExtractToDirectory(tempZip.FullName, outputDir.FullName, false);
            using (var archive = ZipFile.OpenRead(tempZip.FullName))
            {
                foreach (var zipFile in archive.Entries)
                {
                    var completeFileName = Path.GetFullPath(Path.Combine(outputDir.FullName, zipFile.FullName));

                    if (!completeFileName.StartsWith(outputDir.FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new IOException("Trying to extract file outside of destination directory. See this link for more info: https://snyk.io/research/zip-slip-vulnerability");
                    }

                    if (zipFile.Name == string.Empty)
                    {
                        // Assuming Empty for Directory
                        Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                        continue;
                    }

                    try
                    {
                        zipFile.ExtractToFile(completeFileName, true);
                    }
                    catch (Exception ex)
                    {
                        Log.Information($"Could not overwrite {zipFile.Name}: {ex.Message}");
                    }
                }
            }

            tempZip.Delete();

            var dllFile = LocalPluginManifest.GetPluginFile(outputDir, repoManifest);
            var manifestFile = LocalPluginManifest.GetManifestFile(dllFile);

            // Reload as a local manifest, add some attributes, and save again.
            var manifest = LocalPluginManifest.Load(manifestFile);

            if (useTesting)
            {
                manifest.Testing = true;
            }

            if (repoManifest.SourceRepo.IsThirdParty)
            {
                // Only document the url if it came from a third party repo.
                manifest.InstalledFromUrl = repoManifest.SourceRepo.PluginMasterUrl;
            }

            manifest.Save(manifestFile);

            Log.Information($"Installed plugin {manifest.Name} (testing={useTesting})");

            this.LoadPlugin(dllFile, manifest, reason);

            this.NotifyInstalledPluginsChanged();
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
        public void LoadPlugin(FileInfo dllFile, LocalPluginManifest manifest, PluginLoadReason reason, bool isDev = false, bool isBoot = false, bool doNotLoad = false)
        {
            var name = manifest?.Name ?? dllFile.Name;
            var loadPlugin = !doNotLoad;

            LocalPlugin plugin;

            if (isDev)
            {
                Log.Information($"Loading dev plugin {name}");
                var devPlugin = new LocalDevPlugin(this.dalamud, dllFile, manifest);
                loadPlugin &= !isBoot || devPlugin.StartOnBoot;

                plugin = devPlugin;
            }
            else
            {
                Log.Information($"Loading plugin {name}");
                plugin = new LocalPlugin(this.dalamud, dllFile, manifest);
            }

            if (loadPlugin)
            {
                try
                {
                    if (plugin.IsDisabled)
                        plugin.Enable();

                    plugin.Load(reason);
                }
                catch (InvalidPluginException)
                {
                    PluginLocations.Remove(plugin.AssemblyName.FullName);
                    throw;
                }
                catch (Exception ex)
                {
                    // Dev plugins always get added to the list so they can be fiddled with in the UI
                    if (plugin.IsDev)
                    {
                        Log.Information(ex, $"Dev plugin failed to load, adding anyways:  {dllFile.Name}");
                    }
                    else
                    {
                        PluginLocations.Remove(plugin.AssemblyName.FullName);
                        throw;
                    }
                }
            }

            this.installedPlugins.Add(plugin);
        }

        /// <summary>
        /// Remove a plugin.
        /// </summary>
        /// <param name="plugin">Plugin to remove.</param>
        public void RemovePlugin(LocalPlugin plugin)
        {
            if (plugin.State != PluginState.Unloaded)
                throw new InvalidPluginOperationException($"Unable to remove {plugin.Name}, not unloaded");

            this.installedPlugins.Remove(plugin);
            PluginLocations.Remove(plugin.AssemblyName.FullName);

            this.NotifyInstalledPluginsChanged();
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
                                dir.Delete();
                            }

                            return version;
                        })
                        .Where(version => version != null)
                        .ToArray();

                    if (versionDirs.Length == 0)
                    {
                        Log.Information($"No versions: cleaning up {pluginDir.FullName}");
                        pluginDir.Delete(true);
                        continue;
                    }
                    else
                    {
                        foreach (var versionDir in versionDirs)
                        {
                            try
                            {
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

                                var manifest = LocalPluginManifest.Load(manifestFile);
                                if (manifest.Disabled)
                                {
                                    Log.Information($"Disabled: cleaning up {versionDir.FullName}");
                                    versionDir.Delete(true);
                                    continue;
                                }

                                if (manifest.DalamudApiLevel < DalamudApiLevel)
                                {
                                    Log.Information($"Lower API: cleaning up {versionDir.FullName}");
                                    versionDir.Delete(true);
                                    continue;
                                }

                                if (manifest.ApplicableVersion < this.dalamud.StartInfo.GameVersion)
                                {
                                    Log.Information($"Inapplicable version: cleaning up {versionDir.FullName}");
                                    versionDir.Delete(true);
                                    continue;
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
        /// Update all plugins.
        /// </summary>
        /// <param name="dryRun">Perform a dry run, don't install anything.</param>
        /// <returns>Success or failure and a list of updated plugin metadata.</returns>
        public List<PluginUpdateStatus> UpdatePlugins(bool dryRun = false)
        {
            Log.Information("Starting plugin update");

            var listChanged = false;

            var updatedList = new List<PluginUpdateStatus>();

            // Prevent collection was modified errors
            for (var i = 0; i < this.updatablePlugins.Count; i++)
            {
                var metadata = this.updatablePlugins[i];

                var plugin = metadata.InstalledPlugin;

                // Can't update that!
                if (plugin is LocalDevPlugin)
                    continue;

                var updateStatus = new PluginUpdateStatus()
                {
                    InternalName = plugin.Manifest.InternalName,
                    Name = plugin.Manifest.Name,
                    Version = metadata.UseTesting
                        ? metadata.UpdateManifest.TestingAssemblyVersion
                        : metadata.UpdateManifest.AssemblyVersion,
                };

                if (dryRun)
                {
                    updateStatus.WasUpdated = true;
                    updatedList.Add(updateStatus);
                }
                else
                {
                    // Unload if loaded
                    if (plugin.State == PluginState.Loaded || plugin.State == PluginState.LoadError)
                    {
                        try
                        {
                            plugin.Unload();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error during unload (update)");
                            continue;
                        }
                    }

                    try
                    {
                        plugin.Disable();
                        this.installedPlugins.Remove(plugin);
                        listChanged = true;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error during disable (update)");
                        continue;
                    }

                    try
                    {
                        this.InstallPlugin(metadata.UpdateManifest, metadata.UseTesting, PluginLoadReason.Update);
                        listChanged = true;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error during install (update)");
                        continue;
                    }
                }
            }

            if (listChanged)
                this.NotifyInstalledPluginsChanged();

            Log.Information("Plugin update OK.");

            return updatedList;
        }

        /// <summary>
        /// Print to chat any plugin updates and whether they were successful.
        /// </summary>
        /// <param name="updateMetadata">The list of updated plugin metadata.</param>
        /// <param name="header">The header text to send to chat prior to any update info.</param>
        public void PrintUpdatedPlugins(List<PluginUpdateStatus> updateMetadata, string header)
        {
            if (updateMetadata != null && updateMetadata.Count > 0)
            {
                this.dalamud.Framework.Gui.Chat.Print(header);

                foreach (var metadata in updateMetadata)
                {
                    if (metadata.WasUpdated)
                    {
                        this.dalamud.Framework.Gui.Chat.Print(Locs.DalamudPluginUpdateSuccessful(metadata.Name, metadata.Version));
                    }
                    else
                    {
                        this.dalamud.Framework.Gui.Chat.PrintChat(new XivChatEntry
                        {
                            MessageBytes = Encoding.UTF8.GetBytes(Locs.DalamudPluginUpdateFailed(metadata.Name, metadata.Version)),
                            Type = XivChatType.Urgent,
                        });
                    }
                }
            }
        }

        /// <summary>
        /// For a given manifest, determine if the testing version should be used over the normal version.
        /// The higher of the two versions is calculated after checking other settings.
        /// </summary>
        /// <param name="manifest">Manifest to check.</param>
        /// <returns>A value indicating whether testing should be used.</returns>
        public bool UseTesting(PluginManifest manifest)
        {
            if (!this.dalamud.Configuration.DoPluginTest)
                return false;

            if (manifest.IsTestingExclusive)
                return true;

            var av = manifest.AssemblyVersion;
            var tv = manifest.TestingAssemblyVersion;
            var hasAv = av != null;
            var hasTv = tv != null;

            if (hasAv && hasTv)
            {
                return tv > av;
            }
            else
            {
                return hasTv;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the given repo manifest should be visible to the user.
        /// </summary>
        /// <param name="manifest">Repo manifest.</param>
        /// <returns>If the manifest is visible.</returns>
        public bool IsManifestVisible(RemotePluginManifest manifest)
        {
            // Hidden by user
            if (this.dalamud.Configuration.HiddenPluginInternalName.Contains(manifest.InternalName))
                return false;

            // Hidden by manifest
            if (manifest.IsHide)
                return false;

            return true;
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
            if (manifest.IsTestingExclusive && !this.dalamud.Configuration.DoPluginTest)
                return false;

            // Applicable version
            if (manifest.ApplicableVersion < this.dalamud.StartInfo.GameVersion)
                return false;

            // API level
            if (manifest.DalamudApiLevel < DalamudApiLevel && !this.dalamud.Configuration.LoadAllApiLevels)
                return false;

            // Banned
            if (this.IsManifestBanned(manifest))
                return false;

            return true;
        }

        private bool IsManifestBanned(PluginManifest manifest)
        {
            return this.bannedPlugins.Any(ban => ban.Name == manifest.InternalName && ban.AssemblyVersion == manifest.AssemblyVersion);
        }

        private void DetectAvailablePluginUpdates()
        {
            var updatablePlugins = new List<AvailablePluginUpdate>();

            for (var i = 0; i < this.installedPlugins.Count; i++)
            {
                var plugin = this.installedPlugins[i];

                var installedVersion = plugin.IsTesting
                    ? plugin.Manifest.TestingAssemblyVersion
                    : plugin.Manifest.AssemblyVersion;

                var updates = this.availablePlugins
                    .Where(remoteManifest => plugin.Manifest.InternalName == remoteManifest.InternalName)
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
                    updatablePlugins.Add(new(plugin, update.remoteManifest, update.useTesting));
                }
            }

            this.updatablePlugins = updatablePlugins;
        }

        private void NotifyAvailablePluginsChanged()
        {
            this.DetectAvailablePluginUpdates();

            try
            {
                this.AvailablePlugins = ImmutableList.CreateRange(this.availablePlugins);
                this.UpdatablePlugins = ImmutableList.CreateRange(this.updatablePlugins);
                this.OnAvailablePluginsChanged.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error notifying {nameof(this.OnAvailablePluginsChanged)}");
            }
        }

        private void NotifyInstalledPluginsChanged()
        {
            this.DetectAvailablePluginUpdates();

            try
            {
                this.InstalledPlugins = ImmutableList.CreateRange(this.installedPlugins);
                this.UpdatablePlugins = ImmutableList.CreateRange(this.updatablePlugins);
                this.OnInstalledPluginsChanged.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error notifying {nameof(this.OnInstalledPluginsChanged)}");
            }
        }

        private struct BannedPlugin
        {
            [JsonProperty]
            public string Name { get; private set; }

            [JsonProperty]
            public Version AssemblyVersion { get; private set; }
        }

        private struct PluginDef
        {
            public PluginDef(FileInfo dllFile, LocalPluginManifest manifest, bool isDev)
            {
                this.DllFile = dllFile;
                this.Manifest = manifest;
                this.IsDev = isDev;
            }

            public FileInfo DllFile { get; init; }

            public LocalPluginManifest Manifest { get; init; }

            public bool IsDev { get; init; }

            public static int Sorter(PluginDef def1, PluginDef def2)
            {
                var prio1 = def1.Manifest?.LoadPriority ?? 0;
                var prio2 = def2.Manifest?.LoadPriority ?? 0;
                return prio2.CompareTo(prio1);
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
        internal static readonly Dictionary<string, PluginPatchData> PluginLocations = new();

        /// <summary>
        /// Patch method for internal class RuntimeAssembly.Location, also known as Assembly.Location.
        /// This patch facilitates resolving the assembly location for plugins that are loaded via byte[].
        /// It should never be called manually.
        /// </summary>
        /// <param name="__instance">The equivalent of `this`.</param>
        /// <param name="__result">The result from the original method.</param>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Enforced naming for special injected parameters")]
        private static void AssemblyLocationPatch(Assembly __instance, ref string __result)
        {
            // Assembly.GetExecutingAssembly can return this.
            // Check for it as a special case and find the plugin.
            if (__result.EndsWith("System.Private.CoreLib.dll", StringComparison.InvariantCultureIgnoreCase))
            {
                foreach (var assemblyName in GetStackFrameAssemblyNames())
                {
                    if (PluginLocations.TryGetValue(assemblyName, out var data))
                    {
                        __result = data.Location;
                        return;
                    }
                }
            }
            else if (string.IsNullOrEmpty(__result))
            {
                if (PluginLocations.TryGetValue(__instance.FullName, out var data))
                {
                    __result = data.Location;
                }
            }
        }

        /// <summary>
        /// Patch method for internal class RuntimeAssembly.CodeBase, also known as Assembly.CodeBase.
        /// This patch facilitates resolving the assembly location for plugins that are loaded via byte[].
        /// It should never be called manually.
        /// </summary>
        /// <param name="__instance">The equivalent of `this`.</param>
        /// <param name="__result">The result from the original method.</param>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Enforced naming for special injected parameters")]
        private static void AssemblyCodeBasePatch(Assembly __instance, ref string __result)
        {
            // Assembly.GetExecutingAssembly can return this.
            // Check for it as a special case and find the plugin.
            if (__result.EndsWith("System.Private.CoreLib.dll"))
            {
                foreach (var assemblyName in GetStackFrameAssemblyNames())
                {
                    if (PluginLocations.TryGetValue(assemblyName, out var data))
                    {
                        __result = data.Location;
                        return;
                    }
                }
            }
            else if (string.IsNullOrEmpty(__result))
            {
                if (PluginLocations.TryGetValue(__instance.FullName, out var data))
                {
                    __result = data.Location;
                }
            }
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

                yield return methodBase.Module.Assembly.FullName;
            }
        }

        private void ApplyPatches()
        {
            var harmony = new Harmony("goatcorp.dalamud.pluginmanager");

            var targetType = typeof(PluginManager).Assembly.GetType();

            var locationTarget = AccessTools.PropertyGetter(targetType, nameof(Assembly.Location));
            var locationPatch = AccessTools.Method(typeof(PluginManager), nameof(PluginManager.AssemblyLocationPatch));
            harmony.Patch(locationTarget, postfix: new(locationPatch));

#pragma warning disable SYSLIB0012 // Type or member is obsolete
            var codebaseTarget = AccessTools.PropertyGetter(targetType, nameof(Assembly.CodeBase));
            var codebasePatch = AccessTools.Method(typeof(PluginManager), nameof(PluginManager.AssemblyCodeBasePatch));
            harmony.Patch(codebaseTarget, postfix: new(codebasePatch));
#pragma warning restore SYSLIB0012 // Type or member is obsolete
        }

        internal record PluginPatchData
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PluginPatchData"/> class.
            /// </summary>
            /// <param name="dllFile">DLL file being loaded.</param>
            public PluginPatchData(FileInfo dllFile)
            {
                this.Location = dllFile.FullName;
                this.CodeBase = new Uri(dllFile.FullName).AbsoluteUri;
            }

            /// <summary>
            /// Gets simulated Assembly.Location output.
            /// </summary>
            public string Location { get; }

            /// <summary>
            /// Gets simulated Assembly.CodeBase output.
            /// </summary>
            public string CodeBase { get; }
        }
    }
}
