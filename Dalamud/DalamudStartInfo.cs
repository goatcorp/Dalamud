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
        /// Gets a value indicating whether or not market board information should be uploaded by default.
        /// </summary>
        public bool OptOutMbCollection { get; init; }

        /// <summary>
        /// Gets a value that specifies how much to wait before a new Dalamud session.
        /// </summary>
        public int DelayInitializeMs { get; init; } = 0;
    }
}
