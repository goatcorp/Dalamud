using Dalamud.Excel.Sheets;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.Evaluator;
using Dalamud.Utility;

using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Text;

/// <summary>
/// This class provices static utility functions for ReadOnlySeStrings.
/// </summary>
public static class SeString
{
    /// <summary>
    /// Creates a ReadOnlySeString representing an entire macro chain that can be used to link an item in the chat log.
    /// </summary>
    /// <param name="itemId">The id of the item to link.</param>
    /// <param name="isHq">Whether to link the high-quality variant of the item.</param>
    /// <param name="displayNameOverride">An optional name override to display, instead of the actual item name.</param>
    /// <returns>A ReadOnlySeString containing all the macros necessary to display an item link in the chat log.</returns>
    public static ReadOnlySeString CreateItemLink(uint itemId, bool isHq, ReadOnlySeString displayNameOverride = default)
        => CreateItemLink(itemId, isHq ? ItemKind.Hq : ItemKind.Normal, displayNameOverride);

    /// <summary>
    /// Creates a ReadOnlySeString representing an entire macro chain that can be used to link an item in the chat log.
    /// </summary>
    /// <param name="item">The Lumina Item to link.</param>
    /// <param name="isHq">Whether to link the high-quality variant of the item.</param>
    /// <param name="displayNameOverride">An optional name override to display, instead of the actual item name.</param>
    /// <returns>A ReadOnlySeString containing all the macros necessary to display an item link in the chat log.</returns>
    public static ReadOnlySeString CreateItemLink(Item item, bool isHq, ReadOnlySeString displayNameOverride = default)
        => CreateItemLink(item.RowId, isHq, !displayNameOverride.IsEmpty ? displayNameOverride : item.Name);

    /// <summary>
    /// Creates a ReadOnlySeString representing an entire macro chain that can be used to link an item in the chat log.
    /// </summary>
    /// <param name="itemId">The id of the item to link.</param>
    /// <param name="kind">The kind of item to link.</param>
    /// <param name="displayNameOverride">An optional name override to display, instead of the actual item name.</param>
    /// <returns>A ReadOnlySeString containing all the macros necessary to display an item link in the chat log.</returns>
    public static ReadOnlySeString CreateItemLink(uint itemId, ItemKind kind = ItemKind.Normal, ReadOnlySeString displayNameOverride = default)
    {
        var clientState = Service<ClientState.ClientState>.Get();
        var evaluator = Service<SeStringEvaluator>.Get();

        var rawId = ItemUtil.GetRawId(itemId, kind);

        var displayName = !displayNameOverride.IsEmpty ? displayNameOverride : ItemUtil.GetItemName(rawId);
        if (displayName.IsEmpty)
            throw new Exception("Invalid item ID specified, could not determine item name.");

        var copyName = ItemUtil.GetItemName(rawId, false).ExtractText();
        var textColor = ItemUtil.GetItemRarityColorType(rawId);
        var textEdgeColor = textColor + 1u;

        using var rssb = new RentedSeStringBuilder();

        var itemLink = rssb
            .PushColorType(textColor)
            .PushEdgeColorType(textEdgeColor)
            .PushLinkItem(rawId, copyName)
            .Append(displayName)
            .PopLink()
            .PopEdgeColorType()
            .PopColorType()
            .ToReadOnlySeString();

        return evaluator.EvaluateFromAddon(371, [itemLink], clientState.ClientLanguage);
    }

    /// <summary>
    /// Creates a ReadOnlySeString representing an entire macros chain that can be used to link a map position of a GameObject in the chat log.
    /// </summary>
    /// <param name="obj">The GameObject, which position should be used.</param>
    /// <returns>A ReadOnlySeString containing all of the macros necessary to display a map link in the chat log.</returns>
    public static ReadOnlySeString CreateMapLink(IGameObject obj)
    {
        return CreateMapLink(obj.GetWorldCoordinate());
    }

    /// <summary>
    /// Creates a ReadOnlySeString representing an entire macros chain that can be used to link a Level position in the chat log.
    /// </summary>
    /// <param name="levelRow">The Level row.</param>
    /// <returns>A ReadOnlySeString containing all of the macros necessary to display a map link of a Level position in the chat log.</returns>
    public static ReadOnlySeString CreateMapLink(Level levelRow)
    {
        return CreateMapLink(levelRow.ToWorldCoordinate());
    }

