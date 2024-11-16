using System.Buffers.Binary;
using System.Numerics;

using Dalamud.Data;
using Dalamud.Game.Text;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Client.Game;

using ImGuiNET;

using Lumina.Excel.Sheets;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

#pragma warning disable SeStringRenderer

/// <summary>
/// Widget for displaying inventory data.
/// </summary>
internal class InventoryWidget : IDataWindowWidget
{
    private DataManager dataManager;
    private TextureManager textureManager;
    private InventoryType? selectedInventoryType = InventoryType.Inventory1;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["inv", "inventory"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Inventory";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        this.dataManager ??= Service<DataManager>.Get();
        this.textureManager ??= Service<TextureManager>.Get();

        this.DrawInventoryTypeList();

        if (this.selectedInventoryType == null)
            return;

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

        this.DrawInventoryType((InventoryType)this.selectedInventoryType);
    }

    private static string StripSoftHypen(string input)
    {
        return input.Replace("\u00AD", string.Empty);
    }

    private unsafe void DrawInventoryTypeList()
    {
        using var table = ImRaii.Table("InventoryTypeTable", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings, new Vector2(300, -1));
        if (!table) return;

        ImGui.TableSetupColumn("Type");
        ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        var inventoryManager = InventoryManager.Instance();

        foreach (var inventoryType in Enum.GetValues<InventoryType>())
        {
            var listContainer = inventoryManager->GetInventoryContainer(inventoryType);

            using var itemDisabled = ImRaii.Disabled(listContainer == null || listContainer->Loaded == 0);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Type
            if (ImGui.Selectable(inventoryType.ToString(), this.selectedInventoryType == inventoryType, ImGuiSelectableFlags.SpanAllColumns))
            {
                this.selectedInventoryType = inventoryType;
            }

            using (var contextMenu = ImRaii.ContextPopupItem($"##InventoryContext{inventoryType}"))
            {
                if (contextMenu)
                {
                    if (ImGui.MenuItem("Copy Name"))
                    {
                        ImGui.SetClipboardText(inventoryType.ToString());
                    }

                    if (ImGui.MenuItem("Copy Address"))
                    {
                        var container = InventoryManager.Instance()->GetInventoryContainer(inventoryType);
                        ImGui.SetClipboardText($"0x{(nint)container:X}");
                    }
                }
            }

            ImGui.TableNextColumn(); // Size
            ImGui.TextUnformatted((listContainer != null ? listContainer->Size : 0).ToString());
        }
    }

    private unsafe void DrawInventoryType(InventoryType inventoryType)
    {
        var container = InventoryManager.Instance()->GetInventoryContainer(inventoryType);
        if (container == null || container->Loaded == 0)
        {
            ImGui.TextUnformatted($"{inventoryType} not loaded.");
            return;
        }

        using var itemTable = ImRaii.Table("InventoryItemTable", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!itemTable) return;
        ImGui.TableSetupColumn("Slot", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("ItemId", ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableSetupColumn("Quantity", ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableSetupColumn("Item");
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var slotIndex = 0; slotIndex < container->GetSize(); slotIndex++)
        {
            var slot = container->GetInventorySlot(slotIndex);
            if (slot == null) continue;

            var itemId = slot->GetItemId();
            var quantity = slot->GetQuantity();

            using var disableditem = ImRaii.Disabled(itemId == 0);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Slot
            ImGui.TextUnformatted(slotIndex.ToString());

            ImGui.TableNextColumn(); // ItemId
            ImGui.TextUnformatted(itemId.ToString());

            ImGui.TableNextColumn(); // Quantity
            ImGui.TextUnformatted(quantity.ToString());

            ImGui.TableNextColumn(); // Item
            if (itemId != 0 && quantity != 0)
            {
                var itemName = this.GetItemName(itemId);
                var iconId = this.GetItemIconId(itemId);
                var isHQ = this.IsHighQuality(itemId);

                if (isHQ)
                    itemName += " " + SeIconChar.HighQuality.ToIconString();

                if (this.textureManager.Shared.TryGetFromGameIcon(new GameIconLookup(iconId, isHQ), out var tex) && tex.TryGetWrap(out var texture, out _))
                {
                    ImGui.Image(texture.ImGuiHandle, new Vector2(ImGui.GetTextLineHeight()));

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted("Click to copy IconId");
                        ImGui.TextUnformatted($"ID: {iconId} – Size: {texture.Width}x{texture.Height}");
                        ImGui.Image(texture.ImGuiHandle, new(texture.Width, texture.Height));
                        ImGui.EndTooltip();
                    }

                    if (ImGui.IsItemClicked())
                        ImGui.SetClipboardText(iconId.ToString());
                }

                ImGui.SameLine();

                using var itemNameColor = ImRaii.PushColor(ImGuiCol.Text, this.GetItemRarityColor(itemId));
                using var node = ImRaii.TreeNode($"{itemName}###{inventoryType}_{slotIndex}", ImGuiTreeNodeFlags.SpanAvailWidth);
                itemNameColor.Dispose();

                using (var contextMenu = ImRaii.ContextPopupItem($"{inventoryType}_{slotIndex}_ContextMenu"))
                {
                    if (contextMenu)
                    {
                        if (ImGui.MenuItem("Copy Name"))
                        {
                            ImGui.SetClipboardText(itemName);
                        }

                        if (ImGui.MenuItem("Copy Address"))
                        {
                            ImGui.SetClipboardText($"0x{(nint)slot:X}");
                        }
                    }
                }

                if (!node) continue;

                using var itemInfoTable = ImRaii.Table($"{inventoryType}_{slotIndex}_Table", 2, ImGuiTableFlags.BordersInner | ImGuiTableFlags.NoSavedSettings);
                if (!itemInfoTable) continue;

                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("Value");
                // ImGui.TableHeadersRow();

                static void AddRow(string fieldName, string value)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(fieldName);
                    ImGui.TableNextColumn();
                    ImGuiHelpers.ClickToCopyText(value);
                }

                AddRow("Base ItemId", slot->GetBaseItemId().ToString());
                AddRow("ItemId", slot->GetItemId().ToString());
                AddRow("Quantity", slot->GetQuantity().ToString());
                AddRow("Spiritbond / Collectability", slot->GetSpiritbondOrCollectability().ToString());
                AddRow("Flags", slot->GetFlags().ToString());
            }
        }
    }

