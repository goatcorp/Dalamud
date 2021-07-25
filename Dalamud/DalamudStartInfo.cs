using System;

using Dalamud.Game;
using Newtonsoft.Json;

namespace Dalamud
{
    /// <summary>
    /// Struct containing information needed to initialize Dalamud.
    /// </summary>
    [Serializable]
    public struct DalamudStartInfo
    {
        /// <summary>
        /// The working directory of the XIVLauncher installations.
        /// </summary>
        public string WorkingDirectory;

        /// <summary>
        /// The path to the configuration file.
        /// </summary>
        public string ConfigurationPath;

        /// <summary>
        /// The path to the directory for installed plugins.
        /// </summary>
        public string PluginDirectory;

        /// <summary>
        /// The path to the directory for developer plugins.
        /// </summary>
        public string DefaultPluginDirectory;

        /// <summary>
        /// The path to core Dalamud assets.
        /// </summary>
        public string AssetDirectory;

        /// <summary>
        /// The language of the game client.
        /// </summary>
        public ClientLanguage Language;

        /// <summary>
        /// The current game version code.
        /// </summary>
        [JsonConverter(typeof(GameVersionConverter))]
        public GameVersion GameVersion;

        /// <summary>
        /// Whether or not market board information should be uploaded by default.
        /// </summary>
        public bool OptOutMbCollection;
    }
}
