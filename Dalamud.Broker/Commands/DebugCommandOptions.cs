using CommandLine;

namespace Dalamud.Broker.Commands;

[Verb("debug", Hidden = true)]
internal sealed class DebugCommandOptions
{
    [Option("game")]
    public string? ExecutablePath { get; init; }
}