    private bool IsEventItem(uint itemId) => itemId is > 2_000_000;

    private bool IsHighQuality(uint itemId) => itemId is > 1_000_000 and < 2_000_000;

    private bool IsCollectible(uint itemId) => itemId is > 500_000 and < 1_000_000;

    private bool IsNormalItem(uint itemId) => itemId is < 500_000;

    private uint GetBaseItemId(uint itemId)
    {
        if (this.IsEventItem(itemId)) return itemId; // uses EventItem sheet
        if (this.IsHighQuality(itemId)) return itemId - 1_000_000;
        if (this.IsCollectible(itemId)) return itemId - 500_000;
        return itemId;
    }

    private string GetItemName(uint itemId)
    {
        // EventItem
        if (this.IsEventItem(itemId))
        {
            return this.dataManager.Excel.GetSheet<EventItem>().TryGetRow(itemId, out var eventItemRow)
                ? StripSoftHypen(eventItemRow.Name.ExtractText())
                : $"EventItem#{itemId}";
        }

        // HighQuality
        if (this.IsHighQuality(itemId))
            itemId -= 1_000_000;

        // Collectible
        if (this.IsCollectible(itemId))
            itemId -= 500_000;

        return this.dataManager.Excel.GetSheet<Item>().TryGetRow(itemId, out var itemRow)
            ? StripSoftHypen(itemRow.Name.ExtractText())
            : $"Item#{itemId}";
    }

    private uint GetItemRarityColorType(Item item, bool isEdgeColor = false)
    {
        return (isEdgeColor ? 548u : 547u) + item.Rarity * 2u;
    }

    private uint GetItemRarityColorType(uint itemId, bool isEdgeColor = false)
    {
        // EventItem
        if (this.IsEventItem(itemId))
            return this.GetItemRarityColorType(1, isEdgeColor);

        if (!this.dataManager.Excel.GetSheet<Item>().TryGetRow(this.GetBaseItemId(itemId), out var item))
            return this.GetItemRarityColorType(1, isEdgeColor);

        return this.GetItemRarityColorType(item, isEdgeColor);
    }

    private uint GetItemRarityColor(uint itemId, bool isEdgeColor = false)
    {
        if (this.IsEventItem(itemId))
            return isEdgeColor ? 0xFF000000 : 0xFFFFFFFF;

        if (!this.dataManager.Excel.GetSheet<Item>().TryGetRow(this.GetBaseItemId(itemId), out var item))
            return isEdgeColor ? 0xFF000000 : 0xFFFFFFFF;

        var rowId = this.GetItemRarityColorType(item, isEdgeColor);
        return this.dataManager.Excel.GetSheet<UIColor>().TryGetRow(rowId, out var color)
            ? BinaryPrimitives.ReverseEndianness(color.UIForeground) | 0xFF000000
            : 0xFFFFFFFF;
    }

    private uint GetItemIconId(uint itemId)
    {
        // EventItem
        if (this.IsEventItem(itemId))
            return this.dataManager.Excel.GetSheet<EventItem>().TryGetRow(itemId, out var eventItem) ? eventItem.Icon : 0u;

        // HighQuality
        if (this.IsHighQuality(itemId))
            itemId -= 1_000_000;

        // Collectible
        if (this.IsCollectible(itemId))
            itemId -= 500_000;

        return this.dataManager.Excel.GetSheet<Item>().TryGetRow(itemId, out var item) ? item.Icon : 0u;
    }
}
