using Dalamud.Common.Game;
using Newtonsoft.Json;

namespace Dalamud.Common;

/// <summary>
/// Struct containing information needed to initialize Dalamud.
/// Modify DalamudStartInfo.h and DalamudStartInfo.cpp along with this record.
/// </summary>
[Serializable]
public record DalamudStartInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudStartInfo"/> class.
    /// </summary>
    public DalamudStartInfo()
    {
        // ignored
    }

    /// <summary>
    /// Gets or sets the Dalamud load method.
    /// </summary>
    public LoadMethod LoadMethod { get; set; }

    /// <summary>
    /// Gets or sets the working directory of the XIVLauncher installations.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets the path to the configuration file.
    /// </summary>
    public string? ConfigurationPath { get; set; }

    /// <summary>
    /// Gets or sets the path of the log files.
    /// </summary>
    public string? LogPath { get; set; }

    /// <summary>
    /// Gets or sets the name of the log file.
    /// </summary>
    public string? LogName { get; set; }

    /// <summary>
    /// Gets or sets the path to the directory for installed plugins.
    /// </summary>
    public string? PluginDirectory { get; set; }

    /// <summary>
    /// Gets or sets the path to core Dalamud assets.
    /// </summary>
    public string? AssetDirectory { get; set; }

    /// <summary>
    /// Gets or sets the language of the game client.
    /// </summary>
    public ClientLanguage Language { get; set; } = ClientLanguage.English;

    /// <summary>
    /// Gets or sets the current game version code.
    /// </summary>
    [JsonConverter(typeof(GameVersionConverter))]
    public GameVersion? GameVersion { get; set; }

    /// <summary>
    /// Gets or sets troubleshooting information to attach when generating a tspack file.
    /// </summary>
    public string? TroubleshootingPackData { get; set; }

    /// <summary>
    /// Gets or sets a value that specifies how much to wait before a new Dalamud session.
    /// </summary>
    public int DelayInitializeMs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether no plugins should be loaded.
    /// </summary>
    public bool NoLoadPlugins { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether no third-party plugins should be loaded.
    /// </summary>
    public bool NoLoadThirdPartyPlugins { get; set; }

    /// <summary>
    /// Gets or sets the path the boot log file is supposed to be written to.
    /// </summary>
    public string? BootLogPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a Boot console should be shown.
    /// </summary>
    public bool BootShowConsole { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the fallback console should be shown, if needed.
    /// </summary>
    public bool BootDisableFallbackConsole { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating where Dalamud should wait with a message box.
    /// </summary>
    public int BootWaitMessageBox { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Dalamud should wait for a debugger to be attached before initializing.
    /// </summary>
    public bool BootWaitDebugger { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the VEH should be enabled.
    /// </summary>
    public bool BootVehEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the VEH should be doing full crash dumps.
    /// </summary>
    public bool BootVehFull { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether ETW should be enabled.
    /// </summary>
    public bool BootEnableEtw { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable legacy corrupted state exceptions.
    /// </summary>
    public bool BootDisableLegacyCorruptedStateExceptions { get; set; }

    /// <summary>
    /// Gets or sets a value choosing the OpenProcess hookmode.
    /// </summary>
    public int BootDotnetOpenProcessHookMode { get; set; }

    /// <summary>
    /// Gets or sets a list of enabled game fixes.
    /// </summary>
    public List<string>? BootEnabledGameFixes { get; set; }

    /// <summary>
    /// Gets or sets a list of DLLs that should be unhooked.
    /// </summary>
    public List<string>? BootUnhookDlls { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show crash handler console window.
    /// </summary>
    public bool CrashHandlerShow { get; set; }

    /// <summary>
    /// Gets or sets a value indicating how to deal with unhandled exceptions.
    /// </summary>
    public UnhandledExceptionHandlingMode UnhandledException { get; set; }
}
