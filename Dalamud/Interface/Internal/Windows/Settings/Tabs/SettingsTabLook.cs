using System;
using System.Diagnostics.CodeAnalysis;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Windows.PluginInstaller;
using Dalamud.Interface.Internal.Windows.Settings.Widgets;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.Internal.Windows.Settings.Tabs;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
public class SettingsTabLook : SettingsTab
{
    private float globalUiScale;
    private float fontGamma;

    public override SettingsEntry[] Entries { get; } =
    {
        new GapSettingsEntry(5),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleAxisFonts", "Use AXIS fonts as default Dalamud font"),
            Loc.Localize("DalamudSettingToggleUiAxisFontsHint", "Use AXIS fonts (the game's main UI fonts) as default Dalamud font."),
            c => c.UseAxisFontsFromGame,
            (v, c) => c.UseAxisFontsFromGame = v,
            v =>
            {
                var im = Service<InterfaceManager>.Get();
                im.UseAxisOverride = v;
                im.RebuildFonts();
            }),

        new GapSettingsEntry(5, true),

        new ButtonSettingsEntry(
            Loc.Localize("DalamudSettingsOpenStyleEditor", "Open Style Editor"),
            Loc.Localize("DalamudSettingsStyleEditorHint", "Modify the look & feel of Dalamud windows."),
            () => Service<DalamudInterface>.Get().OpenStyleEditor()),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingsUseDarkMode", "Use Windows immersive/dark mode"),
            Loc.Localize("DalamudSettingsUseDarkModeHint", "This will cause the FFXIV window title bar to follow your preferred Windows color settings, and switch to dark mode if enabled."),
            c => c.WindowIsImmersive,
            (v, c) => c.WindowIsImmersive = v,
            b =>
            {
                try
                {
                    Service<InterfaceManager>.GetNullable()?.SetImmersiveMode(b);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Could not toggle immersive mode");
                }
            },
            visibility: Util.IsWindows11),

        new GapSettingsEntry(5, true),

        new HintSettingsEntry(Loc.Localize("DalamudSettingToggleUiHideOptOutNote", "Plugins may independently opt out of the settings below.")),
        new GapSettingsEntry(3),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleUiHide", "Hide plugin UI when the game UI is toggled off"),
            Loc.Localize("DalamudSettingToggleUiHideHint", "Hide any open windows by plugins when toggling the game overlay."),
            c => c.ToggleUiHide,
            (v, c) => c.ToggleUiHide = v),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleUiHideDuringCutscenes", "Hide plugin UI during cutscenes"),
            Loc.Localize("DalamudSettingToggleUiHideDuringCutscenesHint", "Hide any open windows by plugins during cutscenes."),
            c => c.ToggleUiHideDuringCutscenes,
            (v, c) => c.ToggleUiHideDuringCutscenes = v),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleUiHideDuringGpose", "Hide plugin UI while gpose is active"),
            Loc.Localize("DalamudSettingToggleUiHideDuringGposeHint", "Hide any open windows by plugins while gpose is active."),
            c => c.ToggleUiHideDuringGpose,
            (v, c) => c.ToggleUiHideDuringGpose = v),

        new GapSettingsEntry(5, true),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleFocusManagement", "Use escape to close Dalamud windows"),
            Loc.Localize("DalamudSettingToggleFocusManagementHint", "This will cause Dalamud windows to behave like in-game windows when pressing escape.\nThey will close one after another until all are closed. May not work for all plugins."),
            c => c.IsFocusManagementEnabled,
            (v, c) => c.IsFocusManagementEnabled = v),

        // This is applied every frame in InterfaceManager::CheckViewportState()
        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleViewports", "Enable multi-monitor windows"),
            Loc.Localize("DalamudSettingToggleViewportsHint", "This will allow you move plugin windows onto other monitors.\nWill only work in Borderless Window or Windowed mode."),
            c => !c.IsDisableViewport,
            (v, c) => c.IsDisableViewport = !v),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleDocking", "Enable window docking"),
            Loc.Localize("DalamudSettingToggleDockingHint", "This will allow you to fuse and tab plugin windows."),
            c => c.IsDocking,
            (v, c) => c.IsDocking = v),
        
        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingEnablePluginUISoundEffects", "Enable sound effects for plugin windows"),
            Loc.Localize("DalamudSettingEnablePluginUISoundEffectsHint", "This will allow you to enable or disable sound effects generated by plugin user interfaces.\nThis is affected by your in-game `System Sounds` volume settings."),
            c => c.EnablePluginUISoundEffects,
            (v, c) => c.EnablePluginUISoundEffects = v),
        
        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingEnablePluginUIAdditionalOptions", "Add a button to the title bar of plugin windows to open additional options"),
            Loc.Localize("DalamudSettingEnablePluginUIAdditionalOptionsHint", "This will allow you to pin certain plugin windows, make them clickthrough or adjust their opacity.\nThis may not be supported by all of your plugins. Contact the plugin author if you want them to support this feature."),
            c => c.EnablePluginUiAdditionalOptions,
            (v, c) => c.EnablePluginUiAdditionalOptions = v),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleGamepadNavigation", "Control plugins via gamepad"),
            Loc.Localize("DalamudSettingToggleGamepadNavigationHint", "This will allow you to toggle between game and plugin navigation via L1+L3.\nToggle the PluginInstaller window via R3 if ImGui navigation is enabled."),
            c => c.IsGamepadNavigationEnabled,
            (v, c) => c.IsGamepadNavigationEnabled = v),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleTsm", "Show title screen menu"),
            Loc.Localize("DalamudSettingToggleTsmHint", "This will allow you to access certain Dalamud and Plugin functionality from the title screen."),
            c => c.ShowTsm,
            (v, c) => c.ShowTsm = v),
        
        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingInstallerOpenDefault", "Open the Plugin Installer to the \"Installed Plugins\" tab by default"),
            Loc.Localize("DalamudSettingInstallerOpenDefaultHint", "This will allow you to open the Plugin Installer to the \"Installed Plugins\" tab by default, instead of the \"Available Plugins\" tab."),
            c => c.PluginInstallerOpen == PluginInstallerWindow.PluginInstallerOpenKind.InstalledPlugins,
            (v, c) => c.PluginInstallerOpen = v ? PluginInstallerWindow.PluginInstallerOpenKind.InstalledPlugins : PluginInstallerWindow.PluginInstallerOpenKind.AllPlugins),
    };

    public override string Title => Loc.Localize("DalamudSettingsVisual", "Look & Feel");

    public override void Draw()
    {
        var interfaceManager = Service<InterfaceManager>.Get();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);
        ImGui.Text(Loc.Localize("DalamudSettingsGlobalUiScale", "Global Font Scale"));
        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3);
        if (ImGui.Button("9.6pt##DalamudSettingsGlobalUiScaleReset96"))
        {
            this.globalUiScale = 9.6f / 12.0f;
            ImGui.GetIO().FontGlobalScale = this.globalUiScale;
            interfaceManager.RebuildFonts();
        }

        ImGui.SameLine();
        if (ImGui.Button("12pt##DalamudSettingsGlobalUiScaleReset12"))
        {
            this.globalUiScale = 1.0f;
            ImGui.GetIO().FontGlobalScale = this.globalUiScale;
            interfaceManager.RebuildFonts();
        }

        ImGui.SameLine();
        if (ImGui.Button("14pt##DalamudSettingsGlobalUiScaleReset14"))
        {
            this.globalUiScale = 14.0f / 12.0f;
            ImGui.GetIO().FontGlobalScale = this.globalUiScale;
            interfaceManager.RebuildFonts();
        }

        ImGui.SameLine();
        if (ImGui.Button("18pt##DalamudSettingsGlobalUiScaleReset18"))
        {
            this.globalUiScale = 18.0f / 12.0f;
            ImGui.GetIO().FontGlobalScale = this.globalUiScale;
            interfaceManager.RebuildFonts();
        }

        ImGui.SameLine();
        if (ImGui.Button("24pt##DalamudSettingsGlobalUiScaleReset24"))
        {
            this.globalUiScale = 24.0f / 12.0f;
            ImGui.GetIO().FontGlobalScale = this.globalUiScale;
            interfaceManager.RebuildFonts();
        }

        ImGui.SameLine();
        if (ImGui.Button("36pt##DalamudSettingsGlobalUiScaleReset36"))
        {
            this.globalUiScale = 36.0f / 12.0f;
            ImGui.GetIO().FontGlobalScale = this.globalUiScale;
            interfaceManager.RebuildFonts();
        }

        var globalUiScaleInPt = 12f * this.globalUiScale;
        if (ImGui.DragFloat("##DalamudSettingsGlobalUiScaleDrag", ref globalUiScaleInPt, 0.1f, 9.6f, 36f, "%.1fpt", ImGuiSliderFlags.AlwaysClamp))
        {
            this.globalUiScale = globalUiScaleInPt / 12f;
            ImGui.GetIO().FontGlobalScale = this.globalUiScale;
            interfaceManager.RebuildFonts();
        }

        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsGlobalUiScaleHint", "Scale text in all XIVLauncher UI elements - this is useful for 4K displays."));

        ImGuiHelpers.ScaledDummy(5);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);
        ImGui.Text(Loc.Localize("DalamudSettingsFontGamma", "Font Gamma"));
        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3);
        if (ImGui.Button(Loc.Localize("DalamudSettingsIndividualConfigResetToDefaultValue", "Reset") + "##DalamudSettingsFontGammaReset"))
        {
            this.fontGamma = 1.4f;
            interfaceManager.FontGammaOverride = this.fontGamma;
            interfaceManager.RebuildFonts();
        }

        if (ImGui.DragFloat("##DalamudSettingsFontGammaDrag", ref this.fontGamma, 0.005f, 0.3f, 3f, "%.2f", ImGuiSliderFlags.AlwaysClamp))
        {
            interfaceManager.FontGammaOverride = this.fontGamma;
            interfaceManager.RebuildFonts();
        }

        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsFontGammaHint", "Changes the thickness of text."));

        base.Draw();
    }

    public override void Load()
    {
        this.globalUiScale = Service<DalamudConfiguration>.Get().GlobalUiScale;
        this.fontGamma = Service<DalamudConfiguration>.Get().FontGammaLevel;

        base.Load();
    }

    public override void Save()
    {
        Service<DalamudConfiguration>.Get().GlobalUiScale = this.globalUiScale;

        base.Save();
    }
}
