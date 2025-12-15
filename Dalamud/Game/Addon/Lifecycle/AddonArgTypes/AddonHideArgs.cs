namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Hide events.
/// </summary>
public class AddonHideArgs : AddonArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonHideArgs"/> class.
    /// </summary>
    internal AddonHideArgs()
    {
    }

    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Hide;

    /// <summary>
    /// Gets or sets a value indicating whether to call the hide callback handler when this hides.
    /// </summary>
    public bool CallHideCallback { get; set; }

    /// <summary>
    /// Gets or sets the flags that the window will set when it Shows/Hides.
    /// </summary>
    public uint SetShowHideFlags { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether something for this event message.
    /// </summary>
    internal bool UnknownBool { get; set; }
}
