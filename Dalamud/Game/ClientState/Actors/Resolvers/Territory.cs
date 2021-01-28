using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Actors.Resolvers
{
    /// <summary>
    /// This object represents a territory.
    /// </summary>
    public class Territory : BaseResolver
    {
        /// <summary>
        /// ID of the Territory.
        /// </summary>
        public readonly ushort Id;

        /// <summary>
        /// GameData linked to this Territory.
        /// </summary>
        public Lumina.Excel.GeneratedSheets.TerritoryType GameData =>
            this.dalamud.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.TerritoryType>().GetRow(this.Id);

        /// <summary>
        /// Set up the Territory resolver with the provided ID.
        /// </summary>
        /// <param name="id">The ID of the territory.</param>
        public Territory(ushort id, Dalamud dalamud) : base(dalamud)
        {
            this.Id = id;
        }
    }
}
