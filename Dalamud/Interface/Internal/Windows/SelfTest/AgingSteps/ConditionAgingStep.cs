using Dalamud.Game.ClientState.Conditions;

using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Test setup for Condition.
/// </summary>
internal class ConditionAgingStep : IAgingStep
{
    /// <inheritdoc/>
    public string Name => "Test Condition";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var condition = Service<Condition>.Get();

        if (!condition.Any())
        {
            Log.Error("No condition flags present.");
            return SelfTestStepResult.Fail;
        }

        ImGui.Text("Please jump...");

        return condition[ConditionFlag.Jumping] ? SelfTestStepResult.Pass : SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        // ignored
    }
}
