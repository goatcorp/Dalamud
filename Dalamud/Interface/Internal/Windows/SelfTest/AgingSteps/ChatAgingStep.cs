using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Test setup for Chat.
/// </summary>
internal class ChatAgingStep : IAgingStep
{
    private int step = 0;
    private bool subscribed = false;
    private bool hasPassed = false;

    /// <inheritdoc/>
    public string Name => "Test Chat";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var chatGui = Service<ChatGui2>.Get();

        switch (this.step)
        {
            case 0:
                chatGui.Print_Internal("Testing!");
                this.step++;

                break;

            case 1:
                ImGui.Text("Type \"/e DALAMUD\" in chat...");

                if (!this.subscribed)
                {
                    this.subscribed = true;
                    chatGui.ChatMessage += this.ChatOnOnChatMessage;
                }

                if (this.hasPassed)
                {
                    chatGui.ChatMessage -= this.ChatOnOnChatMessage;
                    this.subscribed = false;
                    return SelfTestStepResult.Pass;
                }

                break;
        }

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        var chatGui = Service<ChatGui2>.Get();

        chatGui.ChatMessage -= this.ChatOnOnChatMessage;
        this.subscribed = false;
    }

    private void ChatOnOnChatMessage(
        XivChatType2 type, uint timestamp, ref SeString sender, ref SeString message, XivChatMessageSource source, string sourceName, ref bool ishandled)
    {
        if (type == XivChatType2.Echo && message.TextValue == "DALAMUD")
        {
            this.hasPassed = true;
        }
    }
}
