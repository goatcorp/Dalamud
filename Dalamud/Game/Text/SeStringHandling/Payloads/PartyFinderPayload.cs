using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using Lumina.Extensions;
using Newtonsoft.Json;

namespace Dalamud.Game.Text.SeStringHandling.Payloads
{
    /// <summary>
    /// An SeString Payload representing an interactable party finder link.
    /// </summary>
    public class PartyFinderPayload : Payload
    {
        /// <summary>
        /// Delimiting byte for party finder payload flags.
        /// </summary>
        // at least that's what i think it is... in any case, this works.
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "This is preferred.")]
        protected const byte FLAG_DELIMITER = 0x01;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartyFinderPayload"/> class.
        /// Creates a payload representing an interactable party finder link for the specified party finder listing.
        /// </summary>
        /// <param name="listingId">The listing ID of the party finder listing.</param>
        /// <param name="type">The party finder link type that should be encoded with this link.</param>
        public PartyFinderPayload(uint listingId, PartyFinderLinkType type)
        {
            this.ListingId = listingId;
            this.LinkType = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PartyFinderPayload"/> class.
        /// Creates a payload representing an interactable party finder notification link.
        /// </summary>
        public PartyFinderPayload()
        {
        }

        /// <summary>
        /// Represents the flags in a party finder link.
        /// </summary>
        public enum PartyFinderLinkType
        {
            /// <summary>
            /// Indicates that the party finder link is for a party that is limited to the host's home world.
            /// </summary>
            LimitedToHomeWorld = 0xF3,

            /// <summary>
            /// Indicates that the party finder link is for the "Display advanced search results in log." option.
            /// </summary>
            PartyFinderNotification = 0xFF,

            /// <summary>
            /// Indicates that the party finder link type was unspecified. Only for internal use. Not used by SE and omitted from encoding.
            /// </summary>
            NotSpecified = 0x00,
        }

        /// <summary>
        /// Gets the party finder listing ID.
        /// </summary>
        [JsonProperty]
        public uint ListingId { get; private set; }

        /// <summary>
        /// Gets the link type.
        /// </summary>
        [JsonProperty]
        public PartyFinderLinkType LinkType { get; private set; } = PartyFinderLinkType.PartyFinderNotification;

        /// <inheritdoc/>
        public override PayloadType Type => PayloadType.PartyFinder;

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Type} - ListingId: {this.ListingId}, PartyFinderLinkType: {this.LinkType}";
        }

        /// <inheritdoc/>
        protected override void DecodeImpl(BinaryReader reader, long endOfStream)
        {
            // 0x01 here indicates a party finder notification, which needs to be handled uniquely
            if (reader.PeekByte() == 0x01)
            {
                this.LinkType = PartyFinderLinkType.PartyFinderNotification;
                return;
            }

            this.ListingId = GetInteger(reader);

            // throw away always 0x01
            reader.ReadByte();

            // if the next byte is 0xF3 then this listing is limited to home world
            byte nextByte = reader.ReadByte();
            switch (nextByte)
            {
                case (byte)PartyFinderLinkType.LimitedToHomeWorld:
                    this.LinkType = PartyFinderLinkType.LimitedToHomeWorld;
                    break;

                // if this byte is just the flag delimiter, then nothing was specified.
                case FLAG_DELIMITER:
                    this.LinkType = PartyFinderLinkType.NotSpecified;
                    break;

                default:
                    Serilog.Log.Information($"Unrecognized PartyFinderLinkType code {nextByte} (Hex - {nextByte:X2})");
                    break;
            }
        }

        /// <inheritdoc/>
        protected override byte[] EncodeImpl()
        {
            // if the link type is notification, just use premade payload data since it's always the same.
            // i have no idea why it is formatted like this, but it is how it is.
            // note it is identical to the link terminator payload except the embedded info type is 0x08
            if (this.LinkType == PartyFinderLinkType.PartyFinderNotification) return new byte[] { 0x02, 0x27, 0x07, 0x08, 0x01, 0x01, 0x01, 0xFF, 0x01, 0x03, };

            // back to our regularly scheduled programming...
            var listingIDBytes = MakeInteger(this.ListingId);
            bool isFlagSpecified = this.LinkType != PartyFinderLinkType.NotSpecified;

            var chunkLen = listingIDBytes.Length + 4;
            // 1 more byte for the type flag if it is specified
            if (isFlagSpecified) chunkLen++;

            var bytes = new List<byte>()
            {
                START_BYTE, (byte)SeStringChunkType.Interactable, (byte)chunkLen, (byte)EmbeddedInfoType.PartyFinderLink,
            };

            bytes.AddRange(listingIDBytes);

            bytes.Add(FLAG_DELIMITER);

            if (isFlagSpecified) bytes.Add((byte)this.LinkType);

            bytes.Add(FLAG_DELIMITER);

            bytes.Add(END_BYTE);

            return bytes.ToArray();
        }
    }
}
