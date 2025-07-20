using System.Globalization;
using System.Linq;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.MarketBoard;
using Dalamud.Game.Network.Structures;
using Dalamud.Interface.Utility;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Tests the various market board events.
/// </summary>
internal class MarketBoardSelfTestStep : ISelfTestStep
{
    private SubStep currentSubStep;
    private bool eventsSubscribed;

    private IMarketBoardHistoryListing? historyListing;
    private IMarketBoardItemListing? itemListing;
    private IMarketTaxRates? marketTaxRate;
    private IMarketBoardPurchaseHandler? marketBoardPurchaseRequest;
    private IMarketBoardPurchase? marketBoardPurchase;

    private enum SubStep
    {
        History,
        Offerings,
        PurchaseRequests,
        Purchases,
        Taxes,
        Done,
    }

    /// <inheritdoc/>
    public string Name => "Test MarketBoard";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        if (!this.eventsSubscribed)
        {
            this.SubscribeToEvents();
        }

        ImGui.TextUnformatted($"Testing: {this.currentSubStep.ToString()}");

        switch (this.currentSubStep)
        {
            case SubStep.History:

                if (this.historyListing == null)
                {
                    ImGui.TextUnformatted("Goto a Market Board. Open any item that has historical sale listings."u8);
                }
                else
                {
                    ImGui.TextUnformatted("Does one of the historical sales match this information?"u8);
                    ImGui.Separator();
                    ImGui.TextUnformatted($"Quantity: {this.historyListing.Quantity.ToString()}");
                    ImGui.TextUnformatted($"Buyer: {this.historyListing.BuyerName}");
                    ImGui.TextUnformatted($"Sale Price: {this.historyListing.SalePrice.ToString()}");
                    ImGui.TextUnformatted($"Purchase Time: {this.historyListing.PurchaseTime.ToLocalTime().ToString(CultureInfo.InvariantCulture)}");
                    ImGui.Separator();
                    if (ImGui.Button("Looks Correct / Skip"u8))
                    {
                        this.currentSubStep++;
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("No"u8))
                    {
                        return SelfTestStepResult.Fail;
                    }
                }

                break;
            case SubStep.Offerings:

                if (this.itemListing == null)
                {
                    ImGui.TextUnformatted("Goto a Market Board. Open any item that has sale listings."u8);
                }
                else
                {
                    ImGui.TextUnformatted("Does one of the sales match this information?"u8);
                    ImGui.Separator();
                    ImGui.TextUnformatted($"Quantity: {this.itemListing.ItemQuantity.ToString()}");
                    ImGui.TextUnformatted($"Price Per Unit: {this.itemListing.PricePerUnit}");
                    ImGui.TextUnformatted($"Retainer Name: {this.itemListing.RetainerName}");
                    ImGui.TextUnformatted($"Is HQ?: {(this.itemListing.IsHq ? "Yes" : "No")}");
                    ImGui.Separator();
                    if (ImGui.Button("Looks Correct / Skip"u8))
                    {
                        this.currentSubStep++;
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("No"u8))
                    {
                        return SelfTestStepResult.Fail;
                    }
                }

                break;
            case SubStep.PurchaseRequests:
                if (this.marketBoardPurchaseRequest == null)
                {
                    ImGui.TextUnformatted("Goto a Market Board. Purchase any item, the cheapest you can find."u8);
                }
                else
                {
                    ImGuiHelpers.SafeTextWrapped("Does this information match the purchase you made? This is testing the request to the server."u8);
                    ImGui.Separator();
                    ImGui.TextUnformatted($"Quantity: {this.marketBoardPurchaseRequest.ItemQuantity.ToString()}");
                    ImGui.TextUnformatted($"Item ID: {this.marketBoardPurchaseRequest.CatalogId}");
                    ImGui.TextUnformatted($"Price Per Unit: {this.marketBoardPurchaseRequest.PricePerUnit}");
                    ImGui.Separator();
                    if (ImGui.Button("Looks Correct / Skip"u8))
                    {
                        this.currentSubStep++;
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("No"u8))
                    {
                        return SelfTestStepResult.Fail;
                    }
                }

                break;
            case SubStep.Purchases:
                if (this.marketBoardPurchase == null)
                {
                    ImGui.TextUnformatted("Goto a Market Board. Purchase any item, the cheapest you can find."u8);
                }
                else
                {
                    ImGuiHelpers.SafeTextWrapped("Does this information match the purchase you made? This is testing the response from the server."u8);
                    ImGui.Separator();
                    ImGui.TextUnformatted($"Quantity: {this.marketBoardPurchase.ItemQuantity.ToString()}");
                    ImGui.TextUnformatted($"Item ID: {this.marketBoardPurchase.CatalogId}");
                    ImGui.Separator();
                    if (ImGui.Button("Looks Correct / Skip"u8))
                    {
                        this.currentSubStep++;
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("No"u8))
                    {
                        return SelfTestStepResult.Fail;
                    }
                }

                break;
            case SubStep.Taxes:
                if (this.marketTaxRate == null)
                {
                    ImGuiHelpers.SafeTextWrapped("Goto a Retainer Vocate and talk to then. Click the 'View market tax rates' menu item."u8);
                }
                else
                {
                    ImGui.TextUnformatted("Does this market tax rate information look correct?"u8);
                    ImGui.Separator();
                    ImGui.TextUnformatted($"Uldah: {this.marketTaxRate.UldahTax.ToString()}");
                    ImGui.TextUnformatted($"Gridania: {this.marketTaxRate.GridaniaTax.ToString()}");
                    ImGui.TextUnformatted($"Limsa Lominsa: {this.marketTaxRate.LimsaLominsaTax.ToString()}");
                    ImGui.TextUnformatted($"Ishgard: {this.marketTaxRate.IshgardTax.ToString()}");
                    ImGui.TextUnformatted($"Kugane: {this.marketTaxRate.KuganeTax.ToString()}");
                    ImGui.TextUnformatted($"Crystarium: {this.marketTaxRate.CrystariumTax.ToString()}");
                    ImGui.TextUnformatted($"Sharlayan: {this.marketTaxRate.SharlayanTax.ToString()}");
                    ImGui.TextUnformatted($"Tuliyollal: {this.marketTaxRate.TuliyollalTax.ToString()}");
                    ImGui.Separator();
                    if (ImGui.Button("Looks Correct / Skip"u8))
                    {
                        this.currentSubStep++;
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("No"u8))
                    {
                        return SelfTestStepResult.Fail;
                    }
                }

                break;
            case SubStep.Done:
                return SelfTestStepResult.Pass;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        this.currentSubStep = SubStep.History;
        this.historyListing = null;
        this.marketTaxRate = null;
        this.marketBoardPurchase = null;
        this.marketBoardPurchaseRequest = null;
        this.itemListing = null;
        this.UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        var marketBoard = Service<MarketBoard>.Get();
        marketBoard.HistoryReceived += this.OnHistoryReceived;
        marketBoard.OfferingsReceived += this.OnOfferingsReceived;
        marketBoard.ItemPurchased += this.OnItemPurchased;
        marketBoard.PurchaseRequested += this.OnPurchaseRequested;
        marketBoard.TaxRatesReceived += this.OnTaxRatesReceived;
        this.eventsSubscribed = true;
    }

