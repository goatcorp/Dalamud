using Dalamud.Data;
using Lumina.Excel;

namespace Dalamud.Game.ClientState.Resolvers
{
    /// <summary>
    /// This object represents a class or job.
    /// </summary>
    /// <typeparam name="T">The type of Lumina sheet to resolve.</typeparam>
    public class BaseResolver<T> where T : ExcelRow
    {
        private readonly DataManager data;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseResolver{T}"/> class.
        /// </summary>
        /// <param name="id">The ID of the classJob.</param>
        internal BaseResolver(uint id)
        {
            this.data = Service<DataManager>.Get();
            this.Id = id;
        }

        /// <summary>
        /// Gets the ID to be resolved.
        /// </summary>
        public uint Id { get; }

        /// <summary>
        /// Gets GameData linked to this excel row.
        /// </summary>
        public T? GameData => this.data.GetExcelSheet<T>().GetRow(this.Id);
    }
}
