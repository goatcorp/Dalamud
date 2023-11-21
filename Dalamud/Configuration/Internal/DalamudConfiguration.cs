using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;

using Dalamud.Game.Text;
using Dalamud.Interface.Internal.Windows.PluginInstaller;
using Dalamud.Interface.Style;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Internal.Profiles;
using Dalamud.Storage;
using Dalamud.Utility;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;

namespace Dalamud.Configuration.Internal;

/// <summary>
/// Class containing Dalamud settings.
/// </summary>
[Serializable]
[ServiceManager.Service]
#pragma warning disable SA1015
[InherentDependency<ReliableFileStorage>] // We must still have this when unloading
#pragma warning restore SA1015
internal sealed class DalamudConfiguration : IServiceType, IDisposable
{
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        Formatting = Formatting.Indented,
    };

    [JsonIgnore]
    private string? configPath;

    [JsonIgnore]
    private bool isSaveQueued;

    /// <summary>
    /// Delegate for the <see cref="DalamudConfiguration.DalamudConfigurationSaved"/> event that occurs when the dalamud configuration is saved.
    /// </summary>
    /// <param name="dalamudConfiguration">The current dalamud configuration.</param>
    public delegate void DalamudConfigurationSavedDelegate(DalamudConfiguration dalamudConfiguration);

    /// <summary>
    /// Event that occurs when dalamud configuration is saved.
    /// </summary>
    public event DalamudConfigurationSavedDelegate? DalamudConfigurationSaved;

    /// <summary>
    /// Gets or sets a list of muted works.
    /// </summary>
    public List<string>? BadWords { get; set; }

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
    public string? LanguageOverride { get; set; } = null;

    /// <summary>
    /// Gets or sets the last loaded Dalamud version.
    /// </summary>
    public string? LastVersion { get; set; } = null;

    /// <summary>
    /// Gets or sets a value indicating the last seen FTUE version.
    /// Unused for now, added to prevent existing users from seeing level 0 FTUE.
    /// </summary>
    public int SeenFtueLevel { get; set; } = 1;

    /// <summary>
    /// Gets or sets the last loaded Dalamud version.
    /// </summary>
    public string? LastChangelogMajorMinor { get; set; } = null;

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
    /// Gets or sets a value indicating whether or not a disclaimer regarding third-party repos has been dismissed.
    /// </summary>
    public bool? ThirdRepoSpeedbumpDismissed { get; set; } = null;

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
    /// Gets or sets a value indicating whether to use AXIS fonts from the game.
    /// </summary>
    public bool UseAxisFontsFromGame { get; set; } = false;

    /// <summary>
    /// Gets or sets the gamma value to apply for Dalamud fonts. Effects text thickness.
    ///
    /// Before gamma is applied...
    /// * ...TTF fonts loaded with stb or FreeType are in linear space.
    /// * ...the game's prebaked AXIS fonts are in gamma space with gamma value of 1.4.
    /// </summary>
    public float FontGammaLevel { get; set; } = 1.4f;

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
    /// Gets or sets a value indicating whether or not a message containing Dalamud's current version and the number of loaded plugins should be sent at login.
    /// </summary>
    public bool PrintDalamudWelcomeMsg { get; set; } = true;

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
    /// Gets or sets a value indicating whether to write to log files synchronously.
    /// </summary>
    public bool LogSynchronously { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not the debug log should scroll automatically.
    /// </summary>
    public bool LogAutoScroll { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not the debug log should open at startup.
    /// </summary>
    public bool LogOpenAtStartup { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not the dev bar should open at startup.
    /// </summary>
    public bool DevBarOpenAtStartup { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not ImGui asserts should be enabled at startup.
    /// </summary>
    public bool AssertsEnabledAtStartup { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not docking should be globally enabled in ImGui.
    /// </summary>
    public bool IsDocking { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether or not plugin user interfaces should trigger sound effects.
    /// This setting is effected by the in-game "System Sounds" option and volume.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "ABI")]
    public bool EnablePluginUISoundEffects { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not an additional button allowing pinning and clickthrough options should be shown
    /// on plugin title bars when using the Window System.
    /// </summary>
    [JsonProperty("EnablePluginUiAdditionalOptionsExperimental")]
    public bool EnablePluginUiAdditionalOptions { get; set; } = false;

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
    /// Gets or sets a value indicating whether to resume game main thread after plugins load.
    /// </summary>
    public bool IsResumeGameAfterPluginLoad { get; set; } = false;

    /// <summary>
    /// Gets or sets the kind of beta to download when <see cref="DalamudBetaKey"/> matches the server value.
    /// </summary>
    public string? DalamudBetaKind { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not any plugin should be loaded when the game is started.
    /// It is reset immediately when read.
    /// </summary>
    public bool PluginSafeMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the wait time between plugin unload and plugin assembly unload.
    /// Uses default value that may change between versions if set to null.
    /// </summary>
    public int? PluginWaitBeforeFree { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not crashes during shutdown should be reported.
    /// </summary>
    public bool ReportShutdownCrashes { get; set; }

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
    /// Gets or sets a list of saved plugin profiles.
    /// </summary>
    public List<ProfileModel>? SavedProfiles { get; set; }

    /// <summary>
    /// Gets or sets the default plugin profile.
    /// </summary>
    public ProfileModel? DefaultProfile { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not profiles are enabled.
    /// </summary>
    public bool ProfilesEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not the user has seen the profiles tutorial.
    /// </summary>
    public bool ProfilesHasSeenTutorial { get; set; } = false;
    
    /// <summary>
    /// Gets or sets a value indicating whether or not Dalamud RMT filtering should be disabled.
    /// </summary>
    public bool DisableRmtFiltering { get; set; }

    /// <summary>
    /// Gets or sets the order of DTR elements, by title.
    /// </summary>
    public List<string>? DtrOrder { get; set; }

    /// <summary>
    /// Gets or sets the list of ignored DTR elements, by title.
    /// </summary>
    public List<string>? DtrIgnore { get; set; }

    /// <summary>
    /// Gets or sets the spacing used for DTR entries.
    /// </summary>
    public int DtrSpacing { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether to swap the
    /// direction in which elements are drawn in the DTR.
    /// False indicates that elements will be drawn from the end of
    /// the left side of the Server Info bar, and continue leftwards.
    /// True indicates the opposite.
    /// </summary>
    public bool DtrSwapDirection { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the title screen menu is shown.
    /// </summary>
    public bool ShowTsm { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not market board data should be uploaded.
    /// </summary>
    public bool IsMbCollect { get; set; } = true;

    /// <summary>
    /// Gets the ISO 639-1 two-letter code for the language of the effective Dalamud display language.
    /// </summary>
    public string EffectiveLanguage
    {
        get
        {
            var languages = Localization.ApplicableLangCodes.Prepend("en").ToArray();
            try
            {
                if (string.IsNullOrEmpty(this.LanguageOverride))
                {
                    var currentUiLang = CultureInfo.CurrentUICulture;

                    if (Localization.ApplicableLangCodes.Any(x => currentUiLang.TwoLetterISOLanguageName == x))
                        return currentUiLang.TwoLetterISOLanguageName;
                    else
                        return languages[0];
                }
                else
                {
                    return this.LanguageOverride;
                }
            }
            catch (Exception)
            {
                return languages[0];
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not to show info on dev bar.
    /// </summary>
    public bool ShowDevBarInfo { get; set; } = true;

    /// <summary>
    /// Gets or sets the last-used contact details for the plugin feedback form.
    /// </summary>
    public string LastFeedbackContactDetails { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a list of plugins that testing builds should be downloaded for.
    /// </summary>
    public List<PluginTestingOptIn>? PluginTestingOptIns { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the FFXIV window should be toggled to immersive mode.
    /// </summary>
    public bool WindowIsImmersive { get; set; } = false;

    /// <summary>
    /// Gets or sets hitch threshold for game network up in milliseconds.
    /// </summary>
    public double GameNetworkUpHitch { get; set; } = 30;

    /// <summary>
    /// Gets or sets hitch threshold for game network down in milliseconds.
    /// </summary>
    public double GameNetworkDownHitch { get; set; } = 30;

    /// <summary>
    /// Gets or sets hitch threshold for framework update in milliseconds.
    /// </summary>
    public double FrameworkUpdateHitch { get; set; } = 50;

    /// <summary>
    /// Gets or sets hitch threshold for ui builder in milliseconds.
    /// </summary>
    public double UiBuilderHitch { get; set; } = 100;

    /// <summary>
    /// Gets or sets the page of the plugin installer that is shown by default when opened.
    /// </summary>
    public PluginInstallerWindow.PluginInstallerOpenKind PluginInstallerOpen { get; set; } = PluginInstallerWindow.PluginInstallerOpenKind.AllPlugins;

    /// <summary>
    /// Load a configuration from the provided path.
    /// </summary>
    /// <param name="path">Path to read from.</param>
    /// <param name="fs">File storage.</param>
    /// <returns>The deserialized configuration file.</returns>
    public static DalamudConfiguration Load(string path, ReliableFileStorage fs)
    {
        DalamudConfiguration deserialized = null;

        try
        {
            fs.ReadAllText(path, text =>
            {
                deserialized =
                    JsonConvert.DeserializeObject<DalamudConfiguration>(text, SerializerSettings);
                
                // If this reads as null, the file was empty, that's no good
                if (deserialized == null)
                    throw new Exception("Read config was null.");
            });
        }
        catch (FileNotFoundException)
        {
            // ignored
        }
        catch (Exception e)
        {
            Log.Error(e, "Could not load DalamudConfiguration at {Path}, creating new", path);
        }

        deserialized ??= new DalamudConfiguration();
        deserialized.configPath = path;
        
        return deserialized;
    }

    /// <summary>
    /// Save the configuration at the path it was loaded from, at the next frame.
    /// </summary>
    public void QueueSave()
    {
        this.isSaveQueued = true;
    }

    /// <summary>
    /// Immediately save the configuration.
    /// </summary>
    public void ForceSave()
    {
        this.Save();
    }
    
    /// <inheritdoc/>
    public void Dispose()
    {
        // Make sure that we save, if a save is queued while we are shutting down
        this.Update();
    }

    /// <summary>
    /// Save the file, if needed. Only needs to be done once a frame.
    /// </summary>
    internal void Update()
    {
        if (this.isSaveQueued)
        {
            this.Save();
            this.isSaveQueued = false;

            Log.Verbose("Config saved");
        }
    }

    private void Save()
    {
        ThreadSafety.AssertMainThread();
        if (this.configPath is null)
            throw new InvalidOperationException("configPath is not set.");

        Service<ReliableFileStorage>.Get().WriteAllText(
            this.configPath, JsonConvert.SerializeObject(this, SerializerSettings));
        this.DalamudConfigurationSaved?.Invoke(this);
    }
}
