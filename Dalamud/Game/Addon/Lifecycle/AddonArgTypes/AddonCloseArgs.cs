namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Close events.
/// </summary>
public class AddonCloseArgs : AddonArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonCloseArgs"/> class.
    /// </summary>
    internal AddonCloseArgs()
    {
    }

    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Close;

    /// <summary>
    /// Gets or sets a value indicating whether the window should fire the callback method on close.
    /// </summary>
    public bool FireCallback { get; set; }
}
