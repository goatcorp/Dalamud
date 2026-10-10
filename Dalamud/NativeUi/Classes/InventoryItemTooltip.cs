using FFXIVClientStructs.FFXIV.Client.Game;

namespace Dalamud.NativeUi.Classes;

/// <summary>
/// Data object representing an item in <see cref="Inventory"/> in slot <see cref="Slot"/> for use in Item Tooltips.
/// </summary>
internal record InventoryItemTooltip(InventoryType Inventory, short Slot);
