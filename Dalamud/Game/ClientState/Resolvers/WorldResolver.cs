namespace Dalamud.Game.ClientState.Resolvers
{
    /// <summary>
    /// This object represents a world a character can reside on.
    /// </summary>
    public class WorldResolver : BaseResolver<Lumina.Excel.GeneratedSheets.World>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorldResolver"/> class.
        /// Set up the world resolver with the provided ID.
        /// </summary>
        /// <param name="id">The ID of the world.</param>
        /// <param name="dalamud">The Dalamud instance.</param>
        internal WorldResolver(ushort id, Dalamud dalamud)
            : base(id)
        {
        }
    }
}
