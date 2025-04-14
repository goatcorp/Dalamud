using System.Buffers.Binary;
using System.Numerics;
using System.Text;

using Dalamud.Bindings.ImGui;
using Dalamud.Data;
using Dalamud.Game.Inventory;
using Dalamud.Game.Text;
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

        foreach (var inventoryType in Enum.GetValues<InventoryType>())
        {
            var items = GameInventoryItem.GetReadOnlySpanOfInventory((GameInventoryType)inventoryType);

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
            ImGui.TextUnformatted(items.Length.ToString());
        }
    }

    private unsafe void DrawInventoryType(InventoryType inventoryType)
    {
        var items = GameInventoryItem.GetReadOnlySpanOfInventory((GameInventoryType)inventoryType);
        if (items.IsEmpty)
        {
            ImGui.TextUnformatted($"{inventoryType} is empty.");
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

        for (var slotIndex = 0; slotIndex < items.Length; slotIndex++)
        {
            var item = items[slotIndex];

            using var disableditem = ImRaii.Disabled(item.ItemId == 0);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Slot
            ImGui.TextUnformatted(slotIndex.ToString());

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
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted("Click to copy IconId");
                        ImGui.TextUnformatted($"ID: {iconId} – Size: {texture.Width}x{texture.Height}");
                        ImGui.Image(texture.Handle, new(texture.Width, texture.Height));
                        ImGui.EndTooltip();
                    }

                    if (ImGui.IsItemClicked())
                        ImGui.SetClipboardText(iconId.ToString());
                }

                ImGui.SameLine();

                using var itemNameColor = ImRaii.PushColor(ImGuiCol.Text, this.GetItemRarityColor(item.ItemId));
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
                    }
                }

                if (!node) continue;

                using var itemInfoTable = ImRaii.Table($"{inventoryType}_{slotIndex}_Table", 2, ImGuiTableFlags.BordersInner | ImGuiTableFlags.NoSavedSettings);
                if (!itemInfoTable) continue;

                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("Value");
                // ImGui.TableHeadersRow();

                static void AddKeyValueRow(string fieldName, string value)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(fieldName);
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
                        ImGui.TextUnformatted("Stains");
                        ImGui.TableNextColumn();

                        using var stainTable = ImRaii.Table($"{inventoryType}_{slotIndex}_StainTable", 2, ImGuiTableFlags.BordersInner | ImGuiTableFlags.NoSavedSettings);
                        if (!stainTable) continue;

                        ImGui.TableSetupColumn("Stain Id", ImGuiTableColumnFlags.WidthFixed, 80);
                        ImGui.TableSetupColumn("Name");
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
                        ImGui.TextUnformatted("Materia");
                        ImGui.TableNextColumn();

                        using var materiaTable = ImRaii.Table($"{inventoryType}_{slotIndex}_MateriaTable", 2, ImGuiTableFlags.BordersInner | ImGuiTableFlags.NoSavedSettings);
                        if (!materiaTable) continue;

                        ImGui.TableSetupColumn("Materia Id", ImGuiTableColumnFlags.WidthFixed, 80);
                        ImGui.TableSetupColumn("MateriaGrade Id");
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
        if (ItemUtil.IsEventItem(itemId))
            return isEdgeColor ? 0xFF000000 : 0xFFFFFFFF;

        if (!this.dataManager.Excel.GetSheet<Item>().TryGetRow(ItemUtil.GetBaseId(itemId).ItemId, out var item))
            return isEdgeColor ? 0xFF000000 : 0xFFFFFFFF;

        var rowId = ItemUtil.GetItemRarityColorType(item.RowId, isEdgeColor);
        return this.dataManager.Excel.GetSheet<UIColor>().TryGetRow(rowId, out var color)
            ? BinaryPrimitives.ReverseEndianness(color.Dark) | 0xFF000000
            : 0xFFFFFFFF;
    }

    private uint GetItemIconId(uint itemId)
    {
        // EventItem
        if (ItemUtil.IsEventItem(itemId))
            return this.dataManager.Excel.GetSheet<EventItem>().TryGetRow(itemId, out var eventItem) ? eventItem.Icon : 0u;

        // HighQuality
        if (ItemUtil.IsHighQuality(itemId))
            itemId -= 1_000_000;

        // Collectible
        if (ItemUtil.IsCollectible(itemId))
            itemId -= 500_000;

        return this.dataManager.Excel.GetSheet<Item>().TryGetRow(itemId, out var item) ? item.Icon : 0u;
    }
}
