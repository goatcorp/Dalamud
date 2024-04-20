using Dalamud.Game.Gui.PartyFinder;
using Dalamud.Game.Gui.PartyFinder.Types;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

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
    public SelfTestStepResult RunStep()
    {
        var partyFinderGui = Service<PartyFinderGui>.Get();

        if (!this.subscribed)
        {
            partyFinderGui.ReceiveListing += this.PartyFinderOnReceiveListing;
            this.subscribed = true;
        }

        if (this.hasPassed)
        {
            partyFinderGui.ReceiveListing -= this.PartyFinderOnReceiveListing;
            this.subscribed = false;
            return SelfTestStepResult.Pass;
        }

        ImGui.Text("Open Party Finder");

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        var partyFinderGui = Service<PartyFinderGui>.Get();

        if (this.subscribed)
        {
            partyFinderGui.ReceiveListing -= this.PartyFinderOnReceiveListing;
            this.subscribed = false;
        }
    }

    private void PartyFinderOnReceiveListing(PartyFinderListing listing, PartyFinderListingEventArgs args)
    {
        this.hasPassed = true;
    }
}
