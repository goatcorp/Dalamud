using Dalamud.Game.ClientState.Fates;
using Dalamud.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Test setup for the Fate Table.
/// </summary>
internal class FateTableAgingStep : IAgingStep
{
    private int index = 0;

    /// <inheritdoc/>
    public string Name => "Test FateTable";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var fateTable = Service<FateTable>.Get();

        ImGui.Text("Checking fate table...");

        if (this.index == fateTable.Length - 1)
        {
            return SelfTestStepResult.Pass;
        }

        var actor = fateTable[this.index];
        this.index++;

        if (actor == null)
        {
            return SelfTestStepResult.Waiting;
        }

        Util.ShowObject(actor);

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        // ignored
    }
}
