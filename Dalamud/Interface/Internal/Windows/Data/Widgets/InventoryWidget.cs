using System.Buffers.Binary;
using System.Numerics;
using System.Text;

using Dalamud.Bindings.ImGui;
using Dalamud.Data;
using Dalamud.Game.Inventory;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying inventory data.
/// </summary>
internal class InventoryWidget : IDataWindowWidget
{
    private const ImGuiTableFlags TableFlags = ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders |
                                               ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings;

    private const ImGuiTableFlags InnerTableFlags = ImGuiTableFlags.BordersInner | ImGuiTableFlags.NoSavedSettings;

    private DataManager dataManager;
    private TextureManager textureManager;
    private GameInventoryType? selectedInventoryType = GameInventoryType.Inventory1;

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

        this.DrawInventoryType(this.selectedInventoryType.Value);
    }

    private static string StripSoftHypen(string input)
    {
        return input.Replace("\u00AD", string.Empty);
    }

    private unsafe void DrawInventoryTypeList()
    {
        using var table = ImRaii.Table("InventoryTypeTable"u8, 2, TableFlags, new Vector2(300, -1));
        if (!table) return;

        ImGui.TableSetupColumn("Type"u8);
        ImGui.TableSetupColumn("Size"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        foreach (var inventoryType in Enum.GetValues<GameInventoryType>())
        {
            var items = GameInventoryItem.GetReadOnlySpanOfInventory(inventoryType);

            using var itemDisabled = ImRaii.Disabled(items.IsEmpty);

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
                    if (ImGui.MenuItem("Copy Name"u8))
                    {
                        ImGui.SetClipboardText(inventoryType.ToString());
                    }

                    if (ImGui.MenuItem("Copy Address"u8))
                    {
                        var container = InventoryManager.Instance()->GetInventoryContainer((InventoryType)inventoryType);
                        ImGui.SetClipboardText($"0x{(nint)container:X}");
                    }
                }
            }

            ImGui.TableNextColumn(); // Size
            ImGui.Text(items.Length.ToString());
        }
    }

    private void DrawInventoryType(GameInventoryType inventoryType)
    {
        var items = GameInventoryItem.GetReadOnlySpanOfInventory(inventoryType);
        if (items.IsEmpty)
        {
            ImGui.Text($"{inventoryType} is empty.");
            return;
        }

        using var itemTable = ImRaii.Table("InventoryItemTable"u8, 4, TableFlags);
        if (!itemTable) return;

        ImGui.TableSetupColumn("Slot"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("ItemId"u8, ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableSetupColumn("Quantity"u8, ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableSetupColumn("Item"u8);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var slotIndex = 0; slotIndex < items.Length; slotIndex++)
        {
            var item = items[slotIndex];

            using var disabledItem = ImRaii.Disabled(item.ItemId == 0);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Slot
            ImGui.Text(slotIndex.ToString());

            ImGui.TableNextColumn(); // ItemId
            ImGuiHelpers.ClickToCopyText(item.ItemId.ToString());

            ImGui.TableNextColumn(); // Quantity
            ImGuiHelpers.ClickToCopyText(item.Quantity.ToString());

            ImGui.TableNextColumn(); // Item
            if (item.ItemId != 0 && item.Quantity != 0)
            {
                var itemName = ItemUtil.GetItemName(item.ItemId).ExtractText();
                var iconId = this.GetItemIconId(item.ItemId);

                if (this.textureManager.Shared.TryGetFromGameIcon(new GameIconLookup(iconId, item.IsHq), out var tex) && tex.TryGetWrap(out var texture, out _))
                {
                    ImGui.Image(texture.Handle, new Vector2(ImGui.GetTextLineHeight()));

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                        using var tooltip = ImRaii.Tooltip();
                        ImGui.Text("Click to copy IconId"u8);
                        ImGui.Text($"ID: {iconId} – Size: {texture.Width}x{texture.Height}");
                        ImGui.Image(texture.Handle, new(texture.Width, texture.Height));
                    }

                    if (ImGui.IsItemClicked())
                        ImGui.SetClipboardText(iconId.ToString());
                }

                ImGui.SameLine();

                using var itemNameColor = ImRaii.PushColor(ImGuiCol.Text, this.GetItemRarityColor(item.ItemId));
                using var node = ImRaii.TreeNode($"{itemName}###{inventoryType}_{slotIndex}", ImGuiTreeNodeFlags.SpanAvailWidth);
                itemNameColor.Pop();

                using (var contextMenu = ImRaii.ContextPopupItem($"{inventoryType}_{slotIndex}_ContextMenu"))
                {
                    if (contextMenu)
                    {
                        if (ImGui.MenuItem("Copy Name"u8))
                        {
                            ImGui.SetClipboardText(itemName);
                        }
                    }
                }

                if (!node) continue;

                using var itemInfoTable = ImRaii.Table($"{inventoryType}_{slotIndex}_Table", 2, InnerTableFlags);
                if (!itemInfoTable) continue;

                ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("Value"u8);
                // ImGui.TableHeadersRow();

                static void AddKeyValueRow(string fieldName, string value)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(fieldName);
                    ImGui.TableNextColumn();
                    ImGuiHelpers.ClickToCopyText(value);
                }

                static void AddValueValueRow(string value1, string value2)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiHelpers.ClickToCopyText(value1);
                    ImGui.TableNextColumn();
                    ImGuiHelpers.ClickToCopyText(value2);
                }

                AddKeyValueRow("ItemId", item.ItemId.ToString());
                AddKeyValueRow("Quantity", item.Quantity.ToString());
                AddKeyValueRow("GlamourId", item.GlamourId.ToString());

                if (!ItemUtil.IsEventItem(item.ItemId))
                {
                    AddKeyValueRow(item.IsCollectable ? "Collectability" : "Spiritbond", item.SpiritbondOrCollectability.ToString());

                    if (item.CrafterContentId != 0)
                        AddKeyValueRow("CrafterContentId", item.CrafterContentId.ToString());
                }

                var flagsBuilder = new StringBuilder();

                if (item.IsHq)
                {
                    flagsBuilder.Append("IsHq");
                }

                if (item.IsCompanyCrestApplied)
                {
                    if (flagsBuilder.Length != 0)
                        flagsBuilder.Append(", ");

                    flagsBuilder.Append("IsCompanyCrestApplied");
                }

                if (item.IsRelic)
                {
                    if (flagsBuilder.Length != 0)
                        flagsBuilder.Append(", ");

                    flagsBuilder.Append("IsRelic");
                }

                if (item.IsCollectable)
                {
                    if (flagsBuilder.Length != 0)
                        flagsBuilder.Append(", ");

                    flagsBuilder.Append("IsCollectable");
                }

                if (flagsBuilder.Length == 0)
                    flagsBuilder.Append("None");

                AddKeyValueRow("Flags", flagsBuilder.ToString());

                if (ItemUtil.IsNormalItem(item.ItemId) && this.dataManager.Excel.GetSheet<Item>().TryGetRow(item.ItemId, out var itemRow))
                {
                    if (itemRow.DyeCount > 0 && item.Stains.Length > 0)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text("Stains"u8);
                        ImGui.TableNextColumn();

                        using var stainTable = ImRaii.Table($"{inventoryType}_{slotIndex}_StainTable", 2, InnerTableFlags);
                        if (!stainTable) continue;

                        ImGui.TableSetupColumn("Stain Id"u8, ImGuiTableColumnFlags.WidthFixed, 80);
                        ImGui.TableSetupColumn("Name"u8);
                        ImGui.TableHeadersRow();

                        for (var i = 0; i < itemRow.DyeCount; i++)
                        {
                            var stainId = item.Stains[i];
                            AddValueValueRow(stainId.ToString(), this.GetStainName(stainId));
                        }
                    }

                    if (itemRow.MateriaSlotCount > 0 && item.Materia.Length > 0)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text("Materia"u8);
                        ImGui.TableNextColumn();

                        using var materiaTable = ImRaii.Table($"{inventoryType}_{slotIndex}_MateriaTable", 2, InnerTableFlags);
                        if (!materiaTable) continue;

                        ImGui.TableSetupColumn("Materia Id"u8, ImGuiTableColumnFlags.WidthFixed, 80);
                        ImGui.TableSetupColumn("MateriaGrade Id"u8);
                        ImGui.TableHeadersRow();

                        for (var i = 0; i < Math.Min(itemRow.MateriaSlotCount, item.Materia.Length); i++)
                        {
                            AddValueValueRow(item.Materia[i].ToString(), item.MateriaGrade[i].ToString());
                        }
                    }
                }
            }
        }
    }

    private string GetStainName(uint stainId)
    {
        return this.dataManager.Excel.GetSheet<Stain>().TryGetRow(stainId, out var stainRow)
            ? StripSoftHypen(stainRow.Name.ExtractText())
            : $"Stain#{stainId}";
    }

    private uint GetItemRarityColor(uint itemId, bool isEdgeColor = false)
    {
        var normalized = ItemUtil.GetBaseId(itemId);

        if (normalized.Kind == ItemKind.EventItem)
            return isEdgeColor ? 0xFF000000 : 0xFFFFFFFF;

        if (!this.dataManager.Excel.GetSheet<Item>().TryGetRow(normalized.ItemId, out var item))
            return isEdgeColor ? 0xFF000000 : 0xFFFFFFFF;

        var rowId = ItemUtil.GetItemRarityColorType(item.RowId, isEdgeColor);
        return this.dataManager.Excel.GetSheet<UIColor>().TryGetRow(rowId, out var color)
            ? BinaryPrimitives.ReverseEndianness(color.Dark) | 0xFF000000
            : 0xFFFFFFFF;
    }

    private uint GetItemIconId(uint itemId)
    {
        var normalized = ItemUtil.GetBaseId(itemId);

        // EventItem
        if (normalized.Kind == ItemKind.EventItem)
            return this.dataManager.Excel.GetSheet<EventItem>().TryGetRow(itemId, out var eventItem) ? eventItem.Icon : 0u;

        return this.dataManager.Excel.GetSheet<Item>().TryGetRow(normalized.ItemId, out var item) ? item.Icon : 0u;
    }
}
