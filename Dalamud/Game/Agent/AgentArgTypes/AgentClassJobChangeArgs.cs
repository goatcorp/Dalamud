namespace Dalamud.Game.Agent.AgentArgTypes;

/// <summary>
/// Agent argument data for game events.
/// </summary>
public class AgentClassJobChangeArgs : AgentArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentClassJobChangeArgs"/> class.
    /// </summary>
    internal AgentClassJobChangeArgs()
    {
    }

    /// <inheritdoc/>
    public override AgentArgsType Type => AgentArgsType.ClassJobChange;

    /// <summary>
    /// Gets or sets a value indicating what the new ClassJob is.
    /// </summary>
    public byte ClassJobId { get; set; }
}
