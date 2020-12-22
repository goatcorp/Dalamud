using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CheapLoc;
using Dalamud.Game.Chat;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Plugin
{
    internal class PluginRepository { 
        private string PluginMasterUrl => "https://goatcorp.github.io/DalamudPlugins/pluginmaster.json";

        private readonly Dalamud dalamud;
        private string pluginDirectory;
        public ReadOnlyCollection<PluginDefinition> PluginMaster;

        public enum InitializationState {
            Unknown,
            InProgress,
            Success,
            Fail,
            FailThirdRepo
        }

        public InitializationState State { get; private set; }

        public PluginRepository(Dalamud dalamud, string pluginDirectory, string gameVersion) {
            this.dalamud = dalamud;
            this.pluginDirectory = pluginDirectory;

            ReloadPluginMasterAsync();
        }

        public void ReloadPluginMasterAsync() {
            Task.Run(() => {
                this.PluginMaster = null;

                State = InitializationState.InProgress;

                var allPlugins = new List<PluginDefinition>();

                var repos = this.dalamud.Configuration.ThirdRepoList.Where(x => x.IsEnabled).Select(x => x.Url)
                                .Prepend(PluginMasterUrl).ToArray();

                try {
                    using var client = new WebClient();

                    foreach (var repo in repos) {
                        Log.Information("[PLUGINR] Fetching repo: {0}", repo);
                        
                        var data = client.DownloadString(repo);

                        var unsortedPluginMaster = JsonConvert.DeserializeObject<List<PluginDefinition>>(data);
                        var host = new Uri(repo).Host;

                        foreach (var pluginDefinition in unsortedPluginMaster) {
                            pluginDefinition.FromRepo = host;
                        }

                        allPlugins.AddRange(unsortedPluginMaster);
                    }

                    this.PluginMaster = allPlugins.AsReadOnly();
                    State = InitializationState.Success;
                }
                catch (Exception ex) {
                    Log.Error(ex, "Could not download PluginMaster");

                    State = repos.Length > 1 ? InitializationState.FailThirdRepo : InitializationState.Fail;
                }
            }).ContinueWith(t => {
                if (t.IsFaulted)
                    State = InitializationState.Fail;
            });
        }

        public bool InstallPlugin(PluginDefinition definition, bool enableAfterInstall = true, bool isUpdate = false, bool fromTesting = false) {
            try {
                using var client = new WebClient();

                var outputDir = new DirectoryInfo(Path.Combine(this.pluginDirectory, definition.InternalName, fromTesting ? definition.TestingAssemblyVersion : definition.AssemblyVersion));
                var dllFile = new FileInfo(Path.Combine(outputDir.FullName, $"{definition.InternalName}.dll"));
                var disabledFile = new FileInfo(Path.Combine(outputDir.FullName, ".disabled"));
                var testingFile = new FileInfo(Path.Combine(outputDir.FullName, ".testing"));
                var wasDisabled = disabledFile.Exists;

                if (dllFile.Exists && enableAfterInstall) {
                    if (disabledFile.Exists)
                        disabledFile.Delete();

                    return this.dalamud.PluginManager.LoadPluginFromAssembly(dllFile, false, PluginLoadReason.Installer);
                }

                if (dllFile.Exists && !enableAfterInstall) {
                    return true;
                }

                try {
                    if (outputDir.Exists)
                        outputDir.Delete(true);
                    outputDir.Create();
                } catch {
                    // ignored, since the plugin may be loaded already
                }

                var path = Path.GetTempFileName();

                var doTestingDownload = false;
                if ((Version.TryParse(definition.TestingAssemblyVersion, out var testingAssemblyVer) || definition.IsTestingExclusive)
                    && fromTesting) {
                    doTestingDownload = testingAssemblyVer > Version.Parse(definition.AssemblyVersion) || definition.IsTestingExclusive;
                }

                var url = definition.DownloadLinkInstall;
                if (doTestingDownload)
                    url = definition.DownloadLinkTesting;
                else if (isUpdate)
                    url = definition.DownloadLinkUpdate;

                Log.Information("Downloading plugin to {0} from {1} doTestingDownload:{2} isTestingExclusive:{3}", path, url, doTestingDownload, definition.IsTestingExclusive);

                client.DownloadFile(url, path);

                Log.Information("Extracting to {0}", outputDir);

                ZipFile.ExtractToDirectory(path, outputDir.FullName);

                if (wasDisabled || !enableAfterInstall) {
                    disabledFile.Create();
                    return true;
                }

                if (doTestingDownload) {
                    testingFile.Create();
                } else {
                    if (testingFile.Exists)
                        testingFile.Delete();
                }

                return this.dalamud.PluginManager.LoadPluginFromAssembly(dllFile, false, PluginLoadReason.Installer);
            }
            catch (Exception e) {
                Log.Error(e, "Plugin download failed hard.");
                return false;
            }
        }

        internal class PluginUpdateStatus {
            public string InternalName { get; set; }
            public string Name { get; set; }
            public string Version { get; set; }
            public bool WasUpdated { get; set; }
        }

        public (bool Success, List<PluginUpdateStatus> UpdatedPlugins) UpdatePlugins(bool dryRun = false) {
            Log.Information("Starting plugin update... dry:{0}", dryRun);

            var updatedList = new List<PluginUpdateStatus>();
            var hasError = false;

            try {
                var pluginsDirectory = new DirectoryInfo(this.pluginDirectory);
                foreach (var installed in pluginsDirectory.GetDirectories()) {
                    try {
                        var versions = installed.GetDirectories();

                        if (versions.Length == 0) {
                            Log.Information("Has no versions: {0}", installed.FullName);
                            continue;
                        }

                        var sortedVersions = versions.OrderBy(dirInfo => {
                            var success = Version.TryParse(dirInfo.Name, out Version version);
                            if (!success) { Log.Debug("Unparseable version: {0}", dirInfo.Name); }
                            return version;
                        });
                        var latest = sortedVersions.Last();

                        var localInfoFile = new FileInfo(Path.Combine(latest.FullName, $"{installed.Name}.json"));

                        if (!localInfoFile.Exists) {
                            Log.Information("Has no definition: {0}", localInfoFile.FullName);
                            continue;
                        }

                        var info = JsonConvert.DeserializeObject<PluginDefinition>(
                            File.ReadAllText(localInfoFile.FullName));

                        var remoteInfo = this.PluginMaster.FirstOrDefault(x => x.Name == info.Name);

                        if (remoteInfo == null) {
                            Log.Information("Is not in pluginmaster: {0}", info.Name);
                            continue;
                        }

                        if (remoteInfo.DalamudApiLevel < PluginManager.DALAMUD_API_LEVEL) {
                            Log.Information("Has not applicable API level: {0}", info.Name);
                            continue;
                        }

                        Version.TryParse(remoteInfo.AssemblyVersion, out Version remoteAssemblyVer);
                        Version.TryParse(info.AssemblyVersion, out Version localAssemblyVer);

                        var testingAvailable = false;
                        if (!string.IsNullOrEmpty(remoteInfo.TestingAssemblyVersion)) {
                            Version.TryParse(remoteInfo.TestingAssemblyVersion, out var testingAssemblyVer);
                            testingAvailable = testingAssemblyVer > localAssemblyVer && this.dalamud.Configuration.DoPluginTest;
                        }
                        
                        if (remoteAssemblyVer > localAssemblyVer || testingAvailable) {
                            Log.Information("Eligible for update: {0}", remoteInfo.InternalName);

                            // DisablePlugin() below immediately creates a .disabled file anyway, but will fail
                            // with an exception if we try to do it twice in row like this

                            if (!dryRun) {
                                var wasEnabled =
                                    this.dalamud.PluginManager.Plugins.Where(x => x.Definition != null).Any(
                                        x => x.Definition.InternalName == info.InternalName);
                                ;

                                Log.Verbose("wasEnabled: {0}", wasEnabled);

                                // Try to disable plugin if it is loaded
                                if (wasEnabled) {
                                    try {
                                        this.dalamud.PluginManager.DisablePlugin(info);
                                    }
                                    catch (Exception ex) {
                                        Log.Error(ex, "Plugin disable failed");
                                        //hasError = true;
                                    }
                                }

                                try {
                                    // Just to be safe
                                    foreach (var sortedVersion in sortedVersions) {
                                        var disabledFile =
                                            new FileInfo(Path.Combine(sortedVersion.FullName, ".disabled"));
                                        if (!disabledFile.Exists)
                                            disabledFile.Create();
                                    }
                                } catch (Exception ex) {
                                    Log.Error(ex, "Plugin disable old versions failed");
                                }

                                var installSuccess = InstallPlugin(remoteInfo, wasEnabled, true, testingAvailable);

                                if (!installSuccess) {
                                    Log.Error("InstallPlugin failed.");
                                    hasError = true;
                                }

                                updatedList.Add(new PluginUpdateStatus {
                                    InternalName = remoteInfo.InternalName,
                                    Name = remoteInfo.Name,
                                    Version = testingAvailable ? remoteInfo.TestingAssemblyVersion : remoteInfo.AssemblyVersion,
                                    WasUpdated = installSuccess
                                });
                            } else {
                                updatedList.Add(new PluginUpdateStatus {
                                    InternalName = remoteInfo.InternalName,
                                    Name = remoteInfo.Name,
                                    Version = testingAvailable ? remoteInfo.TestingAssemblyVersion : remoteInfo.AssemblyVersion,
                                    WasUpdated = true
                                });
                            }
                        } else {
                            Log.Information("Up to date: {0}", remoteInfo.InternalName);
                        }
                    } catch (Exception ex) {
                        Log.Error(ex, "Could not update plugin: {0}", installed.FullName);
                    }
                }
            }
            catch (Exception e) {
                Log.Error(e, "Plugin update failed.");
                hasError = true;
            }

            Log.Information("Plugin update OK.");

            return (!hasError, updatedList);
        }

        public void PrintUpdatedPlugins(List<PluginRepository.PluginUpdateStatus> updatedPlugins, string header) {
            if (updatedPlugins != null && updatedPlugins.Any()) {
                this.dalamud.Framework.Gui.Chat.Print(header);
                foreach (var plugin in updatedPlugins) {
                    if (plugin.WasUpdated) {
                        this.dalamud.Framework.Gui.Chat.Print(string.Format(Loc.Localize("DalamudPluginUpdateSuccessful", "    》 {0} updated to v{1}."), plugin.Name, plugin.Version));
                    } else {
                        this.dalamud.Framework.Gui.Chat.PrintChat(new XivChatEntry {
                            MessageBytes = Encoding.UTF8.GetBytes(string.Format(Loc.Localize("DalamudPluginUpdateFailed", "    》 {0} update to v{1} failed."), plugin.Name, plugin.Version)),
                            Type = XivChatType.Urgent
                        });
                    }
                }
            }
        }

        public void CleanupPlugins() {
            try {
                var pluginsDirectory = new DirectoryInfo(this.pluginDirectory);
                foreach (var installed in pluginsDirectory.GetDirectories()) {
                    var versions = installed.GetDirectories();

                    if (versions.Length == 0) {
                        Log.Information("[PLUGINR] Has no versions: {0}", installed.FullName);
                        continue;
                    }

                    var sortedVersions = versions.OrderBy(dirInfo => {
                        var success = Version.TryParse(dirInfo.Name, out Version version);
                        if (!success) { Log.Debug("Unparseable version: {0}", dirInfo.Name); }
                        return version;
                    }).ToArray();
                    for (var i = 0; i < sortedVersions.Length - 1; i++) {
                        var disabledFile = new FileInfo(Path.Combine(sortedVersions[i].FullName, ".disabled"));
                        if (disabledFile.Exists) {
                            Log.Information("[PLUGINR] Trying to delete old {0} at {1}", installed.Name, sortedVersions[i].FullName);
                            try {
                                sortedVersions[i].Delete(true);
                            }
                            catch (Exception ex) {
                                Log.Error(ex, "[PLUGINR] Could not delete old version");
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                Log.Error(ex, "[PLUGINR] Plugin cleanup failed.");
            }
        }
    }
}
