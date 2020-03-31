using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Serilog;

namespace Dalamud.Game.Chat.SeStringHandling
{
    /// <summary>
    /// This class represents a parsed SeString payload.
    /// </summary>
    public abstract class Payload
    {
        public abstract PayloadType Type { get; }

        protected DataManager dataResolver;

        public abstract void Resolve();

        public abstract byte[] Encode();

        protected abstract void ProcessChunkImpl(BinaryReader reader, long endOfStream);

        public static Payload Process(BinaryReader reader, DataManager dataResolver)
        {
            Payload payload = null;
            if ((byte)reader.PeekChar() != START_BYTE)
            {
                payload = ProcessText(reader);
            }
            else
            {
                payload = ProcessChunk(reader);
            }

            if (payload != null)
            {
                payload.dataResolver = dataResolver;
            }

            return payload;
        }

        private static Payload ProcessChunk(BinaryReader reader)
        {
            Payload payload = null;

            reader.ReadByte();  // START_BYTE
            var chunkType = (SeStringChunkType)reader.ReadByte();
            var chunkLen = GetInteger(reader);

            var packetStart = reader.BaseStream.Position;

            switch (chunkType)
            {
                case SeStringChunkType.Interactable:
                    {
                        var subType = (EmbeddedInfoType)reader.ReadByte();
                        switch (subType)
                        {
                            case EmbeddedInfoType.PlayerName:
                                payload = new PlayerPayload();
                                break;

                            case EmbeddedInfoType.ItemLink:
                                payload = new ItemPayload();
                                break;

                            case EmbeddedInfoType.MapPositionLink:
                                payload = new MapLinkPayload();
                                break;

                            case EmbeddedInfoType.Status:
                                payload = new StatusPayload();
                                break;

                            case EmbeddedInfoType.LinkTerminator:
                                // this has no custom handling and so needs to fallthrough to ensure it is captured
                            default:
                                Log.Verbose("Unhandled EmbeddedInfoType: {0}", subType);
                                // rewind so we capture the Interactable byte in the raw data
                                reader.BaseStream.Seek(-1, SeekOrigin.Current);
                                payload = new RawPayload((byte)chunkType);
                                break;
                        }
                    }
                    break;

                default:
                    Log.Verbose("Unhandled SeStringChunkType: {0}", chunkType);
                    payload = new RawPayload((byte)chunkType);
                    break;
            }

            payload?.ProcessChunkImpl(reader, reader.BaseStream.Position + chunkLen - 1);

            // read through the rest of the packet
            var readBytes = (int)(reader.BaseStream.Position - packetStart);
            reader.ReadBytes(chunkLen - readBytes + 1); // +1 for the END_BYTE marker

            return payload;
        }

        private static Payload ProcessText(BinaryReader reader)
        {
            var payload = new TextPayload();
            payload.ProcessChunkImpl(reader, reader.BaseStream.Length);

            return payload;
        }

        #region parse constants and helpers

        protected const byte START_BYTE = 0x02;
        protected const byte END_BYTE = 0x03;

        protected enum SeStringChunkType
        {
            Interactable = 0x27
        }

        protected enum EmbeddedInfoType
        {
            PlayerName = 0x01,
            ItemLink = 0x03,
            MapPositionLink = 0x04,
            Status = 0x09,

            LinkTerminator = 0xCF // not clear but seems to always follow a link
        }

        protected enum IntegerType
        {
            // used as an internal marker; sometimes single bytes are bare with no marker at all
            None = 0,

            Byte = 0xF0,
            ByteTimes256 = 0xF1,
            Int16 = 0xF2,
            Int16Packed = 0xF4,         // seen in map links, seemingly 2 8-bit values packed into 2 bytes with only one marker
            Int24Special = 0xF6,        // unsure how different form Int24 - used for hq items that add 1 million, also used for normal 24-bit values in map links
            Int24 = 0xFA,
            Int32 = 0xFE
        }

        // made protected, unless we actually want to use it externally
        // in which case it should probably go live somewhere else
        protected static int GetInteger(BinaryReader input)
        {
            var t = input.ReadByte();
            var type = (IntegerType)t;
            return GetInteger(input, type);
        }

        private static int GetInteger(BinaryReader input, IntegerType type)
        {
            const byte ByteLengthCutoff = 0xF0;

            var t = (byte)type;
            if (t < ByteLengthCutoff)
                return t - 1;

            switch (type)
            {
                case IntegerType.Byte:
                    return input.ReadByte();

                case IntegerType.ByteTimes256:
                    return input.ReadByte() * 256;

                case IntegerType.Int16:
                    // fallthrough - same logic
                case IntegerType.Int16Packed:
                    {
                        var v = 0;
                        v |= input.ReadByte() << 8;
                        v |= input.ReadByte();
                        return v;
                    }

                case IntegerType.Int24Special:
                    // Fallthrough - same logic
                case IntegerType.Int24:
                    {
                        var v = 0;
                        v |= input.ReadByte() << 16;
                        v |= input.ReadByte() << 8;
                        v |= input.ReadByte();
                        return v;
                    }

                case IntegerType.Int32:
                    {
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

        protected static byte[] MakeInteger(int value)
        {
            // clearly the epitome of efficiency

            var bytesPadded = BitConverter.GetBytes(value);
            Array.Reverse(bytesPadded);
            return bytesPadded.SkipWhile(b => b == 0x00).ToArray();
        }

        protected static IntegerType GetTypeForIntegerBytes(byte[] bytes)
        {
            // not the most scientific, exists mainly for laziness

            if (bytes.Length == 1)
            {
                return IntegerType.Byte;
            }
            else if (bytes.Length == 2)
            {
                return IntegerType.Int16;
            }
            else if (bytes.Length == 3)
            {
                return IntegerType.Int24;
            }
            else if (bytes.Length == 4)
            {
                return IntegerType.Int32;
            }

            throw new NotSupportedException();
        }

        protected static (int, int) GetPackedIntegers(BinaryReader input)
        {
            var value = (uint)GetInteger(input);
            if (value > 0xFFFF)
            {
                return ((int)((value & 0xFFFF0000) >> 16), (int)(value & 0xFFFF));
            }
            else if (value > 0xFF)
            {
                return ((int)((value & 0xFF00) >> 8), (int)(value & 0xFF));
            }

            // unsure if there are other cases, like "odd" pairings of 2+1 bytes etc
            throw new NotSupportedException();
        }

        protected static byte[] MakePackedInteger(int val1, int val2)
        {
            return MakeInteger(val1).Concat(MakeInteger(val2)).ToArray();
        }
        #endregion
    }
}
