using System.Collections.Generic;
using System.IO;
using System.Linq;

using Dalamud.Data;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Game.Text.SeStringHandling
{
    /// <summary>
    /// This class facilitates creating new SeStrings and breaking down existing ones into their individual payload components.
    /// </summary>
    public class SeStringManager
    {
        private readonly DataManager data;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeStringManager"/> class.
        /// </summary>
        /// <param name="data">The DataManager instance.</param>
        public SeStringManager(DataManager data)
        {
            this.data = data;
        }

        /// <summary>
        /// Parse a binary game message into an SeString.
        /// </summary>
        /// <param name="bytes">Binary message payload data in SE's internal format.</param>
        /// <returns>An SeString containing parsed Payload objects for each payload in the data.</returns>
        public SeString Parse(byte[] bytes)
        {
            var payloads = new List<Payload>();

            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
            {
                while (stream.Position < bytes.Length)
                {
                    var payload = Payload.Decode(reader, this.data);
                    if (payload != null)
                        payloads.Add(payload);
                }
            }

            return new SeString(payloads);
        }

        /// <summary>
        /// Creates an SeString representing an entire Payload chain that can be used to link an item in the chat log.
        /// </summary>
        /// <param name="itemId">The id of the item to link.</param>
        /// <param name="isHQ">Whether to link the high-quality variant of the item.</param>
        /// <param name="displayNameOverride">An optional name override to display, instead of the actual item name.</param>
        /// <returns>An SeString containing all the payloads necessary to display an item link in the chat log.</returns>
        public SeString CreateItemLink(uint itemId, bool isHQ, string displayNameOverride = null)
        {
            var displayName = displayNameOverride ?? this.data.GetExcelSheet<Item>().GetRow(itemId).Name;
            if (isHQ)
            {
                displayName += $" {(char)SeIconChar.HighQuality}";
            }

            // TODO: probably a cleaner way to build these than doing the bulk+insert
            var payloads = new List<Payload>(new Payload[]
            {
                new UIForegroundPayload(this.data, 0x0225),
                new UIGlowPayload(this.data, 0x0226),
                new ItemPayload(this.data, itemId, isHQ),
                // arrow goes here
                new TextPayload(displayName),
                RawPayload.LinkTerminator,
                // sometimes there is another set of uiglow/foreground off payloads here
                // might be necessary when including additional text after the item name
            });
            payloads.InsertRange(3, this.TextArrowPayloads());

            return new SeString(payloads);
        }

        /// <summary>
        /// Creates an SeString representing an entire Payload chain that can be used to link an item in the chat log.
        /// </summary>
        /// <param name="item">The Lumina Item to link.</param>
        /// <param name="isHQ">Whether to link the high-quality variant of the item.</param>
        /// <param name="displayNameOverride">An optional name override to display, instead of the actual item name.</param>
        /// <returns>An SeString containing all the payloads necessary to display an item link in the chat log.</returns>
        public SeString CreateItemLink(Item item, bool isHQ, string displayNameOverride = null)
        {
            return this.CreateItemLink(item.RowId, isHQ, displayNameOverride ?? item.Name);
        }

        /// <summary>
        /// Creates an SeString representing an entire Payload chain that can be used to link a map position in the chat log.
        /// </summary>
        /// <param name="territoryId">The id of the TerritoryType for this map link.</param>
        /// <param name="mapId">The id of the Map for this map link.</param>
        /// <param name="rawX">The raw x-coordinate for this link.</param>
        /// <param name="rawY">The raw y-coordinate for this link..</param>
        /// <returns>An SeString containing all of the payloads necessary to display a map link in the chat log.</returns>
        public SeString CreateMapLink(uint territoryId, uint mapId, int rawX, int rawY)
        {
            var mapPayload = new MapLinkPayload(this.data, territoryId, mapId, rawX, rawY);
            var nameString = $"{mapPayload.PlaceName} {mapPayload.CoordinateString}";

            var payloads = new List<Payload>(new Payload[]
            {
                mapPayload,
                // arrow goes here
                new TextPayload(nameString),
                RawPayload.LinkTerminator,
            });
            payloads.InsertRange(1, this.TextArrowPayloads());

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
        public SeString CreateMapLink(uint territoryId, uint mapId, float xCoord, float yCoord, float fudgeFactor = 0.05f)
        {
            var mapPayload = new MapLinkPayload(this.data, territoryId, mapId, xCoord, yCoord, fudgeFactor);
            var nameString = $"{mapPayload.PlaceName} {mapPayload.CoordinateString}";

            var payloads = new List<Payload>(new Payload[]
            {
                mapPayload,
                // arrow goes here
                new TextPayload(nameString),
                RawPayload.LinkTerminator,
            });
            payloads.InsertRange(1, this.TextArrowPayloads());

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
        public SeString CreateMapLink(string placeName, float xCoord, float yCoord, float fudgeFactor = 0.05f)
        {
            var mapSheet = this.data.GetExcelSheet<Map>();

            var matches = this.data.GetExcelSheet<PlaceName>()
                              .Where(row => row.Name.ToString().ToLowerInvariant() == placeName.ToLowerInvariant())
                              .ToArray();

            foreach (var place in matches)
            {
                var map = mapSheet.FirstOrDefault(row => row.PlaceName.Row == place.RowId);
                if (map != null)
                {
                    return this.CreateMapLink(map.TerritoryType.Row, map.RowId, xCoord, yCoord, fudgeFactor);
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
        public List<Payload> TextArrowPayloads()
        {
            return new List<Payload>(new Payload[]
            {
                new UIForegroundPayload(this.data, 0x01F4),
                new UIGlowPayload(this.data, 0x01F5),
                new TextPayload($"{(char)SeIconChar.LinkMarker}"),
                UIGlowPayload.UIGlowOff,
                UIForegroundPayload.UIForegroundOff,
            });
        }
    }
}
