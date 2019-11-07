using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dalamud.Game.Network.Structures
{
    public class MarketBoardCurrentOfferings
    {
        public List<MarketBoardItemListing> ItemListings;

        public int ListingIndexEnd;
        public int ListingIndexStart;
        public int RequestId;

        public static unsafe MarketBoardCurrentOfferings Read(IntPtr dataPtr)
        {
            var output = new MarketBoardCurrentOfferings();

            using (var stream = new UnmanagedMemoryStream((byte*)dataPtr.ToPointer(), 1544))
            {
                using (var reader = new BinaryReader(stream))
                {
                    output.ItemListings = new List<MarketBoardItemListing>();

                    for (var i = 0; i < 10; i++)
                    {
                        var listingEntry = new MarketBoardItemListing();

                        listingEntry.ListingId = reader.ReadUInt64();
                        listingEntry.RetainerId = reader.ReadUInt64();
                        listingEntry.RetainerOwnerId = reader.ReadUInt64();
                        listingEntry.ArtisanId = reader.ReadUInt64();
                        listingEntry.PricePerUnit = reader.ReadUInt32();
                        listingEntry.TotalTax = reader.ReadUInt32();
                        listingEntry.ItemQuantity = reader.ReadUInt32();
                        listingEntry.CatalogId = reader.ReadUInt32();
                        listingEntry.LastReviewTime = DateTimeOffset.UtcNow.AddSeconds(-reader.ReadUInt16()).DateTime;

                        reader.ReadUInt16(); // container
                        reader.ReadUInt32(); // slot
                        reader.ReadUInt16(); // durability
                        reader.ReadUInt16(); // spiritbond

                        listingEntry.Materia = new List<MarketBoardItemListing.ItemMateria>();

                        for (var materiaIndex = 0; materiaIndex < 5; materiaIndex++)
                        {
                            var materiaVal = reader.ReadUInt16();

                            var materiaEntry = new MarketBoardItemListing.ItemMateria();
                            materiaEntry.MateriaId = (materiaVal & 0xFF0) >> 4;
                            materiaEntry.Index = materiaVal & 0xF;

                            if (materiaEntry.MateriaId != 0)
                                listingEntry.Materia.Add(materiaEntry);
                        }

                        reader.ReadUInt16();
                        reader.ReadUInt32();

                        listingEntry.RetainerName = Encoding.UTF8.GetString(reader.ReadBytes(32)).TrimEnd('\u0000');
                        listingEntry.PlayerName = Encoding.UTF8.GetString(reader.ReadBytes(32)).TrimEnd('\u0000');
                        listingEntry.IsHq = reader.ReadBoolean();
                        listingEntry.MateriaCount = reader.ReadByte();
                        listingEntry.OnMannequin = reader.ReadBoolean();
                        listingEntry.RetainerCityId = reader.ReadByte();
                        listingEntry.StainId = reader.ReadUInt16();

                        reader.ReadUInt16();
                        reader.ReadUInt32();

                        if (listingEntry.CatalogId != 0)
                            output.ItemListings.Add(listingEntry);
                    }

                    output.ListingIndexEnd = reader.ReadByte();
                    output.ListingIndexStart = reader.ReadByte();
                    output.RequestId = reader.ReadUInt16();
                }
            }

            return output;
        }

        public class MarketBoardItemListing
        {
            public ulong ArtisanId;
            public uint CatalogId;
            public bool IsHq;
            public uint ItemQuantity;
            public DateTime LastReviewTime;
            public ulong ListingId;

            public List<ItemMateria> Materia;
            public int MateriaCount;
            public bool OnMannequin;
            public string PlayerName;
            public uint PricePerUnit;
            public int RetainerCityId;
            public ulong RetainerId;

            public string RetainerName;
            public ulong RetainerOwnerId;
            public int StainId;
            public uint TotalTax;

            public class ItemMateria
            {
                public int Index;
                public int MateriaId;
            }
        }
    }
}
