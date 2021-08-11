using System;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps
{
    /// <summary>
    /// Test setup for the login events.
    /// </summary>
    internal class LoginEventAgingStep : IAgingStep
    {
        private bool subscribed = false;
        private bool hasPassed = false;

        /// <inheritdoc/>
        public string Name => "Test Log-In";

        /// <inheritdoc/>
        public SelfTestStepResult RunStep(Dalamud dalamud)
        {
            ImGui.Text("Log in now...");

            if (!this.subscribed)
            {
                dalamud.ClientState.Login += this.ClientStateOnOnLogin;
                this.subscribed = true;
            }

            if (this.hasPassed)
            {
                dalamud.ClientState.Login -= this.ClientStateOnOnLogin;
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
                dalamud.ClientState.Login -= this.ClientStateOnOnLogin;
                this.subscribed = false;
            }
        }

        private void ClientStateOnOnLogin(object sender, EventArgs e)
        {
            this.hasPassed = true;
        }
    }
}
