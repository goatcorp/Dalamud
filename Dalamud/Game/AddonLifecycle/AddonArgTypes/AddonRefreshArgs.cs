using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon;

/// <summary>
/// Addon argument data for Finalize events.
/// </summary>
public class AddonRefreshArgs : AddonArgs
{
    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Refresh;
    
    /// <summary>
    /// Gets the number of AtkValues.
    /// </summary>
    public uint AtkValueCount { get; init; }
    
    /// <summary>
    /// Gets the address of the AtkValue array.
    /// </summary>
    public nint AtkValues { get; init; }
        
    /// <summary>
    /// Gets the AtkValues in the form of a span.
    /// </summary>
    public unsafe Span<AtkValue> AtkValueSpan => new(this.AtkValues.ToPointer(), (int)this.AtkValueCount);
}
