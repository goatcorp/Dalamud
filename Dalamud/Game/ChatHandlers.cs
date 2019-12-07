using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Game.Chat;
using Dalamud.Game.Internal.Libc;
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
                @"4KGOLD|We have sufficient stock|VPK\.OM|Gil for free|www\.so9\.com|Fast & Convenient|Cheap & Safety Guarantee|【Code|A O A U E|igfans|4KGOLD\.COM|Cheapest Gil with|pvp and bank on google|Selling Cheap GIL|ff14mogstation\.com|Cheap Gil 1000k|gilsforyou|server 1000K =|gils_selling|E A S Y\.C O M|bonus code|mins delivery guarantee|Sell cheap NA",
                RegexOptions.Compiled);

        private readonly Regex urlRegex =
            new Regex(@"(http|ftp|https)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?",
                      RegexOptions.Compiled);

        private readonly Dictionary<ClientLanguage, Regex[]> retainerSaleRegexes = new Dictionary<ClientLanguage, Regex[]>() {
            {
                ClientLanguage.Japanese, new Regex[] {
                    new Regex(@"^(?:.+)マーケットに(?<origValue>[\d,.]+)ギルで出品した(?<item>.*)×(?<count>[\d,.]+)が売れ、(?<value>[\d,.]+)ギルを入手しました。$", RegexOptions.Compiled),
                    new Regex(@"^(?:.+)マーケットに(?<origValue>[\d,.]+)ギルで出品した(?<item>.*)が売れ、(?<value>[\d,.]+)ギルを入手しました。$", RegexOptions.Compiled)
                }
            },
            {
                ClientLanguage.English, new Regex[]
                {
                    new Regex(@"^(?<item>.+) you put up for sale in the (?:.+) markets (?:have|has) sold for (?<value>[\d,.]+) gil \(after fees\)\.$", RegexOptions.Compiled)
                }
            },
            {
                ClientLanguage.German, new Regex[]
                {
                    new Regex(@"^Dein Gehilfe hat (?<item>.+) auf dem Markt von (?:.+) für (?<value>[\d,.]+) Gil verkauft\.$", RegexOptions.Compiled),
                    new Regex(@"^Dein Gehilfe hat (?<item>.+) auf dem Markt von (?:.+) verkauft und (?<value>[\d,.]+) Gil erhalten\.$", RegexOptions.Compiled)
                }
            },
            {
                ClientLanguage.French, new Regex[]
                {
                    new Regex(@"^Un servant a vendu (?<item>.+) pour (?<value>[\d,.]+) gil à (?:.+)\.$", RegexOptions.Compiled)
                }
            }
        };

        private bool hasSeenLoadingMsg;

        public ChatHandlers(Dalamud dalamud) {
            this.dalamud = dalamud;

            dalamud.Framework.Gui.Chat.OnChatMessage += ChatOnOnChatMessage;
        }


        public string LastLink { get; private set; }

        private void ChatOnOnChatMessage(XivChatType type, uint senderId, ref StdString sender, 
            ref StdString message, ref bool isHandled) {

            if (type == XivChatType.Notice && !this.hasSeenLoadingMsg) {
                var assemblyVersion = Assembly.GetAssembly(typeof(ChatHandlers)).GetName().Version.ToString();

                this.dalamud.Framework.Gui.Chat.Print($"XIVLauncher in-game addon v{assemblyVersion} loaded.");
                this.hasSeenLoadingMsg = true;

                if (string.IsNullOrEmpty(this.dalamud.Configuration.LastVersion) || !assemblyVersion.StartsWith(this.dalamud.Configuration.LastVersion)) {
                    this.dalamud.Framework.Gui.Chat.PrintChat(new XivChatEntry {
                        MessageBytes = Encoding.UTF8.GetBytes("The In-Game addon has been updated or was reinstalled successfully! Please check the discord for a full changelog."),
                        Type = XivChatType.Urgent
                    });

                    this.dalamud.Configuration.LastVersion = assemblyVersion;
                    this.dalamud.Configuration.Save(this.dalamud.StartInfo.ConfigurationPath);
                }
            }

#if !DEBUG
            if (!this.hasSeenLoadingMsg)
                return;
#endif

            var matched = this.rmtRegex.IsMatch(message.Value);
            if (matched) {
                // This seems to be a RMT ad - let's not show it
                Log.Debug("Handled RMT ad");
                isHandled = true;
                return;
            }

            var messageVal = message.Value;
            var senderVal = sender.Value;

            if (this.dalamud.Configuration.BadWords != null &&
                this.dalamud.Configuration.BadWords.Any(x => messageVal.Contains(x))) {
                // This seems to be in the user block list - let's not show it
                Log.Debug("Blocklist triggered");
                isHandled = true;
                return;
            }

            if (type == XivChatType.RetainerSale)
            {
                foreach (var regex in retainerSaleRegexes[dalamud.StartInfo.Language])
                {
                    var matchInfo = regex.Match(message.Value);

                    // we no longer really need to do/validate the item matching since we read the id from the byte array
                    // but we'd be checking the main match anyway
                    var itemInfo = matchInfo.Groups["item"];
                    if (!itemInfo.Success)
                        continue;
                    //var itemName = SeString.Parse(itemInfo.Value).Output;
                    var (itemId, isHQ) = (ValueTuple<int, bool>)(SeString.Parse(message.RawData).Payloads[0].Param1);

                    Log.Debug($"Probable retainer sale: {message}, decoded item {itemId}, HQ {isHQ}");

                    int itemValue = 0;
                    var valueInfo = matchInfo.Groups["value"];
                    // not sure if using a culture here would work correctly, so just strip symbols instead
                    if (!valueInfo.Success || !int.TryParse(valueInfo.Value.Replace(",", "").Replace(".", ""), out itemValue))
                        continue;

                    Task.Run(() => this.dalamud.BotManager.ProcessRetainerSale(itemId, itemValue, isHQ));
                    break;
                }
            }


            Task.Run(() => this.dalamud.BotManager.ProcessChatMessage(type, messageVal, senderVal).GetAwaiter()
                            .GetResult());


            if ((this.HandledChatTypeColors.ContainsKey(type) || type == XivChatType.Say || type == XivChatType.Shout ||
                type == XivChatType.Alliance || type == XivChatType.TellOutgoing || type == XivChatType.Yell) && !message.Value.Contains((char)0x02)) {
                var italicsStart = message.Value.IndexOf("*");
                var italicsEnd = message.Value.IndexOf("*", italicsStart + 1);

                var messageString = message.Value;

                while (italicsEnd != -1) {
                    var it = MakeItalics(
                        messageString.Substring(italicsStart, italicsEnd - italicsStart + 1).Replace("*", ""));
                    messageString = messageString.Remove(italicsStart, italicsEnd - italicsStart + 1);
                    messageString = messageString.Insert(italicsStart, it);
                    italicsStart = messageString.IndexOf("*");
                    italicsEnd = messageString.IndexOf("*", italicsStart + 1);
                }

                message.RawData = Encoding.UTF8.GetBytes(messageString);
            }


            var linkMatch = this.urlRegex.Match(message.Value);
            if (linkMatch.Value.Length > 0)
                LastLink = linkMatch.Value;
        }

        private static string MakeItalics(string text) {
            return Encoding.UTF8.GetString(new byte[] {0x02, 0x1A, 0x02, 0x02, 0x03}) + text +
                   Encoding.UTF8.GetString(new byte[] {0x02, 0x1A, 0x02, 0x01, 0x03});
        }
    }
}
