using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Data;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Serilog;

// TODOs:
//   - refactor integer handling now that we have multiple packed types
// Maybes:
//   - convert parsing to custom structs for each payload?  would make some code prettier and easier to work with
//     but also wouldn't work out as well for things that are dynamically-sized
//   - [SeString] some way to add surrounding formatting information as flags/data to text (or other?) payloads?
//     eg, if a text payload is surrounded by italics payloads, strip them out and mark the text payload as italicized

namespace Dalamud.Game.Text.SeStringHandling
{
    /// <summary>
    /// This class represents a parsed SeString payload.
    /// </summary>
    public abstract class Payload
    {
        /// <summary>
        /// The type of this payload.
        /// </summary>
        public abstract PayloadType Type { get; }

        /// <summary>
        /// Whether this payload has been modified since the last Encode().
        /// </summary>
        public bool Dirty { get; protected set; } = true;

        /// <summary>
        /// Encodes the internal state of this payload into a byte[] suitable for sending to in-game
        /// handlers such as the chat log.
        /// </summary>
        /// <returns>Encoded binary payload data suitable for use with in-game handlers.</returns>
        protected abstract byte[] EncodeImpl();

        // TODO: endOfStream is somewhat legacy now that payload length is always handled correctly.
        // This could be changed to just take a straight byte[], but that would complicate reading
        // but we could probably at least remove the end param
        /// <summary>
        /// Decodes a byte stream from the game into a payload object.
        /// </summary>
        /// <param name="reader">A BinaryReader containing at least all the data for this payload.</param>
        /// <param name="endOfStream">The location holding the end of the data for this payload.</param>
        protected abstract void DecodeImpl(BinaryReader reader, long endOfStream);

        /// <summary>
        /// The Lumina instance to use for any necessary data lookups.
        /// </summary>
        public DataManager DataResolver;

        // private for now, since subclasses shouldn't interact with this
        // To force-invalidate it, Dirty can be set to true
        private byte[] encodedData;

        /// <summary>
        /// Encode this payload object into a byte[] useable in-game for things like the chat log.
        /// </summary>
        /// <param name="force">If true, ignores any cached value and forcibly reencodes the payload from its internal representation.</param>
        /// <returns>A byte[] suitable for use with in-game handlers such as the chat log.</returns>
        public byte[] Encode(bool force = false)
        {
            if (Dirty || force)
            {
                this.encodedData = EncodeImpl();
                Dirty = false;
            }

            return this.encodedData;
        }

        /// <summary>
        /// Decodes a binary representation of a payload into its corresponding nice object payload.
        /// </summary>
        /// <param name="reader">A reader positioned at the start of the payload, and containing at least one entire payload.</param>
        /// <returns>The constructed Payload-derived object that was decoded from the binary data.</returns>
        public static Payload Decode(BinaryReader reader, DataManager data)
        {
            var payloadStartPos = reader.BaseStream.Position;

            Payload payload = null;

            var initialByte = reader.ReadByte();
            reader.BaseStream.Position--;
            if (initialByte != START_BYTE)
            {
                payload = DecodeText(reader);
            }
            else
            {
                payload = DecodeChunk(reader);
            }

            payload.DataResolver = data;

            // for now, cache off the actual binary data for this payload, so we don't have to
            // regenerate it if the payload isn't modified
            // TODO: probably better ways to handle this
            var payloadEndPos = reader.BaseStream.Position;

            reader.BaseStream.Position = payloadStartPos;
            payload.encodedData = reader.ReadBytes((int)(payloadEndPos - payloadStartPos));
            payload.Dirty = false;

            // Log.Verbose($"got payload bytes {BitConverter.ToString(payload.encodedData).Replace("-", " ")}");

            reader.BaseStream.Position = payloadEndPos;

            return payload;
        }

