using System.Collections.Immutable;
using Dalamud.Game.Network.Internal;
using Dalamud.Game.Network.Structures;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

namespace Dalamud.Game.Network;

/// <summary>
/// A scoped service that reports and manages access to marketboard data.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
[ResolveVia<IMarketBoard>]
public class MarketBoardPluginScoped : IMarketBoard, IServiceType, IDisposable
{
    [ServiceManager.ServiceDependency]
    private readonly MarketBoardService marketBoardService = Service<MarketBoardService>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketBoardPluginScoped"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    internal MarketBoardPluginScoped()
    {
        this.marketBoardService.OnListingsReceived += this.OnListingsReceivedProxy;
        this.marketBoardService.OnPurchaseCompleted += this.OnPurchaseCompletedProxy;
        this.marketBoardService.OnHistoryReceived += this.OnHistoryReceivedProxy;
        this.marketBoardService.OnTaxRatesReceived += this.OnTaxRatesReceivedProxy;
    }

    /// <inheritdoc />
    public event Action<ImmutableList<MarketBoardListing>>? OnListingsReceived;

    /// <inheritdoc />
    public event Action<MarketBoardListing>? OnPurchaseCompleted;

    /// <inheritdoc />
    public event Action<MarketBoardHistory>? OnHistoryReceived;

    /// <inheritdoc />
    public event Action<MarketTaxRates>? OnTaxRatesReceived;

    /// <inheritdoc/>
    void IDisposable.Dispose()
    {
        this.marketBoardService.OnListingsReceived -= this.OnListingsReceivedProxy;
        this.marketBoardService.OnPurchaseCompleted -= this.OnPurchaseCompletedProxy;
        this.marketBoardService.OnHistoryReceived -= this.OnHistoryReceivedProxy;
        this.marketBoardService.OnTaxRatesReceived -= this.OnTaxRatesReceivedProxy;

        GC.SuppressFinalize(this);
    }

    private void OnListingsReceivedProxy(ImmutableList<MarketBoardListing> listings) =>
        this.OnListingsReceived?.Invoke(listings);

    private void OnPurchaseCompletedProxy(MarketBoardListing listing) =>
        this.OnPurchaseCompleted?.Invoke(listing);

    private void OnHistoryReceivedProxy(MarketBoardHistory history) =>
        this.OnHistoryReceived?.Invoke(history);

    private void OnTaxRatesReceivedProxy(MarketTaxRates taxRates) =>
        this.OnTaxRatesReceived?.Invoke(taxRates);
}
