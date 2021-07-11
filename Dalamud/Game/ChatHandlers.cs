using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Internal.Windows;
using Serilog;

namespace Dalamud.Game
{
    /// <summary>
    /// Chat events and public helper functions.
    /// </summary>
    public class ChatHandlers
    {
        // private static readonly Dictionary<string, string> UnicodeToDiscordEmojiDict = new()
        // {
        //     { "", "<:ffxive071:585847382210642069>" },
        //     { "", "<:ffxive083:585848592699490329>" },
        // };

        // private readonly Dictionary<XivChatType, Color> handledChatTypeColors = new()
        // {
        //     { XivChatType.CrossParty, Color.DodgerBlue },
        //     { XivChatType.Party, Color.DodgerBlue },
        //     { XivChatType.FreeCompany, Color.DeepSkyBlue },
        //     { XivChatType.CrossLinkShell1, Color.ForestGreen },
        //     { XivChatType.CrossLinkShell2, Color.ForestGreen },
        //     { XivChatType.CrossLinkShell3, Color.ForestGreen },
        //     { XivChatType.CrossLinkShell4, Color.ForestGreen },
        //     { XivChatType.CrossLinkShell5, Color.ForestGreen },
        //     { XivChatType.CrossLinkShell6, Color.ForestGreen },
        //     { XivChatType.CrossLinkShell7, Color.ForestGreen },
        //     { XivChatType.CrossLinkShell8, Color.ForestGreen },
        //     { XivChatType.Ls1, Color.ForestGreen },
        //     { XivChatType.Ls2, Color.ForestGreen },
        //     { XivChatType.Ls3, Color.ForestGreen },
        //     { XivChatType.Ls4, Color.ForestGreen },
        //     { XivChatType.Ls5, Color.ForestGreen },
        //     { XivChatType.Ls6, Color.ForestGreen },
        //     { XivChatType.Ls7, Color.ForestGreen },
        //     { XivChatType.Ls8, Color.ForestGreen },
        //     { XivChatType.TellIncoming, Color.HotPink },
        //     { XivChatType.PvPTeam, Color.SandyBrown },
        //     { XivChatType.Urgent, Color.DarkViolet },
        //     { XivChatType.NoviceNetwork, Color.SaddleBrown },
        //     { XivChatType.Echo, Color.Gray },
        // };

        private readonly Regex rmtRegex = new(
                @"4KGOLD|We have sufficient stock|VPK\.OM|Gil for free|www\.so9\.com|Fast & Convenient|Cheap & Safety Guarantee|【Code|A O A U E|igfans|4KGOLD\.COM|Cheapest Gil with|pvp and bank on google|Selling Cheap GIL|ff14mogstation\.com|Cheap Gil 1000k|gilsforyou|server 1000K =|gils_selling|E A S Y\.C O M|bonus code|mins delivery guarantee|Sell cheap|Salegm\.com|cheap Mog|Off Code:|FF14Mog.com|使用する5％オ|Off Code( *):|offers Fantasia",
                RegexOptions.Compiled);

        private readonly Dictionary<ClientLanguage, Regex[]> retainerSaleRegexes = new()
        {
            {
                ClientLanguage.Japanese,
                new Regex[]
                {
                    new Regex(@"^(?:.+)マーケットに(?<origValue>[\d,.]+)ギルで出品した(?<item>.*)×(?<count>[\d,.]+)が売れ、(?<value>[\d,.]+)ギルを入手しました。$", RegexOptions.Compiled),
                    new Regex(@"^(?:.+)マーケットに(?<origValue>[\d,.]+)ギルで出品した(?<item>.*)が売れ、(?<value>[\d,.]+)ギルを入手しました。$", RegexOptions.Compiled),
                }
            },
            {
                ClientLanguage.English,
                new Regex[]
                {
                    new Regex(@"^(?<item>.+) you put up for sale in the (?:.+) markets (?:have|has) sold for (?<value>[\d,.]+) gil \(after fees\)\.$", RegexOptions.Compiled),
                }
            },
            {
                ClientLanguage.German,
                new Regex[]
                {
                    new Regex(@"^Dein Gehilfe hat (?<item>.+) auf dem Markt von (?:.+) für (?<value>[\d,.]+) Gil verkauft\.$", RegexOptions.Compiled),
                    new Regex(@"^Dein Gehilfe hat (?<item>.+) auf dem Markt von (?:.+) verkauft und (?<value>[\d,.]+) Gil erhalten\.$", RegexOptions.Compiled),
                }
            },
            {
                ClientLanguage.French,
                new Regex[]
                {
                    new Regex(@"^Un servant a vendu (?<item>.+) pour (?<value>[\d,.]+) gil à (?:.+)\.$", RegexOptions.Compiled),
                }
            },
        };

