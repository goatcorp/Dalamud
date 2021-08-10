using System;

using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Game.Gui.PartyFinder.Types
{
    /// <summary>
    /// Extensions for the <see cref="JobFlags"/> enum.
    /// </summary>
    public static class JobFlagsExtensions
    {
        /// <summary>
        /// Get the actual ClassJob from the in-game sheets for this JobFlags.
        /// </summary>
        /// <param name="job">A JobFlags enum member.</param>
        /// <param name="data">A DataManager to get the ClassJob from.</param>
        /// <returns>A ClassJob if found or null if not.</returns>
        public static ClassJob ClassJob(this JobFlags job, DataManager data)
        {
            var result = Math.Log2((double)job);
            return result % 1 == 0
                ? data.GetExcelSheet<ClassJob>().GetRow((uint)result)
                : null;
        }
    }
}
