using CommandLine;

namespace Dalamud.Broker.Commands;

[Verb("setup")]
internal sealed record SetupCommandOptions
{
    [Option("game-root", Required = true, HelpText = BaseGameDirectoryHelp)]
    public required string BaseGameDirectory { get; set; }

    private const string BaseGameDirectoryHelp = """
        A path to where the root of the game directory is. (e.g. /Games/FINAL FANTASY XIV)
        """;

    [Option("game-config", HelpText = GameConfigDirectoryHelp)]
    public string? GameConfigDirectory { get; set; }

    private const string GameConfigDirectoryHelp = """
        A path to where the game configurations are stored.
        If ommited, "%userprofile%/Documents/My Games\FINAL FANTASY XIV - A Realm Reborn" will be used.
        """;

    [Option("xl-data", HelpText = XlDataDirectoryHelp)]
    public string? XlDataDirectory { get; set; }

    private const string XlDataDirectoryHelp = """
        A path to where the XivLauncher data is stored.
        If ommited, "%appdata%/XIVLauncher" will be used.
        """;

    [Option("dalamud-bin", HelpText = XlDataDirectoryHelp)]
    public string? DalamudBinaryDirectory { get; set; }

    private const string DalamudBinaryDirectoryHelp = """
        A path to where the Dalamud binaries (e.g. Dalamud.dll) are stored.
        If ommited, current directory (i.e. where Dalamud.Broker is stored) will be used.
        """;
}
