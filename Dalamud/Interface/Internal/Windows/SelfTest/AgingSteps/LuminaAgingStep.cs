using System.Collections.Generic;
using System.Linq;

using Dalamud.Utility;
using Lumina.Excel;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps
{
    /// <summary>
    /// Test setup for Lumina.
    /// </summary>
    /// <typeparam name="T">ExcelRow to run test on.</typeparam>
    internal class LuminaAgingStep<T> : IAgingStep
        where T : ExcelRow
    {
        private int step = 0;
        private List<T> rows;

        /// <inheritdoc/>
        public string Name => "Test Lumina";

        /// <inheritdoc/>
        public SelfTestStepResult RunStep(Dalamud dalamud)
        {
            this.rows ??= dalamud.Data.GetExcelSheet<T>().ToList();

            Util.ShowObject(this.rows[this.step]);

            this.step++;
            return this.step >= this.rows.Count ? SelfTestStepResult.Pass : SelfTestStepResult.Waiting;
        }

        /// <inheritdoc/>
        public void CleanUp(Dalamud dalamud)
        {
            // ignored
        }
    }
}
