using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Dalamud.Data;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace Dalamud.Game.Text.SeStringHandling;

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
    public static IEnumerable<Payload> TextArrowPayloads
    {
        get
        {
            var clientState = Service<ClientState.ClientState>.Get();
            var markerSpace = clientState.ClientLanguage switch
            {
                ClientLanguage.German => " ",
                ClientLanguage.French => " ",
                _ => string.Empty,
            };
            return new List<Payload>
            {
                new UIForegroundPayload(500),
                new UIGlowPayload(501),
                new TextPayload($"{(char)SeIconChar.LinkMarker}{markerSpace}"),
                UIGlowPayload.UIGlowOff,
                UIForegroundPayload.UIForegroundOff,
            };
        }
    }

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
        var rarity = 1; // default: white
        if (displayName == null)
        {
            switch (kind)
            {
                case ItemPayload.ItemKind.Normal:
                case ItemPayload.ItemKind.Collectible:
                case ItemPayload.ItemKind.Hq:
                    var item = data.GetExcelSheet<Item>()?.GetRow(itemId);
                    displayName = item?.Name;
                    rarity = item?.Rarity ?? 1;
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

        var textColor = (ushort)(549 + ((rarity - 1) * 2));
        var textGlowColor = (ushort)(textColor + 1);

        // Note: `SeStringBuilder.AddItemLink` uses this function, so don't call it here!
        return new SeStringBuilder()
            .AddUiForeground(textColor)
            .AddUiGlow(textGlowColor)
            .Add(new ItemPayload(itemId, kind))
            .Append(TextArrowPayloads)
            .AddText(displayName)
            .AddUiGlowOff()
            .AddUiForegroundOff()
            .Add(RawPayload.LinkTerminator)
            .Build();
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
    public static SeString CreateMapLink(uint territoryId, uint mapId, int rawX, int rawY) =>
        CreateMapLinkWithInstance(territoryId, mapId, null, rawX, rawY);

    /// <summary>
    /// Creates an SeString representing an entire Payload chain that can be used to link a map position in the chat log.
    /// </summary>
    /// <param name="territoryId">The id of the TerritoryType for this map link.</param>
    /// <param name="mapId">The id of the Map for this map link.</param>
    /// <param name="instance">An optional area instance number to be included in this link.</param>
    /// <param name="rawX">The raw x-coordinate for this link.</param>
    /// <param name="rawY">The raw y-coordinate for this link..</param>
    /// <returns>An SeString containing all of the payloads necessary to display a map link in the chat log.</returns>
    public static SeString CreateMapLinkWithInstance(uint territoryId, uint mapId, int? instance, int rawX, int rawY)
    {
        var mapPayload = new MapLinkPayload(territoryId, mapId, rawX, rawY);
        var nameString = GetMapLinkNameString(mapPayload.PlaceName, instance, mapPayload.CoordinateString);

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
    public static SeString CreateMapLink(
        uint territoryId, uint mapId, float xCoord, float yCoord, float fudgeFactor = 0.05f) =>
        CreateMapLinkWithInstance(territoryId, mapId, null, xCoord, yCoord, fudgeFactor);
    
    /// <summary>
    /// Creates an SeString representing an entire Payload chain that can be used to link a map position in the chat log.
    /// </summary>
    /// <param name="territoryId">The id of the TerritoryType for this map link.</param>
    /// <param name="mapId">The id of the Map for this map link.</param>
    /// <param name="instance">An optional area instance number to be included in this link.</param>
    /// <param name="xCoord">The human-readable x-coordinate for this link.</param>
    /// <param name="yCoord">The human-readable y-coordinate for this link.</param>
    /// <param name="fudgeFactor">An optional offset to account for rounding and truncation errors; it is best to leave this untouched in most cases.</param>
    /// <returns>An SeString containing all of the payloads necessary to display a map link in the chat log.</returns>
    public static SeString CreateMapLinkWithInstance(uint territoryId, uint mapId, int? instance, float xCoord, float yCoord, float fudgeFactor = 0.05f)
    {
        var mapPayload = new MapLinkPayload(territoryId, mapId, xCoord, yCoord, fudgeFactor);
        var nameString = GetMapLinkNameString(mapPayload.PlaceName, instance, mapPayload.CoordinateString);

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
    public static SeString? CreateMapLink(string placeName, float xCoord, float yCoord, float fudgeFactor = 0.05f) =>
        CreateMapLinkWithInstance(placeName, null, xCoord, yCoord, fudgeFactor);
    
    /// <summary>
    /// Creates an SeString representing an entire Payload chain that can be used to link a map position in the chat log, matching a specified zone name.
    /// Returns null if no corresponding PlaceName was found.
    /// </summary>
    /// <param name="placeName">The name of the location for this link.  This should be exactly the name as seen in a displayed map link in-game for the same zone.</param>
    /// <param name="instance">An optional area instance number to be included in this link.</param>
    /// <param name="xCoord">The human-readable x-coordinate for this link.</param>
    /// <param name="yCoord">The human-readable y-coordinate for this link.</param>
    /// <param name="fudgeFactor">An optional offset to account for rounding and truncation errors; it is best to leave this untouched in most cases.</param>
    /// <returns>An SeString containing all of the payloads necessary to display a map link in the chat log.</returns>
    public static SeString? CreateMapLinkWithInstance(string placeName, int? instance, float xCoord, float yCoord, float fudgeFactor = 0.05f)
    {
        var data = Service<DataManager>.Get();

        var mapSheet = data.GetExcelSheet<Map>();

        var matches = data.GetExcelSheet<PlaceName>()
                          .Where(row => row.Name.ToString().ToLowerInvariant() == placeName.ToLowerInvariant())
                          .ToArray();

        foreach (var place in matches)
        {
            var map = mapSheet.FirstOrDefault(row => row.PlaceName.Row == place.RowId);
            if (map != null && map.TerritoryType.Row != 0)
            {
                return CreateMapLinkWithInstance(map.TerritoryType.Row, map.RowId, instance, xCoord, yCoord, fudgeFactor);
            }
        }

        // TODO: empty? throw?
        return null;
    }

    /// <summary>
    /// Creates an SeString representing an entire payload chain that can be used to link party finder listings in the chat log.
    /// </summary>
    /// <param name="listingId">The listing ID of the party finder entry.</param>
    /// <param name="recruiterName">The name of the recruiter.</param>
    /// <param name="isCrossWorld">Whether the listing is limited to the current world or not.</param>
    /// <returns>An SeString containing all the payloads necessary to display a party finder link in the chat log.</returns>
    public static SeString CreatePartyFinderLink(uint listingId, string recruiterName, bool isCrossWorld = false)
    {
        var payloads = new List<Payload>()
        {
            new PartyFinderPayload(listingId, isCrossWorld ? PartyFinderPayload.PartyFinderLinkType.NotSpecified : PartyFinderPayload.PartyFinderLinkType.LimitedToHomeWorld),
            // ->
            new TextPayload($"Looking for Party ({recruiterName})" + (isCrossWorld ? " " : string.Empty)),
        };

        payloads.InsertRange(1, TextArrowPayloads);

        if (isCrossWorld)
            payloads.Add(new IconPayload(BitmapFontIcon.CrossWorld));

        payloads.Add(RawPayload.LinkTerminator);

        return new SeString(payloads);
    }

    /// <summary>
    /// Creates an SeString representing an entire payload chain that can be used to link the party finder search conditions.
    /// </summary>
    /// <param name="message">The text that should be displayed for the link.</param>
    /// <returns>An SeString containing all the payloads necessary to display a link to the party finder search conditions.</returns>
    public static SeString CreatePartyFinderSearchConditionsLink(string message)
    {
        var payloads = new List<Payload>()
        {
            new PartyFinderPayload(),
            // ->
            new TextPayload(message),
        };
        payloads.InsertRange(1, TextArrowPayloads);
        payloads.Add(RawPayload.LinkTerminator);

        return new SeString(payloads);
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
    public SeString Append(IEnumerable<Payload> payloads)
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
    
    private static string GetMapLinkNameString(string placeName, int? instance, string coordinateString)
    {
        var instanceString = string.Empty;
        if (instance is > 0 and < 10)
        {
            instanceString = (SeIconChar.Instance1 + instance.Value - 1).ToIconString();
        }
        
        return $"{placeName}{instanceString} {coordinateString}";
    }
}