    private void UnsubscribeFromEvents()
    {
        var marketBoard = Service<MarketBoard>.Get();
        marketBoard.HistoryReceived -= this.OnHistoryReceived;
        marketBoard.OfferingsReceived -= this.OnOfferingsReceived;
        marketBoard.ItemPurchased -= this.OnItemPurchased;
        marketBoard.PurchaseRequested -= this.OnPurchaseRequested;
        marketBoard.TaxRatesReceived -= this.OnTaxRatesReceived;
        this.eventsSubscribed = false;
    }

    private void OnTaxRatesReceived(IMarketTaxRates marketTaxRates)
    {
        this.marketTaxRate = marketTaxRates;
    }

    private void OnPurchaseRequested(IMarketBoardPurchaseHandler marketBoardPurchaseHandler)
    {
        this.marketBoardPurchaseRequest = marketBoardPurchaseHandler;
    }

    private void OnItemPurchased(IMarketBoardPurchase purchase)
    {
        this.marketBoardPurchase = purchase;
    }

    private void OnOfferingsReceived(IMarketBoardCurrentOfferings marketBoardCurrentOfferings)
    {
        if (marketBoardCurrentOfferings.ItemListings.Count != 0)
        {
            this.itemListing = marketBoardCurrentOfferings.ItemListings.First();
        }
    }

    private void OnHistoryReceived(IMarketBoardHistory marketBoardHistory)
    {
        if (marketBoardHistory.HistoryListings.Count != 0)
        {
            this.historyListing = marketBoardHistory.HistoryListings.First();
        }
    }
}
