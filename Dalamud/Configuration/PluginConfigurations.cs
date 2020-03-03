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

        public void Save(IPluginConfiguration config, string pluginName) {
            File.WriteAllText(GetPath(pluginName).FullName, JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects
            }));
        }

        public IPluginConfiguration Load(string pluginName) {
            var path = GetPath(pluginName);

            if (!path.Exists)
                return null;

            return JsonConvert.DeserializeObject<IPluginConfiguration>(File.ReadAllText(path.FullName),
                                                                       new JsonSerializerSettings {
                                                                           TypeNameAssemblyFormatHandling =
                                                                               TypeNameAssemblyFormatHandling.Simple,
                                                                           TypeNameHandling = TypeNameHandling.Objects
                                                                       });
        }

        private FileInfo GetPath(string pluginName) => new FileInfo(Path.Combine(this.configDirectory.FullName, $"{pluginName}.json"));
    }
}
