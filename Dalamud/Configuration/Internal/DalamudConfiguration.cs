using System;
using System.Collections.Generic;
using System.IO;

using Dalamud.Game.Text;
using Dalamud.Interface.Style;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;

namespace Dalamud.Configuration.Internal
{
    /// <summary>
    /// Class containing Dalamud settings.
    /// </summary>
    [Serializable]
    internal sealed class DalamudConfiguration
    {
        /// <summary>
        /// Currently used beta key for Dalamud staging builds.
        /// </summary>
        public const string DalamudCurrentBetaKey = "Testing6015";

        private static readonly JsonSerializerSettings SerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            Formatting = Formatting.Indented,
        };

        [JsonIgnore]
        private string configPath;

        /// <summary>
        /// Delegate for the <see cref="DalamudConfiguration.DalamudConfigurationSaved"/> event that occurs when the dalamud configuration is saved.
        /// </summary>
        /// <param name="dalamudConfiguration">The current dalamud configuration.</param>
        public delegate void DalamudConfigurationSavedDelegate(DalamudConfiguration dalamudConfiguration);

        /// <summary>
        /// Event that occurs when dalamud configuration is saved.
        /// </summary>
        public event DalamudConfigurationSavedDelegate DalamudConfigurationSaved;

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
        public string LanguageOverride { get; set; } = null;

        /// <summary>
        /// Gets or sets the last loaded Dalamud version.
        /// </summary>
        public string LastVersion { get; set; } = null;

        /// <summary>
        /// Gets or sets the last loaded Dalamud version.
        /// </summary>
        public string LastChangelogMajorMinor { get; set; } = null;

        /// <summary>
        /// Gets or sets the chat type used by default for plugin messages.
        /// </summary>
        public XivChatType GeneralChatType { get; set; } = XivChatType.Debug;

        /// <summary>
        /// Gets or sets a value indicating whether or not plugin testing builds should be shown.
        /// </summary>
        public bool DoPluginTest { get; set; } = false;

        /// <summary>
        /// Gets or sets a key to opt into Dalamud staging builds.
        /// </summary>
        public string? DalamudBetaKey { get; set; } = null;

        /// <summary>
        /// Gets or sets a list of custom repos.
        /// </summary>
        public List<ThirdPartyRepoSettings> ThirdRepoList { get; set; } = new();

        /// <summary>
        /// Gets or sets a list of hidden plugins.
        /// </summary>
        public List<string> HiddenPluginInternalName { get; set; } = new();

        /// <summary>
        /// Gets or sets a list of seen plugins.
        /// </summary>
        public List<string> SeenPluginInternalName { get; set; } = new();

        /// <summary>
        /// Gets or sets a list of additional settings for devPlugins. The key is the absolute path
        /// to the plugin DLL. This is automatically generated for any plugins in the devPlugins folder.
        /// However by specifiying this value manually, you can add arbitrary files outside the normal
        /// file paths.
        /// </summary>
        public Dictionary<string, DevPluginSettings> DevPluginSettings { get; set; } = new();

        /// <summary>
        /// Gets or sets a list of additional locations that dev plugins should be loaded from. This can
        /// be either a DLL or folder, but should be the absolute path, or a path relative to the currently
        /// injected Dalamud instance.
        /// </summary>
        public List<DevPluginLocationSettings> DevPluginLoadLocations { get; set; } = new();

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
        public bool AutoUpdatePlugins { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not Dalamud should add buttons to the system menu.
        /// </summary>
        public bool DoButtonsSystemMenu { get; set; } = true;

        /// <summary>
        /// Gets or sets the default Dalamud debug log level on startup.
        /// </summary>
        public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;

        /// <summary>
        /// Gets or sets a value indicating whether or not the debug log should scroll automatically.
        /// </summary>
        public bool LogAutoScroll { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not the debug log should open at startup.
        /// </summary>
        public bool LogOpenAtStartup { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not ImGui asserts should be enabled at startup.
        /// </summary>
        public bool AssertsEnabledAtStartup { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not docking should be globally enabled in ImGui.
        /// </summary>
        public bool IsDocking { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether viewports should always be disabled.
        /// </summary>
        public bool IsDisableViewport { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not navigation via a gamepad should be globally enabled in ImGui.
        /// </summary>
        public bool IsGamepadNavigationEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not focus management is enabled.
        /// </summary>
        public bool IsFocusManagementEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not the anti-anti-debug check is enabled on startup.
        /// </summary>
        public bool IsAntiAntiDebugEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the kind of beta to download when <see cref="DalamudBetaKey"/> matches the server value.
        /// </summary>
        public string DalamudBetaKind { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not all plugins, regardless of API level, should be loaded.
        /// </summary>
        public bool LoadAllApiLevels { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not banned plugins should be loaded.
        /// </summary>
        public bool LoadBannedPlugins { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not any plugin should be loaded when the game is started.
        /// It is reset immediately when read.
        /// </summary>
        public bool PluginSafeMode { get; set; }

        /// <summary>
        /// Gets or sets a list of saved styles.
        /// </summary>
        [JsonProperty("SavedStyles")]
        public List<StyleModelV1>? SavedStylesOld { get; set; }

        /// <summary>
        /// Gets or sets a list of saved styles.
        /// </summary>
        [JsonProperty("SavedStylesVersioned")]
        public List<StyleModel>? SavedStyles { get; set; }

        /// <summary>
        /// Gets or sets the name of the currently chosen style.
        /// </summary>
        public string ChosenStyle { get; set; } = "Dalamud Standard";

        /// <summary>
        /// Gets or sets a value indicating whether or not Dalamud RMT filtering should be disabled.
        /// </summary>
        public bool DisableRmtFiltering { get; set; }

        /// <summary>
        /// Load a configuration from the provided path.
        /// </summary>
        /// <param name="path">The path to load the configuration file from.</param>
        /// <returns>The deserialized configuration file.</returns>
        public static DalamudConfiguration Load(string path)
        {
            DalamudConfiguration deserialized = null;
            try
            {
                deserialized = JsonConvert.DeserializeObject<DalamudConfiguration>(File.ReadAllText(path), SerializerSettings);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to load DalamudConfiguration at {0}", path);
            }

            deserialized ??= new DalamudConfiguration();
            deserialized.configPath = path;

            return deserialized;
        }

        /// <summary>
        /// Save the configuration at the path it was loaded from.
        /// </summary>
        public void Save()
        {
            File.WriteAllText(this.configPath, JsonConvert.SerializeObject(this, SerializerSettings));
            this.DalamudConfigurationSaved?.Invoke(this);
        }
    }
}
