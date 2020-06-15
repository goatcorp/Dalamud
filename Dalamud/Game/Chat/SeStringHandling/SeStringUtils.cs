using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalamud.Game.Chat.SeStringHandling
{
    /// <summary>
    /// A utility class for working with common SeString variants.
    /// </summary>
    public static class SeStringUtils
    {
        /// <summary>
        /// Creates an SeString representing an entire Payload chain that can be used to link an item in the chat log.
        /// </summary>
        /// <param name="itemId">The id of the item to link.</param>
        /// <param name="isHQ">Whether to link the high-quality variant of the item.</param>
        /// <param name="displayNameOverride">An optional name override to display, instead of the actual item name.</param>
        /// <returns>An SeString containing all the payloads necessary to display an item link in the chat log.</returns>
        public static SeString CreateItemLink(uint itemId, bool isHQ, string displayNameOverride = null)
        {
            string displayName = displayNameOverride ?? SeString.Dalamud.Data.GetExcelSheet<Item>().GetRow(itemId).Name;
            if (isHQ)
            {
                displayName += $" {(char)SeIconChar.HighQuality}";
            }

            // TODO: probably a cleaner way to build these than doing the bulk+insert
            var payloads = new List<Payload>(new Payload[]
            {
                new UIForegroundPayload(0x0225),
                new UIGlowPayload(0x0226),
                new ItemPayload(itemId, isHQ),
                // arrow goes here
                new TextPayload(displayName),
                RawPayload.LinkTerminator
                // sometimes there is another set of uiglow/foreground off payloads here
                // might be necessary when including additional text after the item name
            });
            payloads.InsertRange(3, TextArrowPayloads());

            return new SeString(payloads);
        }

        /// <summary>
        /// Creates an SeString representing an entire Payload chain that can be used to link an item in the chat log.
        /// </summary>
        /// <param name="item">The Lumina Item to link.</param>
        /// <param name="isHQ">Whether to link the high-quality variant of the item.</param>
        /// <param name="displayNameOverride">An optional name override to display, instead of the actual item name.</param>
        /// <returns>An SeString containing all the payloads necessary to display an item link in the chat log.</returns>
        public static SeString CreateItemLink(Item item, bool isHQ, string displayNameOverride = null)
        {
            return CreateItemLink((uint)item.RowId, isHQ, displayNameOverride ?? item.Name);
        }

        public static SeString CreateMapLink(uint territoryId, uint mapId, int rawX, int rawY)
        {
            var mapPayload = new MapLinkPayload(territoryId, mapId, rawX, rawY);
            var nameString = $"{mapPayload.PlaceName} {mapPayload.CoordinateString}";

            var payloads = new List<Payload>(new Payload[]
            {
                mapPayload,
                // arrow goes here
                new TextPayload(nameString),
                RawPayload.LinkTerminator
            });
            payloads.InsertRange(1, TextArrowPayloads());

            return new SeString(payloads);
        }

        /// <summary>
        /// Creates an SeString representing an entire Payload chain that can be used to link a map position in the chat log.
        /// </summary>
        /// <param name="territoryId">The id of the TerritoryType for this map link.</param>
        /// <param name="mapId">The id of the Map for this map link.</param>
        /// <param name="xCoord">The human-readable x-coordinate for this link.</param>
        /// <param name="yCoord">The human-readable y-coordinate for this link.</param>
        /// <param name="fudgeFactor">An optional offset to account for rounding and truncation errors; it is best to leave this untouched in most cases.</param>
        /// <returns>An SeString containing all of the payloads necessary to display a map link in the chat log.</returns>
        public static SeString CreateMapLink(uint territoryId, uint mapId, float xCoord, float yCoord, float fudgeFactor = 0.05f)
        {
            var mapPayload = new MapLinkPayload(territoryId, mapId, xCoord, yCoord, fudgeFactor);
            var nameString = $"{mapPayload.PlaceName} {mapPayload.CoordinateString}";

            var payloads = new List<Payload>(new Payload[]
            {
                mapPayload,
                // arrow goes here
                new TextPayload(nameString),
                RawPayload.LinkTerminator
            });
            payloads.InsertRange(1, TextArrowPayloads());

            return new SeString(payloads);
        }

        /// <summary>
        /// Creates an SeString representing an entire Payload chain that can be used to link a map position in the chat log, matching a specified zone name.
        /// </summary>
        /// <param name="placeName">The name of the location for this link.  This should be exactly the name as seen in a displayed map link in-game for the same zone.</param>
        /// <param name="xCoord">The human-readable x-coordinate for this link.</param>
        /// <param name="yCoord">The human-readable y-coordinate for this link.</param>
        /// <param name="fudgeFactor">An optional offset to account for rounding and truncation errors; it is best to leave this untouched in most cases.</param>
        /// <returns>An SeString containing all of the payloads necessary to display a map link in the chat log.</returns>
        public static SeString CreateMapLink(string placeName, float xCoord, float yCoord, float fudgeFactor = 0.05f)
        {
            var mapSheet = SeString.Dalamud.Data.GetExcelSheet<Map>();

            var matches = SeString.Dalamud.Data.GetExcelSheet<PlaceName>().GetRows()
                .Where(row => row.Name.ToLowerInvariant() == placeName.ToLowerInvariant())
                .ToArray();

            foreach (var place in matches)
            {
                var map = mapSheet.GetRows().FirstOrDefault(row => row.PlaceName.Row == place.RowId);
                if (map != null)
                {
                    return CreateMapLink(map.TerritoryType.Row, (uint)map.RowId, xCoord, yCoord);
                }
            }

            // TODO: empty? throw?
            return null;
        }

        /// <summary>
        /// Creates a list of Payloads necessary to display the arrow link marker icon in chat
        /// with the appropriate glow and coloring.
        /// </summary>
        /// <returns>A list of all the payloads required to insert the link marker.</returns>
        public static List<Payload> TextArrowPayloads()
        {
            return new List<Payload>(new Payload[]
            {
                new UIForegroundPayload(0x01F4),
                new UIGlowPayload(0x01F5),
                new TextPayload($"{(char)SeIconChar.LinkMarker}"),
                UIGlowPayload.UIGlowOff,
                UIForegroundPayload.UIForegroundOff
            });
        }
    }
}
