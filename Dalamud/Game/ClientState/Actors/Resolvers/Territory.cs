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
        /// Initializes a new instance of the <see cref="Territory"/> class.
        /// Set up the territory resolver with the provided ID.
        /// </summary>
        /// <param name="id">The ID of the territory.</param>
        /// <param name="dalamud">The Dalamud instance.</param>
        public Territory(ushort id, Dalamud dalamud)
            : base(dalamud)
        {
            this.Id = id;
        }

        /// <summary>
        /// Gets GameData linked to this territory.
        /// </summary>
        public Lumina.Excel.GeneratedSheets.TerritoryType GameData =>
            this.Dalamud.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.TerritoryType>().GetRow(this.Id);
    }
}
