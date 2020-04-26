using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Serilog;

// TODOs:
//   - refactor integer handling now that we have multiple packed types
//   - common construction/property design for subclasses
//   - lumina DI
//   - design for handling raw values vs resolved values, both for input and output
//   - wrapper class(es) for handling of composite links in chat (item, map etc) and formatting operations

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
            Payload payload = null;

            var initialByte = reader.ReadByte();
            reader.BaseStream.Position--;
            if (initialByte != START_BYTE)
            {
                payload = ProcessText(reader);
            }
            else
            {
                payload = ProcessChunk(reader);
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

                case SeStringChunkType.AutoTranslateKey:
                    payload = new AutoTranslatePayload();
                    break;

                case SeStringChunkType.UIForeground:
                    payload = new UIForegroundPayload();
                    break;

                case SeStringChunkType.UIGlow:
                    payload = new UIGlowPayload();
                    break;

                default:
                    Log.Verbose("Unhandled SeStringChunkType: {0}", chunkType);
                    payload = new RawPayload((byte)chunkType);
                    break;
            }

            payload?.ProcessChunkImpl(reader, reader.BaseStream.Position + chunkLen - 1);

            // read through the rest of the packet
            var readBytes = (uint)(reader.BaseStream.Position - packetStart);
            reader.ReadBytes((int)(chunkLen - readBytes + 1)); // +1 for the END_BYTE marker

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
            Interactable = 0x27,
            AutoTranslateKey = 0x2E,
            UIForeground = 0x48,
            UIGlow = 0x49
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
            Int24Packed = 0xFC,         // used in map links- sometimes short+byte, sometimes... not??
            Int24 = 0xFA,
            Int32 = 0xFE
        }

        // made protected, unless we actually want to use it externally
        // in which case it should probably go live somewhere else
        protected static uint GetInteger(BinaryReader input)
        {
            var t = input.ReadByte();
            var type = (IntegerType)t;
            return GetInteger(input, type);
        }

        private static uint GetInteger(BinaryReader input, IntegerType type)
        {
            const byte ByteLengthCutoff = 0xF0;

            var t = (byte)type;
            if (t < ByteLengthCutoff)
                return (uint)(t - 1);

            switch (type)
            {
                case IntegerType.Byte:
                    return input.ReadByte();

                case IntegerType.ByteTimes256:
                    return input.ReadByte() * (uint)256;

                case IntegerType.Int16:
                    // fallthrough - same logic
                case IntegerType.Int16Packed:
                    {
                        var v = 0;
                        v |= input.ReadByte() << 8;
                        v |= input.ReadByte();
                        return (uint)v;
                    }

                case IntegerType.Int24Special:
                    // Fallthrough - same logic
                case IntegerType.Int24Packed:
                // fallthrough again
                case IntegerType.Int24:
                    {
                        var v = 0;
                        v |= input.ReadByte() << 16;
                        v |= input.ReadByte() << 8;
                        v |= input.ReadByte();
                        return (uint)v;
                    }

                case IntegerType.Int32:
                    {
                        var v = 0;
                        v |= input.ReadByte() << 24;
                        v |= input.ReadByte() << 16;
                        v |= input.ReadByte() << 8;
                        v |= input.ReadByte();
                        return (uint)v;
                    }

                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual byte[] MakeInteger(uint value, bool withMarker = true, bool incrementSmallInts = true) // TODO: better way to handle this
        {
            // single-byte values below the marker values have no marker and have 1 added
            if (incrementSmallInts && (value + 1 < (int)IntegerType.Byte))
            {
                value++;
                return new byte[] { (byte)value };
            }

            var bytesPadded = BitConverter.GetBytes(value);
            Array.Reverse(bytesPadded);
            var shrunkValue = bytesPadded.SkipWhile(b => b == 0x00).ToArray();

            var encodedNum = new List<byte>();

            if (withMarker)
            {
                var marker = GetMarkerForIntegerBytes(shrunkValue);
                if (marker != 0)
                {
                    encodedNum.Add(marker);
                }
            }

            encodedNum.AddRange(shrunkValue);

            return encodedNum.ToArray();
        }

        // This is only accurate in a very general sense
        // Different payloads seem to use different default values for things
        // So this should be overridden where necessary
        protected virtual byte GetMarkerForIntegerBytes(byte[] bytes)
        {
            // not the most scientific, exists mainly for laziness

            var marker = bytes.Length switch
            {
                1 => IntegerType.Byte,
                2 => IntegerType.Int16,
                3 => IntegerType.Int24,
                4 => IntegerType.Int32,
                _ => throw new NotSupportedException()
            };

            return (byte)marker;
        }

        protected virtual byte GetMarkerForPackedIntegerBytes(byte[] bytes)
        {
            // unsure if any 'strange' size groupings exist; only ever seen these
            var type = bytes.Length switch
            {
                4 => IntegerType.Int32,
                3 => IntegerType.Int24Packed,
                2 => IntegerType.Int16Packed,
                _ => throw new NotSupportedException()
            };

            return (byte)type;
        }

        protected (uint, uint) GetPackedIntegers(BinaryReader input)
        {
            // HACK - this was already a hack, but the addition of Int24Packed made it even worse
            // All of this should be redone/removed at some point

            var marker = (IntegerType)input.ReadByte();
            input.BaseStream.Position--;

            var value = GetInteger(input);

            if (marker == IntegerType.Int24Packed)
            {
                return ((uint)((value & 0xFFFF00) >> 8), (uint)(value & 0xFF));
            }
            // this used to be the catchall before Int24Packed; leave it for now to ensure we handle all encodings
            else // if (marker == IntegerType.Int16Packed || marker == IntegerType.Int32)
            {
                if (value > 0xFFFF)
                {
                    return ((uint)((value & 0xFFFF0000) >> 16), (uint)(value & 0xFFFF));
                }
                else if (value > 0xFF)
                {
                    return ((uint)((value & 0xFF00) >> 8), (uint)(value & 0xFF));
                }
            }

            // unsure if there are other cases
            throw new NotSupportedException();
        }

        protected byte[] MakePackedInteger(uint val1, uint val2, bool withMarker = true)
        {
            var value = MakeInteger(val1, false, false).Concat(MakeInteger(val2, false, false)).ToArray();

            var valueBytes = new List<byte>();
            if (withMarker)
            {
                valueBytes.Add(GetMarkerForPackedIntegerBytes(value));
            }

            valueBytes.AddRange(value);

            return valueBytes.ToArray();
        }
        #endregion
    }
}
