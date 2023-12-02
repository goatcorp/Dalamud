using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Refresh events.
/// </summary>
public class AddonRefreshArgs : AddonArgs, ICloneable
{
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

    /// <inheritdoc cref="ICloneable.Clone"/>
    public AddonRefreshArgs Clone() => (AddonRefreshArgs)this.MemberwiseClone();
    
    /// <inheritdoc cref="Clone"/>
    object ICloneable.Clone() => this.Clone();
}
