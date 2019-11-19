using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Game.Chat;
using Serilog;

namespace Dalamud.Game {
    public class ChatHandlers {
        private static readonly Dictionary<string, string> UnicodeToDiscordEmojiDict = new Dictionary<string, string> {
            {"", "<:ffxive071:585847382210642069>"},
            {"", "<:ffxive083:585848592699490329>"}
        };

        private readonly Dalamud dalamud;

        private readonly Dictionary<XivChatType, Color> HandledChatTypeColors = new Dictionary<XivChatType, Color> {
            {XivChatType.CrossParty, Color.DodgerBlue},
            {XivChatType.Party, Color.DodgerBlue},
            {XivChatType.FreeCompany, Color.DeepSkyBlue},
            {XivChatType.CrossLinkShell1, Color.ForestGreen},
            {XivChatType.CrossLinkShell2, Color.ForestGreen},
            {XivChatType.CrossLinkShell3, Color.ForestGreen},
            {XivChatType.CrossLinkShell4, Color.ForestGreen},
            {XivChatType.CrossLinkShell5, Color.ForestGreen},
            {XivChatType.CrossLinkShell6, Color.ForestGreen},
            {XivChatType.CrossLinkShell7, Color.ForestGreen},
            {XivChatType.CrossLinkShell8, Color.ForestGreen},
            {XivChatType.Ls1, Color.ForestGreen},
            {XivChatType.Ls2, Color.ForestGreen},
            {XivChatType.Ls3, Color.ForestGreen},
            {XivChatType.Ls4, Color.ForestGreen},
            {XivChatType.Ls5, Color.ForestGreen},
            {XivChatType.Ls6, Color.ForestGreen},
            {XivChatType.Ls7, Color.ForestGreen},
            {XivChatType.Ls8, Color.ForestGreen},
            {XivChatType.TellIncoming, Color.HotPink},
            {XivChatType.PvPTeam, Color.SandyBrown},
            {XivChatType.Urgent, Color.DarkViolet},
            {XivChatType.NoviceNetwork, Color.SaddleBrown},
            {XivChatType.Echo, Color.Gray}
        };

        private readonly Regex rmtRegex =
            new Regex(
                @"4KGOLD|We have sufficient stock|VPK\.OM|Gil for free|www\.so9\.com|Fast & Convenient|Cheap & Safety Guarantee|【Code|A O A U E|igfans",
                RegexOptions.Compiled);

        private readonly Regex urlRegex =
            new Regex(@"(http|ftp|https)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?",
                      RegexOptions.Compiled);

        private bool hasSeenLoadingMsg;

        public ChatHandlers(Dalamud dalamud) {
            this.dalamud = dalamud;

            dalamud.Framework.Gui.Chat.OnChatMessage += ChatOnOnChatMessage;
        }


        public string LastLink { get; private set; }

        private void ChatOnOnChatMessage(XivChatType type, uint senderId, string sender, ref string message,
                                         ref bool isHandled) {

            if (type == XivChatType.Notice && !this.hasSeenLoadingMsg) {
                this.dalamud.Framework.Gui.Chat.Print($"XIVLauncher in-game addon v{Assembly.GetAssembly(typeof(ChatHandlers)).GetName().Version} loaded.");
                this.hasSeenLoadingMsg = true;
            }

#if !DEBUG
            if (!this.hasSeenLoadingMsg)
                return;
#endif

            var matched = this.rmtRegex.IsMatch(message);
            if (matched) {
                // This seems to be a RMT ad - let's not show it
                Log.Debug("Handled RMT ad");
                isHandled = true;
                return;
            }

            var originalMessage = string.Copy(message);

            if (this.dalamud.Configuration.BadWords != null &&
                this.dalamud.Configuration.BadWords.Any(x => originalMessage.Contains(x))) {
                // This seems to be in the user block list - let's not show it
                Log.Debug("Blocklist triggered");
                isHandled = true;
                return;
            }

            Task.Run(() => this.dalamud.BotManager.ProcessChatMessage(type, originalMessage, sender).GetAwaiter()
                               .GetResult());


            if ((this.HandledChatTypeColors.ContainsKey(type) || type == XivChatType.Say || type == XivChatType.Shout ||
                type == XivChatType.Alliance || type == XivChatType.TellOutgoing || type == XivChatType.Yell) && !message.Contains((char)0x02)) {
                var italicsStart = message.IndexOf("*");
                var italicsEnd = message.IndexOf("*", italicsStart + 1);

                while (italicsEnd != -1) {
                    var it = MakeItalics(
                        message.Substring(italicsStart, italicsEnd - italicsStart + 1).Replace("*", ""));
                    message = message.Remove(italicsStart, italicsEnd - italicsStart + 1);
                    message = message.Insert(italicsStart, it);
                    italicsStart = message.IndexOf("*");
                    italicsEnd = message.IndexOf("*", italicsStart + 1);
                }
            }


            var linkMatch = this.urlRegex.Match(message);
            if (linkMatch.Value.Length > 0)
                LastLink = linkMatch.Value;
        }

        private static string MakeItalics(string text) {
            return Encoding.UTF8.GetString(new byte[] {0x02, 0x1A, 0x02, 0x02, 0x03}) + text +
                   Encoding.UTF8.GetString(new byte[] {0x02, 0x1A, 0x02, 0x01, 0x03});
        }
    }
}
