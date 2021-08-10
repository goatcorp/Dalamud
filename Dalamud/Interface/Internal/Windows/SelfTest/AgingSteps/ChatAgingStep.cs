using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps
{
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
        public SelfTestStepResult RunStep(Dalamud dalamud)
        {
            switch (this.step)
            {
                case 0:
                    dalamud.Framework.Gui.Chat.Print("Testing!");
                    this.step++;

                    break;

                case 1:
                    ImGui.Text("Type \"/e DALAMUD\" in chat...");

                    if (!this.subscribed)
                    {
                        this.subscribed = true;
                        dalamud.Framework.Gui.Chat.OnChatMessage += this.ChatOnOnChatMessage;
                    }

                    if (this.hasPassed)
                    {
                        dalamud.Framework.Gui.Chat.OnChatMessage -= this.ChatOnOnChatMessage;
                        this.subscribed = false;
                        return SelfTestStepResult.Pass;
                    }

                    break;
            }

            return SelfTestStepResult.Waiting;
        }

        /// <inheritdoc/>
        public void CleanUp(Dalamud dalamud)
        {
            dalamud.Framework.Gui.Chat.OnChatMessage -= this.ChatOnOnChatMessage;
            this.subscribed = false;
        }

        private void ChatOnOnChatMessage(
            XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool ishandled)
        {
            if (type == XivChatType.Echo && message.TextValue == "DALAMUD")
            {
                this.hasPassed = true;
            }
        }
    }
}
