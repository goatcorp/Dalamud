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
    public class PluginRepository
    {
        private const string PluginRepoBaseUrl = "https://goaaats.github.io/DalamudPlugins/";

        private PluginManager manager;
        private string pluginDirectory;
        public ReadOnlyCollection<PluginDefinition> PluginMaster;

        public enum InitializationState {
            Unknown,
            InProgress,
            Success,
            Fail
        }

        public InitializationState State { get; private set; }

        public PluginRepository(PluginManager manager, string pluginDirectory, string gameVersion)
        {
            this.manager = manager;
            this.pluginDirectory = pluginDirectory;

            State = InitializationState.InProgress;
            Task.Run(CachePluginMaster).ContinueWith(t => {
                if (t.IsFaulted)
                    State = InitializationState.Fail;
            });
        }

        private void CachePluginMaster()
        {
            try
            {
                using var client = new WebClient();

                var data = client.DownloadString(PluginRepoBaseUrl + "pluginmaster.json");

                this.PluginMaster = JsonConvert.DeserializeObject<ReadOnlyCollection<PluginDefinition>>(data);

                State = InitializationState.Success;
            }
            catch {
                State = InitializationState.Fail;
            }
        }

        public bool InstallPlugin(PluginDefinition definition) {
            try
            {
                var outputDir = new DirectoryInfo(Path.Combine(this.pluginDirectory, definition.InternalName, definition.AssemblyVersion));
                var dllFile = new FileInfo(Path.Combine(outputDir.FullName, $"{definition.InternalName}.dll"));
                var disabledFile = new FileInfo(Path.Combine(outputDir.FullName, ".disabled"));

                if (dllFile.Exists)
                {
                    if (disabledFile.Exists)
                        disabledFile.Delete();

                    return this.manager.LoadPluginFromAssembly(dllFile, false);
                }

                if (outputDir.Exists)
                    outputDir.Delete(true);
                outputDir.Create();

                var path = Path.GetTempFileName();
                Log.Information("Downloading plugin to {0}", path);
                using var client = new WebClient();
                client.DownloadFile(PluginRepoBaseUrl + $"/plugins/{definition.InternalName}/latest.zip", path);

                Log.Information("Extracting to {0}", outputDir);

                ZipFile.ExtractToDirectory(path, outputDir.FullName);

                return this.manager.LoadPluginFromAssembly(dllFile, false);
            }
            catch (Exception e)
            {
                Log.Error(e, "Plugin download failed hard.");
                return false;
            }
        }

        public (bool Success, int UpdatedCount) UpdatePlugins(bool dryRun = false)
        {
            Log.Information("Starting plugin update... dry:{0}", dryRun);

            var updatedCount = 0;
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

                    if (remoteInfo.AssemblyVersion != info.AssemblyVersion)
                    {
                        Log.Information("Eligible for update: {0}", remoteInfo.InternalName);

                        // DisablePlugin() below immediately creates a .disabled file anyway, but will fail
                        // with an exception if we try to do it twice in row like this
                        // TODO: not sure if doing this for all versions is really necessary, since the
                        // others really needed to be disabled before anyway
                        //foreach (var sortedVersion in sortedVersions) {
                        //    File.Create(Path.Combine(sortedVersion.FullName, ".disabled"));
                        //}

                        if (!dryRun)
                        {
                            // Try to disable plugin if it is loaded
                            try
                            {
                                this.manager.DisablePlugin(info);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Plugin disable failed");
                                hasError = true;
                            }

                            var installSuccess = InstallPlugin(remoteInfo);

                            if (installSuccess)
                            {
                                updatedCount++;
                            }
                            else
                            {
                                Log.Error("InstallPlugin failed.");
                                hasError = true;
                            }
                        }
                        else {
                            updatedCount++;
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

            return (!hasError, updatedCount);
        }
    }
}
