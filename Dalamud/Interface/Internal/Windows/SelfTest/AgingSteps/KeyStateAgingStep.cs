using Dalamud.Game.ClientState.Keys;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps
{
    /// <summary>
    /// Test setup for the Key State.
    /// </summary>
    internal class KeyStateAgingStep : IAgingStep
    {
        /// <inheritdoc/>
        public string Name => "Test KeyState";

        /// <inheritdoc/>
        public SelfTestStepResult RunStep(Dalamud dalamud)
        {
            ImGui.Text("Hold down D,A,L,M,U");

            if (dalamud.ClientState.KeyState[VirtualKey.D]
                && dalamud.ClientState.KeyState[VirtualKey.A]
                && dalamud.ClientState.KeyState[VirtualKey.L]
                && dalamud.ClientState.KeyState[VirtualKey.M]
                && dalamud.ClientState.KeyState[VirtualKey.U])
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
