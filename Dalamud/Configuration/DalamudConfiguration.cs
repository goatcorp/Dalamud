using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Configuration;
using Dalamud.DiscordBot;
using Newtonsoft.Json;

namespace Dalamud
{
    [Serializable]
    public class DalamudConfiguration
    {
        public DiscordFeatureConfiguration DiscordFeatureConfig { get; set; }

        public bool OptOutMbCollection { get; set; } = false;

        public List<string> BadWords { get; set; }

        public enum PreferredRole
        {
            None,
            All,
            Tank,
            Dps,
            Healer
        }

        public Dictionary<int, PreferredRole> PreferredRoleReminders { get; set; }

        public string LastVersion { get; set; }

        public Dictionary<string, IPluginConfiguration> PluginConfigurations { get; set; }

        public bool WelcomeGuideDismissed;

        public static DalamudConfiguration Load(string path) {
            return JsonConvert.DeserializeObject<DalamudConfiguration>(File.ReadAllText(path));
        }

        public void Save(string path) {
            File.WriteAllText(path, JsonConvert.SerializeObject(this));
        }
    }
}
