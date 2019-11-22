using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dalamud.Game.Chat {
    // TODO: This class does not work - it's a hack, needs a revamp and better handling for payloads used in player chat
    public class SeString {
        public enum PlayerLinkType {
            ItemLink = 0x03
        }

        public enum SeStringPayloadType {
            PlayerLink = 0x27
        }

        // in all likelihood these are flags of some kind, but these are the only 2 values I've noticed
        public enum ItemQuality {
            NormalQuality = 0xF2,
            HighQuality = 0xF6
        }

        private const int START_BYTE = 0x02;
        private const int END_BYTE = 0x03;

        public static (string Output, List<SeStringPayloadContainer> Payloads) Parse(byte[] bytes)
        {
            var output = new List<byte>();
            var payloads = new List<SeStringPayloadContainer>();

            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
            {
                while (stream.Position < bytes.Length)
                {
                    var b = stream.ReadByte();

                    if (b == START_BYTE)
                        ProcessPacket(reader, output, payloads);
                    else
                        output.Add((byte)b);
                }
            }

            return (Encoding.UTF8.GetString(output.ToArray()), payloads);
        }

        public static (string Output, List<SeStringPayloadContainer> Payloads) Parse(string input) {
            var bytes = Encoding.UTF8.GetBytes(input);
            return Parse(bytes);
        }

        private static void ProcessPacket(BinaryReader reader, List<byte> output,
                                          List<SeStringPayloadContainer> payloads) {
            var type = reader.ReadByte();
            var payloadSize = GetInteger(reader);

            var payload = new byte[payloadSize];

            reader.Read(payload, 0, payloadSize);

            var orphanByte = reader.Read();
            // If the end of the tag isn't what we predicted, let's ignore it for now
            while (orphanByte != END_BYTE) orphanByte = reader.Read();

            //output.AddRange(Encoding.UTF8.GetBytes($"<{type.ToString("X")}:{BitConverter.ToString(payload)}>"));

            switch ((SeStringPayloadType) type) {
                case SeStringPayloadType.PlayerLink:
                    if (payload[0] == (byte)PlayerLinkType.ItemLink)
                    {
                        int itemId;
                        bool isHQ = payload[1] == (byte)ItemQuality.HighQuality;
                        if (isHQ)
                        {
                            // hq items have an extra 0x0F byte before the ID, and the ID is 0x4240 above the actual item ID
                            // This _seems_ consistent but I really don't know
                            itemId = (payload[3] << 8 | payload[4]) - 0x4240;
                        }
                        else
                        {
                            itemId = (payload[2] << 8 | payload[3]);
                        }

                        payloads.Add(new SeStringPayloadContainer
                        {
                            Type = SeStringPayloadType.PlayerLink,
                            Param1 = (itemId, isHQ)
                        });
                    }

                    break;
            }
        }

        public class SeStringPayloadContainer {
            public SeStringPayloadType Type { get; set; }
            public object Param1 { get; set; }
        }

        #region Shared

        public enum IntegerType {
            Byte = 0xF0,
            ByteTimes256 = 0xF1,
            Int16 = 0xF2,
            Int24 = 0xFA,
            Int32 = 0xFE
        }

        protected static int GetInteger(BinaryReader input) {
            var t = input.ReadByte();
            var type = (IntegerType) t;
            return GetInteger(input, type);
        }

        protected static int GetInteger(BinaryReader input, IntegerType type) {
            const byte ByteLengthCutoff = 0xF0;

            var t = (byte) type;
            if (t < ByteLengthCutoff)
                return t - 1;

            switch (type) {
                case IntegerType.Byte:
                    return input.ReadByte();
                case IntegerType.ByteTimes256:
                    return input.ReadByte() * 256;
                case IntegerType.Int16: {
                    var v = 0;
                    v |= input.ReadByte() << 8;
                    v |= input.ReadByte();
                    return v;
                }
                case IntegerType.Int24: {
                    var v = 0;
                    v |= input.ReadByte() << 16;
                    v |= input.ReadByte() << 8;
                    v |= input.ReadByte();
                    return v;
                }
                case IntegerType.Int32: {
                    var v = 0;
                    v |= input.ReadByte() << 24;
                    v |= input.ReadByte() << 16;
                    v |= input.ReadByte() << 8;
                    v |= input.ReadByte();
                    return v;
                }
                default:
                    throw new NotSupportedException();
            }
        }

        #endregion
    }
}
