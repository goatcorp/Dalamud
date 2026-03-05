using Dalamud.Data;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.Evaluator;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

using Lumina.Excel.Sheets;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using CSFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
using MapSheet = Lumina.Excel.Sheets.Map;

namespace Dalamud.Game.Text.SeStringHandling;

/// <summary>
/// This class represents a parsed SeString.
/// </summary>
public static class SeString
{
    /// <summary>
    /// Creates an SeString representing an entire Payload chain that can be used to link an item in the chat log.
    /// </summary>
    /// <param name="itemId">The id of the item to link.</param>
    /// <param name="isHq">Whether to link the high-quality variant of the item.</param>
    /// <param name="displayNameOverride">An optional name override to display, instead of the actual item name.</param>
    /// <returns>An SeString containing all the macros necessary to display an item link in the chat log.</returns>
    public static ReadOnlySeString CreateItemLink(uint itemId, bool isHq, string? displayNameOverride = null)
        => CreateItemLink(itemId, isHq ? ItemKind.Hq : ItemKind.Normal, displayNameOverride);

    /// <summary>
    /// Creates an SeString representing an entire Payload chain that can be used to link an item in the chat log.
    /// </summary>
    /// <param name="item">The Lumina Item to link.</param>
    /// <param name="isHq">Whether to link the high-quality variant of the item.</param>
    /// <param name="displayNameOverride">An optional name override to display, instead of the actual item name.</param>
    /// <returns>An SeString containing all the macros necessary to display an item link in the chat log.</returns>
    public static ReadOnlySeString CreateItemLink(Item item, bool isHq, string? displayNameOverride = null)
        => CreateItemLink(item.RowId, isHq, displayNameOverride ?? item.Name.ExtractText());

    /// <summary>
    /// Creates an SeString representing an entire Payload chain that can be used to link an item in the chat log.
    /// </summary>
    /// <param name="itemId">The id of the item to link.</param>
    /// <param name="kind">The kind of item to link.</param>
    /// <param name="displayNameOverride">An optional name override to display, instead of the actual item name.</param>
    /// <returns>An SeString containing all the macros necessary to display an item link in the chat log.</returns>
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

        var itemLink = rssb.Builder
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
    /// Creates an SeString representing an entire payload chain that can be used to link a map position of a GameObject in the chat log.
    /// </summary>
    /// <param name="obj">The GameObject, which position should be used.</param>
    /// <returns>An SeString containing all of the macros necessary to display a map link in the chat log.</returns>
    public static unsafe ReadOnlySeString CreateMapLink(IGameObject obj)
    {
        var territoryId = GameMain.Instance()->CurrentTerritoryTypeId;
        if (territoryId == 0)
            return default;

        var mapId = TerritoryInfo.Instance()->ChatLinkMapIdOverride;
        if (mapId == 0)
            mapId = GameMain.Instance()->CurrentMapId;
        if (mapId == 0)
            return default;

        var instanceId = CSFramework.Instance()->GetNetworkModuleProxy()->GetCurrentInstance();
        return CreateMapLink(territoryId, mapId, obj.Position.X, obj.Position.Z, obj.Position.Y, instanceId);
    }

    /// <summary>
    /// Creates an SeString representing an entire payload chain that can be used to link a map position in the chat log.
    /// </summary>
    /// <param name="territoryId">The id of the TerritoryType for this map link.</param>
    /// <param name="mapId">The id of the Map for this map link.</param>
    /// <param name="xCoord">The human-readable x-coordinate for this link.</param>
    /// <param name="yCoord">The human-readable y-coordinate for this link.</param>
    /// <param name="zCoord">An optional human-readable z-coordinate for this link.</param>
    /// <param name="instanceId">An optional area instance number to be included in this link.</param>
    /// <param name="fudgeFactor">An optional offset to account for rounding and truncation errors; it is best to leave this untouched in most cases.</param>
    /// <returns>An SeString containing all of the macros necessary to display a map link in the chat log.</returns>
    public static ReadOnlySeString CreateMapLink(uint territoryId, uint mapId, float xCoord, float yCoord, float zCoord = 0, int instanceId = 0, float fudgeFactor = 0.05f)
    {
        if (territoryId == 0)
            return default;

        if (mapId == 0)
            return default;

        var dataManager = Service<DataManager>.Get();

        if (!dataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var territoryType))
            return default;

        if (!dataManager.GetExcelSheet<MapSheet>().TryGetRow(mapId, out var map))
            return default;

        if (!dataManager.GetExcelSheet<PlaceName>().TryGetRow(territoryType.PlaceName.RowId, out var placeName))
            return default;

        var evaluator = Service<SeStringEvaluator>.Get();

        using var rssb = new RentedSeStringBuilder();
        var sb = rssb.Builder;

        sb.Append(placeName.Name);

        if (instanceId > 0)
            sb.Append((char)(SeIconChar.Instance1 + (byte)(instanceId - 1)));

        var placeNameWithInstance = sb.ToReadOnlySeString();

        var mapPosX = MapUtil.ConvertRawToMapPosX(map, xCoord);
        var mapPosY = MapUtil.ConvertRawToMapPosY(map, yCoord);

        ReadOnlySeString linkText;
        if (!dataManager.GetExcelSheet<TerritoryTypeTransient>().TryGetRow(territoryId, out var territoryTransient) && territoryTransient.OffsetZ != -10000)
        {
            var zFloat = zCoord - territoryTransient.OffsetZ;
            var z = (uint)(int)zFloat;
            if (zFloat < 0.0 && zFloat != (int)z)
                z -= 10;
            z /= 10;

            linkText = evaluator.EvaluateFromAddon(1636, [placeNameWithInstance, mapPosX, mapPosY, z]);
        }
        else
        {
            linkText = evaluator.EvaluateFromAddon(1635, [placeNameWithInstance, mapPosX, mapPosY]);
        }

        sb.Clear();

        var mapLink = sb
            .PushLinkMapPosition(territoryId, mapId, (int)(xCoord * 1000f), (int)(yCoord * 1000f))
            .Append(linkText)
            .PopLink()
            .ToReadOnlySeString();

        // Link Marker
        return evaluator.EvaluateFromAddon(371, [mapLink]);
    }

    /// <summary>
    /// Creates an SeString representing an entire payload chain that can be used to link party finder listings in the chat log.
    /// </summary>
    /// <param name="listingId">The listing ID of the party finder entry.</param>
    /// <param name="recruiterName">The name of the recruiter.</param>
    /// <param name="isCrossWorld">Whether the listing is limited to the current world or not.</param>
    /// <returns>An SeString containing all the macros necessary to display a party finder link in the chat log.</returns>
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
    /// Creates an SeString representing an entire payload chain that can be used to link the party finder search conditions.
    /// </summary>
    /// <param name="message">The text that should be displayed for the link.</param>
    /// <returns>An SeString containing all the macros necessary to display a link to the party finder search conditions.</returns>
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
