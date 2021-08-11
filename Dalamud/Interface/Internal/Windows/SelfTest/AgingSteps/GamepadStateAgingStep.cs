using Dalamud.Game.ClientState.GamePad;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps
{
    /// <summary>
    /// Test setup for the Gamepad State.
    /// </summary>
    internal class GamepadStateAgingStep : IAgingStep
    {
        /// <inheritdoc/>
        public string Name => "Test GamePadState";

        /// <inheritdoc/>
        public SelfTestStepResult RunStep(Dalamud dalamud)
        {
            ImGui.Text("Hold down North, East, L1");

            if (dalamud.ClientState.GamepadState.Pressed(GamepadButtons.North) == 1
                && dalamud.ClientState.GamepadState.Pressed(GamepadButtons.East) == 1
                && dalamud.ClientState.GamepadState.Pressed(GamepadButtons.L1) == 1)
            {
                return SelfTestStepResult.Pass;
            }

            return SelfTestStepResult.Waiting;
        }

        /// <inheritdoc/>
        public void CleanUp(Dalamud dalamud)
        {
            // ignored
        }
    }
}
