using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dalamud.Game.Network.Structures;

/// <summary>
/// This class represents the current market board offerings from a game network packet.
/// </summary>
public class MarketBoardCurrentOfferings : IMarketBoardCurrentOfferings
{
    private MarketBoardCurrentOfferings()
    {
    }

    /// <summary>
    /// Gets the list of individual item listings.
    /// </summary>
    public IReadOnlyList<IMarketBoardItemListing> ItemListings => this.InternalItemListings;

    /// <summary>
    /// Gets the request ID.
    /// </summary>
    public int RequestId { get; internal set; }

    /// <summary>
    /// Gets or sets the internal read-write list of marketboard item listings.
    /// </summary>
    internal List<MarketBoardItemListing> InternalItemListings { get; set; } = [];

    /// <summary>
    /// Read a <see cref="MarketBoardCurrentOfferings"/> object from memory.
    /// </summary>
    /// <param name="dataPtr">Address to read.</param>
    /// <returns>A new <see cref="MarketBoardCurrentOfferings"/> object.</returns>
    public static unsafe MarketBoardCurrentOfferings Read(nint dataPtr)
    {
        var output = new MarketBoardCurrentOfferings();

        using var stream = new UnmanagedMemoryStream((byte*)dataPtr.ToPointer(), 1544);
        using var reader = new BinaryReader(stream);

        var listings = new List<MarketBoardItemListing>();

        for (var i = 0; i < 10; i++)
        {
            var listingEntry = new MarketBoardItemListing();

            listingEntry.ListingId = reader.ReadUInt64();
            listingEntry.RetainerId = reader.ReadUInt64();
            listingEntry.RetainerOwnerId = reader.ReadUInt64();
            listingEntry.ArtisanId = reader.ReadUInt64();

            listingEntry.PricePerUnit = reader.ReadUInt32();
            listingEntry.TotalTax = reader.ReadUInt32();
            listingEntry.ItemQuantity = reader.ReadUInt32();
            listingEntry.CatalogId = reader.ReadUInt32();

            reader.ReadUInt16(); // Slot
            reader.ReadUInt16(); // Durability
            reader.ReadUInt16(); // Spiritbond

            var materiaList = new List<IItemMateria>();
            for (var materiaIndex = 0; materiaIndex < 5; materiaIndex++)
            {
                var materiaVal = reader.ReadUInt16();
                var materiaEntry = new MarketBoardItemListing.ItemMateria()
                {
                    MateriaId = (materiaVal & 0xFF0) >> 4,
                    Index = materiaVal & 0xF,
                };

                if (materiaEntry.MateriaId != 0)
                    materiaList.Add(materiaEntry);
            }

            listingEntry.Materia = materiaList;

            reader.ReadBytes(0x6); // Padding

            listingEntry.RetainerName = Encoding.UTF8.GetString(reader.ReadBytes(0x20)).TrimEnd('\u0000');
            reader.ReadBytes(0x20); // Empty Buffer, was PlayerName pre 7.0

            listingEntry.IsHq = reader.ReadBoolean();
            listingEntry.MateriaCount = reader.ReadByte();
            listingEntry.OnMannequin = reader.ReadBoolean();
            listingEntry.RetainerCityId = reader.ReadByte();

            listingEntry.Stain1Id = reader.ReadByte();
            listingEntry.Stain2Id = reader.ReadByte();

            reader.ReadBytes(0x4); // Padding

            if (listingEntry.CatalogId != 0)
                listings.Add(listingEntry);
        }

        output.InternalItemListings = listings;
        reader.ReadByte(); // Was ListingIndexEnd
        reader.ReadByte(); // Was ListingIndexStart
        output.RequestId = reader.ReadUInt16();

        return output;
    }

    /// <summary>
    /// This class represents the current market board offering of a single item from the <see cref="MarketBoardCurrentOfferings"/> network packet.
    /// </summary>
    public class MarketBoardItemListing : IMarketBoardItemListing
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarketBoardItemListing"/> class.
        /// </summary>
        internal MarketBoardItemListing()
        {
        }

        /// <summary>
        /// Gets the artisan ID.
        /// </summary>
        public ulong ArtisanId { get; internal set; }

        /// <inheritdoc/>
        public uint ItemId => this.CatalogId;

        /// <summary>
        /// Gets the catalog ID.
        /// </summary>
        public uint CatalogId { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the item is HQ.
        /// </summary>
        public bool IsHq { get; internal set; }

        /// <summary>
        /// Gets the item quantity.
        /// </summary>
        public uint ItemQuantity { get; internal set; }

        /// <summary>
        /// Gets the listing ID.
        /// </summary>
        public ulong ListingId { get; internal set; }

        /// <summary>
        /// Gets the list of materia attached to this item.
        /// </summary>
        public IReadOnlyList<IItemMateria> Materia { get; internal set; } = new List<IItemMateria>();

        /// <summary>
        /// Gets the amount of attached materia.
        /// </summary>
        public int MateriaCount { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this item is on a mannequin.
        /// </summary>
        public bool OnMannequin { get; internal set; }

        /// <summary>
        /// Gets the price per unit.
        /// </summary>
        public uint PricePerUnit { get; internal set; }

        /// <summary>
        /// Gets the city ID of the retainer selling the item.
        /// </summary>
        public int RetainerCityId { get; internal set; }

        /// <summary>
        /// Gets the ID of the retainer selling the item.
        /// </summary>
        public ulong RetainerId { get; internal set; }

        /// <summary>
        /// Gets the name of the retainer.
        /// </summary>
        public string RetainerName { get; internal set; }

        /// <summary>
        /// Gets the ID of the retainer's owner.
        /// </summary>
        public ulong RetainerOwnerId { get; internal set; }

        /// <summary>
        /// Gets the first stain or applied dye of the item.
        /// </summary>
        public int Stain1Id { get; internal set; }

        /// <summary>
        /// Gets the second stain or applied dye of the item.
        /// </summary>
        public int Stain2Id { get; internal set; }

        /// <summary>
        /// Gets the total tax.
        /// </summary>
        public uint TotalTax { get; internal set; }
        
        /// <summary>
        /// Gets or sets the time this offering was last reviewed.
        /// </summary>
        [Obsolete("Universalis Compatibility, contains a fake value", false)]
        internal DateTime LastReviewTime { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets the stain or applied dye of the item.
        /// </summary>
        [Obsolete("Universalis Compatibility, use Stain1Id and Stain2Id", false)]
        internal int StainId => (this.Stain2Id << 8) | this.Stain1Id;
        
        /// <summary>
        /// Gets or sets the player name.
        /// </summary>
        [Obsolete("Universalis Compatibility, contains a fake value", false)]
        internal string PlayerName { get; set; } = string.Empty;

        /// <summary>
        /// This represents the materia slotted to an <see cref="MarketBoardItemListing"/>.
        /// </summary>
        public class ItemMateria : IItemMateria
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ItemMateria"/> class.
            /// </summary>
            internal ItemMateria()
            {
            }

            /// <summary>
            /// Gets the materia index.
            /// </summary>
            public int Index { get; internal set; }

            /// <summary>
            /// Gets the materia ID.
            /// </summary>
            public int MateriaId { get; internal set; }
        }
    }
}
