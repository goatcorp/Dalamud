using CommandLine;

namespace Dalamud.Broker.Commands;

[Verb("launch")]
internal sealed record LaunchCommandOptions
{
    
    [Option("game", Required = true, HelpText = GameHelpText)]
    public required string Game { get; init; }
    
    [Option("dalamud-working-directory", Required = true, HelpText = DalamudWorkingDirectoryHelpText)]
    public required string DalamudWorkingDirectory { get; init; }

    [Option("dalamud-configuration-path", HelpText = DalamudConfigurationPathHelpText)]
    public string DalamudConfigurationPath { get; init; }

    [Option("dalamud-plugin-directory", HelpText = DalamudPluginDirectoryHelpText)]
    public string DalamudPluginDirectory { get; init; }

    [Option("dalamud-dev-plugin-directory", HelpText = DalamudDevPluginDirectoryHelpText)]
    public string DalamudDevPluginDirectory { get; init; }

    [Option("dalamud-asset-directory", HelpText = DalamudAssetDirectoryHelpText)]
    public string DalamudAssetDirectory { get; init; }

    [Option("dalamud-client-language")]
    public int DalamudClientLanguage { get; init; }

    [Option("dalamud-delay-initialize", Default = 0)]
    public int DalamudDelayInitialize { get; init; }

    [Option("dalamud-tspack-b64")]
    public required string? DalamudTsPackB64 { get; init; }

    [Option("no-dalamud", HelpText = NoDalamudHelpText)]
    public bool NoDalamud { get; init; }

    [Option("no-plugin", HelpText = NoPluginHelpText)]
    public bool NoPlugin { get; init; }

    [Option("no-3rd-plugin", HelpText = NoThirdPartyPluginHelpText)]
    public bool NoThirdPartyPlugin { get; init; }

    #region Help Text
    
    private const string GameHelpText = """
        The path to the game executable. (i.e. ffxiv_dx11.exe)
        """;

    private const string DalamudWorkingDirectoryHelpText = """

        """;

    private const string DalamudConfigurationPathHelpText = """
        If this is omitted then the default value "%appdata%/XIVLauncher/dalamudConfig.json" will be used.
        """;

    private const string DalamudPluginDirectoryHelpText = """

        """;

    private const string DalamudDevPluginDirectoryHelpText = """

        """;

    private const string DalamudAssetDirectoryHelpText = """
    
        """;
    
    private const string NoDalamudHelpText = """
        Do not load Dalamud.
        """;

    private const string NoPluginHelpText = """
        Do not load plugins.
        """;

    private const string NoThirdPartyPluginHelpText = """
        Do not load third-party plugins.
        """;
    
    #endregion
    
    public LaunchCommandOptions()
    {
        var xlRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                       "XIVLauncher");
        
        // Set default values
        this.DalamudConfigurationPath = Path.Combine(xlRoot, "dalamudConfig.json");
        this.DalamudPluginDirectory = Path.Combine(xlRoot, "installedPlugins");
        this.DalamudDevPluginDirectory = Path.Combine(xlRoot, "devPlugins");
        this.DalamudAssetDirectory = Path.Combine(xlRoot, "dalamudAssets", "dev");
    }
}
