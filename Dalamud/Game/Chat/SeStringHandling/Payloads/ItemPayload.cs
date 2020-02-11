using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    public class ItemPayload : Payload
    {
        public override PayloadType Type => PayloadType.Item;

        public int ItemId { get; private set; }
        public string ItemName { get; private set; } = string.Empty;
        public bool IsHQ { get; private set; } = false;

        public ItemPayload() { }

        public ItemPayload(int itemId, bool isHQ)
        {
            ItemId = itemId;
            IsHQ = isHQ;
        }

        public override void Resolve()
        {
            if (string.IsNullOrEmpty(ItemName))
            {
                dynamic item = XivApi.GetItem(ItemId).GetAwaiter().GetResult();
                ItemName = item.Name;
            }
        }

        public override byte[] Encode()
        {
            var actualItemId = IsHQ ? ItemId + 1000000 : ItemId;
            var idBytes = MakeInteger(actualItemId);

            var itemIdFlag = IsHQ ? IntegerType.Int16Plus1Million : IntegerType.Int16;

            var chunkLen = idBytes.Length + 5;
            var bytes = new List<byte>()
            {
                START_BYTE,
                (byte)SeStringChunkType.Interactable, (byte)chunkLen, (byte)EmbeddedInfoType.ItemLink,
                (byte)itemIdFlag
            };
            bytes.AddRange(idBytes);
            // unk
            bytes.AddRange(new byte[] { 0x02, 0x01, END_BYTE });



            return bytes.ToArray();
        }

        public override string ToString()
        {
            return $"{Type} - ItemId: {ItemId}, ItemName: {ItemName}, IsHQ: {IsHQ}";
        }

        protected override void ProcessChunkImpl(BinaryReader reader, long endOfStream)
        {
            ItemId = GetInteger(reader);

            if (ItemId > 1000000)
            {
                ItemId -= 1000000;
                IsHQ = true;
            }

            if (reader.BaseStream.Position + 3 < endOfStream)
            {
                // unk
                reader.ReadBytes(3);

                var itemNameLen = GetInteger(reader);
                ItemName = Encoding.UTF8.GetString(reader.ReadBytes(itemNameLen));
            }
        }
    }
}
