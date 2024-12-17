using Dalamud.Game.ClientState.Fates;
using Dalamud.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Test setup for the Fate Table.
/// </summary>
internal class FateTableAgingStep : IAgingStep
{
    private byte index = 0;

    /// <inheritdoc/>
    public string Name => "Test FateTable";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var fateTable = Service<FateTable>.Get();

        ImGui.TextUnformatted("Checking fate table...");

        if (fateTable.Length == 0)
        {
            ImGui.TextUnformatted("Go to a zone that has FATEs currently up.");
            return SelfTestStepResult.Waiting;
        }

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
