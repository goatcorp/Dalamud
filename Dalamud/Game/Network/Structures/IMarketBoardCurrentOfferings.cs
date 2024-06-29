using System.Collections.Generic;

namespace Dalamud.Game.Network.Structures;

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
    /// Gets the request ID.
    /// </summary>
    public int RequestId { get; }
}

/// <summary>
/// An interface that represents the current market board offering of a single item from the <see cref="IMarketBoardCurrentOfferings"/>.
/// </summary>
public interface IMarketBoardItemListing
{
    /// <summary>
    /// Gets the artisan ID.
    /// </summary>
    public ulong ArtisanId { get; }

    /// <summary>
    /// Gets the item ID.
    /// </summary>
    public uint ItemId { get; }

    /// <summary>
    /// Gets a value indicating whether the item is HQ.
    /// </summary>
    public bool IsHq { get; }

    /// <summary>
    /// Gets the item quantity.
    /// </summary>
    public uint ItemQuantity { get; }

    /// <summary>
    /// Gets the time this offering was last reviewed.
    /// </summary>
    [Obsolete("Universalis Compatibility, contains a fake value", false)]
    public DateTime LastReviewTime { get; }

    /// <summary>
    /// Gets the listing ID.
    /// </summary>
    public ulong ListingId { get; }

    /// <summary>
    /// Gets the list of materia attached to this item.
    /// </summary>
    public IReadOnlyList<IItemMateria> Materia { get; }

    /// <summary>
    /// Gets the amount of attached materia.
    /// </summary>
    public int MateriaCount { get; }

    /// <summary>
    /// Gets a value indicating whether this item is on a mannequin.
    /// </summary>
    public bool OnMannequin { get; }

    /// <summary>
    /// Gets the player name.
    /// </summary>
    [Obsolete("Universalis Compatibility, contains a fake value", false)]
    public string PlayerName { get; }

    /// <summary>
    /// Gets the price per unit.
    /// </summary>
    public uint PricePerUnit { get; }

    /// <summary>
    /// Gets the city ID of the retainer selling the item.
    /// </summary>
    public int RetainerCityId { get; }

    /// <summary>
    /// Gets the ID of the retainer selling the item.
    /// </summary>
    public ulong RetainerId { get; }

    /// <summary>
    /// Gets the name of the retainer.
    /// </summary>
    public string RetainerName { get; }

    /// <summary>
    /// Gets the stain or applied dye of the item.
    /// </summary>
    [Obsolete("Universalis Compatibility, use Stain1Id and Stain2Id", false)]
    public int StainId { get; }

    /// <summary>
    /// Gets the first stain or applied dye of the item.
    /// </summary>
    public int Stain1Id { get; }

    /// <summary>
    /// Gets the second stain or applied dye of the item.
    /// </summary>
    public int Stain2Id { get; }

    /// <summary>
    /// Gets the total tax.
    /// </summary>
    public uint TotalTax { get; }
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
