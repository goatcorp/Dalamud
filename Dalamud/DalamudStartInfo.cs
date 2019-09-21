using System;
using Dalamud.DiscordBot;

namespace Dalamud {
    [Serializable]
    public sealed class DalamudStartInfo {
        public string WorkingDirectory;
        public string PluginDirectory;
        public string DefaultPluginDirectory;
        public ClientLanguage Language;

        public DiscordFeatureConfiguration DiscordFeatureConfig { get; set; }

        public bool OptOutMbCollection { get; set; } = false;
    }

    public enum ClientLanguage
    {
        Japanese,
        English,
        German,
        French
    }
}
