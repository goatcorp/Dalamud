using Dalamud.Bindings.ImGui;
using Dalamud.Game.Chat;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.SelfTest;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for Chat.
/// </summary>
internal class ChatSelfTestStep : ISelfTestStep
{
    private int step = 0;
    private bool subscribedChatMessage = false;
    private bool subscribedLogMessage = false;
    private bool hasSeenEchoMessage = false;
    private bool hasSeenActionMessage = false;
    private string actionName = "";
    private string actionUser = "";

    /// <inheritdoc/>
    public string Name => "Test Chat";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var chatGui = Service<ChatGui>.Get();

        switch (this.step)
        {
            case 0:
                chatGui.Print("Testing!");
                this.step++;

                break;

            case 1:
                ImGui.Text("Type \"/e DALAMUD\" in chat...");

                if (!this.subscribedChatMessage)
                {
                    this.subscribedChatMessage = true;
                    chatGui.ChatMessage += this.ChatOnOnChatMessage;
                }

                if (this.hasSeenEchoMessage)
                {
                    chatGui.ChatMessage -= this.ChatOnOnChatMessage;
                    this.subscribedChatMessage = false;
                    this.step++;
                }

                break;

            case 2:
                ImGui.Text("Use any action (for example Sprint) or be near a player using an action.");

                if (!this.subscribedLogMessage)
                {
                    this.subscribedLogMessage = true;
                    chatGui.LogMessage += this.ChatOnLogMessage;
                }

                if (this.hasSeenActionMessage)
                {
                    ImGui.Text($"{this.actionUser} used {this.actionName}.");
                    ImGui.Text("Is this correct?");

                    if (ImGui.Button("Yes"))
                    {
                        chatGui.LogMessage -= this.ChatOnLogMessage;
                        this.subscribedLogMessage = false;
                        this.step++;
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("No"))
                    {
                        chatGui.LogMessage -= this.ChatOnLogMessage;
                        this.subscribedLogMessage = false;
                        return SelfTestStepResult.Fail;
                    }
                }

                break;

            default:
                return SelfTestStepResult.Pass;
        }

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        var chatGui = Service<ChatGui>.Get();

        chatGui.ChatMessage -= this.ChatOnOnChatMessage;
        chatGui.LogMessage -= this.ChatOnLogMessage;
        this.subscribedChatMessage = false;
        this.subscribedLogMessage = false;
    }

    private void ChatOnOnChatMessage(
        XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool ishandled)
    {
        if (type == XivChatType.Echo && message.TextValue == "DALAMUD")
        {
            this.hasSeenEchoMessage = true;
        }
    }

    private void ChatOnLogMessage(ILogMessage message)
    {
        if (message.LogMessageId == 533 && message.TryGetStringParameter(0, out var value))
        {
            this.hasSeenActionMessage = true;
            this.actionUser = message.SourceEntity?.Name.ExtractText() ?? "<incorrect>";
            this.actionName = value.ExtractText();
        }
    }
}
