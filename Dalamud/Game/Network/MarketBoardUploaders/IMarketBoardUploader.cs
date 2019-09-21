namespace Dalamud.Game.Network.MarketBoardUploaders {
    internal interface IMarketBoardUploader {
        void Upload(MarketBoardItemRequest itemRequest);
    }
}
