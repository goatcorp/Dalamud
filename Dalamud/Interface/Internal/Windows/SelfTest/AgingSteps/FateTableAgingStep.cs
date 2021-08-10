using Dalamud.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps
{
    /// <summary>
    /// Test setup for the Fate Table.
    /// </summary>
    internal class FateTableAgingStep : IAgingStep
    {
        private int index = 0;

        /// <inheritdoc/>
        public string Name => "Test FateTable";

        /// <inheritdoc/>
        public SelfTestStepResult RunStep(Dalamud dalamud)
        {
            ImGui.Text("Checking fate table...");

            if (this.index == dalamud.ClientState.Fates.Length - 1)
            {
                return SelfTestStepResult.Pass;
            }

            var actor = dalamud.ClientState.Fates[this.index];
            this.index++;

            if (actor == null)
            {
                return SelfTestStepResult.Waiting;
            }

            Util.ShowObject(actor);

            return SelfTestStepResult.Waiting;
        }

        /// <inheritdoc/>
        public void CleanUp(Dalamud dalamud)
        {
            // ignored
        }
    }
}
