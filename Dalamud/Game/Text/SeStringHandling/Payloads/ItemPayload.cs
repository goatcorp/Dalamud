using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// An SeString Payload representing an interactable item link.
/// </summary>
public class ItemPayload : Payload
{
    private Item? item;

    // mainly to allow overriding the name (for things like owo)
    // TODO: even though this is present in some item links, it may not really have a use at all
    //   For things like owo, changing the text payload is probably correct, whereas changing the
    //   actual embedded name might not work properly.
    private string? displayName;

    [JsonProperty]
    private uint rawItemId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemPayload"/> class.
    /// Creates a payload representing an interactable item link for the specified item.
    /// </summary>
    /// <param name="itemId">The id of the item.</param>
    /// <param name="isHq">Whether or not the link should be for the high-quality variant of the item.</param>
    /// <param name="displayNameOverride">An optional name to include in the item link.  Typically this should
    /// be left as null, or set to the normal item name.  Actual overrides are better done with the subsequent
    /// TextPayload that is a part of a full item link in chat.</param>
    public ItemPayload(uint itemId, bool isHq, string? displayNameOverride = null)
    {
        this.rawItemId = itemId;
        if (isHq)
            this.rawItemId += (uint)ItemKind.Hq;

        this.Kind = isHq ? ItemKind.Hq : ItemKind.Normal;
        this.displayName = displayNameOverride;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemPayload"/> class.
    /// Creates a payload representing an interactable item link for the specified item.
    /// </summary>
    /// <param name="itemId">The id of the item.</param>
    /// <param name="kind">Kind of item to encode.</param>
    /// <param name="displayNameOverride">An optional name to include in the item link.  Typically this should
    /// be left as null, or set to the normal item name.  Actual overrides are better done with the subsequent
    /// TextPayload that is a part of a full item link in chat.</param>
    public ItemPayload(uint itemId, ItemKind kind = ItemKind.Normal, string? displayNameOverride = null)
    {
        this.Kind = kind;
        this.rawItemId = itemId;
        if (kind != ItemKind.EventItem)
            this.rawItemId += (uint)kind;

        this.displayName = displayNameOverride;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemPayload"/> class.
    /// Creates a payload representing an interactable item link for the specified item.
    /// </summary>
    internal ItemPayload()
    {
    }

    /// <summary>
    /// Kinds of items that can be fetched from this payload.
    /// </summary>
    public enum ItemKind : uint
    {
        /// <summary>
        /// Normal items.
        /// </summary>
        Normal,

        /// <summary>
        /// Collectible Items.
        /// </summary>
        Collectible = 500_000,

        /// <summary>
        /// High-Quality items.
        /// </summary>
        Hq = 1_000_000,

        /// <summary>
        /// Event/Key items.
        /// </summary>
        EventItem = 2_000_000,
    }

    /// <inheritdoc/>
    public override PayloadType Type => PayloadType.Item;

    /// <summary>
    /// Gets or sets the displayed name for this item link.  Note that incoming links only sometimes have names embedded,
    /// often the name is only present in a following text payload.
    /// </summary>
    public string? DisplayName
    {
        get
        {
            return this.displayName;
        }

        set
        {
            this.displayName = value;
            this.Dirty = true;
        }
    }

    /// <summary>
    /// Gets the actual item ID of this payload.
    /// </summary>
    [JsonIgnore]
    public uint ItemId => GetAdjustedId(this.rawItemId).ItemId;

    /// <summary>
    /// Gets the raw, unadjusted item ID of this payload.
    /// </summary>
    [JsonIgnore]
    public uint RawItemId => this.rawItemId;

    /// <summary>
    /// Gets the underlying Lumina Item represented by this payload.
    /// </summary>
    /// <remarks>
    /// The value is evaluated lazily and cached.
    /// </remarks>
    [JsonIgnore]
    public Item? Item
    {
        get
        {
            // TODO(goat): This should be revamped/removed on an API level change.
            if (this.Kind == ItemKind.EventItem)
            {
                Log.Warning("Event items cannot be fetched from the ItemPayload");
                return null;
            }

            this.item ??= this.DataResolver.GetExcelSheet<Item>()!.GetRow(this.ItemId);
            return this.item;
        }
    }

    /// <summary>
    /// Gets a value indicating whether or not this item link is for a high-quality version of the item.
    /// </summary>
    [JsonProperty]
    public bool IsHQ => this.Kind == ItemKind.Hq;

    /// <summary>
    /// Gets or sets the kind of item represented by this payload.
    /// </summary>
    [JsonProperty]
    public ItemKind Kind { get; set; } = ItemKind.Normal;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemPayload"/> class.
    /// Creates a payload representing an interactable item link for the specified item.
    /// </summary>
    /// <param name="rawItemId">The raw, unadjusted id of the item.</param>
    /// <param name="displayNameOverride">An optional name to include in the item link.  Typically this should
    /// be left as null, or set to the normal item name.  Actual overrides are better done with the subsequent
    /// TextPayload that is a part of a full item link in chat.</param>
    /// <returns>The created item payload.</returns>
    public static ItemPayload FromRaw(uint rawItemId, string? displayNameOverride = null)
    {
        var (id, kind) = GetAdjustedId(rawItemId);
        return new ItemPayload(id, kind, displayNameOverride);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{this.Type} - ItemId: {this.ItemId}, Kind: {this.Kind}, Name: {this.displayName ?? this.Item?.Name}";
    }

    /// <inheritdoc/>
    protected override byte[] EncodeImpl()
    {
        var idBytes = MakeInteger(this.rawItemId);
        var hasName = !string.IsNullOrEmpty(this.displayName);

        var chunkLen = idBytes.Length + 4;
        if (hasName)
        {
            // 1 additional unknown byte compared to the nameless version, 1 byte for the name length, and then the name itself
            chunkLen += 1 + 1 + this.displayName.Length;
            if (this.IsHQ)
            {
                chunkLen += 4; // unicode representation of the HQ symbol is 3 bytes, preceded by a space
            }
        }

        var bytes = new List<byte>()
        {
            START_BYTE,
            (byte)SeStringChunkType.Interactable, (byte)chunkLen, (byte)EmbeddedInfoType.ItemLink,
        };
        bytes.AddRange(idBytes);
        // unk
        bytes.AddRange(new byte[] { 0x02, 0x01 });

        // Links don't have to include the name, but if they do, it requires additional work
        if (hasName)
        {
            var nameLen = this.displayName.Length + 1;
            if (this.IsHQ)
            {
                nameLen += 4; // space plus 3 bytes for HQ symbol
            }

            bytes.AddRange(new byte[]
            {
                0xFF, // unk
                (byte)nameLen,
            });
            bytes.AddRange(Encoding.UTF8.GetBytes(this.displayName));

            if (this.IsHQ)
            {
                // space and HQ symbol
                bytes.AddRange(new byte[] { 0x20, 0xEE, 0x80, 0xBC });
            }
        }

        bytes.Add(END_BYTE);

        return bytes.ToArray();
    }

    /// <inheritdoc/>
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        this.rawItemId = GetInteger(reader);
        this.Kind = GetAdjustedId(this.rawItemId).Kind;

        if (reader.BaseStream.Position + 3 < endOfStream)
        {
            // unk
            reader.ReadBytes(3);

            var itemNameLen = (int)GetInteger(reader);
            var itemNameBytes = reader.ReadBytes(itemNameLen);

            // it probably isn't necessary to store this, as we now get the lumina Item
            // on demand from the id, which will have the name
            // For incoming links, the name "should?" always match
            // but we'll store it for use in encode just in case it doesn't

            // HQ items have the HQ symbol as part of the name, but since we already recorded
            // the HQ flag, we want just the bare name
            if (this.IsHQ)
            {
                itemNameBytes = itemNameBytes.Take(itemNameLen - 4).ToArray();
            }

            this.displayName = Encoding.UTF8.GetString(itemNameBytes);
        }
    }

    private static (uint ItemId, ItemKind Kind) GetAdjustedId(uint rawItemId)
    {
        return rawItemId switch
        {
            > 500_000 and < 1_000_000 => (rawItemId - 500_000, ItemKind.Collectible),
            > 1_000_000 and < 2_000_000 => (rawItemId - 1_000_000, ItemKind.Hq),
            > 2_000_000 => (rawItemId, ItemKind.EventItem), // EventItem IDs are NOT adjusted
            _ => (rawItemId, ItemKind.Normal),
        };
    }
}
