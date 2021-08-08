namespace Dalamud.Game.ClientState.Resolvers
{
    /// <summary>
    /// This object represents a Fate a character can participate in.
    /// </summary>
    public class FateResolver : BaseResolver<Lumina.Excel.GeneratedSheets.Fate>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FateResolver"/> class.
        /// Set up the Fate resolver with the provided ID.
        /// </summary>
        /// <param name="id">The ID of the Fate.</param>
        /// <param name="dalamud">The Dalamud instance.</param>
        internal FateResolver(ushort id, Dalamud dalamud)
            : base(id)
        {
        }
    }
}
