using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using CheapLoc;
using Dalamud.Configuration;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Plugin
{
    internal class PluginManager {
        public static int DALAMUD_API_LEVEL = 2;

        private readonly Dalamud dalamud;
        private readonly string pluginDirectory;
        private readonly string devPluginDirectory;

        private readonly PluginConfigurations pluginConfigs;

        private readonly Type interfaceType = typeof(IDalamudPlugin);

        public readonly List<(IDalamudPlugin Plugin, PluginDefinition Definition, DalamudPluginInterface PluginInterface, bool IsRaw)> Plugins = new List<(IDalamudPlugin plugin, PluginDefinition def, DalamudPluginInterface PluginInterface, bool IsRaw)>();

        public List<(string SourcePluginName, string SubPluginName, Action<ExpandoObject> SubAction)> IpcSubscriptions = new List<(string SourcePluginName, string SubPluginName, Action<ExpandoObject> SubAction)>();

        public List<(string pluginName, string cause)> LoadFailedPlugins = new List<(string pluginName, string cause)>();

        public PluginManager(Dalamud dalamud, string pluginDirectory, string devPluginDirectory) {
            this.dalamud = dalamud;
            this.pluginDirectory = pluginDirectory;
            this.devPluginDirectory = devPluginDirectory;

            this.pluginConfigs = new PluginConfigurations(Path.Combine(Path.GetDirectoryName(dalamud.StartInfo.ConfigurationPath), "pluginConfigs"));

            // Try to load missing assemblies from the local directory of the requesting assembly
            // This would usually be implicit when using Assembly.Load(), but Assembly.LoadFile() doesn't do it...
            // This handler should only be invoked on things that fail regular lookups, but it *is* global to this appdomain
            AppDomain.CurrentDomain.AssemblyResolve += (object source, ResolveEventArgs e) =>
            {
                try {
                    Log.Debug($"Resolving missing assembly {e.Name}");
                    // This looks weird but I'm pretty sure it's actually correct.  Pretty sure.  Probably.
                    var assemblyPath = Path.Combine(Path.GetDirectoryName(e.RequestingAssembly.Location),
                                                    new AssemblyName(e.Name).Name + ".dll");
                    if (!File.Exists(assemblyPath)) {
                        Log.Error($"Assembly not found at {assemblyPath}");
                        return null;
                    }

                    return Assembly.LoadFrom(assemblyPath);
                } catch(Exception ex) {
                    Log.Error(ex, "Could not load assembly " + e.Name);
                    return null;
                }
            };
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

            // Need to do it with Open so the file handle gets closed immediately
            // TODO: Don't use the ".disabled" crap, do it in a config
            var disabledFile = File.Open(Path.Combine(outputDir.FullName, ".disabled"), FileMode.Create);
            disabledFile.Close();

            thisPlugin.Plugin.Dispose();

            this.Plugins.Remove(thisPlugin);
        }

        public bool LoadPluginFromAssembly(FileInfo dllFile, bool raw, PluginLoadReason reason) {
            Log.Information("Loading plugin at {0}", dllFile.Directory.FullName);

            // If this entire folder has been marked as a disabled plugin, don't even try to load anything
            var disabledFile = new FileInfo(Path.Combine(dllFile.Directory.FullName, ".disabled"));
            if (disabledFile.Exists && !raw) // should raw/dev plugins really not respect this?
            {
                Log.Information("Plugin {0} is disabled.", dllFile.FullName);
                return false;
            }

            var testingFile = new FileInfo(Path.Combine(dllFile.Directory.FullName, ".testing"));
            if (testingFile.Exists && !this.dalamud.Configuration.DoPluginTest) {
                Log.Information("Plugin {0} was testing, but testing is disabled.", dllFile.FullName);
                return false;
            }

            // read the plugin def if present - again, fail before actually trying to load the dll if there is a problem
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
                    this.LoadFailedPlugins.Add((Path.GetFileNameWithoutExtension(dllFile.Name), Loc.Localize("DalamudPluginImcompatibleAPILvl", "Incompatible with the current game version.")));
                    disabledFile.Create().Close();
                    return false;
                }
            }
            // but developer plugins don't require one to load
            else if (!raw)
            {
                Log.Information("Plugin DLL {0} has no definition.", dllFile.FullName);
                return false;
            }

            // TODO: given that it exists, the pluginDef's InternalName should probably be used
            // as the actual assembly to load
            // But plugins should also probably be loaded by directory and not by looking for every dll

            Log.Information("Loading assembly at {0}", dllFile);

            // Assembly.Load() by name here will not load multiple versions with the same name, in the case of updates
            var pluginAssembly = Assembly.LoadFile(dllFile.FullName);

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
                    if (this.Plugins.Any(x => x.Plugin.GetType().Assembly.GetName().Name == type.Assembly.GetName().Name))
                    {
                        Log.Error("Duplicate plugin found: {0}", dllFile.FullName);
                        return false;
                    }

                    var plugin = (IDalamudPlugin)Activator.CreateInstance(type);

                    // this happens for raw plugins that don't specify a PluginDefinition - just generate a dummy one to avoid crashes anywhere
                    pluginDef ??= new PluginDefinition{
                        Author = "developer",
                        Name = plugin.Name,
                        InternalName = Path.GetFileNameWithoutExtension(dllFile.Name),
                        AssemblyVersion = plugin.GetType().Assembly.GetName().Version.ToString(),
                        Description = "",
                        ApplicableVersion = "any",
                        IsHide = false,
                        DalamudApiLevel = DALAMUD_API_LEVEL
                    };

                    if (pluginDef.DalamudApiLevel < DALAMUD_API_LEVEL) {
                        Log.Error("Incompatible API level: {0}", dllFile.FullName);
                        this.LoadFailedPlugins.Add((Path.GetFileNameWithoutExtension(dllFile.Name), Loc.Localize("DalamudPluginIncompatibleGameVersion", "Incompatible with the current Dalamud version.")));
                        disabledFile.Create().Close();
                        return false;
                    }

                    var dalamudInterface = new DalamudPluginInterface(this.dalamud, type.Assembly.GetName().Name, this.pluginConfigs, reason);
                    plugin.Initialize(dalamudInterface);

                    Log.Information("Loaded plugin: {0}", plugin.Name);
                    this.Plugins.Add((plugin, pluginDef, dalamudInterface, raw));

                    return true;
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
                        LoadPluginFromAssembly(dllFile, raw, PluginLoadReason.Boot);
                    } catch (Exception ex) {
                        Log.Error(ex, $"Plugin load for {dllFile.FullName} failed.");
                        this.LoadFailedPlugins.Add((Path.GetFileNameWithoutExtension(dllFile.Name), Loc.Localize("DalamudPluginLoadError", "The plugin has encountered an error.")));
                    }
                }
            }
        }
    }
}
