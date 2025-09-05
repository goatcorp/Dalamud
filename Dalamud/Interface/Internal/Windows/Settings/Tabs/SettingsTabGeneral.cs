using System.Diagnostics.CodeAnalysis;

using CheapLoc;

using Dalamud.Game.Text;
using Dalamud.Interface.Internal.Windows.Settings.Widgets;
using Dalamud.Utility.Internal;

namespace Dalamud.Interface.Internal.Windows.Settings.Tabs;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
internal sealed class SettingsTabGeneral : SettingsTab
{
    public override string Title => Loc.Localize("DalamudSettingsGeneral", "General");

    public override SettingsOpenKind Kind => SettingsOpenKind.General;

    public override SettingsEntry[] Entries { get; } =
    [
        new LanguageChooserSettingsEntry(),

        new GapSettingsEntry(5),

        new EnumSettingsEntry<XivChatType>(
            LazyLoc.Localize("DalamudSettingsChannel", "Dalamud Chat Channel"),
            LazyLoc.Localize("DalamudSettingsChannelHint", "Select the chat channel that is to be used for general Dalamud messages."),
            c => c.GeneralChatType,
            (v, c) => c.GeneralChatType = v,
            warning: v =>
            {
                // TODO: Maybe actually implement UI for the validity check...
                if (v == XivChatType.None)
                    return Loc.Localize("DalamudSettingsChannelNone", "Do not pick \"None\".");

                return null;
            },
            fallbackValue: XivChatType.Debug),

        new GapSettingsEntry(5),

        new SettingsEntry<bool>(
            LazyLoc.Localize("DalamudSettingsWaitForPluginsOnStartup", "Wait for plugins before game loads"),
            LazyLoc.Localize("DalamudSettingsWaitForPluginsOnStartupHint", "Do not let the game load, until plugins are loaded."),
            c => c.IsResumeGameAfterPluginLoad,
            (v, c) => c.IsResumeGameAfterPluginLoad = v),

        new SettingsEntry<bool>(
            LazyLoc.Localize("DalamudSettingsFlash", "Flash FFXIV window on duty pop"),
            LazyLoc.Localize("DalamudSettingsFlashHint", "Flash the FFXIV window in your task bar when a duty is ready."),
            c => c.DutyFinderTaskbarFlash,
            (v, c) => c.DutyFinderTaskbarFlash = v),

        new SettingsEntry<bool>(
            LazyLoc.Localize("DalamudSettingsDutyFinderMessage", "Chatlog message on duty pop"),
            LazyLoc.Localize("DalamudSettingsDutyFinderMessageHint", "Send a message in FFXIV chat when a duty is ready."),
            c => c.DutyFinderChatMessage,
            (v, c) => c.DutyFinderChatMessage = v),

        new SettingsEntry<bool>(
            LazyLoc.Localize("DalamudSettingsPrintDalamudWelcomeMsg", "Display Dalamud's welcome message"),
            LazyLoc.Localize("DalamudSettingsPrintDalamudWelcomeMsgHint", "Display Dalamud's welcome message in FFXIV chat when logging in with a character."),
            c => c.PrintDalamudWelcomeMsg,
            (v, c) => c.PrintDalamudWelcomeMsg = v),

        new SettingsEntry<bool>(
            LazyLoc.Localize("DalamudSettingsPrintPluginsWelcomeMsg", "Display loaded plugins in the welcome message"),
            LazyLoc.Localize("DalamudSettingsPrintPluginsWelcomeMsgHint", "Display loaded plugins in FFXIV chat when logging in with a character."),
            c => c.PrintPluginsWelcomeMsg,
            (v, c) => c.PrintPluginsWelcomeMsg = v),

        new SettingsEntry<bool>(
            LazyLoc.Localize("DalamudSettingsSystemMenu", "Dalamud buttons in system menu"),
            LazyLoc.Localize("DalamudSettingsSystemMenuMsgHint", "Add buttons for Dalamud plugins and settings to the system menu."),
            c => c.DoButtonsSystemMenu,
            (v, c) => c.DoButtonsSystemMenu = v),

        new GapSettingsEntry(5),

        new SettingsEntry<bool>(
            LazyLoc.Localize("DalamudSettingDoMbCollect", "Anonymously upload market board data"),
            LazyLoc.Localize("DalamudSettingDoMbCollectHint", "Anonymously provide data about in-game economics to Universalis when browsing the market board. This data can't be tied to you in any way and everyone benefits!"),
            c => c.IsMbCollect,
            (v, c) => c.IsMbCollect = v),
    ];
}
