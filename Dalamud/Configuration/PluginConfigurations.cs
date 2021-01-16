using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Dalamud.Configuration
{
    public class PluginConfigurations {
        private DirectoryInfo configDirectory;

        public PluginConfigurations(string storageFolder) {
            this.configDirectory = new DirectoryInfo(storageFolder);
            this.configDirectory.Create();
        }

        // NOTE:  Save/Load are still using Type information for now, despite LoadForType<> superseding Load
        // and not requiring or using it.  It might be worth removing the Type info from Save, to strip it from
        // all future saved configs, and then Load() can probably be removed entirey.

        public void Save(IPluginConfiguration config, string pluginName) {
            File.WriteAllText(GetConfigFile(pluginName).FullName, JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects
            }));
        }

        public IPluginConfiguration Load(string pluginName) {
            var path = GetConfigFile(pluginName);

            if (!path.Exists)
                return null;

            return JsonConvert.DeserializeObject<IPluginConfiguration>(File.ReadAllText(path.FullName),
                                                                       new JsonSerializerSettings {
                                                                           TypeNameAssemblyFormatHandling =
                                                                               TypeNameAssemblyFormatHandling.Simple,
                                                                           TypeNameHandling = TypeNameHandling.Objects
                                                                       });
        }

        public string GetDirectory(string pluginName) {
            try {
                var path = GetDirectoryPath(pluginName);
                if (!path.Exists) {
                    path.Create();
                }
                return path.FullName;
            } catch {
                return string.Empty;
            }
        }

        // Parameterized deserialization
        // Currently this is called via reflection from DalamudPluginInterface.GetPluginConfig()
        // Eventually there may be an additional pluginInterface method that can call this directly
        // without reflection - for now this is in support of the existing plugin api
        public T LoadForType<T>(string pluginName) where T : IPluginConfiguration
        {
            var path = GetConfigFile(pluginName);

            if (!path.Exists)
                return default(T);

            // intentionally no type handling - it will break when updating a plugin at runtime
            // and turns out to be unnecessary when we fully qualify the object type
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path.FullName));
        }

        /// <summary>
        /// Get FileInfo to plugin config file.
        /// </summary>
        /// <param name="pluginName">InternalName of the plugin.</param>
        /// <returns>FileInfo of the config file</returns>
        public FileInfo GetConfigFile(string pluginName) => new FileInfo(Path.Combine(this.configDirectory.FullName, $"{pluginName}.json"));

        private DirectoryInfo GetDirectoryPath(string pluginName) => new DirectoryInfo(Path.Combine(this.configDirectory.FullName, pluginName));

    }
}
