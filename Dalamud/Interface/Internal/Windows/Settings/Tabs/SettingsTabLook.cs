using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Interface.Colors;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ImGuiFontChooserDialog;
using Dalamud.Interface.Internal.Windows.PluginInstaller;
using Dalamud.Interface.Internal.Windows.Settings.Widgets;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.Internal.Windows.Settings.Tabs;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
public class SettingsTabLook : SettingsTab
{
    private static readonly (string, float)[] GlobalUiScalePresets = 
    {
        ("80%##DalamudSettingsGlobalUiScaleReset96", 0.8f),
        ("100%##DalamudSettingsGlobalUiScaleReset12", 1f),
        ("117%##DalamudSettingsGlobalUiScaleReset14", 14 / 12f),
        ("150%##DalamudSettingsGlobalUiScaleReset18", 1.5f),
        ("200%##DalamudSettingsGlobalUiScaleReset24", 2f),
        ("300%##DalamudSettingsGlobalUiScaleReset36", 3f),
    };

    private float globalUiScale;
    private IFontSpec defaultFontSpec = null!;

    public override SettingsEntry[] Entries { get; } =
    {
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
            Loc.Localize("DalamudSettingToggleGamepadNavigation", "Control plugins via gamepad"),
            Loc.Localize("DalamudSettingToggleGamepadNavigationHint", "This will allow you to toggle between game and plugin navigation via L1+L3.\nToggle the PluginInstaller window via R3 if ImGui navigation is enabled."),
            c => c.IsGamepadNavigationEnabled,
            (v, c) => c.IsGamepadNavigationEnabled = v),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleTsm", "Show title screen menu"),
            Loc.Localize("DalamudSettingToggleTsmHint", "This will allow you to access certain Dalamud and Plugin functionality from the title screen.\nDisabling this will also hide the Dalamud version text on the title screen."),
            c => c.ShowTsm,
            (v, c) =>
            {
                c.ShowTsm = v;

                Service<GameGui>.GetNullable()?.RequestAddonUpdate("_TitleRevision", force: true);
            }),
        
        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingInstallerOpenDefault", "Open the Plugin Installer to the \"Installed Plugins\" tab by default"),
            Loc.Localize("DalamudSettingInstallerOpenDefaultHint", "This will allow you to open the Plugin Installer to the \"Installed Plugins\" tab by default, instead of the \"Available Plugins\" tab."),
            c => c.PluginInstallerOpen == PluginInstallerOpenKind.InstalledPlugins,
            (v, c) => c.PluginInstallerOpen = v ? PluginInstallerOpenKind.InstalledPlugins : PluginInstallerOpenKind.AllPlugins),
        
        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingReducedMotion", "Reduce motions"),
            Loc.Localize("DalamudSettingReducedMotionHint", "This will suppress certain animations from Dalamud, such as the notification popup."),
            c => c.ReduceMotions ?? false,
            (v, c) => c.ReduceMotions = v),
        
        new SettingsEntry<float>(
            Loc.Localize("DalamudSettingImeStateIndicatorOpacity", "IME State Indicator Opacity (CJK only)"),
            Loc.Localize("DalamudSettingImeStateIndicatorOpacityHint", "When any of CJK IMEs is in use, the state of IME will be shown with the opacity specified here."),
            c => c.ImeStateIndicatorOpacity,
            (v, c) => c.ImeStateIndicatorOpacity = v)
        {
            CustomDraw = static e =>
            {
                ImGuiHelpers.SafeTextWrapped(e.Name!);

                var v = e.Value * 100f;
                if (ImGui.SliderFloat($"###{e}", ref v, 0f, 100f, "%.1f%%"))
                    e.Value = v / 100f;
                ImGui.SameLine();

                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, v / 100);
                ImGui.TextUnformatted("\uE020\uE021\uE022\uE023\uE024\uE025\uE026\uE027");
                ImGui.PopStyleVar(1);
            },
        },
    };

    public override string Title => Loc.Localize("DalamudSettingsVisual", "Look & Feel");

    public override void Draw()
    {
        var interfaceManager = Service<InterfaceManager>.Get();
        var fontBuildTask = interfaceManager.FontBuildTask;

        ImGui.AlignTextToFramePadding();
        ImGui.Text(Loc.Localize("DalamudSettingsGlobalUiScale", "Global Font Scale"));

        var buttonSize =
            GlobalUiScalePresets
                .Select(x => ImGui.CalcTextSize(x.Item1, 0, x.Item1.IndexOf('#')))
                .Aggregate(Vector2.Zero, Vector2.Max)
            + (ImGui.GetStyle().FramePadding * 2);
        foreach (var (buttonLabel, scale) in GlobalUiScalePresets)
        {
            ImGui.SameLine();
            if (ImGui.Button(buttonLabel, buttonSize) && Math.Abs(this.globalUiScale - scale) > float.Epsilon)
            {
                ImGui.GetIO().FontGlobalScale = this.globalUiScale = scale;
                interfaceManager.RebuildFonts();
            }
        }

        if (!fontBuildTask.IsCompleted)
        {
            ImGui.SameLine();
            var buildingFonts = Loc.Localize("DalamudSettingsFontBuildInProgressWithEndingThreeDots", "Building fonts...");
            unsafe
            {
                var len = Encoding.UTF8.GetByteCount(buildingFonts);
                var p = stackalloc byte[len];
                Encoding.UTF8.GetBytes(buildingFonts, new(p, len));
                ImGuiNative.igTextUnformatted(p, (p + len + ((Environment.TickCount / 200) % 3)) - 2);
            }
        }

        var globalUiScaleInPct = 100f * this.globalUiScale;
        if (ImGui.DragFloat("##DalamudSettingsGlobalUiScaleDrag", ref globalUiScaleInPct, 1f, 80f, 300f, "%.0f%%", ImGuiSliderFlags.AlwaysClamp))
        {
            this.globalUiScale = globalUiScaleInPct / 100f;
            ImGui.GetIO().FontGlobalScale = this.globalUiScale;
            interfaceManager.RebuildFonts();
        }

        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsGlobalUiScaleHint", "Scale text in all XIVLauncher UI elements - this is useful for 4K displays."));

        if (fontBuildTask.IsFaulted || fontBuildTask.IsCanceled)
        {
            ImGui.TextColored(
                ImGuiColors.DalamudRed,
                Loc.Localize("DalamudSettingsFontBuildFaulted", "Failed to load fonts as requested."));
            if (fontBuildTask.Exception is not null
                && ImGui.CollapsingHeader("##DalamudSetingsFontBuildFaultReason"))
            {
                foreach (var e in fontBuildTask.Exception.InnerExceptions)
                    ImGui.TextUnformatted(e.ToString());
            }
        }

        ImGuiHelpers.ScaledDummy(5);

        if (ImGui.Button(Loc.Localize("DalamudSettingChooseDefaultFont", "Choose Default Font")))
        {
            var faf = Service<FontAtlasFactory>.Get();
            var fcd = new SingleFontChooserDialog(faf, $"{nameof(SettingsTabLook)}:Default");
            fcd.SelectedFont = (SingleFontSpec)this.defaultFontSpec;
            fcd.FontFamilyExcludeFilter = x => x is DalamudDefaultFontAndFamilyId;
            fcd.SetPopupPositionAndSizeToCurrentWindowCenter();
            interfaceManager.Draw += fcd.Draw;
            fcd.ResultTask.ContinueWith(
                r => Service<Framework>.Get().RunOnFrameworkThread(
                    () =>
                    {
                        interfaceManager.Draw -= fcd.Draw;
                        fcd.Dispose();

                        _ = r.Exception;
                        if (!r.IsCompletedSuccessfully)
                            return;

                        faf.DefaultFontSpecOverride = this.defaultFontSpec = r.Result;
                        interfaceManager.RebuildFonts();
                    }));
        }

        ImGui.SameLine();

        using (interfaceManager.MonoFontHandle?.Push())
        {
            if (ImGui.Button(Loc.Localize("DalamudSettingResetDefaultFont", "Reset Default Font")))
            {
                var faf = Service<FontAtlasFactory>.Get();
                faf.DefaultFontSpecOverride =
                    this.defaultFontSpec =
                        new SingleFontSpec { FontId = new GameFontAndFamilyId(GameFontFamily.Axis) };
                interfaceManager.RebuildFonts();
            }
        }

        base.Draw();
    }

    public override void Load()
    {
        this.globalUiScale = Service<DalamudConfiguration>.Get().GlobalUiScale;
        this.defaultFontSpec = Service<FontAtlasFactory>.Get().DefaultFontSpec;

        base.Load();
    }

    public override void Save()
    {
        Service<DalamudConfiguration>.Get().GlobalUiScale = this.globalUiScale;
        Service<DalamudConfiguration>.Get().DefaultFontSpec = this.defaultFontSpec;

        base.Save();
    }
}
