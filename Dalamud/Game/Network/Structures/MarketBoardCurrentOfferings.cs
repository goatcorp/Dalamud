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
    IReadOnlyList<IMarketBoardItemListing> IMarketBoardCurrentOfferings.ItemListings => this.InternalItemListings;

    internal List<MarketBoardItemListing> InternalItemListings { get; set; } = new List<MarketBoardItemListing>();


    /// <summary>
    /// Gets the listing end index.
    /// </summary>
    public int ListingIndexEnd { get; internal set; }

    /// <summary>
    /// Gets the listing start index.
    /// </summary>
    public int ListingIndexStart { get; internal set; }

    /// <summary>
    /// Gets the request ID.
    /// </summary>
    public int RequestId { get; internal set; }

    /// <summary>
    /// Read a <see cref="MarketBoardCurrentOfferings"/> object from memory.
    /// </summary>
    /// <param name="dataPtr">Address to read.</param>
    /// <returns>A new <see cref="MarketBoardCurrentOfferings"/> object.</returns>
    public static unsafe MarketBoardCurrentOfferings Read(IntPtr dataPtr)
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
            listingEntry.LastReviewTime = DateTimeOffset.UtcNow.AddSeconds(-reader.ReadUInt16()).DateTime;

            reader.ReadUInt16(); // container
            reader.ReadUInt32(); // slot
            reader.ReadUInt16(); // durability
            reader.ReadUInt16(); // spiritbond

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

            reader.ReadUInt16();
            reader.ReadUInt32();

            listingEntry.RetainerName = Encoding.UTF8.GetString(reader.ReadBytes(32)).TrimEnd('\u0000');
            listingEntry.PlayerName = Encoding.UTF8.GetString(reader.ReadBytes(32)).TrimEnd('\u0000');
            listingEntry.IsHq = reader.ReadBoolean();
            listingEntry.MateriaCount = reader.ReadByte();
            listingEntry.OnMannequin = reader.ReadBoolean();
            listingEntry.RetainerCityId = reader.ReadByte();
            listingEntry.StainId = reader.ReadUInt16();

            reader.ReadUInt16();
            reader.ReadUInt32();

            if (listingEntry.CatalogId != 0)
                listings.Add(listingEntry);
        }

        output.InternalItemListings = listings;
        output.ListingIndexEnd = reader.ReadByte();
        output.ListingIndexStart = reader.ReadByte();
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
        /// Gets the time this offering was last reviewed.
        /// </summary>
        public DateTime LastReviewTime { get; internal set; }

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
        /// Gets the player name.
        /// </summary>
        public string PlayerName { get; internal set; }

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
        /// Gets the stain or applied dye of the item.
        /// </summary>
        public int StainId { get; internal set; }

        /// <summary>
        /// Gets the total tax.
        /// </summary>
        public uint TotalTax { get; internal set; }

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

/// <summary>
/// An interface that represents the current market board offerings.
/// </summary>
public interface IMarketBoardCurrentOfferings
{
    /// <summary>
    /// Gets the list of individual item listings.
    /// </summary>
    IReadOnlyList<IMarketBoardItemListing> ItemListings { get; }

    /// <summary>
    /// Gets the listing end index.
    /// </summary>
    int ListingIndexEnd { get; }

    /// <summary>
    /// Gets the listing start index.
    /// </summary>
    int ListingIndexStart { get; }

    /// <summary>
    /// Gets the request ID.
    /// </summary>
    int RequestId { get; }
}

/// <summary>
/// An interface that represents the current market board offering of a single item from the <see cref="IMarketBoardCurrentOfferings"/>.
/// </summary>
public interface IMarketBoardItemListing
{
    /// <summary>
    /// Gets the artisan ID.
    /// </summary>
    ulong ArtisanId { get; }

    /// <summary>
    /// Gets the item ID.
    /// </summary>
    uint ItemId { get; }

    /// <summary>
    /// Gets a value indicating whether the item is HQ.
    /// </summary>
    bool IsHq { get; }

    /// <summary>
    /// Gets the item quantity.
    /// </summary>
    uint ItemQuantity { get; }

    /// <summary>
    /// Gets the time this offering was last reviewed.
    /// </summary>
    DateTime LastReviewTime { get; }

    /// <summary>
    /// Gets the listing ID.
    /// </summary>
    ulong ListingId { get; }

    /// <summary>
    /// Gets the list of materia attached to this item.
    /// </summary>
    IReadOnlyList<IItemMateria> Materia { get; }

    /// <summary>
    /// Gets the amount of attached materia.
    /// </summary>
    int MateriaCount { get; }

    /// <summary>
    /// Gets a value indicating whether this item is on a mannequin.
    /// </summary>
    bool OnMannequin { get; }

    /// <summary>
    /// Gets the player name.
    /// </summary>
    string PlayerName { get; }

    /// <summary>
    /// Gets the price per unit.
    /// </summary>
    uint PricePerUnit { get; }

    /// <summary>
    /// Gets the city ID of the retainer selling the item.
    /// </summary>
    int RetainerCityId { get; }

    /// <summary>
    /// Gets the ID of the retainer selling the item.
    /// </summary>
    ulong RetainerId { get; }

    /// <summary>
    /// Gets the name of the retainer.
    /// </summary>
    string RetainerName { get; }

    /// <summary>
    /// Gets the stain or applied dye of the item.
    /// </summary>
    int StainId { get; }

    /// <summary>
    /// Gets the total tax.
    /// </summary>
    uint TotalTax { get; }
}

/// <summary>
/// An interface that represents the materia slotted to an <see cref="IMarketBoardItemListing"/>.
/// </summary>
public interface IItemMateria
{
    /// <summary>
    /// Gets the materia index.
    /// </summary>
    int Index { get; }

    /// <summary>
    /// Gets the materia ID.
    /// </summary>
    int MateriaId { get; }
}

