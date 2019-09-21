using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dalamud.Game.Network.Structures {
    public class MarketBoardHistory {
        public uint CatalogId;
        public uint CatalogId2;

        public List<MarketBoardHistoryListing> HistoryListings;

        public static unsafe MarketBoardHistory Read(IntPtr dataPtr) {
            var output = new MarketBoardHistory();

            using (var stream = new UnmanagedMemoryStream((byte*) dataPtr.ToPointer(), 1544)) {
                using (var reader = new BinaryReader(stream)) {
                    output.CatalogId = reader.ReadUInt32();
                    output.CatalogId2 = reader.ReadUInt32();

                    output.HistoryListings = new List<MarketBoardHistoryListing>();

                    for (var i = 0; i < 10; i++) {
                        var listingEntry = new MarketBoardHistoryListing();

                        listingEntry.SalePrice = reader.ReadUInt32();
                        listingEntry.PurchaseTime = DateTimeOffset.FromUnixTimeSeconds(reader.ReadUInt32()).UtcDateTime;
                        listingEntry.Quantity = reader.ReadUInt32();
                        listingEntry.IsHq = reader.ReadBoolean();

                        reader.ReadBoolean();

                        listingEntry.OnMannequin = reader.ReadBoolean();
                        listingEntry.BuyerName = Encoding.UTF8.GetString(reader.ReadBytes(33)).TrimEnd('\u0000');
                        listingEntry.CatalogId = reader.ReadUInt32();

                        if (listingEntry.CatalogId != 0)
                            output.HistoryListings.Add(listingEntry);
                    }
                }
            }

            return output;
        }

        public class MarketBoardHistoryListing {
            public string BuyerName;

            public uint CatalogId;
            public bool IsHq;
            public bool OnMannequin;
            public DateTime PurchaseTime;
            public uint Quantity;
            public uint SalePrice;
        }
    }
}
