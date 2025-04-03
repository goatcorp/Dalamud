using System.Runtime.CompilerServices;

using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Text;
using Lumina.Excel.Sheets;
using Lumina.Text;
using Lumina.Text.ReadOnly;

using static Dalamud.Game.Text.SeStringHandling.Payloads.ItemPayload;

namespace Dalamud.Utility;

/// <summary>
/// Utilities related to Items.
/// </summary>
internal static class ItemUtil
{
    private static int? eventItemRowCount;

    /// <summary>Converts raw item ID to item ID with its classification.</summary>
    /// <param name="rawItemId">Raw item ID.</param>
    /// <returns>Item ID and its classification.</returns>
    internal static (uint ItemId, ItemKind Kind) GetBaseId(uint rawItemId)
    {
        if (IsEventItem(rawItemId)) return (rawItemId, ItemKind.EventItem); // EventItem IDs are NOT adjusted
        if (IsHighQuality(rawItemId)) return (rawItemId - 1_000_000, ItemKind.Hq);
        if (IsCollectible(rawItemId)) return (rawItemId - 500_000, ItemKind.Collectible);
        return (rawItemId, ItemKind.Normal);
    }

    /// <summary>Converts item ID with its classification to raw item ID.</summary>
    /// <param name="itemId">Item ID.</param>
    /// <param name="kind">Item classification.</param>
    /// <returns>Raw Item ID.</returns>
    internal static uint GetRawId(uint itemId, ItemKind kind)
    {
        return kind switch
        {
            ItemKind.Collectible when itemId < 500_000 => itemId + 500_000,
            ItemKind.Hq when itemId < 1_000_000 => itemId + 1_000_000,
            ItemKind.EventItem => itemId, // EventItem IDs are not adjusted
            _ => itemId,
        };
    }

    /// <summary>
    /// Checks if the item id belongs to a normal item.
    /// </summary>
    /// <param name="itemId">The item id to check.</param>
    /// <returns><c>true</c> when the item id belongs to a normal item.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsNormalItem(uint itemId)
    {
        return itemId < 500_000;
    }

    /// <summary>
    /// Checks if the item id belongs to a collectible item.
    /// </summary>
    /// <param name="itemId">The item id to check.</param>
    /// <returns><c>true</c> when the item id belongs to a collectible item.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsCollectible(uint itemId)
    {
        return itemId is >= 500_000 and < 1_000_000;
    }

    /// <summary>
    /// Checks if the item id belongs to a high quality item.
    /// </summary>
    /// <param name="itemId">The item id to check.</param>
    /// <returns><c>true</c> when the item id belongs to a high quality item.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsHighQuality(uint itemId)
    {
        return itemId is >= 1_000_000 and < 2_000_000;
    }

    /// <summary>
    /// Checks if the item id belongs to an event item.
    /// </summary>
    /// <param name="itemId">The item id to check.</param>
    /// <returns><c>true</c> when the item id belongs to an event item.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsEventItem(uint itemId)
    {
        return itemId >= 2_000_000 && itemId - 2_000_000 < (eventItemRowCount ??= Service<DataManager>.Get().GetExcelSheet<EventItem>().Count);
    }

    /// <summary>
    /// Gets the name of an item.
    /// </summary>
    /// <param name="itemId">The raw item id.</param>
    /// <param name="includeIcon">Whether to include the High Quality or Collectible icon.</param>
    /// <param name="language">An optional client language override.</param>
    /// <returns>The item name.</returns>
    internal static ReadOnlySeString GetItemName(uint itemId, bool includeIcon = true, ClientLanguage? language = null)
    {
        var dataManager = Service<DataManager>.Get();

        if (IsEventItem(itemId))
        {
            return dataManager
                .GetExcelSheet<EventItem>(language)
                .TryGetRow(itemId, out var eventItem)
                    ? eventItem.Name
                    : default;
        }

        var (baseId, kind) = GetBaseId(itemId);

        if (!dataManager
            .GetExcelSheet<Item>(language)
            .TryGetRow(baseId, out var item))
        {
            return default;
        }

        if (!includeIcon || kind is not (ItemKind.Hq or ItemKind.Collectible))
            return item.Name;

        var builder = SeStringBuilder.SharedPool.Get();

        builder.Append(item.Name);

        switch (kind)
        {
            case ItemKind.Hq:
                builder.Append($" {(char)SeIconChar.HighQuality}");
                break;
            case ItemKind.Collectible:
                builder.Append($" {(char)SeIconChar.Collectible}");
                break;
        }

        var itemName = builder.ToReadOnlySeString();
        SeStringBuilder.SharedPool.Return(builder);
        return itemName;
    }

    /// <summary>
    /// Gets the color row id for an item name.
    /// </summary>
    /// <param name="itemId">The raw item Id.</param>
    /// <param name="isEdgeColor">Wheather this color is used as edge color.</param>
    /// <returns>The Color row id.</returns>
    internal static uint GetItemRarityColorType(uint itemId, bool isEdgeColor = false)
    {
        var rarity = 1u;

        if (!IsEventItem(itemId) && Service<DataManager>.Get().GetExcelSheet<Item>().TryGetRow(GetBaseId(itemId).ItemId, out var item))
            rarity = item.Rarity;

        return (isEdgeColor ? 548u : 547u) + (rarity * 2u);
    }
}
