using System.Collections.Generic;

using Dalamud.Game.NativeWrapper;
using Dalamud.Utility;

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
    internal AddonRefreshArgs()
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
    [Obsolete("Pending removal, Use AtkValueEnumerable instead.")]
    [Api15ToDo("Make this internal, remove obsolete")]
    public unsafe Span<AtkValue> AtkValueSpan => new(this.AtkValues.ToPointer(), (int)this.AtkValueCount);

    /// <summary>
    /// Gets an enumerable collection of <see cref="AtkValuePtr"/> of the event's AtkValues.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of <see cref="AtkValuePtr"/> corresponding to the event's AtkValues.
    /// </returns>
    public IEnumerable<AtkValuePtr> AtkValueEnumerable
    {
        get
        {
            for (var i = 0; i < this.AtkValueCount; i++)
            {
                AtkValuePtr ptr;
                unsafe
                {
                    ptr = new AtkValuePtr((nint)this.AtkValueSpan[i].Pointer);
                }

                yield return ptr;
            }
        }
    }
}