        private static Payload DecodeChunk(BinaryReader reader)
        {
            Payload payload = null;

            reader.ReadByte();  // START_BYTE
            var chunkType = (SeStringChunkType)reader.ReadByte();
            var chunkLen = GetInteger(reader);

            var packetStart = reader.BaseStream.Position;

            // any unhandled payload types will be turned into a RawPayload with the exact same binary data
            switch (chunkType)
            {
                case SeStringChunkType.EmphasisItalic:
                    payload = new EmphasisItalicPayload();
                    break;

                case SeStringChunkType.SeHyphen:
                    payload = SeHyphenPayload.Payload;
                    break;

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

                            case EmbeddedInfoType.QuestLink:
                                payload = new QuestPayload();
                                break;

                            case EmbeddedInfoType.DalamudLink:
                                payload = new DalamudLinkPayload();
                                break;

                            case EmbeddedInfoType.LinkTerminator:
                                // this has no custom handling and so needs to fallthrough to ensure it is captured
                            default:
                                // but I'm also tired of this log
                                if (subType != EmbeddedInfoType.LinkTerminator)
                                {
                                    Log.Verbose("Unhandled EmbeddedInfoType: {0}", subType);
                                }
                                // rewind so we capture the Interactable byte in the raw data
                                reader.BaseStream.Seek(-1, SeekOrigin.Current);
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

                case SeStringChunkType.Icon:
                    payload = new IconPayload();
                    break;

                default:
                    Log.Verbose("Unhandled SeStringChunkType: {0}", chunkType);
                    break;
            }

            payload ??= new RawPayload((byte)chunkType);
            payload.DecodeImpl(reader, reader.BaseStream.Position + chunkLen - 1);

            // read through the rest of the packet
            var readBytes = (uint)(reader.BaseStream.Position - packetStart);
            reader.ReadBytes((int)(chunkLen - readBytes + 1)); // +1 for the END_BYTE marker

            return payload;
        }

        private static Payload DecodeText(BinaryReader reader)
        {
            var payload = new TextPayload();
            payload.DecodeImpl(reader, reader.BaseStream.Length);

            return payload;
        }

        #region parse constants and helpers

        protected const byte START_BYTE = 0x02;
        protected const byte END_BYTE = 0x03;

        protected enum SeStringChunkType
        {
            Icon = 0x12,
            EmphasisItalic = 0x1A,
            SeHyphen = 0x1F,
            Interactable = 0x27,
            AutoTranslateKey = 0x2E,
            UIForeground = 0x48,
            UIGlow = 0x49
        }

        public enum EmbeddedInfoType
        {
            PlayerName = 0x01,
            ItemLink = 0x03,
            MapPositionLink = 0x04,
            QuestLink = 0x05,
            Status = 0x09,

            DalamudLink = 0x0F, // Dalamud Custom

            LinkTerminator = 0xCF // not clear but seems to always follow a link
        }


        // made protected, unless we actually want to use it externally
        // in which case it should probably go live somewhere else
        protected static uint GetInteger(BinaryReader input)
        {
            uint marker = input.ReadByte();
            if (marker < 0xD0) return marker - 1;

            // the game adds 0xF0 marker for values >= 0xCF
            // uasge of 0xD0-0xEF is unknown, should we throw here?
            // if (marker < 0xF0) throw new NotSupportedException();

            marker = (marker + 1) & 0b1111;

            var ret = new byte[4];
            for (var i = 3; i >= 0; i--)
            {
                ret[i] = (marker & (1 << i)) == 0 ? (byte)0 : input.ReadByte();
            }

            return BitConverter.ToUInt32(ret, 0);
        }

        protected static byte[] MakeInteger(uint value)
        {
            if (value < 0xCF)
            {
                return new byte[] { (byte)(value + 1) };
            }

            var bytes = BitConverter.GetBytes(value);

            var ret = new List<byte>() { 0xF0 };
            for (var i = 3; i >= 0; i--)
            {
                if (bytes[i] != 0)
                {
                    ret.Add(bytes[i]);
                    ret[0] |= (byte)(1 << i);
                }
            }
            ret[0] -= 1;

            return ret.ToArray();
        }

        protected static (uint, uint) GetPackedIntegers(BinaryReader input)
        {
            var value = GetInteger(input);
            return (value >> 16, value & 0xFFFF);
        }

        protected static byte[] MakePackedInteger(uint val1, uint val2)
        {
            var value = (val1 << 16) | (val2 & 0xFFFF);
            return MakeInteger(value);
        }
        #endregion
    }
}
