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
    public class World {
        /// <summary>
        /// ID of the world.
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// Name of the world.
        /// </summary>
        public string Name => (string) XivApi.GetWorld(this.Id).GetAwaiter().GetResult()["Name"];

        /// <summary>
        /// Set up the world resolver with the provided ID.
        /// </summary>
        /// <param name="id">The ID of the world.</param>
        public World(byte id) {
            this.Id = id;
        }
    }
}
