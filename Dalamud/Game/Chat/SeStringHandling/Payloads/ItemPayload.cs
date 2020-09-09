using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    /// <summary>
    /// An SeString Payload representing an interactable item link.
    /// </summary>
    public class ItemPayload : Payload
    {
        public override PayloadType Type => PayloadType.Item;

        private Item item;
        /// <summary>
        /// The underlying Lumina Item represented by this payload.
        /// </summary>
        /// <remarks>
        /// Value is evaluated lazily and cached.
        /// </remarks>
        [JsonIgnore]
        public Item Item
        {
            get
            {
                this.item ??= this.DataResolver.GetExcelSheet<Item>().GetRow(this.itemId);
                return this.item;
            }
        }

        // mainly to allow overriding the name (for things like owo)
        // TODO: even though this is present in some item links, it may not really have a use at all
        //   For things like owo, changing the text payload is probably correct, whereas changing the
        //   actual embedded name might not work properly.
        private string displayName = null;
        /// <summary>
        /// The displayed name for this item link.  Note that incoming links only sometimes have names embedded,
        /// often the name is only present in a following text payload.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return this.displayName;
            }

            set
            {
                this.displayName = value;
                Dirty = true;
            }
        }

        /// <summary>
        /// Whether or not this item link is for a high-quality version of the item.
        /// </summary>
        [JsonProperty]
        public bool IsHQ { get; private set; } = false;

        [JsonProperty]
        private uint itemId;

        internal ItemPayload() { }

        /// <summary>
        /// Creates a payload representing an interactable item link for the specified item.
        /// </summary>
        /// <param name="data">DataManager instance needed to resolve game data.</param>
        /// <param name="itemId">The id of the item.</param>
        /// <param name="isHQ">Whether or not the link should be for the high-quality variant of the item.</param>
        /// <param name="displayNameOverride">An optional name to include in the item link.  Typically this should
        /// be left as null, or set to the normal item name.  Actual overrides are better done with the subsequent
        /// TextPayload that is a part of a full item link in chat.</param>
        public ItemPayload(DataManager data, uint itemId, bool isHQ, string displayNameOverride = null) {
            this.DataResolver = data;
            this.itemId = itemId;
            this.IsHQ = isHQ;
            this.displayName = displayNameOverride;
        }

        public override string ToString()
        {
            return $"{Type} - ItemId: {itemId}, IsHQ: {IsHQ}, Name: {this.displayName ?? Item.Name}";
        }

        protected override byte[] EncodeImpl()
        {
            var actualItemId = IsHQ ? this.itemId + 1000000 : this.itemId;
            var idBytes = MakeInteger(actualItemId);
            bool hasName = !string.IsNullOrEmpty(this.displayName);

            var chunkLen = idBytes.Length + 4;
            if (hasName)
            {
                // 1 additional unknown byte compared to the nameless version, 1 byte for the name length, and then the name itself
                chunkLen += (1 + 1 + this.displayName.Length);
                if (IsHQ)
                {
                    chunkLen += 4;  // unicode representation of the HQ symbol is 3 bytes, preceded by a space
                }
            }

            var bytes = new List<byte>()
            {
                START_BYTE,
                (byte)SeStringChunkType.Interactable, (byte)chunkLen, (byte)EmbeddedInfoType.ItemLink
            };
            bytes.AddRange(idBytes);
            // unk
            bytes.AddRange(new byte[] { 0x02, 0x01 });

            // Links don't have to include the name, but if they do, it requires additional work
            if (hasName)
            {
                var nameLen = this.displayName.Length + 1;
                if (IsHQ)
                {
                    nameLen += 4;   // space plus 3 bytes for HQ symbol
                }

                bytes.AddRange(new byte[]
                {
                    0xFF,   // unk
                    (byte)nameLen
                });
                bytes.AddRange(Encoding.UTF8.GetBytes(this.displayName));

                if (IsHQ)
                {
                    // space and HQ symbol
                    bytes.AddRange(new byte[] { 0x20, 0xEE, 0x80, 0xBC });
                }
            }

            bytes.Add(END_BYTE);

            return bytes.ToArray();
        }

        protected override void DecodeImpl(BinaryReader reader, long endOfStream)
        {
            this.itemId = GetInteger(reader);

            if (this.itemId > 1000000)
            {
                this.itemId -= 1000000;
                IsHQ = true;
            }

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
                if (IsHQ)
                {
                    itemNameBytes = itemNameBytes.Take(itemNameLen - 4).ToArray();
                }

                this.displayName = Encoding.UTF8.GetString(itemNameBytes);
            }
        }

        protected override byte[] MakeInteger(uint value, bool withMarker = true, bool incrementSmallInts = true)
        {
            // TODO: as part of refactor

            // linking an item id that is a multiple of 256 seemingly *requires* using byte*256 marker encoding
            // or the link will not display correctly
            // I am unsure if this applies to other data types as well, so keeping localized here, pending the
            // refactor of all this integer handling mess
            if (value % 256 == 0)
            {
                value /= 256;
                // this is no longer a small int, but it was likely converted to that range
                incrementSmallInts = false;
            }

            return base.MakeInteger(value, withMarker, incrementSmallInts);
        }

        protected override byte GetMarkerForIntegerBytes(byte[] bytes)
        {
            // custom marker just for hq items?
            if (bytes.Length == 3 && IsHQ)
            {
                return (byte)IntegerType.Int24Special;
            }

            // TODO: as in the above function
            if (bytes.Length == 1 && (this.itemId % 256 == 0))
            {
                return (byte)IntegerType.ByteTimes256;
            }

            return base.GetMarkerForIntegerBytes(bytes);
        }
    }
}
