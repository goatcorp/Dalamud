using Dalamud.Data.TransientSheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    public class ItemPayload : Payload
    {
        public override PayloadType Type => PayloadType.Item;

        private Item item;
        public Item Item
        {
            get
            {
                this.item ??= this.dataResolver.GetExcelSheet<Item>().GetRow((int)this.itemId);
                return this.item;
            }
        }

        // mainly to allow overriding the name (for things like owo)
        private string displayName;
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

        public bool IsHQ { get; private set; } = false;

        private uint itemId;

        public override string ToString()
        {
            return $"{Type} - ItemId: {itemId}, IsHQ: {IsHQ}";
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

        protected override byte GetMarkerForIntegerBytes(byte[] bytes)
        {
            // custom marker just for hq items?
            if (bytes.Length == 3 && IsHQ)
            {
                return (byte)IntegerType.Int24Special;
            }

            return base.GetMarkerForIntegerBytes(bytes);
        }
    }
}
