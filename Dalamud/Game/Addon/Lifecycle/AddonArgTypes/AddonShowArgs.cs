namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Show events.
/// </summary>
public class AddonShowArgs : AddonArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonShowArgs"/> class.
    /// </summary>
    internal AddonShowArgs()
    {
    }

    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Show;

    /// <summary>
    /// Gets or sets a value indicating whether the window should play open sound effects.
    /// </summary>
    public bool SilenceOpenSoundEffect { get; set; }

    /// <summary>
    /// Gets or sets the flags that the window will unset when it Shows/Hides.
    /// </summary>
    public uint UnsetShowHideFlags { get; set; }
}
