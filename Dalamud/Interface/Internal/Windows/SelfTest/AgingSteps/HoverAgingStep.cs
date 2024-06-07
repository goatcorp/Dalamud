using Dalamud.Game.Gui;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Test setup for the Hover events.
/// </summary>
internal class HoverAgingStep : IAgingStep
{
    private bool clearedItem = false;
    private bool clearedAction = false;

    /// <inheritdoc/>
    public string Name => "Test Hover";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var gameGui = Service<GameGui>.Get();

        if (!this.clearedItem)
        {
            ImGui.Text("Hover WHM soul crystal...");

            if (gameGui.HoveredItem == 4547)
            {
                this.clearedItem = true;
            }
        }

        if (!this.clearedAction)
        {
            ImGui.Text("Hover \"Open Linkshells\" action...");

            if (gameGui.HoveredAction != null &&
                gameGui.HoveredAction.ActionKind == HoverActionKind.MainCommand &&
                gameGui.HoveredAction.ActionID == 28)
            {
                this.clearedAction = true;
            }
        }

        if (this.clearedItem && this.clearedAction)
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
