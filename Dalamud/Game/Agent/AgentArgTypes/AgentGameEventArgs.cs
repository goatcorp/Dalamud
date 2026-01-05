namespace Dalamud.Game.Agent.AgentArgTypes;

/// <summary>
/// Agent argument data for game events.
/// </summary>
public class AgentGameEventArgs : AgentArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentGameEventArgs"/> class.
    /// </summary>
    internal AgentGameEventArgs()
    {
    }

    /// <inheritdoc/>
    public override AgentArgsType Type => AgentArgsType.GameEvent;

    /// <summary>
    /// Gets or sets a value representing which gameEvent was triggered.
    /// </summary>
    public int GameEvent { get; set; }
}
