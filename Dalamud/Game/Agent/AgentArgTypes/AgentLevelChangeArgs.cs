namespace Dalamud.Game.Agent.AgentArgTypes;

/// <summary>
/// Agent argument data for game events.
/// </summary>
public class AgentLevelChangeArgs : AgentArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentLevelChangeArgs"/> class.
    /// </summary>
    internal AgentLevelChangeArgs()
    {
    }

    /// <inheritdoc/>
    public override AgentArgsType Type => AgentArgsType.LevelChange;

    /// <summary>
    /// Gets or sets a value indicating which ClassJob was switched to.
    /// </summary>
    public byte ClassJobId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating what the new level is.
    /// </summary>
    public ushort Level { get; set; }
}
