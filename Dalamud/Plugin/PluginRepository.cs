using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Plugin
{
    internal class PluginRepository
    {
        private string PluginRepoBaseUrl => "https://raw.githubusercontent.com/goatcorp/DalamudPlugins/" + (this.dalamud.Configuration.DoPluginTest ? "testing/" : "master/");

        private readonly Dalamud dalamud;
        private string pluginDirectory;
        public ReadOnlyCollection<PluginDefinition> PluginMaster;

        public enum InitializationState {
            Unknown,
            InProgress,
            Success,
            Fail
        }

        public InitializationState State { get; private set; }

        public PluginRepository(Dalamud dalamud, string pluginDirectory, string gameVersion)
        {
            this.dalamud = dalamud;
            this.pluginDirectory = pluginDirectory;

            ReloadPluginMasterAsync();
        }

        public void ReloadPluginMasterAsync()
        {
            Task.Run(() => {
                State = InitializationState.InProgress;

                try
                {
                    using var client = new WebClient();

                    var data = client.DownloadString(PluginRepoBaseUrl + "pluginmaster.json");

                    this.PluginMaster = JsonConvert.DeserializeObject<ReadOnlyCollection<PluginDefinition>>(data);

                    State = InitializationState.Success;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Could not download PluginMaster");
                    State = InitializationState.Fail;
                }
            }).ContinueWith(t => {
                if (t.IsFaulted)
                    State = InitializationState.Fail;
            });
        }

        public bool InstallPlugin(PluginDefinition definition, bool enableAfterInstall = true) {
            try
            {
                var outputDir = new DirectoryInfo(Path.Combine(this.pluginDirectory, definition.InternalName, definition.AssemblyVersion));
                var dllFile = new FileInfo(Path.Combine(outputDir.FullName, $"{definition.InternalName}.dll"));
                var disabledFile = new FileInfo(Path.Combine(outputDir.FullName, ".disabled"));
                var wasDisabled = disabledFile.Exists;

                if (dllFile.Exists && enableAfterInstall)
                {
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
                Log.Information("Downloading plugin to {0}", path);
                using var client = new WebClient();
                client.DownloadFile(PluginRepoBaseUrl + $"/plugins/{definition.InternalName}/latest.zip", path);

                Log.Information("Extracting to {0}", outputDir);

                ZipFile.ExtractToDirectory(path, outputDir.FullName);

                if (wasDisabled || !enableAfterInstall) {
                    disabledFile.Create();
                    return true;
                }

                return this.dalamud.PluginManager.LoadPluginFromAssembly(dllFile, false, PluginLoadReason.Installer);
            }
            catch (Exception e)
            {
                Log.Error(e, "Plugin download failed hard.");
                return false;
            }
        }

        internal class PluginUpdateStatus {
            public string InternalName { get; set; }
            public bool WasUpdated { get; set; }
        }

        public (bool Success, List<PluginUpdateStatus> UpdatedPlugins) UpdatePlugins(bool dryRun = false)
        {
            Log.Information("Starting plugin update... dry:{0}", dryRun);

            var updatedList = new List<PluginUpdateStatus>();
            var hasError = false;

            try
            {
                var pluginsDirectory = new DirectoryInfo(this.pluginDirectory);
                foreach (var installed in pluginsDirectory.GetDirectories())
                {
                    var versions = installed.GetDirectories();

                    if (versions.Length == 0)
                    {
                        Log.Information("Has no versions: {0}", installed.FullName);
                        continue;
                    }

                    var sortedVersions = versions.OrderBy(x => int.Parse(x.Name.Replace(".", "")));
                    var latest = sortedVersions.Last();

                    var localInfoFile = new FileInfo(Path.Combine(latest.FullName, $"{installed.Name}.json"));

                    if (!localInfoFile.Exists)
                    {
                        Log.Information("Has no definition: {0}", localInfoFile.FullName);
                        continue;
                    }

                    var info = JsonConvert.DeserializeObject<PluginDefinition>(File.ReadAllText(localInfoFile.FullName));

                    var remoteInfo = this.PluginMaster.FirstOrDefault(x => x.Name == info.Name);

                    if (remoteInfo == null)
                    {
                        Log.Information("Is not in pluginmaster: {0}", info.Name);
                        continue;
                    }

                    if (remoteInfo.DalamudApiLevel != PluginManager.DALAMUD_API_LEVEL)
                    {
                        Log.Information("Has not applicable API level: {0}", info.Name);
                        continue;
                    }

                    if (remoteInfo.AssemblyVersion != info.AssemblyVersion)
                    {
                        Log.Information("Eligible for update: {0}", remoteInfo.InternalName);

                        // DisablePlugin() below immediately creates a .disabled file anyway, but will fail
                        // with an exception if we try to do it twice in row like this

                        if (!dryRun)
                        {
                            var wasEnabled =
                                this.dalamud.PluginManager.Plugins.Where(x => x.Definition != null).Any(
                                    x => x.Definition.InternalName == info.InternalName); ;

                            Log.Verbose("wasEnabled: {0}", wasEnabled);

                            // Try to disable plugin if it is loaded
                            try
                            {
                                this.dalamud.PluginManager.DisablePlugin(info);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Plugin disable failed");
                                //hasError = true;
                            }

                            try {
                                // Just to be safe
                                foreach (var sortedVersion in sortedVersions)
                                {
                                    var disabledFile = new FileInfo(Path.Combine(sortedVersion.FullName, ".disabled"));
                                    if (!disabledFile.Exists)
                                        disabledFile.Create();
                                }
                            } catch (Exception ex) {
                                Log.Error(ex, "Plugin disable old versions failed");
                            }

                            var installSuccess = InstallPlugin(remoteInfo, wasEnabled);

                            if (!installSuccess)
                            {
                                Log.Error("InstallPlugin failed.");
                                hasError = true;
                            }

                            updatedList.Add(new PluginUpdateStatus {
                                InternalName = remoteInfo.InternalName,
                                WasUpdated = installSuccess
                            });
                        }
                        else {
                            updatedList.Add(new PluginUpdateStatus
                            {
                                InternalName = remoteInfo.InternalName,
                                WasUpdated = true
                            });
                        }
                    }
                    else
                    {
                        Log.Information("Up to date: {0}", remoteInfo.InternalName);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Plugin update failed hard.");
                hasError = true;
            }

            Log.Information("Plugin update OK.");

            return (!hasError, updatedList);
        }
    }
}
