using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Internal.Windows;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Utility;
using Serilog;

namespace Dalamud.Game;

/// <summary>
/// Chat events and public helper functions.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
public class ChatHandlers : IServiceType
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
        @"4KGOLD|We have sufficient stock|VPK\.OM|[Gg]il for free|[Gg]il [Cc]heap|5GOLD|www\.so9\.com|Fast & Convenient|Cheap & Safety Guarantee|【Code|A O A U E|igfans|4KGOLD\.COM|Cheapest Gil with|pvp and bank on google|Selling Cheap GIL|ff14mogstation\.com|Cheap Gil 1000k|gilsforyou|server 1000K =|gils_selling|E A S Y\.C O M|bonus code|mins delivery guarantee|Sell cheap|Salegm\.com|cheap Mog|Off Code:|FF14Mog.com|使用する5％オ|[Oo][Ff][Ff] [Cc]ode( *)[:;]|offers Fantasia",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
        {
            ClientLanguage.Korean,
            new Regex[]
            {
                new Regex(@"^(?:.+)장터에 (?<origValue>[\d,.]+)길에 출품한 (?<item>.*)[이가] 판매되어 (?<value>[\d,.]+)길을 획득했습니다.$", RegexOptions.Compiled),
                new Regex(@"^(?:.+)장터에 (?<origValue>[\d,.]+)길에 출품한 (?<item>.*)×(?<count>[\d,.]+)개가 판매되어 (?<value>[\d,.]+)길을 획득했습니다.$", RegexOptions.Compiled),
            }
        },
    };

    private readonly Regex urlRegex = new(@"(http|ftp|https)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?", RegexOptions.Compiled);

    private readonly DalamudLinkPayload openInstallerWindowLink;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private bool hasSeenLoadingMsg;
    private bool startedAutoUpdatingPlugins;

    [ServiceManager.ServiceConstructor]
    private ChatHandlers(ChatGui chatGui)
    {
        chatGui.CheckMessageHandled += this.OnCheckMessageHandled;
        chatGui.ChatMessage += this.OnChatMessage;

        this.openInstallerWindowLink = chatGui.AddChatLinkHandler("Dalamud", 1001, (i, m) =>
        {
            Service<DalamudInterface>.GetNullable()?.OpenPluginInstaller();
        });
    }

    /// <summary>
    /// Gets the last URL seen in chat.
    /// </summary>
    public string? LastLink { get; private set; }

    /// <summary>
    /// Gets a value indicating whether or not auto-updates have already completed this session.
    /// </summary>
    public bool IsAutoUpdateComplete { get; private set; }

    /// <summary>
    /// Convert a TextPayload to SeString and wrap in italics payloads.
    /// </summary>
    /// <param name="text">Text to convert.</param>
    /// <returns>SeString payload of italicized text.</returns>
    public static SeString MakeItalics(string text)
        => MakeItalics(new TextPayload(text));

    /// <summary>
    /// Convert a TextPayload to SeString and wrap in italics payloads.
    /// </summary>
    /// <param name="text">Text to convert.</param>
    /// <returns>SeString payload of italicized text.</returns>
    public static SeString MakeItalics(TextPayload text)
        => new(EmphasisItalicPayload.ItalicsOn, text, EmphasisItalicPayload.ItalicsOff);

    private void OnCheckMessageHandled(XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        var textVal = message.TextValue;

        if (!this.configuration.DisableRmtFiltering)
        {
            var matched = this.rmtRegex.IsMatch(textVal);
            if (matched)
            {
                // This seems to be a RMT ad - let's not show it
                Log.Debug("Handled RMT ad: " + message.TextValue);
                isHandled = true;
                return;
            }
        }

        if (this.configuration.BadWords != null &&
            this.configuration.BadWords.Any(x => !string.IsNullOrEmpty(x) && textVal.Contains(x)))
        {
            // This seems to be in the user block list - let's not show it
            Log.Debug("Blocklist triggered");
            isHandled = true;
            return;
        }
    }

    private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        var startInfo = Service<DalamudStartInfo>.Get();
        var clientState = Service<ClientState.ClientState>.GetNullable();
        if (clientState == null)
            return;

        if (type == XivChatType.Notice && !this.hasSeenLoadingMsg)
            this.PrintWelcomeMessage();

        // For injections while logged in
        if (clientState.LocalPlayer != null && clientState.TerritoryType == 0 && !this.hasSeenLoadingMsg)
            this.PrintWelcomeMessage();

        if (!this.startedAutoUpdatingPlugins)
            this.AutoUpdatePlugins();

#if !DEBUG && false
            if (!this.hasSeenLoadingMsg)
                return;