        private readonly Regex urlRegex = new(@"(http|ftp|https)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?", RegexOptions.Compiled);

        private readonly Dalamud dalamud;
        private readonly DalamudLinkPayload openInstallerWindowLink;
        private bool hasSeenLoadingMsg;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatHandlers"/> class.
        /// </summary>
        /// <param name="dalamud">Dalamud instance.</param>
        internal ChatHandlers(Dalamud dalamud)
        {
            this.dalamud = dalamud;

            dalamud.Framework.Gui.Chat.OnCheckMessageHandled += this.OnCheckMessageHandled;
            dalamud.Framework.Gui.Chat.OnChatMessage += this.OnChatMessage;

            this.openInstallerWindowLink = this.dalamud.Framework.Gui.Chat.AddChatLinkHandler("Dalamud", 1001, (i, m) =>
            {
                this.dalamud.DalamudUi.OpenPluginInstaller();
            });
        }

        /// <summary>
        /// Gets the last URL seen in chat.
        /// </summary>
        public string LastLink { get; private set; }

        // /// <summary>
        // /// Convert a string to SeString and wrap in italics payloads.
        // /// </summary>
        // /// <param name="text">Text to convert.</param>
        // /// <returns>SeString payload of italicized text.</returns>
        // private static SeString MakeItalics(string text)
        // {
        //     // TODO: when the code OnCharMessage is switched to SeString, this can be a straight insertion of the
        //     // italics payloads only, and be a lot cleaner
        //     var italicString = new SeString(new List<Payload>(new Payload[]
        //     {
        //         EmphasisItalicPayload.ItalicsOn,
        //         new TextPayload(text),
        //         EmphasisItalicPayload.ItalicsOff,
        //     }));
        //
        //     return italicString;
        // }

        private void OnCheckMessageHandled(XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            var textVal = message.TextValue;

            var matched = this.rmtRegex.IsMatch(textVal);
            if (matched)
            {
                // This seems to be a RMT ad - let's not show it
                Log.Debug("Handled RMT ad: " + message.TextValue);
                isHandled = true;
                return;
            }

            if (this.dalamud.Configuration.BadWords != null &&
                this.dalamud.Configuration.BadWords.Any(x => !string.IsNullOrEmpty(x) && textVal.Contains(x)))
            {
                // This seems to be in the user block list - let's not show it
                Log.Debug("Blocklist triggered");
                isHandled = true;
                return;
            }
        }

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (type == XivChatType.Notice && !this.hasSeenLoadingMsg)
                this.PrintWelcomeMessage();

            // For injections while logged in
            if (this.dalamud.ClientState.LocalPlayer != null && this.dalamud.ClientState.TerritoryType == 0 && !this.hasSeenLoadingMsg)
                this.PrintWelcomeMessage();

#if !DEBUG && false
            if (!this.hasSeenLoadingMsg)
                return;
#endif

            if (type == XivChatType.RetainerSale)
            {
                foreach (var regex in this.retainerSaleRegexes[this.dalamud.StartInfo.Language])
                {
                    var matchInfo = regex.Match(message.TextValue);

                    // we no longer really need to do/validate the item matching since we read the id from the byte array
                    // but we'd be checking the main match anyway
                    var itemInfo = matchInfo.Groups["item"];
                    if (!itemInfo.Success)
                        continue;

                    var itemLink = message.Payloads.FirstOrDefault(x => x.Type == PayloadType.Item) as ItemPayload;
                    if (itemLink == default)
                    {
                        Log.Error("itemLink was null. Msg: {0}", BitConverter.ToString(message.Encode()));
                        break;
                    }

                    Log.Debug($"Probable retainer sale: {message}, decoded item {itemLink.Item.RowId}, HQ {itemLink.IsHQ}");

                    var valueInfo = matchInfo.Groups["value"];
                    // not sure if using a culture here would work correctly, so just strip symbols instead
                    if (!valueInfo.Success || !int.TryParse(valueInfo.Value.Replace(",", string.Empty).Replace(".", string.Empty), out var itemValue))
                        continue;

                    // Task.Run(() => this.dalamud.BotManager.ProcessRetainerSale(itemLink.Item.RowId, itemValue, itemLink.IsHQ));
                    break;
                }
            }

