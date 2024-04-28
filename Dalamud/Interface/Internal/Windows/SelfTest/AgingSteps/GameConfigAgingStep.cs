using Dalamud.Game.Config;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Test of GameConfig.
/// </summary>
internal class GameConfigAgingStep : IAgingStep
{
    private bool started;
    private bool isStartedLegacy;
    private bool isSwitchedLegacy;
    private bool isSwitchedStandard;

    /// <inheritdoc/>
    public string Name => "Test GameConfig";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var gameConfig = Service<GameConfig>.Get();

        if (!gameConfig.UiControl.TryGetBool("MoveMode", out var isLegacy))
        {
            return SelfTestStepResult.Fail;
        }

        if (!this.started)
        {
            this.started = true;
            this.isStartedLegacy = isLegacy;
            return SelfTestStepResult.Waiting;
        }

        if (this.isStartedLegacy)
        {
            if (!this.isSwitchedStandard)
            {
                if (!isLegacy)
                {
                    this.isSwitchedStandard = true;
                }
                else
                {
                    ImGui.Text("Switch Movement Type to Standard");
                }

                return SelfTestStepResult.Waiting;
            }

            if (!this.isSwitchedLegacy)
            {
                if (isLegacy)
                {
                    this.isSwitchedLegacy = true;
                }
                else
                {
                    ImGui.Text("Switch Movement Type to Legacy");
                }

                return SelfTestStepResult.Waiting;
            }
        }
        else
        {
             if (!this.isSwitchedLegacy)
             {
                 if (isLegacy)
                 {
                     this.isSwitchedLegacy = true;
                 }
                 else
                 {
                     ImGui.Text("Switch Movement Type to Legacy");
                 }

                 return SelfTestStepResult.Waiting;
             }

             if (!this.isSwitchedStandard)
             {
                 if (!isLegacy)
                 {
                     this.isSwitchedStandard = true;
                 }
                 else
                 {
                     ImGui.Text("Switch Movement Type to Standard");
                 }

                 return SelfTestStepResult.Waiting;
             }
        }

        return SelfTestStepResult.Pass;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        this.isSwitchedLegacy = false;
        this.isSwitchedStandard = false;
        this.started = false;
    }
}