#endif

        if (type == XivChatType.RetainerSale)
        {
            foreach (var regex in this.retainerSaleRegexes[startInfo.Language])
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
    }

    private void PrintWelcomeMessage()
    {
        var chatGui = Service<ChatGui>.GetNullable();
        var pluginManager = Service<PluginManager>.GetNullable();
        var dalamudInterface = Service<DalamudInterface>.GetNullable();

        if (chatGui == null || pluginManager == null || dalamudInterface == null)
            return;

        var assemblyVersion = Assembly.GetAssembly(typeof(ChatHandlers)).GetName().Version.ToString();

        if (this.configuration.PrintDalamudWelcomeMsg)
        {
            chatGui.Print(string.Format(Loc.Localize("DalamudWelcome", "Dalamud vD{0} loaded."), assemblyVersion)
                          + string.Format(Loc.Localize("PluginsWelcome", " {0} plugin(s) loaded."), pluginManager.InstalledPlugins.Count(x => x.IsLoaded)));
        }

        if (this.configuration.PrintPluginsWelcomeMsg)
        {
            foreach (var plugin in pluginManager.InstalledPlugins.OrderBy(plugin => plugin.Name).Where(x => x.IsLoaded))
            {
                chatGui.Print(string.Format(Loc.Localize("DalamudPluginLoaded", "    》 {0} v{1} loaded."), plugin.Name, plugin.Manifest.AssemblyVersion));
            }
        }

        if (string.IsNullOrEmpty(this.configuration.LastVersion) || !assemblyVersion.StartsWith(this.configuration.LastVersion))
        {
            chatGui.PrintChat(new XivChatEntry
            {
                Message = Loc.Localize("DalamudUpdated", "Dalamud has been updated successfully! Please check the discord for a full changelog."),
                Type = XivChatType.Notice,
            });

            if (string.IsNullOrEmpty(this.configuration.LastChangelogMajorMinor) || (!ChangelogWindow.WarrantsChangelogForMajorMinor.StartsWith(this.configuration.LastChangelogMajorMinor) && assemblyVersion.StartsWith(ChangelogWindow.WarrantsChangelogForMajorMinor)))
            {
                dalamudInterface.OpenChangelogWindow();
                this.configuration.LastChangelogMajorMinor = ChangelogWindow.WarrantsChangelogForMajorMinor;
            }

            this.configuration.LastVersion = assemblyVersion;
            this.configuration.QueueSave();
        }

        this.hasSeenLoadingMsg = true;
    }

    private void AutoUpdatePlugins()
    {
        var chatGui = Service<ChatGui>.GetNullable();
        var pluginManager = Service<PluginManager>.GetNullable();
        var notifications = Service<NotificationManager>.GetNullable();

        if (chatGui == null || pluginManager == null || notifications == null)
            return;

        if (!pluginManager.ReposReady || !pluginManager.InstalledPlugins.Any() || !pluginManager.AvailablePlugins.Any())
        {
            // Plugins aren't ready yet.
            // TODO: We should retry. This sucks, because it means we won't ever get here again until another notice.
            return;
        }

        this.startedAutoUpdatingPlugins = true;

        Task.Run(() => pluginManager.UpdatePluginsAsync(true, !this.configuration.AutoUpdatePlugins, true)).ContinueWith(task =>
        {
            this.IsAutoUpdateComplete = true;

            if (task.IsFaulted)
            {
                Log.Error(task.Exception, Loc.Localize("DalamudPluginUpdateCheckFail", "Could not check for plugin updates."));
                return;
            }

            var updatedPlugins = task.Result.ToList();
            if (updatedPlugins.Any())
            {
                if (this.configuration.AutoUpdatePlugins)
                {
                    Service<PluginManager>.Get().PrintUpdatedPlugins(updatedPlugins, Loc.Localize("DalamudPluginAutoUpdate", "Auto-update:"));
                    notifications.AddNotification(Loc.Localize("NotificationUpdatedPlugins", "{0} of your plugins were updated.").Format(updatedPlugins.Count), Loc.Localize("NotificationAutoUpdate", "Auto-Update"), NotificationType.Info);
                }
                else
                {
                    chatGui.PrintChat(new XivChatEntry
                    {
                        Message = new SeString(new List<Payload>()
                        {
                            new TextPayload(Loc.Localize("DalamudPluginUpdateRequired", "One or more of your plugins needs to be updated. Please use the /xlplugins command in-game to update them!")),
                            new TextPayload("  ["),
                            new UIForegroundPayload(500),
                            this.openInstallerWindowLink,
                            new TextPayload(Loc.Localize("DalamudInstallerHelp", "Open the plugin installer")),
                            RawPayload.LinkTerminator,
                            new UIForegroundPayload(0),
                            new TextPayload("]"),
                        }),
                        Type = XivChatType.Urgent,
                    });
                }
            }
        });
    }
}
