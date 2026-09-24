using Dalamud.Game.Tooltip.Classes;

using FFXIVClientStructs.FFXIV.Component.GUI;

using InteropGenerator.Runtime;

namespace Dalamud.Game.Tooltip.TooltipArgTypes;

/// <summary>
/// Object representing a tooltip args object passed to a plugin to either construct a custom node,
/// or edit/respond to the data provided.
/// </summary>
public class ItemTooltipArgs : TooltipArgs
{
    /// <inheritdoc/>
    public override TooltipType Type => TooltipType.Item;

    /// <summary>
    /// Gets the icon id for this item tooltip.
    /// </summary>
    public uint IconId => (uint)this.NumberArrayData[0];

    /// <summary>
    /// Gets the internal pointer to the NumberArray data as passed from OnRequestedUpdate.
    /// </summary>
    internal unsafe NumberArrayData** NumberArrayDataPointer { get; init; }

    /// <summary>
    /// Gets the number array span.
    /// </summary>
    internal unsafe Span<int> NumberArrayData
        => this.NumberArrayDataPointer[(int)NumberArrayType.ItemDetail]->Span;

    /// <summary>
    /// Gets the internal pointer to the StringArray data as passed from OnRequestedUpdate.
    /// </summary>
    internal unsafe StringArrayData** StringArrayDataRoot { get; init; }

    /// <summary>
    /// Gets the string array span.
    /// </summary>
    internal unsafe Span<CStringPointer> StringArrayData
        => this.StringArrayDataRoot[(int)StringArrayType.ItemDetail]->Span;
}
