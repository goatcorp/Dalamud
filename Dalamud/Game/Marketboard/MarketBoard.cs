using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

namespace Dalamud.Game.MarketBoard;

using Network.Internal;
using Network.Structures;

/// <summary>
/// This class represents the state of the currently occupied duty.
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
    public event IMarketBoard.MarketBoardHistoryReceivedDelegate? MarketBoardHistoryReceived;

    /// <inheritdoc/>
    public event IMarketBoard.MarketBoardItemPurchasedDelegate? MarketBoardItemPurchased;

    /// <inheritdoc/>
    public event IMarketBoard.MarketBoardOfferingsReceivedDelegate? MarketBoardOfferingsReceived;

    /// <inheritdoc/>
    public event IMarketBoard.MarketBoardPurchaseRequestedDelegate? MarketBoardPurchaseRequested;

    /// <inheritdoc/>
    public event IMarketBoard.MarketTaxRatesReceivedDelegate? MarketTaxRatesReceived;

    /// <inheritdoc/>
    public void DisposeService()
    {
    }

    private void OnMbHistory(MarketBoardHistory marketBoardHistory)
    {
        this.MarketBoardHistoryReceived?.Invoke(marketBoardHistory);
    }

    private void OnPurchase(MarketBoardPurchase marketBoardHistory)
    {
        this.MarketBoardItemPurchased?.Invoke(marketBoardHistory);
    }

    private void OnOfferings(MarketBoardCurrentOfferings currentOfferings)
    {
        this.MarketBoardOfferingsReceived?.Invoke(currentOfferings);
    }

    private void OnPurchaseSent(MarketBoardPurchaseHandler purchaseHandler)
    {
        this.MarketBoardPurchaseRequested?.Invoke(purchaseHandler);
    }

    private void OnTaxRates(MarketTaxRates taxRates)
    {
        this.MarketTaxRatesReceived?.Invoke(taxRates);
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
        this.marketBoardService.MarketBoardHistoryReceived += this.OnMarketBoardHistoryReceived;
        this.marketBoardService.MarketBoardItemPurchased += this.OnMarketBoardItemPurchased;
        this.marketBoardService.MarketBoardOfferingsReceived += this.OnMarketBoardOfferingsReceived;
        this.marketBoardService.MarketBoardPurchaseRequested += this.OnMarketBoardPurchaseRequested;
        this.marketBoardService.MarketTaxRatesReceived += this.OnMarketTaxRatesReceived;
    }

    /// <inheritdoc/>
    public event IMarketBoard.MarketBoardHistoryReceivedDelegate? MarketBoardHistoryReceived;

    /// <inheritdoc/>
    public event IMarketBoard.MarketBoardItemPurchasedDelegate? MarketBoardItemPurchased;

    /// <inheritdoc/>
    public event IMarketBoard.MarketBoardOfferingsReceivedDelegate? MarketBoardOfferingsReceived;

    /// <inheritdoc/>
    public event IMarketBoard.MarketBoardPurchaseRequestedDelegate? MarketBoardPurchaseRequested;

    /// <inheritdoc/>
    public event IMarketBoard.MarketTaxRatesReceivedDelegate? MarketTaxRatesReceived;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.marketBoardService.MarketBoardHistoryReceived -= this.OnMarketBoardHistoryReceived;
        this.marketBoardService.MarketBoardItemPurchased -= this.OnMarketBoardItemPurchased;
        this.marketBoardService.MarketBoardOfferingsReceived -= this.OnMarketBoardOfferingsReceived;
        this.marketBoardService.MarketBoardPurchaseRequested -= this.OnMarketBoardPurchaseRequested;
        this.marketBoardService.MarketTaxRatesReceived -= this.OnMarketTaxRatesReceived;
    }

    private void OnMarketBoardHistoryReceived(IMarketBoardHistory marketBoardHistory)
    {
        this.MarketBoardHistoryReceived?.Invoke(marketBoardHistory);
    }

    private void OnMarketBoardItemPurchased(IMarketBoardPurchase marketBoardPurchase)
    {
        this.MarketBoardItemPurchased?.Invoke(marketBoardPurchase);
    }

    private void OnMarketBoardOfferingsReceived(IMarketBoardCurrentOfferings marketBoardCurrentOfferings)
    {
        this.MarketBoardOfferingsReceived?.Invoke(marketBoardCurrentOfferings);
    }

    private void OnMarketBoardPurchaseRequested(IMarketBoardPurchaseHandler marketBoardPurchaseHandler)
    {
        this.MarketBoardPurchaseRequested?.Invoke(marketBoardPurchaseHandler);
    }

    private void OnMarketTaxRatesReceived(IMarketTaxRates marketTaxRates)
    {
        this.MarketTaxRatesReceived?.Invoke(marketTaxRates);
    }
}
