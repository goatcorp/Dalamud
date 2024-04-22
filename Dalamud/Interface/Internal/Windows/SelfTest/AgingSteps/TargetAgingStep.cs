using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Test setup for targets.
/// </summary>
internal class TargetAgingStep : IAgingStep
{
    private int step = 0;

    /// <inheritdoc/>
    public string Name => "Test Target";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var targetManager = Service<TargetManager>.Get();

        switch (this.step)
        {
            case 0:
                targetManager.Target = null;
                targetManager.FocusTarget = null;

                this.step++;

                break;

            case 1:
                ImGui.Text("Target a player...");

                var cTarget = targetManager.Target;
                if (cTarget is PlayerCharacter)
                {
                    this.step++;
                }

                break;

            case 2:
                ImGui.Text("Focus-Target a Battle NPC...");

                var fTarget = targetManager.FocusTarget;
                if (fTarget is BattleNpc)
                {
                    this.step++;
                }

                break;

            case 3:
                ImGui.Text("Soft-Target an EventObj...");

                var sTarget = targetManager.SoftTarget;
                if (sTarget is EventObj)
                {
                    return SelfTestStepResult.Pass;
                }

                break;
        }

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        // ignored
    }
}
