using CommandLine;

namespace Dalamud.Broker.Commands;

[Verb("launch")]
internal sealed record LaunchCommandOptions
{
    [Option("game", Required = true)]
    public required string Game { get; init; }

    [Option("mode")]
    public required string Mode { get; init; }

    [Option("handle-owner")]
    public required long HandleOwner { get; init; }

    [Option("dalamud-working-directory", Required = true)]
    public required string DalamudWorkingDirectory { get; init; }

    [Option("dalamud-configuration-path", Required = true)]
    public required string DalamudConfigurationPath { get; init; }

    [Option("dalamud-plugin-directory", Required = true)]
    public required string DalamudPluginDirectory { get; init; }

    [Option("dalamud-dev-plugin-directory", Required = true)]
    public required string DalamudDevPluginDirectory { get; init; }

    [Option("dalamud-asset-directory", Required = true)]
    public required string DalamudAssetDirectory { get; init; }

    [Option("dalamud-client-language", Required = true)]
    public required int DalamudClientLanguage { get; init; }

    [Option("dalamud-delay-initialize", Default = 0)]
    public int DalamudDelayInitialize { get; init; }

    [Option("dalamud-tspack-b64")]
    public required string? DalamudTsPackB64 { get; init; }

    [Option("without-dalamud")]
    public bool WithoutDalamud { get; init; }

    [Option("fake-arguments")]
    public bool FakeArguments { get; init; }

    [Option("no-plugin")]
    public bool NoPlugin { get; init; }

    [Option("no-3rd-plugin")]
    public bool NoThirdPartyPlugin { get; init; }
}
