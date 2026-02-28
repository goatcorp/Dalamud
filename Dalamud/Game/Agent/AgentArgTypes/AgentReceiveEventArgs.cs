namespace Dalamud.Game.Agent.AgentArgTypes;

/// <summary>
/// Agent argument data for ReceiveEvent events.
/// </summary>
public class AgentReceiveEventArgs : AgentArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentReceiveEventArgs"/> class.
    /// </summary>
    internal AgentReceiveEventArgs()
    {
    }

    /// <inheritdoc/>
    public override AgentArgsType Type => AgentArgsType.ReceiveEvent;

    /// <summary>
    /// Gets or sets the AtkValue return value for this event message.
    /// </summary>
    public nint ReturnValue { get; set; }

    /// <summary>
    /// Gets or sets the AtkValue array for this event message.
    /// </summary>
    public nint AtkValues { get; set; }

    /// <summary>
    /// Gets or sets the AtkValue count for this event message.
    /// </summary>
    public uint ValueCount { get; set; }

    /// <summary>
    /// Gets or sets the event kind for this event message.
    /// </summary>
    public ulong EventKind { get; set; }
}
