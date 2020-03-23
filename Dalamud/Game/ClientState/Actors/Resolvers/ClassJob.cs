using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Actors.Resolvers
{
    /// <summary>
    /// This object represents a class or job.
    /// </summary>
    public class ClassJob : BaseResolver {
        /// <summary>
        /// ID of the ClassJob.
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// GameData linked to this ClassJob.
        /// </summary>
        public Lumina.Excel.GeneratedSheets.ClassJob GameData =>
            this.dalamud.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.ClassJob>().GetRow(this.Id);

        /// <summary>
        /// Set up the ClassJob resolver with the provided ID.
        /// </summary>
        /// <param name="id">The ID of the world.</param>
        public ClassJob(byte id, Dalamud dalamud) : base(dalamud) {
            this.Id = id;
        }
    }
}
