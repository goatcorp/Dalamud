using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps
{
    /// <summary>
    /// Test setup for Territory Change.
    /// </summary>
    internal class EnterTerritoryAgingStep : IAgingStep
    {
        private readonly ushort territory;
        private readonly string terriName;
        private bool subscribed = false;
        private bool hasPassed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnterTerritoryAgingStep"/> class.
        /// </summary>
        /// <param name="terri">The territory to check for.</param>
        /// <param name="name">Name to show.</param>
        public EnterTerritoryAgingStep(ushort terri, string name)
        {
            this.terriName = name;
            this.territory = terri;
        }

        /// <inheritdoc/>
        public string Name => $"Enter Terri: {this.terriName}";

        /// <inheritdoc/>
        public SelfTestStepResult RunStep(Dalamud dalamud)
        {
            ImGui.TextUnformatted(this.Name);

            if (!this.subscribed)
            {
                dalamud.ClientState.TerritoryChanged += this.ClientStateOnTerritoryChanged;
                this.subscribed = true;
            }

            if (this.hasPassed)
            {
                dalamud.ClientState.TerritoryChanged -= this.ClientStateOnTerritoryChanged;
                this.subscribed = false;
                return SelfTestStepResult.Pass;
            }

            return SelfTestStepResult.Waiting;
        }

        /// <inheritdoc/>
        public void CleanUp(Dalamud dalamud)
        {
            dalamud.ClientState.TerritoryChanged -= this.ClientStateOnTerritoryChanged;
            this.subscribed = false;
        }

        private void ClientStateOnTerritoryChanged(object sender, ushort e)
        {
            if (e == this.territory)
            {
                this.hasPassed = true;
            }
        }
    }
}
