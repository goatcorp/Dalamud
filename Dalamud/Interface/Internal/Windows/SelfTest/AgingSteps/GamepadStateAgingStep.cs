using Dalamud.Game.ClientState.GamePad;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Test setup for the Gamepad State.
/// </summary>
internal class GamepadStateAgingStep : IAgingStep
{
    /// <inheritdoc/>
    public string Name => "Test GamePadState";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var gamepadState = Service<GamepadState>.Get();

        ImGui.Text("Hold down North, East, L1");

        if (gamepadState.Pressed(GamepadButtons.North) == 1
            && gamepadState.Pressed(GamepadButtons.East) == 1
            && gamepadState.Pressed(GamepadButtons.L1) == 1)
        {
            return SelfTestStepResult.Pass;
        }

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        // ignored
    }
}
