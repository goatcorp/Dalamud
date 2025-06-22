using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for Chat.
/// </summary>
internal class CompletionSelfTestStep : ISelfTestStep
{
    private int step = 0;
    private bool registered;
    private bool commandRun;

    /// <inheritdoc/>
    public string Name => "Test Completion";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var cmdManager = Service<CommandManager>.Get();
        switch (this.step)
        {
            case 0:
                this.step++;

                break;

            case 1:
                ImGui.Text("[Chat Log]");
                ImGui.TextWrapped("Use the category menus to navigate to [Dalamud], then complete a command from the list. Did it work?");
                if (ImGui.Button("Yes"))
                    this.step++;
                ImGui.SameLine();

                if (ImGui.Button("No"))
                    return SelfTestStepResult.Fail;
                break;
            case 2:
                ImGui.Text("[Chat Log]");
                ImGui.Text("Type /xl into the chat log and tab-complete a dalamud command. Did it work?");

                if (ImGui.Button("Yes"))
                    this.step++;
                ImGui.SameLine();

                if (ImGui.Button("No"))
                    return SelfTestStepResult.Fail;

                break;

            case 3:
                ImGui.Text("[Chat Log]");
                if (!this.registered)
                {
                    cmdManager.AddHandler("/xlselftestcompletion", new CommandInfo((_, _) => this.commandRun = true));
                    this.registered = true;
                }

                ImGui.Text("Tab-complete /xlselftestcompletion in the chat log and send the command");

                if (this.commandRun)
                    this.step++;

                break;

            case 4:
                ImGui.Text("[Other text inputs]");
                ImGui.Text("Open the party finder recruitment criteria dialog and try to tab-complete /xldev in the text box.");
                ImGui.Text("Did the command appear in the text box? (It should not have)");
                if (ImGui.Button("Yes"))
                    return SelfTestStepResult.Fail;
                ImGui.SameLine();

                if (ImGui.Button("No"))
                    this.step++;
                break;
            case 5:
                return SelfTestStepResult.Pass;
        }

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        Service<CommandManager>.Get().RemoveHandler("/completionselftest");
    }
}
