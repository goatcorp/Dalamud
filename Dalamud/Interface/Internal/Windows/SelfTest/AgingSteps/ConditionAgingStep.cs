using Dalamud.Game.ClientState.Conditions;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps
{
    /// <summary>
    /// Test setup for Condition.
    /// </summary>
    internal class ConditionAgingStep : IAgingStep
    {
        /// <inheritdoc/>
        public string Name => "Test Condition";

        /// <inheritdoc/>
        public SelfTestStepResult RunStep(Dalamud dalamud)
        {
            if (!dalamud.ClientState.Condition.Any())
            {
                Log.Error("No condition flags present.");
                return SelfTestStepResult.Fail;
            }

            ImGui.Text("Please jump...");

            return dalamud.ClientState.Condition[ConditionFlag.Jumping] ? SelfTestStepResult.Pass : SelfTestStepResult.Waiting;
        }

        /// <inheritdoc/>
        public void CleanUp(Dalamud dalamud)
        {
            // ignored
        }
    }
}
