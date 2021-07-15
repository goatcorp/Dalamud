using System.IO;

using Newtonsoft.Json;

namespace Dalamud.Configuration
{
    /// <summary>
    /// Configuration to store settings for a dalamud plugin.
    /// </summary>
    public sealed class PluginConfigurations
    {
        private readonly DirectoryInfo configDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfigurations"/> class.
        /// </summary>
        /// <param name="storageFolder">Directory for storage of plugin configuration files.</param>
        public PluginConfigurations(string storageFolder)
        {
            this.configDirectory = new DirectoryInfo(storageFolder);
            this.configDirectory.Create();
        }

        /// <summary>
        /// Save/Load plugin configuration.
        /// NOTE: Save/Load are still using Type information for now,
        /// despite LoadForType superseding Load and not requiring or using it.
        /// It might be worth removing the Type info from Save, to strip it from all future saved configs,
        /// and then Load() can probably be removed entirely.
        /// </summary>
        /// <param name="config">Plugin configuration.</param>
        /// <param name="pluginName">Plugin name.</param>
        public void Save(IPluginConfiguration config, string pluginName)
        {
            File.WriteAllText(this.GetConfigFile(pluginName).FullName, JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects,
            }));
        }

        /// <summary>
        /// Load plugin configuration.
        /// </summary>
        /// <param name="pluginName">Plugin name.</param>
        /// <returns>Plugin configuration.</returns>
        public IPluginConfiguration Load(string pluginName)
        {
            var path = this.GetConfigFile(pluginName);

            if (!path.Exists)
                return null;

            return JsonConvert.DeserializeObject<IPluginConfiguration>(
                File.ReadAllText(path.FullName),
                new JsonSerializerSettings
                {
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    TypeNameHandling = TypeNameHandling.Objects,
                });
        }

        /// <summary>
        /// Get plugin directory.
        /// </summary>
        /// <param name="pluginName">Plugin name.</param>
        /// <returns>Plugin directory path.</returns>
        public string GetDirectory(string pluginName)
        {
            try
            {
                var path = this.GetDirectoryPath(pluginName);
                if (!path.Exists)
                {
                    path.Create();
                }

                return path.FullName;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Load Plugin configuration. Parameterized deserialization.
        /// Currently this is called via reflection from DalamudPluginInterface.GetPluginConfig().
        /// Eventually there may be an additional pluginInterface method that can call this directly
        /// without reflection - for now this is in support of the existing plugin api.
        /// </summary>
        /// <param name="pluginName">Plugin Name.</param>
        /// <typeparam name="T">Configuration Type.</typeparam>
        /// <returns>Plugin Configuration.</returns>
        public T LoadForType<T>(string pluginName) where T : IPluginConfiguration
        {
            var path = this.GetConfigFile(pluginName);

            return !path.Exists ? default : JsonConvert.DeserializeObject<T>(File.ReadAllText(path.FullName));

            // intentionally no type handling - it will break when updating a plugin at runtime
            // and turns out to be unnecessary when we fully qualify the object type
        }

        /// <summary>
        /// Get FileInfo to plugin config file.
        /// </summary>
        /// <param name="pluginName">InternalName of the plugin.</param>
        /// <returns>FileInfo of the config file.</returns>
        public FileInfo GetConfigFile(string pluginName) => new(Path.Combine(this.configDirectory.FullName, $"{pluginName}.json"));

        private DirectoryInfo GetDirectoryPath(string pluginName) => new(Path.Combine(this.configDirectory.FullName, pluginName));
    }
}
