using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dalamud.Configuration;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Plugin
{
    public class PluginManager {
        private readonly Dalamud dalamud;
        private readonly string pluginDirectory;
        private readonly string devPluginDirectory;

        private readonly PluginConfigurations pluginConfigs;

        private readonly Type interfaceType = typeof(IDalamudPlugin);

        public readonly List<(IDalamudPlugin Plugin, PluginDefinition Definition, DalamudPluginInterface PluginInterface)> Plugins = new List<(IDalamudPlugin plugin, PluginDefinition def, DalamudPluginInterface PluginInterface)>();

        public PluginManager(Dalamud dalamud, string pluginDirectory, string devPluginDirectory) {
            this.dalamud = dalamud;
            this.pluginDirectory = pluginDirectory;
            this.devPluginDirectory = devPluginDirectory;

            this.pluginConfigs = new PluginConfigurations(Path.Combine(Path.GetDirectoryName(dalamud.StartInfo.ConfigurationPath), "pluginConfigs"));
        }

        public void UnloadPlugins() {
            if (this.Plugins == null)
                return;

            for (var i = 0; i < this.Plugins.Count; i++) {
                this.Plugins[i].Plugin.Dispose();
            }

            this.Plugins.Clear();
        }

        public void LoadPlugins() {
            LoadPluginsAt(new DirectoryInfo(this.pluginDirectory), false);
            LoadPluginsAt(new DirectoryInfo(this.devPluginDirectory), true);
        }

        public void DisablePlugin(PluginDefinition definition) {
            var thisPlugin = this.Plugins.Where(x => x.Definition != null)
                                 .First(x => x.Definition.InternalName == definition.InternalName);

            var outputDir = new DirectoryInfo(Path.Combine(this.pluginDirectory, definition.InternalName, definition.AssemblyVersion));
            File.Create(Path.Combine(outputDir.FullName, ".disabled"));

            thisPlugin.Plugin.Dispose();

            this.Plugins.Remove(thisPlugin);
        }

        public bool LoadPluginFromAssembly(FileInfo dllFile, bool raw) {
            Log.Information("Loading assembly at {0}", dllFile);
            var assemblyName = AssemblyName.GetAssemblyName(dllFile.FullName);
            var pluginAssembly = Assembly.Load(assemblyName);

            if (pluginAssembly != null)
            {
                Log.Information("Loading types for {0}", pluginAssembly.FullName);
                var types = pluginAssembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.IsInterface || type.IsAbstract)
                    {
                        continue;
                    }

                    if (type.GetInterface(interfaceType.FullName) != null)
                    {
                        var disabledFile = new FileInfo(Path.Combine(dllFile.Directory.FullName, ".disabled"));

                        if (disabledFile.Exists && !raw) {
                            Log.Information("Plugin {0} is disabled.", dllFile.FullName);
                            return false;
                        }

                        var defJsonFile = new FileInfo(Path.Combine(dllFile.Directory.FullName, $"{Path.GetFileNameWithoutExtension(dllFile.Name)}.json"));

                        PluginDefinition pluginDef = null;
                        // load the definition if it exists, even for raw/developer plugins
                        if (defJsonFile.Exists)
                        {
                            Log.Information("Loading definition for plugin DLL {0}", dllFile.FullName);

                            pluginDef =
                                JsonConvert.DeserializeObject<PluginDefinition>(
                                    File.ReadAllText(defJsonFile.FullName));

                            if (pluginDef.ApplicableVersion != this.dalamud.StartInfo.GameVersion && pluginDef.ApplicableVersion != "any")
                            {
                                Log.Information("Plugin {0} has not applicable version.", dllFile.FullName);
                                return false;
                            }
                        }
                        // but developer plugins don't require one to load
                        else if (!raw)
                        {
                            Log.Information("Plugin DLL {0} has no definition.", dllFile.FullName);
                            return false;
                        }

                        if (this.Plugins.Any(x => x.Plugin.GetType().Assembly.GetName().Name == type.Assembly.GetName().Name)) {
                            Log.Error("Duplicate plugin found: {0}", dllFile.FullName);
                            return false;
                        }

                        var plugin = (IDalamudPlugin)Activator.CreateInstance(type);

                        // this happens for raw plugins that don't specify a PluginDefinition - just generate a dummy one to avoid crashes anywhere
                        if (pluginDef == null)
                        {
                            pluginDef = new PluginDefinition
                            {
                                Author = "developer",
                                Name = plugin.Name,
                                InternalName = "devPlugin_" + plugin.Name,
                                AssemblyVersion = plugin.GetType().Assembly.GetName().Version.ToString(),
                                Description = "",
                                ApplicableVersion = "any",
                                IsHide = false
                            };
                        }

                        var dalamudInterface = new DalamudPluginInterface(this.dalamud, type.Assembly.GetName().Name, this.pluginConfigs);
                        plugin.Initialize(dalamudInterface);
                        Log.Information("Loaded plugin: {0}", plugin.Name);
                        this.Plugins.Add((plugin, pluginDef, dalamudInterface));

                        return true;
                    }
                }
            }

            Log.Information("Plugin DLL {0} has no plugin interface.", dllFile.FullName);

            return false;
        }

        private void LoadPluginsAt(DirectoryInfo folder, bool raw) {
            if (folder.Exists)
            { 
                Log.Information("Loading plugins at {0}", folder);

                var pluginDlls = folder.GetFiles("*.dll", SearchOption.AllDirectories);

                foreach (var dllFile in pluginDlls) {
                    try {
                        LoadPluginFromAssembly(dllFile, raw);
                    } catch (Exception ex) {
                        Log.Error(ex, $"Plugin load for {dllFile.FullName} failed.");
                    }
                }
            }
        }
    }
}
