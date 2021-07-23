namespace Dalamud.Game.ClientState.Resolvers
{
    /// <summary>
    /// This object represents a pet.
    /// </summary>
    public class PetResolver : BaseResolver<Lumina.Excel.GeneratedSheets.Pet>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PetResolver"/> class.
        /// Set up the Pet resolver with the provided ID.
        /// </summary>
        /// <param name="id">The ID of the classJob.</param>
        /// <param name="dalamud">The Dalamud instance.</param>
        internal PetResolver(uint id, Dalamud dalamud)
            : base(id, dalamud)
        {
        }
    }
}
