namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for OnRequestedUpdate events.
/// </summary>
public class AddonRequestedUpdateArgs : AddonArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonRequestedUpdateArgs"/> class.
    /// </summary>
    [Obsolete("Not intended for public construction.", false)]
    public AddonRequestedUpdateArgs()
    {
    }

    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.RequestedUpdate;

    /// <summary>
    /// Gets or sets the NumberArrayData** for this event.
    /// </summary>
    public nint NumberArrayData { get; set; }

    /// <summary>
    /// Gets or sets the StringArrayData** for this event.
    /// </summary>
    public nint StringArrayData { get; set; }

    /// <inheritdoc cref="AddonArgs.Clear"/>
    internal override void Clear()
    {
        base.Clear();
        this.NumberArrayData = 0;
        this.StringArrayData = 0;
    }
}