            var messageCopy = message;
            var senderCopy = sender;

            var linkMatch = this.urlRegex.Match(message.TextValue);
            if (linkMatch.Value.Length > 0)
                this.LastLink = linkMatch.Value;

            // Handle all of this with SeString some day
            /*
            if ((this.HandledChatTypeColors.ContainsKey(type) || type == XivChatType.Say || type == XivChatType.Shout ||
                type == XivChatType.Alliance || type == XivChatType.TellOutgoing || type == XivChatType.Yell)) {
                var italicsStart = message.TextValue.IndexOf("*", StringComparison.InvariantCulture);
                var italicsEnd = message.TextValue.IndexOf("*", italicsStart + 1, StringComparison.InvariantCulture);

                var messageString = message.TextValue;

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
            */
        }

        private void PrintWelcomeMessage()
        {
            var assemblyVersion = Assembly.GetAssembly(typeof(ChatHandlers)).GetName().Version.ToString();

            this.dalamud.Framework.Gui.Chat.Print(string.Format(Loc.Localize("DalamudWelcome", "Dalamud vD{0} loaded."), assemblyVersion)
                                                + string.Format(Loc.Localize("PluginsWelcome", " {0} plugin(s) loaded."), this.dalamud.PluginManager.InstalledPlugins.Count));

            if (this.dalamud.Configuration.PrintPluginsWelcomeMsg)
            {
                foreach (var plugin in this.dalamud.PluginManager.InstalledPlugins.OrderBy(plugin => plugin.Name))
                {
                    this.dalamud.Framework.Gui.Chat.Print(string.Format(Loc.Localize("DalamudPluginLoaded", "    》 {0} v{1} loaded."), plugin.Name, plugin.Manifest.AssemblyVersion));
                }
            }

            if (string.IsNullOrEmpty(this.dalamud.Configuration.LastVersion) || !assemblyVersion.StartsWith(this.dalamud.Configuration.LastVersion))
            {
                this.dalamud.Framework.Gui.Chat.PrintChat(new XivChatEntry
                {
                    MessageBytes = Encoding.UTF8.GetBytes(Loc.Localize("DalamudUpdated", "The In-Game addon has been updated or was reinstalled successfully! Please check the discord for a full changelog.")),
                    Type = XivChatType.Notice,
                });

                if (this.dalamud.DalamudUi.WarrantsChangelog)
                    this.dalamud.DalamudUi.OpenChangelogWindow();

                this.dalamud.Configuration.LastVersion = assemblyVersion;
                this.dalamud.Configuration.Save();
            }

            Task.Run(() => this.dalamud.PluginManager.UpdatePlugins(!this.dalamud.Configuration.AutoUpdatePlugins))
                .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Log.Error(t.Exception, Loc.Localize("DalamudPluginUpdateCheckFail", "Could not check for plugin updates."));
                }
                else
                {
                    var updatedPlugins = t.Result;

                    if (updatedPlugins != null && updatedPlugins.Any())
                    {
                        if (this.dalamud.Configuration.AutoUpdatePlugins)
                        {
                            this.dalamud.PluginManager.PrintUpdatedPlugins(updatedPlugins, Loc.Localize("DalamudPluginAutoUpdate", "Auto-update:"));
                        }
                        else
                        {
                            this.dalamud.Framework.Gui.Chat.PrintChat(new XivChatEntry
                            {
                                MessageBytes = new SeString(new List<Payload>()
                                {
                                    new TextPayload(Loc.Localize("DalamudPluginUpdateRequired", "One or more of your plugins needs to be updated. Please use the /xlplugins command in-game to update them!")),
                                    new TextPayload("  ["),
                                    new UIForegroundPayload(this.dalamud.Data, 500),
                                    this.openInstallerWindowLink,
                                    new TextPayload(Loc.Localize("DalamudInstallerHelp", "Open the plugin installer")),
                                    RawPayload.LinkTerminator,
                                    new UIForegroundPayload(this.dalamud.Data, 0),
                                    new TextPayload("]"),
                                }).Encode(),
                                Type = XivChatType.Urgent,
                            });
                        }
                    }
                }
            });

            this.hasSeenLoadingMsg = true;
        }
    }
}
