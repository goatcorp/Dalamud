using System;
using System.Collections.Generic;
using System.IO;

using Dalamud.Game.Text;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Configuration
{
    /// <summary>
    /// Class containing Dalamud settings.
    /// </summary>
    [Serializable]
    internal class DalamudConfiguration
    {
        [JsonIgnore]
        private string configPath;

        /// <summary>
        /// Gets or sets a list of muted works.
        /// </summary>
        public List<string> BadWords { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the taskbar should flash once a duty is found.
        /// </summary>
        public bool DutyFinderTaskbarFlash { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not a message should be sent in chat once a duty is found.
        /// </summary>
        public bool DutyFinderChatMessage { get; set; } = true;

        /// <summary>
        /// Gets or sets the language code to load Dalamud localization with.
        /// </summary>
        public string LanguageOverride { get; set; }

        /// <summary>
        /// Gets or sets the last loaded Dalamud version.
        /// </summary>
        public string LastVersion { get; set; }

        /// <summary>
        /// Gets or sets the chat type used by default for plugin messages.
        /// </summary>
        public XivChatType GeneralChatType { get; set; } = XivChatType.Debug;

        /// <summary>
        /// Gets or sets a value indicating whether or not plugin testing builds should be shown.
        /// </summary>
        public bool DoPluginTest { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether or not Dalamud testing builds should be used.
        /// </summary>
        public bool DoDalamudTest { get; set; } = false;

        /// <summary>
        /// Gets or sets a list of custom repos.
        /// </summary>
        public List<ThirdRepoSetting> ThirdRepoList { get; set; } = new List<ThirdRepoSetting>();

        /// <summary>
        /// Gets or sets a list of hidden plugins.
        /// </summary>
        public List<string> HiddenPluginInternalName { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the global UI scale.
        /// </summary>
        public float GlobalUiScale { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets a value indicating whether or not plugin UI should be hidden.
        /// </summary>
        public bool ToggleUiHide { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not plugin UI should be hidden during cutscenes.
        /// </summary>
        public bool ToggleUiHideDuringCutscenes { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not plugin UI should be hidden during GPose.
        /// </summary>
        public bool ToggleUiHideDuringGpose { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not a message containing detailed plugin information should be sent at login.
        /// </summary>
        public bool PrintPluginsWelcomeMsg { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not plugins should be auto-updated.
        /// </summary>
        public bool AutoUpdatePlugins { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether or not the debug log should scroll automatically.
        /// </summary>
        public bool LogAutoScroll { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not the debug log should open at startup.
        /// </summary>
        public bool LogOpenAtStartup { get; set; }

        /// <summary>
        /// Load a configuration from the provided path.
        /// </summary>
        /// <param name="path">The path to load the configuration file from.</param>
        /// <returns>The deserialized configuration file.</returns>
        public static DalamudConfiguration Load(string path)
        {
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

            deserialized.configPath = path;

            return deserialized;
        }

        /// <summary>
        /// Save the configuration at the path it was loaded from.
        /// </summary>
        public void Save()
        {
            File.WriteAllText(this.configPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
