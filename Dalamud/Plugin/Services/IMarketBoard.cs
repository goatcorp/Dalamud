using Dalamud.Game.Network.Structures;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Provides access to market board related events as the client receives/sends them.
/// </summary>
public interface IMarketBoard
{
    /// <summary>
    /// A delegate type used with the <see cref="HistoryReceived"/> event.
    /// </summary>
    /// <param name="history">The historical listings for a particular item on the market board.</param>
    public delegate void HistoryReceivedDelegate(IMarketBoardHistory history);

    /// <summary>
    /// A delegate type used with the <see cref="ItemPurchased"/> event.
    /// </summary>
    /// <param name="purchase">The item that has been purchased.</param>
    public delegate void ItemPurchasedDelegate(IMarketBoardPurchase purchase);

    /// <summary>
    /// A delegate type used with the <see cref="OfferingsReceived"/> event.
    /// </summary>
    /// <param name="currentOfferings">The current offerings for a particular item on the market board.</param>
    public delegate void OfferingsReceivedDelegate(IMarketBoardCurrentOfferings currentOfferings);

    /// <summary>
    /// A delegate type used with the <see cref="PurchaseRequested"/> event.
    /// </summary>
    /// <param name="purchaseRequested">The details about the item being purchased.</param>
    public delegate void PurchaseRequestedDelegate(IMarketBoardPurchaseHandler purchaseRequested);

    /// <summary>
    /// A delegate type used with the <see cref="PurchaseRequested"/> event.
    /// </summary>
    /// <param name="taxRates">The new tax rates.</param>
    public delegate void TaxRatesReceivedDelegate(IMarketTaxRates taxRates);

    /// <summary>
    /// Event that fires when historical sale listings are received for a specific item on the market board.
    /// </summary>
    public event HistoryReceivedDelegate HistoryReceived;

    /// <summary>
    /// Event that fires when a item is purchased on the market board.
    /// </summary>
    public event ItemPurchasedDelegate ItemPurchased;

    /// <summary>
    /// Event that fires when current offerings are received for a specific item on the market board.
    /// </summary>
    public event OfferingsReceivedDelegate OfferingsReceived;

    /// <summary>
    /// Event that fires when a player requests to purchase an item from the market board.
    /// </summary>
    public event PurchaseRequestedDelegate PurchaseRequested;

    /// <summary>
    /// Event that fires when the client receives new tax rates. These events only occur when accessing a retainer vocate and requesting the tax rates.
    /// </summary>
    public event TaxRatesReceivedDelegate TaxRatesReceived;
}
