using Dalamud.Game.Network.Structures;

namespace Dalamud.Game.Network.MarketBoardUploaders {
    internal interface IMarketBoardUploader {
        void Upload(MarketBoardItemRequest itemRequest);
        void UploadTax(MarketTaxRates taxRates);
    }
}
