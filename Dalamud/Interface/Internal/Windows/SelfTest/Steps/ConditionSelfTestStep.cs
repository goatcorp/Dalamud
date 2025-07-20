using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Serilog;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for Condition.
/// </summary>
internal class ConditionSelfTestStep : ISelfTestStep
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

        ImGui.TextUnformatted("Please jump..."u8);

        return condition[ConditionFlag.Jumping] ? SelfTestStepResult.Pass : SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        // ignored
    }
}
