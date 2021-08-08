namespace Dalamud.Game.ClientState.Resolvers
{
    /// <summary>
    /// This object represents a territory a character can be in.
    /// </summary>
    public class TerritoryTypeResolver : BaseResolver<Lumina.Excel.GeneratedSheets.TerritoryType>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TerritoryTypeResolver"/> class.
        /// Set up the territory type resolver with the provided ID.
        /// </summary>
        /// <param name="id">The ID of the territory type.</param>
        /// <param name="dalamud">The Dalamud instance.</param>
        internal TerritoryTypeResolver(ushort id, Dalamud dalamud)
            : base(id)
        {
        }
    }
}
