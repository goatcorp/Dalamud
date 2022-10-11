using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Dalamud.Data;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace Dalamud.Game.Text.SeStringHandling
{
    /// <summary>
    /// This class represents a parsed SeString.
    /// </summary>
    public class SeString
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SeString"/> class.
        /// Creates a new SeString from an ordered list of payloads.
        /// </summary>
        public SeString()
        {
            this.Payloads = new List<Payload>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SeString"/> class.
        /// Creates a new SeString from an ordered list of payloads.
        /// </summary>
        /// <param name="payloads">The Payload objects to make up this string.</param>
        [JsonConstructor]
        public SeString(List<Payload> payloads)
        {
            this.Payloads = payloads;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SeString"/> class.
        /// Creates a new SeString from an ordered list of payloads.
        /// </summary>
        /// <param name="payloads">The Payload objects to make up this string.</param>
        public SeString(params Payload[] payloads)
        {
            this.Payloads = new List<Payload>(payloads);
        }

        /// <summary>
        /// Gets a list of Payloads necessary to display the arrow link marker icon in chat
        /// with the appropriate glow and coloring.
        /// </summary>
        /// <returns>A list of all the payloads required to insert the link marker.</returns>
        public static IEnumerable<Payload> TextArrowPayloads => new List<Payload>(new Payload[]
        {
            new UIForegroundPayload(0x01F4),
            new UIGlowPayload(0x01F5),
            new TextPayload($"{(char)SeIconChar.LinkMarker}"),
            UIGlowPayload.UIGlowOff,
            UIForegroundPayload.UIForegroundOff,
        });

        /// <summary>
        /// Gets an empty SeString.
        /// </summary>
        public static SeString Empty => new();

        /// <summary>
        /// Gets the ordered list of payloads included in this SeString.
        /// </summary>
        public List<Payload> Payloads { get; }

        /// <summary>
        /// Gets all of the raw text from a message as a single joined string.
        /// </summary>
        /// <returns>
        /// All the raw text from the contained payloads, joined into a single string.
        /// </returns>
        public string TextValue
        {
            get
            {
                return this.Payloads
                    .Where(p => p is ITextProvider)
                    .Cast<ITextProvider>()
                    .Aggregate(new StringBuilder(), (sb, tp) => sb.Append(tp.Text), sb => sb.ToString());
            }
        }

        /// <summary>
        /// Implicitly convert a string into a SeString containing a <see cref="TextPayload"/>.
        /// </summary>
        /// <param name="str">string to convert.</param>
        /// <returns>Equivalent SeString.</returns>
        public static implicit operator SeString(string str) => new(new TextPayload(str));

        /// <summary>
        /// Implicitly convert a string into a SeString containing a <see cref="TextPayload"/>.
        /// </summary>
        /// <param name="str">string to convert.</param>
        /// <returns>Equivalent SeString.</returns>
        public static explicit operator SeString(Lumina.Text.SeString str) => str.ToDalamudString();

        /// <summary>
        /// Parse a binary game message into an SeString.
        /// </summary>
        /// <param name="ptr">Pointer to the string's data in memory.</param>
        /// <param name="len">Length of the string's data in memory.</param>
        /// <returns>An SeString containing parsed Payload objects for each payload in the data.</returns>
        public static unsafe SeString Parse(byte* ptr, int len)
        {
            if (ptr == null)
                return Empty;

            var payloads = new List<Payload>();

            using (var stream = new UnmanagedMemoryStream(ptr, len))
            using (var reader = new BinaryReader(stream))
            {
                while (stream.Position < len)
                {
                    var payload = Payload.Decode(reader);
                    if (payload != null)
                        payloads.Add(payload);
                }
            }

            return new SeString(payloads);
        }

        /// <summary>
        /// Parse a binary game message into an SeString.
        /// Searches for null terminator to calculate string length.
        /// </summary>
        /// <param name="ptr">Pointer to the string's data in memory.</param>
        /// <returns>An SeString containing parsed Payload objects for each payload in the data.</returns>
        public static unsafe SeString Parse(byte* ptr)
        {
            if (ptr == null)
                return Empty;

            var countPtr = ptr;
            while (*countPtr != 0) countPtr++;

            var len = (int)(countPtr - ptr);

            return Parse(ptr, len);
        }

        /// <summary>
        /// Parse a binary game message into an SeString.
        /// </summary>
        /// <param name="data">Binary message payload data in SE's internal format.</param>
        /// <returns>An SeString containing parsed Payload objects for each payload in the data.</returns>
        public static unsafe SeString Parse(ReadOnlySpan<byte> data)
        {
            fixed (byte* ptr = data)
            {
                return Parse(ptr, data.Length);
            }
        }

        /// <summary>
        /// Parse a binary game message into an SeString.
        /// </summary>
        /// <param name="bytes">Binary message payload data in SE's internal format.</param>
        /// <returns>An SeString containing parsed Payload objects for each payload in the data.</returns>
        public static SeString Parse(byte[] bytes) => Parse(new ReadOnlySpan<byte>(bytes));

        /// <summary>
        /// Creates an SeString representing an entire Payload chain that can be used to link an item in the chat log.
        /// </summary>
        /// <param name="itemId">The id of the item to link.</param>
        /// <param name="isHq">Whether to link the high-quality variant of the item.</param>
        /// <param name="displayNameOverride">An optional name override to display, instead of the actual item name.</param>
        /// <returns>An SeString containing all the payloads necessary to display an item link in the chat log.</returns>
        public static SeString CreateItemLink(uint itemId, bool isHq, string? displayNameOverride = null) =>
            CreateItemLink(itemId, isHq ? ItemPayload.ItemKind.Hq : ItemPayload.ItemKind.Normal, displayNameOverride);

        /// <summary>
        /// Creates an SeString representing an entire Payload chain that can be used to link an item in the chat log.
        /// </summary>
        /// <param name="itemId">The id of the item to link.</param>
        /// <param name="kind">The kind of item to link.</param>
        /// <param name="displayNameOverride">An optional name override to display, instead of the actual item name.</param>
        /// <returns>An SeString containing all the payloads necessary to display an item link in the chat log.</returns>
        public static SeString CreateItemLink(uint itemId, ItemPayload.ItemKind kind = ItemPayload.ItemKind.Normal, string? displayNameOverride = null)
        {
            var data = Service<DataManager>.Get();

            var displayName = displayNameOverride;
            if (displayName == null)
            {
                switch (kind)
                {
                    case ItemPayload.ItemKind.Normal:
                    case ItemPayload.ItemKind.Collectible:
                    case ItemPayload.ItemKind.Hq:
                        displayName = data.GetExcelSheet<Item>()?.GetRow(itemId)?.Name;
                        break;
                    case ItemPayload.ItemKind.EventItem:
                        displayName = data.GetExcelSheet<EventItem>()?.GetRow(itemId)?.Name;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
                }
            }

            if (displayName == null)
            {
                throw new Exception("Invalid item ID specified, could not determine item name.");
            }

            if (kind == ItemPayload.ItemKind.Hq)
            {
                displayName += $" {(char)SeIconChar.HighQuality}";
            }
            else if (kind == ItemPayload.ItemKind.Collectible)
            {
                displayName += $" {(char)SeIconChar.Collectible}";
            }

            // TODO: probably a cleaner way to build these than doing the bulk+insert
            var payloads = new List<Payload>(new Payload[]
            {
                new UIForegroundPayload(0x0225),
                new UIGlowPayload(0x0226),
                new ItemPayload(itemId, kind),
                // arrow goes here
                new TextPayload(displayName),
                RawPayload.LinkTerminator,
                // sometimes there is another set of uiglow/foreground off payloads here
                // might be necessary when including additional text after the item name
            });
            payloads.InsertRange(3, TextArrowPayloads);

            return new SeString(payloads);
        }

        /// <summary>
        /// Creates an SeString representing an entire Payload chain that can be used to link an item in the chat log.
        /// </summary>
        /// <param name="item">The Lumina Item to link.</param>
        /// <param name="isHq">Whether to link the high-quality variant of the item.</param>
        /// <param name="displayNameOverride">An optional name override to display, instead of the actual item name.</param>
        /// <returns>An SeString containing all the payloads necessary to display an item link in the chat log.</returns>
        public static SeString CreateItemLink(Item item, bool isHq, string? displayNameOverride = null)
        {
            return CreateItemLink(item.RowId, isHq, displayNameOverride ?? item.Name);
        }

        /// <summary>
        /// Creates an SeString representing an entire Payload chain that can be used to link a map position in the chat log.
        /// </summary>
        /// <param name="territoryId">The id of the TerritoryType for this map link.</param>
        /// <param name="mapId">The id of the Map for this map link.</param>
        /// <param name="rawX">The raw x-coordinate for this link.</param>
        /// <param name="rawY">The raw y-coordinate for this link..</param>
        /// <returns>An SeString containing all of the payloads necessary to display a map link in the chat log.</returns>
        public static SeString CreateMapLink(uint territoryId, uint mapId, int rawX, int rawY)
        {
            var mapPayload = new MapLinkPayload(territoryId, mapId, rawX, rawY);
            var nameString = $"{mapPayload.PlaceName} {mapPayload.CoordinateString}";

            var payloads = new List<Payload>(new Payload[]
            {
                mapPayload,
                // arrow goes here
                new TextPayload(nameString),
                RawPayload.LinkTerminator,
            });
            payloads.InsertRange(1, TextArrowPayloads);

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
                RawPayload.LinkTerminator,
            });
            payloads.InsertRange(1, TextArrowPayloads);

            return new SeString(payloads);
        }

        /// <summary>
        /// Creates an SeString representing an entire Payload chain that can be used to link a map position in the chat log, matching a specified zone name.
        /// Returns null if no corresponding PlaceName was found.
        /// </summary>
        /// <param name="placeName">The name of the location for this link.  This should be exactly the name as seen in a displayed map link in-game for the same zone.</param>
        /// <param name="xCoord">The human-readable x-coordinate for this link.</param>
        /// <param name="yCoord">The human-readable y-coordinate for this link.</param>
        /// <param name="fudgeFactor">An optional offset to account for rounding and truncation errors; it is best to leave this untouched in most cases.</param>
        /// <returns>An SeString containing all of the payloads necessary to display a map link in the chat log.</returns>
        public static SeString? CreateMapLink(string placeName, float xCoord, float yCoord, float fudgeFactor = 0.05f)
        {
            var data = Service<DataManager>.Get();

            var mapSheet = data.GetExcelSheet<Map>();

            var matches = data.GetExcelSheet<PlaceName>()
                              .Where(row => row.Name.ToString().ToLowerInvariant() == placeName.ToLowerInvariant())
                              .ToArray();

            foreach (var place in matches)
            {
                var map = mapSheet.FirstOrDefault(row => row.PlaceName.Row == place.RowId);
                if (map != null)
                {
                    return CreateMapLink(map.TerritoryType.Row, map.RowId, xCoord, yCoord, fudgeFactor);
                }
            }

            // TODO: empty? throw?
            return null;
        }

        /// <summary>
        /// Creates a SeString from a json. (For testing - not recommended for production use.)
        /// </summary>
        /// <param name="json">A serialized SeString produced by ToJson() <see cref="ToJson"/>.</param>
        /// <returns>A SeString initialized with values from the json.</returns>
        public static SeString? FromJson(string json)
        {
            var s = JsonConvert.DeserializeObject<SeString>(json, new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                TypeNameHandling = TypeNameHandling.Auto,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            });

            return s;
        }

        /// <summary>
        /// Serializes the SeString to json.
        /// </summary>
        /// <returns>An json representation of this object.</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings()
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
            });
        }

        /// <summary>
        /// Appends the contents of one SeString to this one.
        /// </summary>
        /// <param name="other">The SeString to append to this one.</param>
        /// <returns>This object.</returns>
        public SeString Append(SeString other)
        {
            this.Payloads.AddRange(other.Payloads);
            return this;
        }

        /// <summary>
        /// Appends a list of payloads to this SeString.
        /// </summary>
        /// <param name="payloads">The Payloads to append.</param>
        /// <returns>This object.</returns>
        public SeString Append(List<Payload> payloads)
        {
            this.Payloads.AddRange(payloads);
            return this;
        }

        /// <summary>
        /// Appends a single payload to this SeString.
        /// </summary>
        /// <param name="payload">The payload to append.</param>
        /// <returns>This object.</returns>
        public SeString Append(Payload payload)
        {
            this.Payloads.Add(payload);
            return this;
        }

        /// <summary>
        /// Encodes the Payloads in this SeString into a binary representation
        /// suitable for use by in-game handlers, such as the chat log.
        /// </summary>
        /// <returns>The binary encoded payload data.</returns>
        public byte[] Encode()
        {
            var messageBytes = new List<byte>();
            foreach (var p in this.Payloads)
            {
                messageBytes.AddRange(p.Encode());
            }

            return messageBytes.ToArray();
        }

        /// <summary>
        /// Get the text value of this SeString.
        /// </summary>
        /// <returns>The TextValue property.</returns>
        public override string ToString()
        {
            return this.TextValue;
        }
    }
}
