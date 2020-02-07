using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public abstract void Resolve();

        public abstract byte[] Encode();

        protected abstract void ProcessChunkImpl(BinaryReader reader, long endOfStream);

        public static Payload Process(BinaryReader reader)
        {
            if ((byte)reader.PeekChar() != START_BYTE)
            {
                return ProcessText(reader);
            }
            else
            {
                return ProcessChunk(reader);
            }
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

                            case EmbeddedInfoType.Status:
                                payload = new StatusPayload();
                                break;
                            case EmbeddedInfoType.LinkTerminator:
                                // Does not need to be handled
                                break;
                            default:
                                Log.Verbose("Unhandled EmbeddedInfoType: {0}", subType);
                                break;
                        }
                    }
                    break;
                default:
                    Log.Verbose("Unhandled SeStringChunkType: {0}", chunkType);
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
            Status = 0x09,

            LinkTerminator = 0xCF // not clear but seems to always follow a link
        }

        protected enum IntegerType
        {
            Byte = 0xF0,
            ByteTimes256 = 0xF1,
            Int16 = 0xF2,
            Int16Plus1Million = 0xF6,
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
                    {
                        var v = 0;
                        v |= input.ReadByte() << 8;
                        v |= input.ReadByte();
                        return v;
                    }
                case IntegerType.Int16Plus1Million:
                    {
                        var v = 0;
                        v |= input.ReadByte() << 16;
                        v |= input.ReadByte() << 8;
                        v |= input.ReadByte();
                        // need the actual value since it's used as a flag
                        // v -= 1000000;
                        return v;
                    }
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
        #endregion
    }
}
