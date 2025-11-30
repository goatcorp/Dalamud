using Dalamud.Utility;

namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for ReceiveEvent events.
/// </summary>
public class AddonReceiveEventArgs : AddonArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonReceiveEventArgs"/> class.
    /// </summary>
    internal AddonReceiveEventArgs()
    {
    }

    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.ReceiveEvent;

    /// <summary>
    /// Gets or sets the AtkEventType for this event message.
    /// </summary>
    public byte AtkEventType { get; set; }

    /// <summary>
    /// Gets or sets the event id for this event message.
    /// </summary>
    public int EventParam { get; set; }

    /// <summary>
    /// Gets or sets the pointer to an AtkEvent for this event message.
    /// </summary>
    public nint AtkEvent { get; set; }

    /// <summary>
    /// Gets or sets the pointer to an AtkEventData for this event message.
    /// </summary>
    [Api14ToDo("Rename to AtkEventData")]
    public nint Data { get; set; }
}
