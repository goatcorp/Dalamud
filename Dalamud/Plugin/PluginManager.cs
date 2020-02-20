using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Plugin
{
    public class PluginManager {
        private readonly Dalamud dalamud;
        private readonly string pluginDirectory;

        private readonly Type interfaceType = typeof(IDalamudPlugin);

        public readonly List<(IDalamudPlugin Plugin, PluginDefinition Definition)> Plugins = new List<(IDalamudPlugin plugin, PluginDefinition def)>();

        public PluginManager(Dalamud dalamud, string pluginDirectory) {
            this.dalamud = dalamud;
            this.pluginDirectory = pluginDirectory;
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
            LoadPluginsAt(new DirectoryInfo(this.pluginDirectory));
        }

        public void DisablePlugin(PluginDefinition definition) {
            var thisPlugin = this.Plugins.Where(x => x.Definition != null)
                                 .First(x => x.Definition.InternalName == definition.InternalName);

            var outputDir = new DirectoryInfo(Path.Combine(this.pluginDirectory, definition.InternalName, definition.AssemblyVersion));
            File.Create(Path.Combine(outputDir.FullName, ".disabled"));

            thisPlugin.Plugin.Dispose();

            this.Plugins.Remove(thisPlugin);
        }

        public void LoadPluginFromAssembly(FileInfo dllFile) {
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

                        if (disabledFile.Exists) {
                            Log.Information("Plugin {0} is disabled.", dllFile.FullName);
                            return;
                        }

                        var defJsonFile = new FileInfo(Path.Combine(dllFile.Directory.FullName, $"{Path.GetFileNameWithoutExtension(dllFile.Name)}.json"));

                        PluginDefinition pluginDef = null;
                        if (defJsonFile.Exists)
                        {
                            Log.Information("Loading definition for plugin DLL {0}", dllFile.FullName);

                            pluginDef =
                                JsonConvert.DeserializeObject<PluginDefinition>(
                                    File.ReadAllText(defJsonFile.FullName));
                        }
                        else
                        {
                            Log.Information("Plugin DLL {0} has no definition.", dllFile.FullName);
                            return;
                        }

                        var plugin = (IDalamudPlugin)Activator.CreateInstance(type);

                        var dalamudInterface = new DalamudPluginInterface(this.dalamud, type.Assembly.GetName().Name);
                        plugin.Initialize(dalamudInterface);
                        Log.Information("Loaded plugin: {0}", plugin.Name);
                        this.Plugins.Add((plugin, pluginDef));
                    }
                }
            }
        }

        private void LoadPluginsAt(DirectoryInfo folder) {
            if (folder.Exists)
            { 
                Log.Information("Loading plugins at {0}", folder);

                var pluginDlls = folder.GetFiles("*.dll", SearchOption.AllDirectories);

                foreach (var dllFile in pluginDlls) {
                    LoadPluginFromAssembly(dllFile);
                }
            }
        }
    }
}
