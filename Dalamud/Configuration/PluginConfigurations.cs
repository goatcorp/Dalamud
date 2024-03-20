using System.IO;

using Dalamud.Storage;
using Newtonsoft.Json;

namespace Dalamud.Configuration;

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
    /// <param name="workingPluginId">WorkingPluginId of the plugin.</param>
    public void Save(IPluginConfiguration config, string pluginName, Guid workingPluginId)
    {
        Service<ReliableFileStorage>.Get()
                                    .WriteAllText(this.GetConfigFile(pluginName).FullName, SerializeConfig(config), workingPluginId);
    }

    /// <summary>
    /// Load plugin configuration.
    /// </summary>
    /// <param name="pluginName">Plugin name.</param>
    /// <param name="workingPluginId">WorkingPluginID of the plugin.</param>
    /// <returns>Plugin configuration.</returns>
    public IPluginConfiguration? Load(string pluginName, Guid workingPluginId)
    {
        var path = this.GetConfigFile(pluginName);

        IPluginConfiguration? config = null;
        try
        {
            Service<ReliableFileStorage>.Get().ReadAllText(path.FullName, text =>
            {
                config = DeserializeConfig(text);
                if (config == null)
                    throw new Exception("Read config was null.");
            }, workingPluginId);
        }
        catch (FileNotFoundException)
        {
            // ignored
        }

        return config;
    }

    /// <summary>
    /// Delete the configuration file and folder for the specified plugin.
    /// This will throw an <see cref="IOException"/> if the plugin did not correctly close its handles.
    /// </summary>
    /// <param name="pluginName">The name of the plugin.</param>
    public void Delete(string pluginName)
    {
        var directory = this.GetDirectoryPath(pluginName);
        if (directory.Exists)
            directory.Delete(true);

        var file = this.GetConfigFile(pluginName);
        if (file.Exists)
            file.Delete();
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

    /// <summary>
    /// Serializes a plugin configuration object.
    /// </summary>
    /// <param name="config">The configuration object.</param>
    /// <returns>A string representing the serialized configuration object.</returns>
    internal static string SerializeConfig(object? config)
    {
        return JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.Objects,
        });
    }

    /// <summary>
    /// Deserializes a plugin configuration from a string.
    /// </summary>
    /// <param name="data">The serialized configuration.</param>
    /// <returns>The configuration object, or null.</returns>
    internal static IPluginConfiguration? DeserializeConfig(string data)
    {
        return JsonConvert.DeserializeObject<IPluginConfiguration>(
            data,
            new JsonSerializerSettings
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects,
            });
    }

    private DirectoryInfo GetDirectoryPath(string pluginName) => new(Path.Combine(this.configDirectory.FullName, pluginName));
}
