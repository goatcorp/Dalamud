using Dalamud.Game.Gui.PartyFinder.Types;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps
{
    /// <summary>
    /// Test setup for Party Finder events.
    /// </summary>
    internal class PartyFinderAgingStep : IAgingStep
    {
        private bool subscribed = false;
        private bool hasPassed = false;

        /// <inheritdoc/>
        public string Name => "Test Party Finder";

        /// <inheritdoc/>
        public SelfTestStepResult RunStep(Dalamud dalamud)
        {
            if (!this.subscribed)
            {
                dalamud.Framework.Gui.PartyFinder.ReceiveListing += this.PartyFinderOnReceiveListing;
                this.subscribed = true;
            }

            if (this.hasPassed)
            {
                dalamud.Framework.Gui.PartyFinder.ReceiveListing -= this.PartyFinderOnReceiveListing;
                this.subscribed = false;
                return SelfTestStepResult.Pass;
            }

            ImGui.Text("Open Party Finder");

            return SelfTestStepResult.Waiting;
        }

        /// <inheritdoc/>
        public void CleanUp(Dalamud dalamud)
        {
            if (this.subscribed)
            {
                dalamud.Framework.Gui.PartyFinder.ReceiveListing -= this.PartyFinderOnReceiveListing;
                this.subscribed = false;
            }
        }

        private void PartyFinderOnReceiveListing(PartyFinderListing listing, PartyFinderListingEventArgs args)
        {
            this.hasPassed = true;
        }
    }
}
