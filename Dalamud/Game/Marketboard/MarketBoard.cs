using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

namespace Dalamud.Game.MarketBoard;

using Network.Internal;
using Network.Structures;

/// <summary>
/// This class provides access to market board events
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal class MarketBoard : IInternalDisposableService, IMarketBoard
{
    [ServiceManager.ServiceDependency]
    private readonly NetworkHandlers networkHandlers = Service<NetworkHandlers>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketBoard"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    public MarketBoard()
    {
        this.networkHandlers.MbHistoryObservable.Subscribe(this.OnMbHistory);
        this.networkHandlers.MbPurchaseObservable.Subscribe(this.OnPurchase);
        this.networkHandlers.MbOfferingsObservable.Subscribe(this.OnOfferings);
        this.networkHandlers.MbPurchaseSentObservable.Subscribe(this.OnPurchaseSent);
        this.networkHandlers.MbTaxesObservable.Subscribe(this.OnTaxRates);
    }

    /// <inheritdoc/>
    public event IMarketBoard.HistoryReceivedDelegate? HistoryReceived;

    /// <inheritdoc/>
    public event IMarketBoard.ItemPurchasedDelegate? ItemPurchased;

    /// <inheritdoc/>
    public event IMarketBoard.OfferingsReceivedDelegate? OfferingsReceived;

    /// <inheritdoc/>
    public event IMarketBoard.PurchaseRequestedDelegate? PurchaseRequested;

    /// <inheritdoc/>
    public event IMarketBoard.TaxRatesReceivedDelegate? TaxRatesReceived;

    /// <inheritdoc/>
    public void DisposeService()
    {
        this.HistoryReceived = null;
        this.ItemPurchased = null;
        this.OfferingsReceived = null;
        this.PurchaseRequested = null;
        this.TaxRatesReceived = null;
    }

    private void OnMbHistory(MarketBoardHistory marketBoardHistory)
    {
        this.HistoryReceived?.Invoke(marketBoardHistory);
    }

    private void OnPurchase(MarketBoardPurchase marketBoardHistory)
    {
        this.ItemPurchased?.Invoke(marketBoardHistory);
    }

    private void OnOfferings(MarketBoardCurrentOfferings currentOfferings)
    {
        this.OfferingsReceived?.Invoke(currentOfferings);
    }

    private void OnPurchaseSent(MarketBoardPurchaseHandler purchaseHandler)
    {
        this.PurchaseRequested?.Invoke(purchaseHandler);
    }

    private void OnTaxRates(MarketTaxRates taxRates)
    {
        this.TaxRatesReceived?.Invoke(taxRates);
    }
}

/// <summary>
/// Plugin scoped version of MarketBoard.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IMarketBoard>]
#pragma warning restore SA1015
internal class MarketBoardPluginScoped : IInternalDisposableService, IMarketBoard
{
    [ServiceManager.ServiceDependency]
    private readonly MarketBoard marketBoardService = Service<MarketBoard>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketBoardPluginScoped"/> class.
    /// </summary>
    internal MarketBoardPluginScoped()
    {
        this.marketBoardService.HistoryReceived += this.OnHistoryReceived;
        this.marketBoardService.ItemPurchased += this.OnItemPurchased;
        this.marketBoardService.OfferingsReceived += this.OnOfferingsReceived;
        this.marketBoardService.PurchaseRequested += this.OnPurchaseRequested;
        this.marketBoardService.TaxRatesReceived += this.OnTaxRatesReceived;
    }

    /// <inheritdoc/>
    public event IMarketBoard.HistoryReceivedDelegate? HistoryReceived;

    /// <inheritdoc/>
    public event IMarketBoard.ItemPurchasedDelegate? ItemPurchased;

    /// <inheritdoc/>
    public event IMarketBoard.OfferingsReceivedDelegate? OfferingsReceived;

    /// <inheritdoc/>
    public event IMarketBoard.PurchaseRequestedDelegate? PurchaseRequested;

    /// <inheritdoc/>
    public event IMarketBoard.TaxRatesReceivedDelegate? TaxRatesReceived;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.marketBoardService.HistoryReceived -= this.OnHistoryReceived;
        this.marketBoardService.ItemPurchased -= this.OnItemPurchased;
        this.marketBoardService.OfferingsReceived -= this.OnOfferingsReceived;
        this.marketBoardService.PurchaseRequested -= this.OnPurchaseRequested;
        this.marketBoardService.TaxRatesReceived -= this.OnTaxRatesReceived;
    }

    private void OnHistoryReceived(IMarketBoardHistory history)
    {
        this.HistoryReceived?.Invoke(history);
    }

    private void OnItemPurchased(IMarketBoardPurchase purchase)
    {
        this.ItemPurchased?.Invoke(purchase);
    }

    private void OnOfferingsReceived(IMarketBoardCurrentOfferings currentOfferings)
    {
        this.OfferingsReceived?.Invoke(currentOfferings);
    }

    private void OnPurchaseRequested(IMarketBoardPurchaseHandler purchaseHandler)
    {
        this.PurchaseRequested?.Invoke(purchaseHandler);
    }

    private void OnTaxRatesReceived(IMarketTaxRates taxRates)
    {
        this.TaxRatesReceived?.Invoke(taxRates);
    }
}
