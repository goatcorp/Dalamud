using System;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps
{
    /// <summary>
    /// Test setup for the login events.
    /// </summary>
    internal class LogoutEventAgingStep : IAgingStep
    {
        private bool subscribed = false;
        private bool hasPassed = false;

        /// <inheritdoc/>
        public string Name => "Test Log-In";

        /// <inheritdoc/>
        public SelfTestStepResult RunStep(Dalamud dalamud)
        {
            ImGui.Text("Log out now...");

            if (!this.subscribed)
            {
                dalamud.ClientState.Logout += this.ClientStateOnOnLogout;
                this.subscribed = true;
            }

            if (this.hasPassed)
            {
                dalamud.ClientState.Logout -= this.ClientStateOnOnLogout;
                this.subscribed = false;
                return SelfTestStepResult.Pass;
            }

            return SelfTestStepResult.Waiting;
        }

        /// <inheritdoc/>
        public void CleanUp(Dalamud dalamud)
        {
            if (this.subscribed)
            {
                dalamud.ClientState.Logout -= this.ClientStateOnOnLogout;
                this.subscribed = false;
            }
        }

        private void ClientStateOnOnLogout(object sender, EventArgs e)
        {
            this.hasPassed = true;
        }
    }
}
