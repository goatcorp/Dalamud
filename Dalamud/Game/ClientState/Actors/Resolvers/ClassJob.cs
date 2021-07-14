namespace Dalamud.Game.ClientState.Actors.Resolvers
{
    /// <summary>
    /// This object represents a class or job.
    /// </summary>
    public class ClassJob : BaseResolver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClassJob"/> class.
        /// Set up the ClassJob resolver with the provided ID.
        /// </summary>
        /// <param name="id">The ID of the classJob.</param>
        /// <param name="dalamud">The Dalamud instance.</param>
        internal ClassJob(byte id, Dalamud dalamud)
            : base(dalamud)
        {
            this.Id = id;
        }

        /// <summary>
        /// Gets the ID of the ClassJob.
        /// </summary>
        public uint Id { get; }

        /// <summary>
        /// Gets GameData linked to this ClassJob.
        /// </summary>
        public Lumina.Excel.GeneratedSheets.ClassJob GameData =>
            this.Dalamud.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.ClassJob>().GetRow(this.Id);
    }
}
