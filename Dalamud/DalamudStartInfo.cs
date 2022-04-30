using System;

using Dalamud.Game;
using Newtonsoft.Json;

namespace Dalamud
{
    /// <summary>
    /// Struct containing information needed to initialize Dalamud.
    /// </summary>
    [Serializable]
    public record DalamudStartInfo
    {
        /// <summary>
        /// Gets or sets the working directory of the XIVLauncher installations.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Gets the path to the configuration file.
        /// </summary>
        public string ConfigurationPath { get; init; }

        /// <summary>
        /// Gets the path to the directory for installed plugins.
        /// </summary>
        public string PluginDirectory { get; init; }

        /// <summary>
        /// Gets the path to the directory for developer plugins.
        /// </summary>
        public string DefaultPluginDirectory { get; init; }

        /// <summary>
        /// Gets the path to core Dalamud assets.
        /// </summary>
        public string AssetDirectory { get; init; }

        /// <summary>
        /// Gets the language of the game client.
        /// </summary>
        public ClientLanguage Language { get; init; }

        /// <summary>
        /// Gets the current game version code.
        /// </summary>
        [JsonConverter(typeof(GameVersionConverter))]
        public GameVersion GameVersion { get; init; }

        /// <summary>
        /// Gets a value that specifies how much to wait before a new Dalamud session.
        /// </summary>
        public int DelayInitializeMs { get; init; } = 0;

        /// <summary>
        /// Returns a new copy of <see cref="DalamudStartInfo"/>, with altered fields.
        /// </summary>
        /// <param name="workingDirectory">New working directory.</param>
        /// <param name="configurationPath">New configuration path.</param>
        /// <param name="pluginDirectory">New plugin directory.</param>
        /// <param name="defaultPluginDirectory">New default plugin directory.</param>
        /// <param name="assetDirectory">New asset directory.</param>
        /// <param name="language">New language.</param>
        /// <param name="gameVersion">New game version.</param>
        /// <returns>New copy of <see cref="DalamudStartInfo"/>.</returns>
        public DalamudStartInfo Alter(string? workingDirectory = null, string? configurationPath = null, string? pluginDirectory = null, string? defaultPluginDirectory = null, string? assetDirectory = null, ClientLanguage? language = null, GameVersion? gameVersion = null)
        {
            return new()
            {
                WorkingDirectory = workingDirectory ?? this.WorkingDirectory,
                ConfigurationPath = configurationPath ?? this.ConfigurationPath,
                PluginDirectory = pluginDirectory ?? this.PluginDirectory,
                DefaultPluginDirectory = defaultPluginDirectory ?? this.DefaultPluginDirectory,
                AssetDirectory = assetDirectory ?? this.AssetDirectory,
                Language = language ?? this.Language,
                GameVersion = workingDirectory ?? this.GameVersion,
            };
        }
    }
}
