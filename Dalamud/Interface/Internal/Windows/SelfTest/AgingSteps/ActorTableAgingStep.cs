using Dalamud.Game.ClientState.Objects;
using Dalamud.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Test setup for the Actor Table.
/// </summary>
internal class ActorTableAgingStep : IAgingStep
{
    private int index = 0;

    /// <inheritdoc/>
    public string Name => "Test ActorTable";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var objectTable = Service<ObjectTable>.Get();

        ImGui.Text("Checking actor table...");

        if (this.index == objectTable.Length - 1)
        {
            return SelfTestStepResult.Pass;
        }

        var actor = objectTable[this.index];
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
