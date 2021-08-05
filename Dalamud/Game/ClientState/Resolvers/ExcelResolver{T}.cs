using Lumina.Excel;

namespace Dalamud.Game.ClientState.Resolvers
{
    /// <summary>
    /// This object resolves a rowID within an Excel sheet.
    /// </summary>
    /// <typeparam name="T">The type of Lumina sheet to resolve.</typeparam>
    public class ExcelResolver<T> where T : ExcelRow
    {
        private readonly Dalamud dalamud;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelResolver{T}"/> class.
        /// </summary>
        /// <param name="id">The ID of the classJob.</param>
        /// <param name="dalamud">The Dalamud instance.</param>
        internal ExcelResolver(uint id, Dalamud dalamud)
        {
            this.dalamud = dalamud;
            this.Id = id;
        }

        /// <summary>
        /// Gets the ID to be resolved.
        /// </summary>
        public uint Id { get; }

        /// <summary>
        /// Gets GameData linked to this excel row.
        /// </summary>
        public T GameData => this.dalamud.Data.GetExcelSheet<T>().GetRow(this.Id);
    }
}
