namespace Dalamud.Game.ClientState.Resolvers
{
    /// <summary>
    /// This object represents a mount.
    /// </summary>
    public class MountResolver : BaseResolver<Lumina.Excel.GeneratedSheets.Mount>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MountResolver"/> class.
        /// Set up the Mount resolver with the provided ID.
        /// </summary>
        /// <param name="id">The ID of the classJob.</param>
        /// <param name="dalamud">The Dalamud instance.</param>
        internal MountResolver(uint id, Dalamud dalamud)
            : base(id, dalamud)
        {
        }
    }
}
