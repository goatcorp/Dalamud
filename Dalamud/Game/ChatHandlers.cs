using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.Windows;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Utility;

namespace Dalamud.Game;

/// <summary>
/// Chat events and public helper functions.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class ChatHandlers : IServiceType
{
    private static readonly ModuleLog Log = new("CHATHANDLER");

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

    [ServiceManager.ServiceDependency]
    private readonly Dalamud dalamud = Service<Dalamud>.Get();
    
    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private bool hasSeenLoadingMsg;

    [ServiceManager.ServiceConstructor]
    private ChatHandlers(ChatGui chatGui)
    {
        chatGui.CheckMessageHandled += this.OnCheckMessageHandled;
        chatGui.ChatMessage += this.OnChatMessage;
    }

    /// <summary>
    /// Gets the last URL seen in chat.
    /// </summary>
    public string? LastLink { get; private set; }

    /// <summary>
    /// Gets a value indicating whether or not auto-updates have already completed this session.
    /// </summary>
    public bool IsAutoUpdateComplete { get; private set; }

    private void OnCheckMessageHandled(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        var textVal = message.TextValue;

        if (this.configuration.BadWords != null &&
            this.configuration.BadWords.Any(x => !string.IsNullOrEmpty(x) && textVal.Contains(x)))
        {
            // This seems to be in the user block list - let's not show it
            Log.Debug("Blocklist triggered");
            isHandled = true;
            return;
        }
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        var clientState = Service<ClientState.ClientState>.GetNullable();
        if (clientState == null)
            return;

        if (type == XivChatType.Notice)
        {
            if (!this.hasSeenLoadingMsg)
                this.PrintWelcomeMessage();
        }

        // For injections while logged in
        if (clientState.LocalPlayer != null && clientState.TerritoryType == 0 && !this.hasSeenLoadingMsg)
            this.PrintWelcomeMessage();

#if !DEBUG && false
            if (!this.hasSeenLoadingMsg)
                return;
#endif

        if (type == XivChatType.RetainerSale)
        {
            foreach (var regex in this.retainerSaleRegexes[(ClientLanguage)this.dalamud.StartInfo.Language])
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
                chatGui.Print(string.Format(Loc.Localize("DalamudPluginLoaded", "    》 {0} v{1} loaded."), plugin.Name, plugin.EffectiveVersion));
            }
        }

        if (string.IsNullOrEmpty(this.configuration.LastVersion) || !assemblyVersion.StartsWith(this.configuration.LastVersion))
        {
            chatGui.Print(new XivChatEntry
            {
                Message = Loc.Localize("DalamudUpdated", "Dalamud has been updated successfully! Please check the discord for a full changelog."),
                Type = XivChatType.Notice,
            });

            this.configuration.LastVersion = assemblyVersion;
            this.configuration.QueueSave();
        }

        this.hasSeenLoadingMsg = true;
    }
}
