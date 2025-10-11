using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.SelfTest;

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
                ImGui.Text("[Chat Log]"u8);
                ImGui.TextWrapped("Use the category menus to navigate to [Dalamud], then complete a command from the list. Did it work?"u8);
                if (ImGui.Button("Yes"u8))
                    this.step++;
                ImGui.SameLine();

                if (ImGui.Button("No"u8))
                    return SelfTestStepResult.Fail;
                break;
            case 2:
                ImGui.Text("[Chat Log]"u8);
                ImGui.Text("Type /xl into the chat log and tab-complete a dalamud command. Did it work?"u8);

                if (ImGui.Button("Yes"u8))
                    this.step++;
                ImGui.SameLine();

                if (ImGui.Button("No"u8))
                    return SelfTestStepResult.Fail;

                break;

            case 3:
                ImGui.Text("[Chat Log]"u8);
                if (!this.registered)
                {
                    cmdManager.AddHandler("/xlselftestcompletion", new CommandInfo((_, _) => this.commandRun = true));
                    this.registered = true;
                }

                ImGui.Text("Tab-complete /xlselftestcompletion in the chat log and send the command"u8);

                if (this.commandRun)
                    this.step++;

                break;

            case 4:
                ImGui.Text("[Other text inputs]"u8);
                ImGui.Text("Open the party finder recruitment criteria dialog and try to tab-complete /xldev in the text box."u8);
                ImGui.Text("Did the command appear in the text box? (It should not have)"u8);
                if (ImGui.Button("Yes"u8))
                    return SelfTestStepResult.Fail;
                ImGui.SameLine();

                if (ImGui.Button("No"u8))
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
