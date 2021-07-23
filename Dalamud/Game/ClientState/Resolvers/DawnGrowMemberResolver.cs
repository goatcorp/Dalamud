namespace Dalamud.Game.ClientState.Resolvers
{
    /// <summary>
    /// This object represents a trust buddy.
    /// </summary>
    public class DawnGrowMemberResolver : BaseResolver<Lumina.Excel.GeneratedSheets.DawnGrowMember>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DawnGrowMemberResolver"/> class.
        /// Set up the DawnGrowMember resolver with the provided ID.
        /// </summary>
        /// <param name="id">The ID of the classJob.</param>
        /// <param name="dalamud">The Dalamud instance.</param>
        internal DawnGrowMemberResolver(uint id, Dalamud dalamud)
            : base(id, dalamud)
        {
        }
    }
}
