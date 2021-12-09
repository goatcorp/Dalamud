using Dalamud.Game.ClientState.Resolvers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace Dalamud.Game.ClientState.Aetherytes
{
    public class AetheryteEntry
    {
        private readonly TeleportInfo data;

        internal AetheryteEntry(TeleportInfo data)
        {
            this.data = data;
        }

        public uint AetheryteId => this.data.AetheryteId;

        public uint TerritoryId => this.data.TerritoryId;

        public byte SubIndex => this.data.SubIndex;

        public byte Ward => this.data.Ward;

        public byte Plot => this.data.Plot;

        public uint GilCost => this.data.GilCost;

        public bool IsFavourite => this.data.IsFavourite != 0;
        
        public bool IsSharedHouse => this.data.IsSharedHouse;

        public bool IsAppartment => this.data.IsAppartment;

        public ExcelResolver<Lumina.Excel.GeneratedSheets.Aetheryte> AetheryteData => new(this.AetheryteId);
    }
}
