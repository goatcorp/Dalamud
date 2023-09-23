namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Finalize events.
/// </summary>
public class AddonRequestedUpdateArgs : AddonArgs
{
    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.RequestedUpdate;
    
    /// <summary>
    /// Gets the NumberArrayData** for this event.
    /// </summary>
    public nint NumberArrayData { get; init; }
    
    /// <summary>
    /// Gets the StringArrayData** for this event.
    /// </summary>
    public nint StringArrayData { get; init; }
}
