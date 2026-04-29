namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for OnFocusChanged events.
/// </summary>
public class AddonFocusChangedArgs : AddonArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonFocusChangedArgs"/> class.
    /// </summary>
    internal AddonFocusChangedArgs()
    {
    }

    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.FocusChanged;

    /// <summary>
    /// Gets or sets a value indicating whether the window is being focused or unfocused.
    /// </summary>
    public bool ShouldFocus { get; set; }
}
