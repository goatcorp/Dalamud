using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Configuration;
using Dalamud.Game.Chat;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud
{
    [Serializable]
    internal class DalamudConfiguration
    {
        public bool OptOutMbCollection { get; set; } = false;

        public List<string> BadWords { get; set; }

        public bool DutyFinderTaskbarFlash { get; set; } = true;
        public bool DutyFinderChatMessage { get; set; } = true;

        public string LanguageOverride { get; set; }

        public string LastVersion { get; set; }

        public XivChatType GeneralChatType { get; set; } = XivChatType.Debug;

        public bool DoPluginTest { get; set; } = false;
        public bool DoDalamudTest { get; set; } = false;
        public List<ThirdRepoSetting> ThirdRepoList { get; set; }= new List<ThirdRepoSetting>();

        public float GlobalUiScale { get; set; } = 1.0f;
        public bool ToggleUiHide { get; set; } = true;
        public bool ToggleUiHideDuringCutscenes { get; set; } = true;
        public bool ToggleUiHideDuringGpose { get; set; } = true;

        public bool PrintPluginsWelcomeMsg { get; set; } = true;
        public bool AutoUpdatePlugins { get; set; } = false;

        public bool IsInstalledFirstInstaller { get; set; } = false;

        [JsonIgnore]
        public string ConfigPath;

        public static DalamudConfiguration Load(string path) {
            DalamudConfiguration deserialized;
            try
            {
                deserialized = JsonConvert.DeserializeObject<DalamudConfiguration>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to load DalamudConfiguration at {0}", path);
                deserialized = new DalamudConfiguration();
            }

            deserialized.ConfigPath = path;

            return deserialized;
        }

        /// <summary>
        /// Save the configuration at the path it was loaded from.
        /// </summary>
        public void Save() {
            File.WriteAllText(this.ConfigPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
