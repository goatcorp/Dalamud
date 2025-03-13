using System.Threading.Tasks;

using Dalamud.Game.Network.Structures;

namespace Dalamud.Game.Network.Internal.MarketBoardUploaders;

/// <summary>
/// An interface binding for the Universalis uploader.
/// </summary>
internal interface IMarketBoardUploader
{
    /// <summary>
    /// Upload data about an item.
    /// </summary>
    /// <param name="item">The item request data being uploaded.</param>
    /// <param name="uploaderId">The uploaders ContentId.</param>
    /// <param name="worldId">The uploaders WorldId.</param>
    /// <returns>An async task.</returns>
    Task Upload(MarketBoardItemRequest item, ulong uploaderId, uint worldId);

    /// <summary>
    /// Upload tax rate data.
    /// </summary>
    /// <param name="taxRates">The tax rate data being uploaded.</param>
    /// <param name="uploaderId">The uploaders ContentId.</param>
    /// <param name="worldId">The uploaders WorldId.</param>
    /// <returns>An async task.</returns>
    Task UploadTax(MarketTaxRates taxRates, ulong uploaderId, uint worldId);

    /// <summary>
    /// Upload information about a purchase this client has made.
    /// </summary>
    /// <param name="purchaseHandler">The purchase handler data associated with the sale.</param>
    /// <param name="uploaderId">The uploaders ContentId.</param>
    /// <param name="worldId">The uploaders WorldId.</param>
    /// <returns>An async task.</returns>
    Task UploadPurchase(MarketBoardPurchaseHandler purchaseHandler, ulong uploaderId, uint worldId);
}
