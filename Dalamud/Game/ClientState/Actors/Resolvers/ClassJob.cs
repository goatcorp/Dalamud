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
    public class ClassJob {
        /// <summary>
        /// ID of the ClassJob.
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// Name of the ClassJob.
        /// </summary>
        public string Name => (string) XivApi.GetClassJob(this.Id).GetAwaiter().GetResult()["Name"];

        /// <summary>
        /// Set up the ClassJob resolver with the provided ID.
        /// </summary>
        /// <param name="id">The ID of the world.</param>
        public ClassJob(byte id) {
            this.Id = id;
        }
    }
}
