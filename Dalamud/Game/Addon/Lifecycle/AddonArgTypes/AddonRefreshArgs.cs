using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Refresh events.
/// </summary>
public class AddonRefreshArgs : AddonArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonRefreshArgs"/> class.
    /// </summary>
    [Obsolete("Not intended for public construction.", false)]
    public AddonRefreshArgs()
    {
    }

    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Refresh;

    /// <summary>
    /// Gets or sets the number of AtkValues.
    /// </summary>
    public uint AtkValueCount { get; set; }

    /// <summary>
    /// Gets or sets the address of the AtkValue array.
    /// </summary>
    public nint AtkValues { get; set; }

    /// <summary>
    /// Gets the AtkValues in the form of a span.
    /// </summary>
    public unsafe Span<AtkValue> AtkValueSpan => new(this.AtkValues.ToPointer(), (int)this.AtkValueCount);

    /// <inheritdoc cref="AddonArgs.Clear"/>
    internal override void Clear()
    {
        base.Clear();
        this.AtkValueCount = 0;
        this.AtkValues = 0;
    }
}
