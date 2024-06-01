namespace Dalamud.Plugin.Services;

using Game.Network.Structures;

/// <summary>
/// Provides access to market board related events as the client receives/sends them.
/// </summary>
public interface IMarketBoard
{
    /// <summary>
    /// A delegate type used with the <see cref="MarketBoardHistoryReceived"/> event.
    /// </summary>
    /// <param name="marketBoardHistory">The historical listings for a particular item on the market board.</param>
    public delegate void MarketBoardHistoryReceivedDelegate(IMarketBoardHistory marketBoardHistory);

    /// <summary>
    /// A delegate type used with the <see cref="MarketBoardItemPurchased"/> event.
    /// </summary>
    /// <param name="marketBoardPurchased">The item that has been purchased.</param>
    public delegate void MarketBoardItemPurchasedDelegate(IMarketBoardPurchase marketBoardPurchased);

    /// <summary>
    /// A delegate type used with the <see cref="MarketBoardOfferingsReceived"/> event.
    /// </summary>
    /// <param name="marketBoardCurrentOfferings">The current offerings for a particular item on the market board.</param>
    public delegate void MarketBoardOfferingsReceivedDelegate(IMarketBoardCurrentOfferings marketBoardCurrentOfferings);

    /// <summary>
    /// A delegate type used with the <see cref="MarketBoardPurchaseRequested"/> event.
    /// </summary>
    /// <param name="marketBoardPurchaseRequested">The details about the item being purchased.</param>
    public delegate void MarketBoardPurchaseRequestedDelegate(IMarketBoardPurchaseHandler marketBoardPurchaseRequested);

    /// <summary>
    /// A delegate type used with the <see cref="MarketBoardPurchaseRequested"/> event.
    /// </summary>
    /// <param name="marketTaxRates">The new tax rates.</param>
    public delegate void MarketTaxRatesReceivedDelegate(IMarketTaxRates marketTaxRates);

    /// <summary>
    /// Event that fires when historical sale listings are received for a specific item on the market board.
    /// </summary>
    public event MarketBoardHistoryReceivedDelegate MarketBoardHistoryReceived;

    /// <summary>
    /// Event that fires when a item is purchased on the market board.
    /// </summary>
    public event MarketBoardItemPurchasedDelegate MarketBoardItemPurchased;

    /// <summary>
    /// Event that fires when current offerings are received for a specific item on the market board.
    /// </summary>
    public event MarketBoardOfferingsReceivedDelegate MarketBoardOfferingsReceived;

    /// <summary>
    /// Event that fires when a player requests to purchase an item from the market board.
    /// </summary>
    public event MarketBoardPurchaseRequestedDelegate MarketBoardPurchaseRequested;

    /// <summary>
    /// Event that fires when the client receives new tax rates. These events only occur when accessing a retainer vocate and requesting the tax rates.
    /// </summary>
    public event MarketTaxRatesReceivedDelegate MarketTaxRatesReceived;
}
