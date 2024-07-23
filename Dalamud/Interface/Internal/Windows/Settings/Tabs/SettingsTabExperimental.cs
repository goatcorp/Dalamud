using System.Diagnostics.CodeAnalysis;

using CheapLoc;

using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.ReShadeHandling;
using Dalamud.Interface.Internal.Windows.PluginInstaller;
using Dalamud.Interface.Internal.Windows.Settings.Widgets;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Internal;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.Settings.Tabs;

[SuppressMessage(
    "StyleCop.CSharp.DocumentationRules",
    "SA1600:Elements should be documented",
    Justification = "Internals")]
public class SettingsTabExperimental : SettingsTab
{
    public override SettingsEntry[] Entries { get; } =
    [
        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingsPluginTest", "Get plugin testing builds"),
            string.Format(
                Loc.Localize(
                    "DalamudSettingsPluginTestHint",
                    "Receive testing prereleases for selected plugins.\nTo opt-in to testing builds for a plugin, you have to right click it in the \"{0}\" tab of the plugin installer and select \"{1}\"."),
                PluginCategoryManager.Locs.Group_Installed,
                PluginInstallerWindow.Locs.PluginContext_TestingOptIn),
            c => c.DoPluginTest,
            (v, c) => c.DoPluginTest = v),
        new HintSettingsEntry(
            Loc.Localize(
                "DalamudSettingsPluginTestWarning",
                "Testing plugins may contain bugs or crash your game. Please only enable this if you are aware of the risks."),
            ImGuiColors.DalamudRed),

        new GapSettingsEntry(5),

        new SettingsEntry<bool>(
            Loc.Localize(
                "DalamudSettingEnablePluginUIAdditionalOptions",
                "Add a button to the title bar of plugin windows to open additional options"),
            Loc.Localize(
                "DalamudSettingEnablePluginUIAdditionalOptionsHint",
                "This will allow you to pin certain plugin windows, make them clickthrough or adjust their opacity.\nThis may not be supported by all of your plugins. Contact the plugin author if you want them to support this feature."),
            c => c.EnablePluginUiAdditionalOptions,
            (v, c) => c.EnablePluginUiAdditionalOptions = v),

        new GapSettingsEntry(5),

        new ButtonSettingsEntry(
            Loc.Localize("DalamudSettingsClearHidden", "Clear hidden plugins"),
            Loc.Localize(
                "DalamudSettingsClearHiddenHint",
                "Restore plugins you have previously hidden from the plugin installer."),
            () =>
            {
                Service<DalamudConfiguration>.Get().HiddenPluginInternalName.Clear();
                Service<PluginManager>.Get().RefilterPluginMasters();
            }),

        new GapSettingsEntry(5, true),

        new DevPluginsSettingsEntry(),

        new GapSettingsEntry(5, true),

        new ThirdRepoSettingsEntry(),

        new GapSettingsEntry(5, true),

        new EnumSettingsEntry<ReShadeHandlingMode>(
            Loc.Localize("DalamudSettingsReShadeHandlingMode", "ReShade handling mode"),
            Loc.Localize(
                "DalamudSettingsReShadeHandlingModeHint",
                "You may try different options to work around problems you may encounter.\nRestart is required for changes to take effect."),
            c => c.ReShadeHandlingMode,
            (v, c) => c.ReShadeHandlingMode = v,
            fallbackValue: ReShadeHandlingMode.ReShadeAddon)
        {
            FriendlyEnumNameGetter = x => x switch
            {
                ReShadeHandlingMode.ReShadeAddon => Loc.Localize(
                    "DalamudSettingsReShadeHandlingModeReShadeAddon",
                    "ReShade addon"),
                ReShadeHandlingMode.UnwrapReShade => Loc.Localize(
                    "DalamudSettingsReShadeHandlingModeUnwrapReShade",
                    "Unwrap ReShade"),
                ReShadeHandlingMode.None => Loc.Localize(
                    "DalamudSettingsReShadeHandlingModeNone",
                    "Do not handle"),
                _ => "<invalid>",
            },
            FriendlyEnumDescriptionGetter = x => x switch
            {
                ReShadeHandlingMode.ReShadeAddon => Loc.Localize(
                    "DalamudSettingsReShadeHandlingModeReShadeAddonDescription",
                    "Dalamud will register itself as a ReShade addon. Most compatibility is expected, but multi-monitor window option will require reloading ReShade every time a new window is opened, or even may not work at all."),
                ReShadeHandlingMode.UnwrapReShade => Loc.Localize(
                    "DalamudSettingsReShadeHandlingModeUnwrapReShadeDescription",
                    "Dalamud will exclude itself from all ReShade handling. Multi-monitor windows should work fine with this mode, but it may not be supported and crash in future ReShade versions."),
                ReShadeHandlingMode.None => Loc.Localize(
                    "DalamudSettingsReShadeHandlingModeNoneDescription",
                    "No special handling will be done for ReShade. Dalamud will be under the effect of ReShade postprocessing."),
                _ => "<invalid>",
            },
        },

        new GapSettingsEntry(5, true),

        new EnumSettingsEntry<SwapChainHelper.HookMode>(
            Loc.Localize("DalamudSettingsSwapChainHookMode", "Swap chain hooking mode"),
            Loc.Localize(
                "DalamudSettingsSwapChainHookModeHint",
                "Depending on addons aside from Dalamud you use, you may have to use different options for Dalamud and other addons to cooperate.\nRestart is required for changes to take effect."),
            c => c.SwapChainHookMode,
            (v, c) => c.SwapChainHookMode = v,
            fallbackValue: SwapChainHelper.HookMode.ByteCode),

        /* Disabling profiles after they've been enabled doesn't make much sense, at least not if the user has already created profiles.
        new GapSettingsEntry(5, true),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingsEnableProfiles", "Enable plugin collections"),
            Loc.Localize("DalamudSettingsEnableProfilesHint", "Enables plugin collections, which lets you create toggleable lists of plugins."),
            c => c.ProfilesEnabled,
            (v, c) => c.ProfilesEnabled = v),
            */
    ];

    public override string Title => Loc.Localize("DalamudSettingsExperimental", "Experimental");

    public override void Draw()
    {
        base.Draw();

        ImGuiHelpers.SafeTextColoredWrapped(
            ImGuiColors.DalamudGrey,
            "Total memory used by Dalamud & Plugins: " + Util.FormatBytes(GC.GetTotalMemory(false)));
        ImGuiHelpers.ScaledDummy(15);
    }
}
