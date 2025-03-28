using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Data;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Game;

using Lumina.Excel.Sheets;

namespace Dalamud.Game.Inventory;

/// <summary>
/// Dalamud wrapper around a ClientStructs InventoryItem.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = InventoryItem.StructSize)]
public unsafe struct GameInventoryItem : IEquatable<GameInventoryItem>
{
    /// <summary>
    /// The actual data.
    /// </summary>
    [FieldOffset(0)]
    internal readonly InventoryItem InternalItem;

    /// <summary>
    /// The view of the backing data, in <see cref="ulong"/>.
    /// </summary>
    [FieldOffset(0)]
    private fixed ulong dataUInt64[InventoryItem.StructSize / 0x8];

    /// <summary>
    /// Initializes a new instance of the <see cref="GameInventoryItem"/> struct.
    /// </summary>
    public GameInventoryItem()
    {
        this.InternalItem.Ctor();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameInventoryItem"/> struct.
    /// </summary>
    /// <param name="item">Inventory item to wrap.</param>
    internal GameInventoryItem(InventoryItem item) => this.InternalItem = item;

    /// <summary>
    /// Gets a value indicating whether the this <see cref="GameInventoryItem"/> is empty.
    /// </summary>
    public bool IsEmpty => this.InternalItem.IsEmpty();

    /// <summary>
    /// Gets the container inventory type.
    /// </summary>
    public GameInventoryType ContainerType => (GameInventoryType)this.InternalItem.GetInventoryType();

    /// <summary>
    /// Gets the inventory slot index this item is in.
    /// </summary>
    public uint InventorySlot => this.InternalItem.GetSlot();

    /// <summary>
    /// Gets the item id.
    /// </summary>
    public uint ItemId => this.InternalItem.GetItemId();

    /// <summary>
    /// Gets the base item id (without HQ or Collectible offset applied).
    /// </summary>
    public uint BaseItemId => ItemUtil.GetBaseId(this.ItemId).ItemId;

    /// <summary>
    /// Gets the quantity of items in this item stack.
    /// </summary>
    public int Quantity => (int)this.InternalItem.GetQuantity();

    /// <summary>
    /// Gets the spiritbond or collectability of this item.
    /// </summary>
    public uint SpiritbondOrCollectability => this.InternalItem.GetSpiritbondOrCollectability();

    /// <summary>
    /// Gets the spiritbond of this item.
    /// </summary>
    [Obsolete($"Renamed to {nameof(SpiritbondOrCollectability)}", true)]
    public uint Spiritbond => this.SpiritbondOrCollectability;

    /// <summary>
    /// Gets the repair condition of this item.
    /// </summary>
    public uint Condition => this.InternalItem.GetCondition(); // Note: This will be the Breeding Capacity of Race Chocobos

    /// <summary>
    /// Gets a value indicating whether the item is High Quality.
    /// </summary>
    public bool IsHq => this.InternalItem.GetFlags().HasFlag(InventoryItem.ItemFlags.HighQuality);

    /// <summary>
    /// Gets a value indicating whether the  item has a company crest applied.
    /// </summary>
    public bool IsCompanyCrestApplied => this.InternalItem.GetFlags().HasFlag(InventoryItem.ItemFlags.CompanyCrestApplied);

    /// <summary>
    /// Gets a value indicating whether the item is a relic.
    /// </summary>
    public bool IsRelic => this.InternalItem.GetFlags().HasFlag(InventoryItem.ItemFlags.Relic);

    /// <summary>
    /// Gets a value indicating whether the is a collectable.
    /// </summary>
    public bool IsCollectable => this.InternalItem.GetFlags().HasFlag(InventoryItem.ItemFlags.Collectable);

    /// <summary>
    /// Gets the array of materia types.
    /// </summary>
    public ReadOnlySpan<ushort> Materia
    {
        get
        {
            var baseItemId = this.BaseItemId;

            if (ItemUtil.IsEventItem(baseItemId) || this.IsMateriaUsedForDate)
                return [];

            var dataManager = Service<DataManager>.Get();

            if (!dataManager.GetExcelSheet<Item>().TryGetRow(baseItemId, out var item) || item.MateriaSlotCount == 0)
                return [];

            Span<ushort> materiaIds = new ushort[item.MateriaSlotCount];
            var materiaRowCount = dataManager.GetExcelSheet<Materia>().Count;

            for (byte i = 0; i < item.MateriaSlotCount; i++)
            {
                var materiaId = this.InternalItem.GetMateriaId(i);
                if (materiaId < materiaRowCount)
                    materiaIds[i] = materiaId;
            }

            return materiaIds;
        }
    }

    /// <summary>
    /// Gets the array of materia grades.
    /// </summary>
    public ReadOnlySpan<byte> MateriaGrade
    {
        get
        {
            var baseItemId = this.BaseItemId;

            if (ItemUtil.IsEventItem(baseItemId) || this.IsMateriaUsedForDate)
                return [];

            var dataManager = Service<DataManager>.Get();

            if (!dataManager.GetExcelSheet<Item>().TryGetRow(baseItemId, out var item) || item.MateriaSlotCount == 0)
                return [];

            Span<byte> materiaGrades = new byte[item.MateriaSlotCount];
            var materiaGradeRowCount = dataManager.GetExcelSheet<MateriaGrade>().Count;

            for (byte i = 0; i < item.MateriaSlotCount; i++)
            {
                var materiaGrade = this.InternalItem.GetMateriaGrade(i);
                if (materiaGrade < materiaGradeRowCount)
                    materiaGrades[i] = materiaGrade;
            }

            return materiaGrades;
        }
    }

    /// <summary>
    /// Gets the address of native inventory item in the game.<br />
    /// Can be 0 if this instance of <see cref="GameInventoryItem"/> does not point to a valid set of container type and slot.<br />
    /// Note that this instance of <see cref="GameInventoryItem"/> can be a snapshot; it may not necessarily match the
    /// data you can query from the game using this address value.
    /// </summary>
    public nint Address
    {
        get
        {
            var s = GetReadOnlySpanOfInventory(this.ContainerType);
            if (s.IsEmpty)
                return 0;

            foreach (ref readonly var i in s)
            {
                if (i.InventorySlot == this.InventorySlot)
                    return (nint)Unsafe.AsPointer(ref Unsafe.AsRef(in i));
            }

            return 0;
        }
    }

    /// <summary>
    /// Gets the color used for this item.
    /// </summary>
    public ReadOnlySpan<byte> Stains
    {
        get
        {
            var baseItemId = this.BaseItemId;

            if (ItemUtil.IsEventItem(baseItemId))
                return [];

            var dataManager = Service<DataManager>.Get();

            if (!dataManager.GetExcelSheet<Item>().TryGetRow(baseItemId, out var item) || item.DyeCount == 0)
                return [];

            Span<byte> stainIds = new byte[item.DyeCount];
            var stainRowCount = dataManager.GetExcelSheet<Stain>().Count;

            for (byte i = 0; i < item.DyeCount; i++)
            {
                var stainId = this.InternalItem.GetStain(i);
                if (stainId < stainRowCount)
                    stainIds[i] = stainId;
            }

            return stainIds;
        }
    }

    /// <summary>
    /// Gets the glamour id for this item.
    /// </summary>
    public uint GlamourId => this.InternalItem.GetGlamourId();

    /// <summary>
    /// Gets the items crafter's content id.
    /// NOTE: I'm not sure if this is a good idea to include or not in the dalamud api. Marked internal for now.
    /// </summary>
    internal ulong CrafterContentId => this.InternalItem.GetCrafterContentId();

    /// <summary>
    /// Gets a value indicating whether the Materia fields are used to store a date.
    /// </summary>
    private bool IsMateriaUsedForDate => this.BaseItemId
                // Race Chocobo related items
                is 9560 // Proof of Covering

                // Wedding related items
                or 8575 // Eternity Ring
                or 8693 // Promise of Innocence
                or 8694 // Promise of Passion
                or 8695 // Promise of Devotion
                or 8696 // (Unknown/unused)
                or 8698 // Blank Invitation
                or 8699; // Ceremony Invitation

    public static bool operator ==(in GameInventoryItem l, in GameInventoryItem r) => l.Equals(r);

    public static bool operator !=(in GameInventoryItem l, in GameInventoryItem r) => !l.Equals(r);

    /// <inheritdoc/>
    readonly bool IEquatable<GameInventoryItem>.Equals(GameInventoryItem other) => this.Equals(other);

    /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns><c>true</c> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <c>false</c>.</returns>
    public readonly bool Equals(in GameInventoryItem other)
    {
        for (var i = 0; i < InventoryItem.StructSize / 8; i++)
        {
            if (this.dataUInt64[i] != other.dataUInt64[i])
                return false;
        }

        return true;
    }

    /// <inheritdoc cref="object.Equals(object?)" />
    public override bool Equals(object obj) => obj is GameInventoryItem gii && this.Equals(gii);

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode()
    {
        var k = 0x5a8447b91aff51b4UL;
        for (var i = 0; i < InventoryItem.StructSize / 8; i++)
            k ^= this.dataUInt64[i];
        return unchecked((int)(k ^ (k >> 32)));
    }

    /// <inheritdoc cref="object.ToString"/>
    public override string ToString() =>
        this.IsEmpty
            ? "empty"
            : $"item({this.ItemId}@{this.ContainerType}#{this.InventorySlot})";

    /// <summary>
    /// Gets a <see cref="Span{T}"/> view of <see cref="InventoryItem"/>s, wrapped as <see cref="GameInventoryItem"/>.
    /// </summary>
    /// <param name="type">The inventory type.</param>
    /// <returns>The span.</returns>
    internal static ReadOnlySpan<GameInventoryItem> GetReadOnlySpanOfInventory(GameInventoryType type)
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager is null) return default;

        var inventory = inventoryManager->GetInventoryContainer((InventoryType)type);
        if (inventory is null) return default;

        return new ReadOnlySpan<GameInventoryItem>(inventory->Items, (int)inventory->Size);
    }
}
