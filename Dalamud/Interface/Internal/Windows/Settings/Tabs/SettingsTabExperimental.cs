using System.Diagnostics.CodeAnalysis;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Windows.PluginInstaller;
using Dalamud.Interface.Internal.Windows.Settings.Widgets;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Internal;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.Settings.Tabs;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
public class SettingsTabExperimental : SettingsTab
{
    public override SettingsEntry[] Entries { get; } =
    {
        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingsPluginTest", "Get plugin testing builds"),
            string.Format(
                Loc.Localize("DalamudSettingsPluginTestHint", "Receive testing prereleases for selected plugins.\nTo opt-in to testing builds for a plugin, you have to right click it in the \"{0}\" tab of the plugin installer and select \"{1}\"."),
                PluginCategoryManager.Locs.Group_Installed,
                PluginInstallerWindow.Locs.PluginContext_TestingOptIn),
            c => c.DoPluginTest,
            (v, c) => c.DoPluginTest = v),
        new HintSettingsEntry(
            Loc.Localize("DalamudSettingsPluginTestWarning", "Testing plugins may contain bugs or crash your game. Please only enable this if you are aware of the risks."),
            ImGuiColors.DalamudRed),
        
        new GapSettingsEntry(5),
        
        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingEnablePluginUIAdditionalOptions", "Add a button to the title bar of plugin windows to open additional options"),
            Loc.Localize("DalamudSettingEnablePluginUIAdditionalOptionsHint", "This will allow you to pin certain plugin windows, make them clickthrough or adjust their opacity.\nThis may not be supported by all of your plugins. Contact the plugin author if you want them to support this feature."),
            c => c.EnablePluginUiAdditionalOptions,
            (v, c) => c.EnablePluginUiAdditionalOptions = v),

        new GapSettingsEntry(5),

        new ButtonSettingsEntry(
            Loc.Localize("DalamudSettingsClearHidden", "Clear hidden plugins"),
            Loc.Localize("DalamudSettingsClearHiddenHint", "Restore plugins you have previously hidden from the plugin installer."),
            () =>
            {
                Service<DalamudConfiguration>.Get().HiddenPluginInternalName.Clear();
                Service<PluginManager>.Get().RefilterPluginMasters();
            }),

        new GapSettingsEntry(5, true),

        new DevPluginsSettingsEntry(),

        new GapSettingsEntry(5, true),

        new ThirdRepoSettingsEntry(),

        /* Disabling profiles after they've been enabled doesn't make much sense, at least not if the user has already created profiles.
        new GapSettingsEntry(5, true),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingsEnableProfiles", "Enable plugin collections"),
            Loc.Localize("DalamudSettingsEnableProfilesHint", "Enables plugin collections, which lets you create toggleable lists of plugins."),
            c => c.ProfilesEnabled,
            (v, c) => c.ProfilesEnabled = v),
            */
    };

    public override string Title => Loc.Localize("DalamudSettingsExperimental", "Experimental");

    public override void Draw()
    {
        base.Draw();

        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "Total memory used by Dalamud & Plugins: " + Util.FormatBytes(GC.GetTotalMemory(false)));
        ImGuiHelpers.ScaledDummy(15);
    }
}
