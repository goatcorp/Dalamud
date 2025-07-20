using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for targets.
/// </summary>
internal class TargetSelfTestStep : ISelfTestStep
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
                ImGui.TextUnformatted("Target a player..."u8);

                var cTarget = targetManager.Target;
                if (cTarget is PlayerCharacter)
                {
                    this.step++;
                }

                break;

            case 2:
                ImGui.TextUnformatted("Focus-Target a Battle NPC..."u8);

                var fTarget = targetManager.FocusTarget;
                if (fTarget is BattleNpc)
                {
                    this.step++;
                }

                break;

            case 3:
                ImGui.TextUnformatted("Soft-Target an EventObj..."u8);

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
