using Dalamud.Game.Network.Internal;
using Dalamud.Game.Network.Structures;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;

using static Dalamud.Plugin.Services.IMarketBoard;

namespace Dalamud.Game.MarketBoard;

/// <summary>
/// This class provides access to market board events.
/// </summary>
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
    public event HistoryReceivedDelegate? HistoryReceived;

    /// <inheritdoc/>
    public event ItemPurchasedDelegate? ItemPurchased;

    /// <inheritdoc/>
    public event OfferingsReceivedDelegate? OfferingsReceived;

    /// <inheritdoc/>
    public event PurchaseRequestedDelegate? PurchaseRequested;

    /// <inheritdoc/>
    public event TaxRatesReceivedDelegate? TaxRatesReceived;

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
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IMarketBoard>]
#pragma warning restore SA1015
internal class MarketBoardPluginScoped : IInternalDisposableService, IMarketBoard
{
    private static readonly ModuleLog Log = ModuleLog.Create<MarketBoardPluginScoped>();

    [ServiceManager.ServiceDependency]
    private readonly MarketBoard marketBoardService = Service<MarketBoard>.Get();

    private readonly string owningPluginName;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketBoardPluginScoped"/> class.
    /// </summary>
    /// <param name="plugin">The plugin owning this service.</param>
    internal MarketBoardPluginScoped(LocalPlugin? plugin)
    {
        this.marketBoardService.HistoryReceived += this.OnHistoryReceived;
        this.marketBoardService.ItemPurchased += this.OnItemPurchased;
        this.marketBoardService.OfferingsReceived += this.OnOfferingsReceived;
        this.marketBoardService.PurchaseRequested += this.OnPurchaseRequested;
        this.marketBoardService.TaxRatesReceived += this.OnTaxRatesReceived;

        this.owningPluginName = plugin?.InternalName ?? "DalamudInternal";
    }

    /// <inheritdoc/>
    public event HistoryReceivedDelegate? HistoryReceived;

    /// <inheritdoc/>
    public event ItemPurchasedDelegate? ItemPurchased;

    /// <inheritdoc/>
    public event OfferingsReceivedDelegate? OfferingsReceived;

    /// <inheritdoc/>
    public event PurchaseRequestedDelegate? PurchaseRequested;

    /// <inheritdoc/>
    public event TaxRatesReceivedDelegate? TaxRatesReceived;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.marketBoardService.HistoryReceived -= this.OnHistoryReceived;
        this.marketBoardService.ItemPurchased -= this.OnItemPurchased;
        this.marketBoardService.OfferingsReceived -= this.OnOfferingsReceived;
        this.marketBoardService.PurchaseRequested -= this.OnPurchaseRequested;
        this.marketBoardService.TaxRatesReceived -= this.OnTaxRatesReceived;

        this.HistoryReceived = null;
        this.ItemPurchased = null;
        this.OfferingsReceived = null;
        this.PurchaseRequested = null;
        this.TaxRatesReceived = null;
    }

    private void OnHistoryReceived(IMarketBoardHistory history)
    {
        foreach (var action in Delegate.EnumerateInvocationList(this.HistoryReceived))
        {
            try
            {
                action.Invoke(history);
            }
            catch (Exception ex)
            {
                this.LogInvocationError(ex, nameof(this.HistoryReceived));
            }
        }
    }

    private void OnItemPurchased(IMarketBoardPurchase purchase)
    {
        foreach (var action in Delegate.EnumerateInvocationList(this.ItemPurchased))
        {
            try
            {
                action.Invoke(purchase);
            }
            catch (Exception ex)
            {
                this.LogInvocationError(ex, nameof(this.ItemPurchased));
            }
        }
    }

    private void OnOfferingsReceived(IMarketBoardCurrentOfferings currentOfferings)
    {
        foreach (var action in Delegate.EnumerateInvocationList(this.OfferingsReceived))
        {
            try
            {
                action.Invoke(currentOfferings);
            }
            catch (Exception ex)
            {
                this.LogInvocationError(ex, nameof(this.OfferingsReceived));
            }
        }
    }

    private void OnPurchaseRequested(IMarketBoardPurchaseHandler purchaseHandler)
    {
        foreach (var action in Delegate.EnumerateInvocationList(this.PurchaseRequested))
        {
            try
            {
                action.Invoke(purchaseHandler);
            }
            catch (Exception ex)
            {
                this.LogInvocationError(ex, nameof(this.PurchaseRequested));
            }
        }
    }

    private void OnTaxRatesReceived(IMarketTaxRates taxRates)
    {
        foreach (var action in Delegate.EnumerateInvocationList(this.TaxRatesReceived))
        {
            try
            {
                action.Invoke(taxRates);
            }
            catch (Exception ex)
            {
                this.LogInvocationError(ex, nameof(this.TaxRatesReceived));
            }
        }
    }

    private void LogInvocationError(Exception ex, string delegateName)
    {
        Log.Error(
            ex,
            "An error occured while invoking event `{evName}` for {plugin}",
            delegateName,
            this.owningPluginName);
    }
}
