using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using CheapLoc;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Utility;

namespace Dalamud.Game;

/// <summary>
/// Chat events and public helper functions.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal partial class ChatHandlers : IServiceType
{
    private static readonly ModuleLog Log = ModuleLog.Create<ChatHandlers>();

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
    /// Gets a value indicating whether auto-updates have already completed this session.
    /// </summary>
    public bool IsAutoUpdateComplete { get; private set; }

    [GeneratedRegex(@"(http|ftp|https)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?", RegexOptions.Compiled)]
    private static partial Regex CompiledUrlRegex();

    private void OnCheckMessageHandled(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        var textVal = message.TextValue;

        if (this.configuration.BadWords != null &&
            this.configuration.BadWords.Any(x => !string.IsNullOrEmpty(x) && textVal.Contains(x)))
        {
            // This seems to be in the user block list - let's not show it
            Log.Debug("Filtered a message that contained a muted word");
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
        if (clientState.IsLoggedIn && clientState.TerritoryType == 0 && !this.hasSeenLoadingMsg)
            this.PrintWelcomeMessage();

#if !DEBUG && false
            if (!this.hasSeenLoadingMsg)
                return;
#endif

        var messageCopy = message;
        var senderCopy = sender;

        var linkMatch = CompiledUrlRegex().Match(message.TextValue);
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

        if (this.configuration.PrintDalamudWelcomeMsg)
        {
            chatGui.Print(string.Format(Loc.Localize("DalamudWelcome", "Dalamud {0} loaded."), Versioning.GetScmVersion())
                          + string.Format(Loc.Localize("PluginsWelcome", " {0} plugin(s) loaded."), pluginManager.InstalledPlugins.Count(x => x.IsLoaded)));
        }

        if (this.configuration.PrintPluginsWelcomeMsg)
        {
            foreach (var plugin in pluginManager.InstalledPlugins.OrderBy(plugin => plugin.Name).Where(x => x.IsLoaded))
            {
                chatGui.Print(string.Format(Loc.Localize("DalamudPluginLoaded", "    》 {0} v{1} loaded."), plugin.Name, plugin.EffectiveVersion));
            }
        }

        if (string.IsNullOrEmpty(this.configuration.LastVersion) || !Versioning.GetAssemblyVersion().StartsWith(this.configuration.LastVersion))
        {
            var linkPayload = chatGui.AddChatLinkHandler(
                (_, _) => dalamudInterface.OpenPluginInstallerTo(PluginInstallerOpenKind.Changelogs));

            var updateMessage = new SeStringBuilder()
                .AddText(Loc.Localize("DalamudUpdated", "Dalamud has been updated successfully!"))
                .AddUiForeground(500)
                .AddText("  [ ")
                .Add(linkPayload)
                .AddText(Loc.Localize("DalamudClickToViewChangelogs", "Click here to view the changelog."))
                .Add(RawPayload.LinkTerminator)
                .AddText(" ]")
                .AddUiForegroundOff();

            chatGui.Print(new XivChatEntry
            {
                Message = updateMessage.Build(),
                Type = XivChatType.Notice,
            });

            this.configuration.LastVersion = Versioning.GetAssemblyVersion();
            this.configuration.QueueSave();
        }

        this.hasSeenLoadingMsg = true;
    }
}