    /// <summary>
    /// Creates a ReadOnlySeString representing an entire macros chain that can be used to link a map position in the chat log.
    /// </summary>
    /// <param name="worldPos">The world-space coordinate.</param>
    /// <param name="placeNameIdOverride">An optional PlaceName RowId that is used for the locations name.</param>
    /// <returns>A ReadOnlySeString containing all of the macros necessary to display a map link in the chat log.</returns>
    public static ReadOnlySeString CreateMapLink(WorldCoordinate worldPos, uint placeNameIdOverride = 0)
    {
        if (worldPos.TerritoryId == 0 || worldPos.MapId == 0)
            return default;

        using var rssb = new RentedSeStringBuilder();

        return Service<SeStringEvaluator>.Get().Evaluate(rssb
                .BeginMacro(MacroCode.Fixed)
                .AppendIntExpression(200)
                .AppendIntExpression(3)
                .AppendUIntExpression(worldPos.TerritoryId)
                .AppendUIntExpression((worldPos.InstanceId << 16) | worldPos.MapId)
                .AppendIntExpression((int)MathF.Round(worldPos.X * 1000f))
                .AppendIntExpression((int)MathF.Round(worldPos.Z * 1000f))
                .AppendIntExpression(worldPos.Y == MapUtil.NoRawZ ? MapUtil.NoRawZ : MapUtil.ApplyTerritoryOffsetToWorldCoordianteY(worldPos.TerritoryId, worldPos.Y))
                .AppendUIntExpression(placeNameIdOverride)
                .EndMacro()
                .ToReadOnlySeString());
    }

    /// <summary>
    /// Creates a ReadOnlySeString representing an entire macros chain that can be used to link a map position in the chat log.
    /// </summary>
    /// <param name="mapPos">The map-space coordinate.</param>
    /// <returns>A ReadOnlySeString containing all of the macros necessary to display a map link in the chat log.</returns>
    public static ReadOnlySeString CreateMapLink(MapCoordinate mapPos)
    {
        return CreateMapLink(mapPos.ToWorldCoordinate());
    }

    /// <summary>
    /// Creates a ReadOnlySeString representing an entire macros chain that can be used to link party finder listings in the chat log.
    /// </summary>
    /// <param name="listingId">The listing ID of the party finder entry.</param>
    /// <param name="recruiterName">The name of the recruiter.</param>
    /// <param name="isCrossWorld">Whether the listing is limited to the current world or not.</param>
    /// <returns>A ReadOnlySeString containing all the macros necessary to display a party finder link in the chat log.</returns>
    public static ReadOnlySeString CreatePartyFinderLink(uint listingId, string recruiterName, bool isCrossWorld = false)
    {
        using var rssb = new RentedSeStringBuilder();
        var evaluator = Service<SeStringEvaluator>.Get();
        return evaluator.Evaluate(rssb.Builder
            .BeginMacro(MacroCode.Fixed)
            .AppendIntExpression(200)
            .AppendIntExpression(11)
            .AppendUIntExpression(listingId) // Listing Id
            .AppendUIntExpression(0) // Unknown
            .AppendUIntExpression(0) // World Id
            .AppendUIntExpression(isCrossWorld ? 0 : 1u) // Cross World flag (0 = Cross World, 1 = not Cross World)
            .AppendStringExpression(recruiterName) // Player Name
            .EndMacro()
            .ToReadOnlySeString());
    }

    /// <summary>
    /// Creates a ReadOnlySeString representing an entire macros chain that can be used to link the party finder search conditions.
    /// </summary>
    /// <param name="message">The text that should be displayed for the link.</param>
    /// <returns>A ReadOnlySeString containing all the macros necessary to display a link to the party finder search conditions.</returns>
    public static ReadOnlySeString CreatePartyFinderSearchConditionsLink(string message)
    {
        using var rssb = new RentedSeStringBuilder();
        var evaluator = Service<SeStringEvaluator>.Get();
        return evaluator.Evaluate(rssb.Builder
            .PushLinkPartyFinderNotification()
            .Append(evaluator.EvaluateFromAddon(371, [message]))
            .PopLink()
            .ToReadOnlySeString());
    }
}
