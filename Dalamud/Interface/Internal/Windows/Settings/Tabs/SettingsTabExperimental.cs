using System.Diagnostics.CodeAnalysis;

using CheapLoc;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.ReShadeHandling;
using Dalamud.Interface.Internal.Windows.Settings.Widgets;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Internal;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.Settings.Tabs;

[SuppressMessage(
    "StyleCop.CSharp.DocumentationRules",
    "SA1600:Elements should be documented",
    Justification = "Internals")]
internal sealed class SettingsTabExperimental : SettingsTab
{
    public override string Title => Loc.Localize("DalamudSettingsExperimental", "Experimental");

    public override SettingsOpenKind Kind => SettingsOpenKind.Experimental;

    public override SettingsEntry[] Entries { get; } =
    [
        new SettingsEntry<bool>(
            ("DalamudSettingsPluginTest", "Get plugin testing builds"),
            ("DalamudSettingsPluginTestHint", "Receive testing prereleases for selected plugins.\nTo opt-in to testing builds for a plugin, you have to right click it in the \"Installed Plugins\" tab of the plugin installer and select \"Receive plugin testing versions\"."),
            c => c.DoPluginTest,
            (v, c) => c.DoPluginTest = v),
        new HintSettingsEntry(
            ("DalamudSettingsPluginTestWarning", "Testing plugins may contain bugs or crash your game. Please only enable this if you are aware of the risks."),
            ImGuiColors.DalamudRed),

        new GapSettingsEntry(5),

        new ButtonSettingsEntry(
            ("DalamudSettingsClearHidden", "Clear hidden plugins"),
            ("DalamudSettingsClearHiddenHint", "Restore plugins you have previously hidden from the plugin installer."),
            () =>
            {
                Service<DalamudConfiguration>.Get().HiddenPluginInternalName.Clear();
                Service<PluginManager>.Get().RefilterPluginMasters();
            }),

        new GapSettingsEntry(5, true),

        new DevPluginsSettingsEntry(),

        new SettingsEntry<bool>(
            ("DalamudSettingEnableImGuiAsserts", "Enable ImGui asserts"),
            ("DalamudSettingEnableImGuiAssertsHint",
                "If this setting is enabled, a window containing further details will be shown when an internal assertion in ImGui fails.\nWe recommend enabling this when developing plugins. " +
                "This setting does not persist and will reset when the game restarts.\nUse the setting below to enable it at startup."),
            c => Service<InterfaceManager>.Get().ShowAsserts,
            (v, _) => Service<InterfaceManager>.Get().ShowAsserts = v),

        new SettingsEntry<bool>(
            ("DalamudSettingEnableImGuiAssertsAtStartup", "Always enable ImGui asserts at startup"),
            ("DalamudSettingEnableImGuiAssertsAtStartupHint", "This will enable ImGui asserts every time the game starts."),
            c => c.ImGuiAssertsEnabledAtStartup ?? false,
            (v, c) => c.ImGuiAssertsEnabledAtStartup = v),

        new GapSettingsEntry(5, true),

        new ThirdRepoSettingsEntry(),

        new GapSettingsEntry(5, true),

        new EnumSettingsEntry<ReShadeHandlingMode>(
            ("DalamudSettingsReShadeHandlingMode", "ReShade handling mode"),
            ("DalamudSettingsReShadeHandlingModeHint", "You may try different options to work around problems you may encounter.\nRestart is required for changes to take effect."),
            c => c.ReShadeHandlingMode,
            (v, c) => c.ReShadeHandlingMode = v,
            fallbackValue: ReShadeHandlingMode.Default,
            warning: static rshm =>
            {
                var warning = string.Empty;
                warning += rshm is ReShadeHandlingMode.UnwrapReShade or ReShadeHandlingMode.None ||
                           Service<DalamudConfiguration>.Get().SwapChainHookMode == SwapChainHelper.HookMode.ByteCode
                               ? string.Empty
                               : "Current option will be ignored and no special ReShade handling will be done, because SwapChain vtable hook mode is set.";

                if (ReShadeAddonInterface.ReShadeIsSignedByReShade)
                {
                    warning += warning.Length > 0 ? "\n" : string.Empty;
                    warning += Loc.Localize(
                        "ReShadeNoAddonSupportNotificationContent",
                        "Your installation of ReShade does not have full addon support, and may not work with Dalamud and/or the game.\n" +
                        "Download and install ReShade with full addon-support.");
                }

                return warning.Length > 0 ? warning : null;
            })
        {
            FriendlyEnumNameGetter = x => x switch
            {
                ReShadeHandlingMode.Default => "Default",
                ReShadeHandlingMode.UnwrapReShade => "Unwrap",
                ReShadeHandlingMode.ReShadeAddonPresent => "ReShade Addon (present)",
                ReShadeHandlingMode.ReShadeAddonReShadeOverlay => "ReShade Addon (reshade_overlay)",
                ReShadeHandlingMode.HookReShadeDxgiSwapChainOnPresent => "Hook ReShade::DXGISwapChain::OnPresent",
                ReShadeHandlingMode.None => "Do not handle",
                _ => "<invalid>",
            },
        },

        /* // Making this a console command instead, for now
        new GapSettingsEntry(5, true),

        new EnumSettingsEntry<SwapChainHelper.HookMode>(
            Loc.Localize("DalamudSettingsSwapChainHookMode", "Swap chain hooking mode"),
            Loc.Localize(
                "DalamudSettingsSwapChainHookModeHint",
                "Depending on addons aside from Dalamud you use, you may have to use different options for Dalamud and other addons to cooperate.\nRestart is required for changes to take effect."),
            c => c.SwapChainHookMode,
            (v, c) => c.SwapChainHookMode = v,
            fallbackValue: SwapChainHelper.HookMode.ByteCode),
            */

        /* Disabling profiles after they've been enabled doesn't make much sense, at least not if the user has already created profiles.
        new GapSettingsEntry(5, true),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingsEnableProfiles", "Enable plugin collections"),
            Loc.Localize("DalamudSettingsEnableProfilesHint", "Enables plugin collections, which lets you create toggleable lists of plugins."),
            c => c.ProfilesEnabled,
            (v, c) => c.ProfilesEnabled = v),
            */
    ];

    public override void Draw()
    {
        base.Draw();

        ImGui.TextColoredWrapped(
            ImGuiColors.DalamudGrey,
            "Total memory used by Dalamud & Plugins: " + Util.FormatBytes(GC.GetTotalMemory(false)));
        ImGuiHelpers.ScaledDummy(15);
    }
}
