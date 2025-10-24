using Dalamud.Data;
using Dalamud.Plugin.SelfTest;
using Dalamud.Utility;
using Lumina.Excel;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for Lumina.
/// </summary>
/// <typeparam name="T">ExcelRow to run test on.</typeparam>
/// <param name="isLargeSheet">Whether the sheet is large. If it is large, the self test will iterate through the full sheet in one frame and benchmark the time taken.</param>
internal class LuminaSelfTestStep<T>(bool isLargeSheet) : ISelfTestStep
    where T : struct, IExcelRow<T>
{
    private int step = 0;
    private ExcelSheet<T> rows;

    /// <inheritdoc/>
    public string Name => $"Test Lumina ({typeof(T).Name})";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        this.rows ??= Service<DataManager>.Get().GetExcelSheet<T>();

        if (isLargeSheet)
        {
            var i = 0;
            T currentRow = default;
            foreach (var row in this.rows)
            {
                i++;
                currentRow = row;
            }

            Util.ShowObject(currentRow);
            return SelfTestStepResult.Pass;
        }
        else
        {
            Util.ShowObject(this.rows.GetRowAt(this.step));

            this.step++;
            return this.step >= this.rows.Count ? SelfTestStepResult.Pass : SelfTestStepResult.Waiting;
        }
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        this.step = 0;
    }
}
