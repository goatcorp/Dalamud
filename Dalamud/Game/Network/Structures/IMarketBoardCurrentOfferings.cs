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
