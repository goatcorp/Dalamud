using CommandLine;

namespace Dalamud.Broker.Commands;

[Verb("setup")]
internal sealed record SetupCommandOptions
{
    [Option("game", Required = true)]
    public required string GameDirectory { get; set; }
}
