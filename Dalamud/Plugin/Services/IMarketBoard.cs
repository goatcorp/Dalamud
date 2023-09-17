using Dalamud.Game.Network.Structures;

namespace Dalamud.Plugin.Services;

/// <summary>
/// A service that allows interaction with the in-game Market Board.
/// </summary>
public interface IMarketBoard
{
    /// <summary>
    /// An event that fires when the marketboard receives a full new batch of listings. Happens after all listings are
    /// populated.
    /// </summary>
    public event Action<MarketBoardSearchResults> OnListingsReceived;

    /// <summary>
    /// An event that fires when a purchase from the marketboard is <em>successfully</em> completed. Partial
    /// information about the bought item is included in this event. 
    /// </summary>
    public event Action<MarketBoardListing> OnPurchaseCompleted;

    /// <summary>
    /// An event that fires when the marketboard receives a new batch of history entries.
    /// </summary>
    public event Action<MarketBoardHistory> OnHistoryReceived;

    /// <summary>
    /// An event that fires when the marketboard receives new tax information.
    /// </summary>
    public event Action<MarketTaxRates> OnTaxRatesReceived;
}
