using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.DiscordBot;
using Newtonsoft.Json;
using XIVLauncher.Dalamud;

namespace Dalamud
{
    [Serializable]
    public class DalamudConfiguration
    {
        public DiscordFeatureConfiguration DiscordFeatureConfig { get; set; }

        public bool OptOutMbCollection { get; set; } = false;

        public CustomComboPreset ComboPresets { get; set; }

        public List<string> BadWords { get; set; }

        public class FateInfo {
            public string Name { get; set; }
            public int Id { get; set; }
        }

        public List<FateInfo> Fates;


        public static DalamudConfiguration Load(string path) {
            return JsonConvert.DeserializeObject<DalamudConfiguration>(File.ReadAllText(path));
        }

        public void Save(string path) {
            File.WriteAllText(path, JsonConvert.SerializeObject(this));
        }
    }
}
