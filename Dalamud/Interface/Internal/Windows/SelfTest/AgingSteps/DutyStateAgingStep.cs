using Dalamud.Game.DutyState;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Test setup for the DutyState service class.
/// </summary>
internal class DutyStateAgingStep : IAgingStep
{
    private bool subscribed = false;
    private bool hasPassed = false;

    /// <inheritdoc/>
    public string Name => "Test DutyState";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var dutyState = Service<DutyState>.Get();

        ImGui.Text("Enter a duty now...");

        if (!this.subscribed)
        {
            dutyState.DutyStarted += this.DutyStateOnDutyStarted;
            this.subscribed = true;
        }

        if (this.hasPassed)
        {
            dutyState.DutyStarted -= this.DutyStateOnDutyStarted;
            this.subscribed = false;
            return SelfTestStepResult.Pass;
        }

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        var dutyState = Service<DutyState>.Get();

        dutyState.DutyStarted -= this.DutyStateOnDutyStarted;
    }

    private void DutyStateOnDutyStarted(object? sender, ushort e)
    {
        this.hasPassed = true;
    }
}
