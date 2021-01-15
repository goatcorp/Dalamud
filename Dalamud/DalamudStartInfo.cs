using System;
#pragma warning disable 1591

namespace Dalamud {
    [Serializable]
    public sealed class DalamudStartInfo
    {
        public string WorkingDirectory;
        public string ConfigurationPath;

        public string PluginDirectory;
        public string DefaultPluginDirectory;
        public ClientLanguage Language;

        public string GameVersion;

        public bool OptOutMbCollection;
    }

    /// <summary>
    /// Enum describing the language the game loads in.
    /// </summary>
    public enum ClientLanguage
    {
        Japanese,
        English,
        German,
        French
    }
}
