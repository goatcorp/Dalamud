using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for item payloads.
/// </summary>
internal class ItemPayloadSelfTestStep : ISelfTestStep
{
    private SubStep currentSubStep;

    private enum SubStep
    {
        PrintNormalItem,
        HoverNormalItem,
        PrintHqItem,
        HoverHqItem,
        PrintCollectable,
        HoverCollectable,
        PrintEventItem,
        HoverEventItem,
        PrintNormalWithText,
        HoverNormalWithText,
        Done,
    }

    /// <inheritdoc/>
    public string Name => "Test Item Payloads";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var gameGui = Service<GameGui>.Get();
        var chatGui = Service<ChatGui>.Get();

        const uint normalItemId = 24002;      // Capybara pup
        const uint hqItemId = 31861;          // Exarchic circlets of healing
        const uint collectableItemId = 36299; // Rarefied Annite
        const uint eventItemId = 2003363;     // Speude bradeos figurine

        SeString? toPrint = null;

        ImGui.Text(this.currentSubStep.ToString());

        switch (this.currentSubStep)
        {
            case SubStep.PrintNormalItem:
                toPrint = SeString.CreateItemLink(normalItemId);
                this.currentSubStep++;
                break;
            case SubStep.HoverNormalItem:
                ImGui.Text("Hover the item."u8);
                if (gameGui.HoveredItem != normalItemId)
                    return SelfTestStepResult.Waiting;
                this.currentSubStep++;
                break;
            case SubStep.PrintHqItem:
                toPrint = SeString.CreateItemLink(hqItemId, ItemKind.Hq);
                this.currentSubStep++;
                break;
            case SubStep.HoverHqItem:
                ImGui.Text("Hover the item."u8);
                if (gameGui.HoveredItem != 1_000_000 + hqItemId)
                    return SelfTestStepResult.Waiting;
                this.currentSubStep++;
                break;
            case SubStep.PrintCollectable:
                toPrint = SeString.CreateItemLink(collectableItemId, ItemKind.Collectible);
                this.currentSubStep++;
                break;
            case SubStep.HoverCollectable:
                ImGui.Text("Hover the item."u8);
                if (gameGui.HoveredItem != 500_000 + collectableItemId)
                    return SelfTestStepResult.Waiting;
                this.currentSubStep++;
                break;
            case SubStep.PrintEventItem:
                toPrint = SeString.CreateItemLink(eventItemId, ItemKind.EventItem);
                this.currentSubStep++;
                break;
            case SubStep.HoverEventItem:
                ImGui.Text("Hover the item."u8);
                if (gameGui.HoveredItem != eventItemId)
                    return SelfTestStepResult.Waiting;
                this.currentSubStep++;
                break;
            case SubStep.PrintNormalWithText:
                toPrint = SeString.CreateItemLink(normalItemId, displayNameOverride: "Gort");
                this.currentSubStep++;
                break;
            case SubStep.HoverNormalWithText:
                ImGui.Text("Hover the item."u8);
                if (gameGui.HoveredItem != normalItemId)
                    return SelfTestStepResult.Waiting;
                this.currentSubStep++;
                break;
            case SubStep.Done:
                return SelfTestStepResult.Pass;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (toPrint != null)
            chatGui.Print(toPrint);

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        this.currentSubStep = SubStep.PrintNormalItem;
    }
}
