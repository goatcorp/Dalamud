using Dalamud.Data;
using Dalamud.Excel.Sheets;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.Evaluator;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using CSFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

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
    public static ReadOnlySeString CreateItemLink(uint itemId, bool isHq, string? displayNameOverride = null)
        => CreateItemLink(itemId, isHq ? ItemKind.Hq : ItemKind.Normal, displayNameOverride);

    /// <summary>
    /// Creates a ReadOnlySeString representing an entire macro chain that can be used to link an item in the chat log.
    /// </summary>
    /// <param name="item">The Lumina Item to link.</param>
    /// <param name="isHq">Whether to link the high-quality variant of the item.</param>
    /// <param name="displayNameOverride">An optional name override to display, instead of the actual item name.</param>
    /// <returns>A ReadOnlySeString containing all the macros necessary to display an item link in the chat log.</returns>
    public static ReadOnlySeString CreateItemLink(Item item, bool isHq, string? displayNameOverride = null)
        => CreateItemLink(item.RowId, isHq, displayNameOverride ?? item.Name.ExtractText());

    /// <summary>
    /// Creates a ReadOnlySeString representing an entire macro chain that can be used to link an item in the chat log.
    /// </summary>
    /// <param name="itemId">The id of the item to link.</param>
    /// <param name="kind">The kind of item to link.</param>
    /// <param name="displayNameOverride">An optional name override to display, instead of the actual item name.</param>
    /// <returns>A ReadOnlySeString containing all the macros necessary to display an item link in the chat log.</returns>
    public static ReadOnlySeString CreateItemLink(uint itemId, ItemKind kind = ItemKind.Normal, string? displayNameOverride = null)
    {
        var clientState = Service<ClientState.ClientState>.Get();
        var evaluator = Service<SeStringEvaluator>.Get();

        var rawId = ItemUtil.GetRawId(itemId, kind);

        var displayName = displayNameOverride ?? ItemUtil.GetItemName(rawId);
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
    public static unsafe ReadOnlySeString CreateMapLink(IGameObject obj)
    {
        var territoryId = GameMain.Instance()->CurrentTerritoryTypeId;
        if (territoryId == 0)
            return default;

        var mapId = TerritoryInfo.Instance()->ChatLinkMapIdOverride;
        if (mapId == 0)
            mapId = GameMain.Instance()->CurrentMapId;
        if (mapId == 0)
            mapId = MapUtil.ResolveMapId(territoryId, mapId);
        if (mapId == 0)
            return default;

        var instanceId = (uint)CSFramework.Instance()->GetNetworkModuleProxy()->GetCurrentInstance();
        return CreateMapLink(new WorldCoordinate(territoryId, mapId, obj.Position.X, obj.Position.Z, obj.Position.Y, instanceId));
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
    /// <returns>A ReadOnlySeString containing all of the macros necessary to display a map link in the chat log.</returns>
    public static ReadOnlySeString CreateMapLink(WorldCoordinate worldPos)
    {
        using var rssb = new RentedSeStringBuilder();

        var rawZ = MapUtil.NoRawZ;

        if (Service<DataManager>.Get().GetExcelSheet<TerritoryTypeTransient>().TryGetRow(worldPos.TerritoryId, out var transient) && transient.OffsetZ != MapUtil.NoOffsetZ)
        {
            var adjustedZ = worldPos.Z - transient.OffsetZ;
            rawZ = (int)adjustedZ;
            if (adjustedZ < 0.0 && adjustedZ != rawZ)
                rawZ -= 10;
        }

        return Service<SeStringEvaluator>.Get().Evaluate(rssb
                .BeginMacro(MacroCode.Fixed)
                .AppendIntExpression(200)
                .AppendIntExpression(3)
                .AppendUIntExpression(worldPos.TerritoryId)
                .AppendUIntExpression((worldPos.InstanceId << 16) | worldPos.MapId)
                .AppendIntExpression((int)(worldPos.X * 1000f))
                .AppendIntExpression((int)(worldPos.Y * 1000f))
                .AppendIntExpression(rawZ)
                .AppendUIntExpression(0)
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
