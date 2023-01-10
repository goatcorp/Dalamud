using Dalamud.Game.ClientState.Aetherytes;
using Dalamud.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Test setup for the Aetheryte List.
/// </summary>
internal class AetheryteListAgingStep : IAgingStep
{
    private int index = 0;

    /// <inheritdoc/>
    public string Name => "Test AetheryteList";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var list = Service<AetheryteList>.Get();

        ImGui.Text("Checking aetheryte list...");

        if (this.index == list.Length - 1)
        {
            return SelfTestStepResult.Pass;
        }

        var aetheryte = list[this.index];
        this.index++;

        if (aetheryte == null)
        {
            return SelfTestStepResult.Waiting;
        }

        if (aetheryte.AetheryteId == 0)
        {
            return SelfTestStepResult.Fail;
        }

        Util.ShowObject(aetheryte);

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        // ignored
    }
}
