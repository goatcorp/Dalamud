using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Actors.Resolvers
{
    /// <summary>
    /// This object represents a world a character can reside on.
    /// </summary>
    public class World : BaseResolver {
        /// <summary>
        /// ID of the world.
        /// </summary>
        public readonly uint Id;

        /// <summary>
        /// GameData linked to this world.
        /// </summary>
        public Lumina.Excel.GeneratedSheets.World GameData =>
            this.dalamud.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.World>().GetRow(this.Id);

        /// <summary>
        /// Set up the world resolver with the provided ID.
        /// </summary>
        /// <param name="id">The ID of the world.</param>
        public World(ushort id, Dalamud dalamud) : base(dalamud) {
            this.Id = id;
        }
    }
}
