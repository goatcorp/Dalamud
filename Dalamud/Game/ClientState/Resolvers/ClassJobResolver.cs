namespace Dalamud.Game.ClientState.Resolvers
{
    /// <summary>
    /// This object represents a class or job.
    /// </summary>
    public class ClassJobResolver : BaseResolver<Lumina.Excel.GeneratedSheets.ClassJob>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClassJobResolver"/> class.
        /// Set up the ClassJob resolver with the provided ID.
        /// </summary>
        /// <param name="id">The ID of the classJob.</param>
        /// <param name="dalamud">The Dalamud instance.</param>
        internal ClassJobResolver(ushort id, Dalamud dalamud)
            : base(id)
        {
        }
    }
}
