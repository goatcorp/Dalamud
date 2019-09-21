using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Dalamud.Plugin
{
    public class PluginManager {
        private readonly Dalamud dalamud;
        private readonly string pluginDirectory;
        private readonly string defaultPluginDirectory;

        private readonly DalamudPluginInterface dalamudInterface;

        private List<IDalamudPlugin> plugins;

        public PluginManager(Dalamud dalamud, string pluginDirectory, string defaultPluginDirectory) {
            this.dalamud = dalamud;
            this.pluginDirectory = pluginDirectory;
            this.defaultPluginDirectory = defaultPluginDirectory;

            this.dalamudInterface = new DalamudPluginInterface(dalamud);
        }

        public void UnloadPlugins() {
            if (this.plugins == null)
                return;

            for (var i = 0; i < this.plugins.Count; i++) {
                this.plugins[i].Dispose();
                this.plugins[i] = null;
            }
        }

        public void LoadPlugins() {
            LoadPluginsAt(this.defaultPluginDirectory);
            LoadPluginsAt(this.pluginDirectory);
        }

        private void LoadPluginsAt(string folder) {
            if (Directory.Exists(folder))
            { 
                Log.Debug("Loading plugins at {0}", folder);

                var pluginFileNames = Directory.GetFiles(folder, "*.dll"); 

                var assemblies = new List<Assembly>(pluginFileNames.Length); 
                foreach (var dllFile in pluginFileNames) 
                { 
                    Log.Debug("Loading assembly at {0}", dllFile);
                    var assemblyName = AssemblyName.GetAssemblyName(dllFile); 
                    var pluginAssembly = Assembly.Load(assemblyName); 
                    assemblies.Add(pluginAssembly); 
                }

                var interfaceType = typeof(IDalamudPlugin);
                var foundImplementations = new List<Type>();
                foreach (var assembly in assemblies) {
                    if (assembly != null) {
                        Log.Debug("Loading types for {0}", assembly.FullName);
                        var types = assembly.GetTypes();
                        foreach (var type in types) {
                            if (type.IsInterface || type.IsAbstract) {
                                continue;
                            }

                            if (type.GetInterface(interfaceType.FullName) != null) {
                                foundImplementations.Add(type);
                            }
                        }
                    }
                }

                this.plugins = new List<IDalamudPlugin>(foundImplementations.Count); 
                foreach (var pluginType in foundImplementations) 
                { 
                    var plugin = (IDalamudPlugin)Activator.CreateInstance(pluginType);
                    plugin.Initialize(this.dalamudInterface);
                    Log.Information("Loaded plugin: {0}", plugin.Name);
                    this.plugins.Add(plugin); 
                }
            }
        }
    }
}
