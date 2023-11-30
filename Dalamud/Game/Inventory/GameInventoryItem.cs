using System.Runtime.CompilerServices;

using FFXIVClientStructs.FFXIV.Client.Game;

namespace Dalamud.Game.Inventory;

/// <summary>
/// Dalamud wrapper around a ClientStructs InventoryItem.
/// </summary>
public unsafe class GameInventoryItem
{
    private InventoryItem internalItem;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameInventoryItem"/> class.
    /// </summary>
    /// <param name="item">Inventory item to wrap.</param>
    internal GameInventoryItem(InventoryItem item)
    {
        this.internalItem = item;
    }

    /// <summary>
    /// Gets the container inventory type.
    /// </summary>
    public GameInventoryType ContainerType => (GameInventoryType)this.internalItem.Container;

    /// <summary>
    /// Gets the inventory slot index this item is in.
    /// </summary>
    public uint InventorySlot => (uint)this.internalItem.Slot;

    /// <summary>
    /// Gets the item id.
    /// </summary>
    public uint ItemId => this.internalItem.ItemID;

    /// <summary>
    /// Gets the quantity of items in this item stack.
    /// </summary>
    public uint Quantity => this.internalItem.Quantity;

    /// <summary>
    /// Gets the spiritbond of this item.
    /// </summary>
    public uint Spiritbond => this.internalItem.Spiritbond;

    /// <summary>
    /// Gets the repair condition of this item.
    /// </summary>
    public uint Condition => this.internalItem.Condition;

    /// <summary>
    /// Gets a value indicating whether the item is High Quality.
    /// </summary>
    public bool IsHq => this.internalItem.Flags.HasFlag(InventoryItem.ItemFlags.HQ);

    /// <summary>
    /// Gets a value indicating whether the  item has a company crest applied.
    /// </summary>
    public bool IsCompanyCrestApplied => this.internalItem.Flags.HasFlag(InventoryItem.ItemFlags.CompanyCrestApplied);
    
    /// <summary>
    /// Gets a value indicating whether the item is a relic.
    /// </summary>
    public bool IsRelic => this.internalItem.Flags.HasFlag(InventoryItem.ItemFlags.Relic);

    /// <summary>
    /// Gets a value indicating whether the is a collectable.
    /// </summary>
    public bool IsCollectable => this.internalItem.Flags.HasFlag(InventoryItem.ItemFlags.Collectable);

    /// <summary>
    /// Gets the array of materia types.
    /// </summary>
    public ReadOnlySpan<ushort> Materia => new(Unsafe.AsPointer(ref this.internalItem.Materia[0]), 5);
    
    /// <summary>
    /// Gets the array of materia grades.
    /// </summary>
    public ReadOnlySpan<ushort> MateriaGrade => new(Unsafe.AsPointer(ref this.internalItem.MateriaGrade[0]), 5);

    /// <summary>
    /// Gets the color used for this item.
    /// </summary>
    public byte Stain => this.internalItem.Stain;

    /// <summary>
    /// Gets the glamour id for this item.
    /// </summary>
    public uint GlmaourId => this.internalItem.GlamourID;
    
    /// <summary>
    /// Gets the items crafter's content id.
    /// NOTE: I'm not sure if this is a good idea to include or not in the dalamud api. Marked internal for now.
    /// </summary>
    internal ulong CrafterContentId => this.internalItem.CrafterContentID;
}
