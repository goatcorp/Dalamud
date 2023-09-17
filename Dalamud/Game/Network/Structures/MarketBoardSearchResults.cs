using System.Collections.Generic;
using System.Collections.Immutable;

namespace Dalamud.Game.Network.Structures;

/// <summary>
/// A record representing a search result from the marketboard. This record is used instead of a raw ImmutableList as
/// it is possible for certain searches to return zero results.
/// </summary>
public record MarketBoardSearchResults
{
    /// <summary>
    /// Gets the Item ID that was searched on the marketboard. 
    /// </summary>
    public uint ItemId { get; init; }

    /// <summary>
    /// Gets a list of all search results found for this item. 
    /// </summary>
    public ImmutableList<MarketBoardListing> Listings { get; init; } = new List<MarketBoardListing>().ToImmutableList();
}
