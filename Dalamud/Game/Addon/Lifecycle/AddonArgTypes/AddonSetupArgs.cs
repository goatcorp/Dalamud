using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Setup events.
/// </summary>
public class AddonSetupArgs : AddonArgs, ICloneable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonSetupArgs"/> class.
    /// </summary>
    [Obsolete("Not intended for public construction.", false)]
    public AddonSetupArgs()
    {
    }

    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Setup;

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

    /// <inheritdoc cref="ICloneable.Clone"/>
    public AddonSetupArgs Clone() => (AddonSetupArgs)this.MemberwiseClone();

    /// <inheritdoc cref="Clone"/>
    object ICloneable.Clone() => this.Clone();
}
