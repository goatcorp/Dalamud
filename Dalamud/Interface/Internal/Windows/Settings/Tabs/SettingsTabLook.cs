using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.EasyFonts;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal.Windows.PluginInstaller;
using Dalamud.Interface.Internal.Windows.Settings.Widgets;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using ImGuiNET;
using Serilog;
using SharpDX.DirectWrite;

using Vector2 = System.Numerics.Vector2;

namespace Dalamud.Interface.Internal.Windows.Settings.Tabs;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented",
                 Justification = "Internals")]
public class SettingsTabLook : SettingsTab
{
    private const int FontIndexAxis = 0;
    private const int FontIndexNotoSans = 1;
    private const int SpecialFontCount = 2;

    private static readonly (string, float)[] GlobalUiScalePresets = 
    {
        ("9.6pt##DalamudSettingsGlobalUiScaleReset96", 9.6f / InterfaceManager.DefaultFontSizePt),
        ("12pt##DalamudSettingsGlobalUiScaleReset12", 12f / InterfaceManager.DefaultFontSizePt),
        ("14pt##DalamudSettingsGlobalUiScaleReset14", 14f / InterfaceManager.DefaultFontSizePt),
        ("18pt##DalamudSettingsGlobalUiScaleReset18", 18f / InterfaceManager.DefaultFontSizePt),
        ("24pt##DalamudSettingsGlobalUiScaleReset24", 24f / InterfaceManager.DefaultFontSizePt),
        ("36pt##DalamudSettingsGlobalUiScaleReset36", 36f / InterfaceManager.DefaultFontSizePt),
    };

    private readonly List<(int FamilyIndex, int VariantIndex)> fontIndices = new();

    private readonly List<FontChainEntry> fontChainEntries = new();
    private float fontChainLineHeight = 1f;
    
    private float globalUiScale;
    private float fontGamma;

    private string scratch = string.Empty;

    private CancellationTokenSource? cancellationTokenSource;
    private Task<ParsedFontCollection> fontListTask = null!;

    public override SettingsEntry[] Entries { get; } =
    {
        new GapSettingsEntry(5, true),

        new ButtonSettingsEntry(
            Loc.Localize("DalamudSettingsOpenStyleEditor", "Open Style Editor"),
            Loc.Localize("DalamudSettingsStyleEditorHint", "Modify the look & feel of Dalamud windows."),
            () => Service<DalamudInterface>.Get().OpenStyleEditor()),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingsUseDarkMode", "Use Windows immersive/dark mode"),
            Loc.Localize("DalamudSettingsUseDarkModeHint",
                         "This will cause the FFXIV window title bar to follow your preferred Windows color settings, and switch to dark mode if enabled."),
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

        new HintSettingsEntry(Loc.Localize("DalamudSettingToggleUiHideOptOutNote",
                                           "Plugins may independently opt out of the settings below.")),
        new GapSettingsEntry(3),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleUiHide", "Hide plugin UI when the game UI is toggled off"),
            Loc.Localize("DalamudSettingToggleUiHideHint",
                         "Hide any open windows by plugins when toggling the game overlay."),
            c => c.ToggleUiHide,
            (v, c) => c.ToggleUiHide = v),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleUiHideDuringCutscenes", "Hide plugin UI during cutscenes"),
            Loc.Localize("DalamudSettingToggleUiHideDuringCutscenesHint",
                         "Hide any open windows by plugins during cutscenes."),
            c => c.ToggleUiHideDuringCutscenes,
            (v, c) => c.ToggleUiHideDuringCutscenes = v),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleUiHideDuringGpose", "Hide plugin UI while gpose is active"),
            Loc.Localize("DalamudSettingToggleUiHideDuringGposeHint",
                         "Hide any open windows by plugins while gpose is active."),
            c => c.ToggleUiHideDuringGpose,
            (v, c) => c.ToggleUiHideDuringGpose = v),

        new GapSettingsEntry(5, true),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleFocusManagement", "Use escape to close Dalamud windows"),
            Loc.Localize("DalamudSettingToggleFocusManagementHint",
                         "This will cause Dalamud windows to behave like in-game windows when pressing escape.\nThey will close one after another until all are closed. May not work for all plugins."),
            c => c.IsFocusManagementEnabled,
            (v, c) => c.IsFocusManagementEnabled = v),

        // This is applied every frame in InterfaceManager::CheckViewportState()
        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleViewports", "Enable multi-monitor windows"),
            Loc.Localize("DalamudSettingToggleViewportsHint",
                         "This will allow you move plugin windows onto other monitors.\nWill only work in Borderless Window or Windowed mode."),
            c => !c.IsDisableViewport,
            (v, c) => c.IsDisableViewport = !v),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleDocking", "Enable window docking"),
            Loc.Localize("DalamudSettingToggleDockingHint", "This will allow you to fuse and tab plugin windows."),
            c => c.IsDocking,
            (v, c) => c.IsDocking = v),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingEnablePluginUISoundEffects", "Enable sound effects for plugin windows"),
            Loc.Localize("DalamudSettingEnablePluginUISoundEffectsHint",
                         "This will allow you to enable or disable sound effects generated by plugin user interfaces.\nThis is affected by your in-game `System Sounds` volume settings."),
            c => c.EnablePluginUISoundEffects,
            (v, c) => c.EnablePluginUISoundEffects = v),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleGamepadNavigation", "Control plugins via gamepad"),
            Loc.Localize("DalamudSettingToggleGamepadNavigationHint",
                         "This will allow you to toggle between game and plugin navigation via L1+L3.\nToggle the PluginInstaller window via R3 if ImGui navigation is enabled."),
            c => c.IsGamepadNavigationEnabled,
            (v, c) => c.IsGamepadNavigationEnabled = v),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingToggleTsm", "Show title screen menu"),
            Loc.Localize("DalamudSettingToggleTsmHint",
                         "This will allow you to access certain Dalamud and Plugin functionality from the title screen."),
            c => c.ShowTsm,
            (v, c) => c.ShowTsm = v),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingInstallerOpenDefault",
                         "Open the Plugin Installer to the \"Installed Plugins\" tab by default"),
            Loc.Localize("DalamudSettingInstallerOpenDefaultHint",
                         "This will allow you to open the Plugin Installer to the \"Installed Plugins\" tab by default, instead of the \"Available Plugins\" tab."),
            c => c.PluginInstallerOpen == PluginInstallerWindow.PluginInstallerOpenKind.InstalledPlugins,
            (v, c) => c.PluginInstallerOpen =
                          v
                              ? PluginInstallerWindow.PluginInstallerOpenKind.InstalledPlugins
                              : PluginInstallerWindow.PluginInstallerOpenKind.AllPlugins),

        new GapSettingsEntry(5, true),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingEnsureSimplifiedChinese", "Ensure Simplified Chinese (简体字始终显示)"),
            Loc.Localize("DalamudSettingEnsureSimplifiedChineseHint",
                         "Use default Windows fonts to ensure that Simplified Chinese characters are displayed."),
            c => c.EnsureSimplifiedChineseCharacters,
            (v, c) => c.EnsureSimplifiedChineseCharacters = v),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingEnsureTraditionalChinese", "Ensure Traditional Chinese (繁體字始終顯示)"),
            Loc.Localize("DalamudSettingEnsureTraditionalChineseHint",
                         "Use default Windows fonts to ensure that Traditional Chinese characters are displayed."),
            c => c.EnsureTraditionalChineseCharacters,
            (v, c) => c.EnsureTraditionalChineseCharacters = v),

        new SettingsEntry<bool>(
            Loc.Localize("DalamudSettingEnsureKorean", "Ensure Korean (한글 항상 표시)"),
            Loc.Localize("DalamudSettingEnsureKoreanHint",
                         "Use default Windows fonts to ensure that Korean characters are displayed."),
            c => c.EnsureKoreanCharacters,
            (v, c) => c.EnsureKoreanCharacters = v),
    };

    public override string Title => Loc.Localize("DalamudSettingsVisual", "Look & Feel");

    private FontChain ChainBeingConfigured => new(this.fontChainEntries, this.fontChainLineHeight);

    public override void Draw()
    {
        var interfaceManager = Service<InterfaceManager>.Get();
        var pad = ImGui.GetStyle().FramePadding;

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + pad.Y);
        ImGui.Text(Loc.Localize("DalamudSettingsFontFamilyAndVariant", "Font Family and Variant"));
        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - pad.Y);
        ImGui.PushFont(InterfaceManager.MonoFont);
        if (ImGui.Button(Loc.Localize("DalamudSettingsIndividualConfigResetToDefaultValue", "Reset") +
                         "##DalamudSettingsFontFamilyAndVariantReset"))
        {
            this.fontChainEntries.Clear();
            this.fontChainEntries.AddRange(InterfaceManager.FontProperties.FallbackFontChain.Fonts);
            this.fontChainLineHeight = InterfaceManager.FontProperties.FallbackFontChain.LineHeight;
            this.fontIndices.Clear();
            interfaceManager.Font.FontChainOverride = InterfaceManager.FontProperties.FallbackFontChain;
            interfaceManager.RebuildFonts();
        }

        ImGui.PopFont();

        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - pad.Y);
        if (ImGui.Button(
                Loc.Localize("DalamudSettingsIndividualConfigRefresh", "Refresh") +
                "##DalamudSettingsFontFamilyAndVariantRefresh"))
        {
            this.RefreshFontList(true);
        }

        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - pad.Y);
        ImGui.InputTextWithHint(
            "##DalamudSettingsFontFamilyAndVariantScratchpad",
            Loc.Localize("DalamudSettingsFontFamilyAndVariantTestHereHint", "Test Here"),
            ref this.scratch,
            1024);

        switch (this.fontListTask.Status)
        {
            case TaskStatus.RanToCompletion:
            {
                const StringComparison ccic = StringComparison.CurrentCultureIgnoreCase;

                var fonts = this.fontListTask.Result;
                
                // sanity check
                if (!this.fontChainEntries.Any())
                    this.fontChainEntries.AddRange(InterfaceManager.FontProperties.FallbackFontChain.Fonts);

                var chainChanged = false;
                Debug.Assert(this.fontChainEntries.Count > 0, "this.fontChainEntries.Count > 0");
                foreach (var chainIndex in Enumerable.Range(0, this.fontChainEntries.Count + 1))
                {
                    if (chainIndex > 0)
                        ImGui.TextUnformatted("+");

                    if (this.fontIndices.Count <= chainIndex)
                        this.fontIndices.Add(new(-1, -1));

                    var (familyIndex, variantIndex) = this.fontIndices[chainIndex];
                    var chainItem = this.fontChainEntries.Count <= chainIndex
                                        ? default
                                        : this.fontChainEntries[chainIndex].Ident;

                    var familyOffset = chainIndex == 0 ? SpecialFontCount : 1;
                    var imguiNames = chainIndex == 0 ? fonts.NamesForImGui : fonts.OptionalNamesForImGui;

                    familyIndex = chainIndex switch
                    {
                        // Note: non-axis default game fonts are not supported in this settings page
                        0 when chainItem.Game is GameFontFamily.Axis => FontIndexAxis,
                        0 when chainItem.NotoSansJ => FontIndexNotoSans,
                        _ when string.IsNullOrWhiteSpace(chainItem.System?.Name) => 0,
                        _ => familyIndex,
                    };

                    if (chainItem.System?.Name is { } name)
                    {
                        if (familyIndex == -1)
                        {
                            familyIndex = fonts.Names.IndexOf(x => string.Equals(x, name, ccic));
                            if (familyIndex != -1)
                                familyIndex += familyOffset;
                        }

                        if (familyIndex == -1)
                        {
                            familyIndex = fonts.LocalizedNames
                                               .IndexOf(x => x.Any(y => string.Equals(y, name, ccic)));
                            if (familyIndex != -1)
                                familyIndex += familyOffset;
                        }
                    }

                    if (familyIndex == -1)
                        familyIndex = 0;

                    if (ImGui.Combo($"##DalamudSettingsFontFamilyCombo{chainIndex}", ref familyIndex, imguiNames))
                    {
                        chainChanged = true;
                        variantIndex = -1;
                    }

                    string variantNames;
                    FontIdent[] idents;
                    switch (familyIndex)
                    {
                        case FontIndexAxis when chainIndex == 0:
                            idents = new[] { new FontIdent(GameFontFamily.Axis) };
                            variantNames = "-\0";
                            variantIndex = 0;
                            break;
                        case FontIndexNotoSans when chainIndex == 0:
                            idents = new[] { new FontIdent(true) };
                            variantNames = "-\0";
                            variantIndex = 0;
                            break;
                        case var _ when familyIndex >= familyOffset:
                        {
                            var variant = chainItem.System?.Variant ?? default(FontVariant);
                            variantNames = fonts.VariantsForImGui[familyIndex - familyOffset];
                            idents = fonts.Variants[familyIndex - familyOffset];
                            if (variantIndex == -1 || variantIndex >= idents.Length)
                            {
                                variantIndex = idents
                                               .Select(x => x.System.GetValueOrDefault().Variant)
                                               .IndexOf(x => variant.Equals(x));
                            }

                            if (variantIndex == -1)
                            {
                                // Note: numeric operation on "FontStyle" makes no sense,
                                // but it has only 3 values and we want the lowest valid value of Normal = 0.
                                variantIndex =
                                    idents
                                        .Select(x => x.System.GetValueOrDefault().Variant)
                                        .Select((x, i) => (x, i))
                                        .OrderBy(x => Math.Abs((int)x.x.Weight - (int)FontWeight.Normal))
                                        .ThenBy(x => Math.Abs((int)x.x.Stretch - (int)FontStretch.Normal))
                                        .ThenBy(x => Math.Abs((int)x.x.Style - (int)FontStyle.Normal))
                                        .First()
                                        .i;
                            }
                            
                            break;
                        }

                        default:
                            idents = Array.Empty<FontIdent>();
                            variantNames = "\0";
                            variantIndex = 0;
                            break;
                    }

                    if (idents.Length < 2)
                        ImGui.BeginDisabled();
                    if (ImGui.Combo($"##DalamudSettingsFontVariantCombo{chainIndex}", ref variantIndex, variantNames))
                        chainChanged = true;
                    if (idents.Length < 2)
                        ImGui.EndDisabled();

                    if (chainIndex < this.fontChainEntries.Count)
                    {
                        this.fontChainEntries[chainIndex] = this.fontChainEntries[chainIndex] with
                        {
                            Ident = idents.ElementAtOrDefault(variantIndex),
                        };
                    }
                    else if (idents.ElementAtOrDefault(variantIndex) != default)
                    {
                        this.fontChainEntries.Add(new(idents[variantIndex], InterfaceManager.DefaultFontSizePx));
                    }
                    else
                    {
                        variantIndex = 0;
                    }

                    this.fontIndices[chainIndex] = (familyIndex, variantIndex);

                    if (chainIndex == 0 || familyIndex > 0)
                    {
                        var sizePt = MathF.Round(this.fontChainEntries[chainIndex].SizePx * 3 / 4 * 10) / 10;
                        if (ImGui.InputFloat($"Size##DalamudSettingsFontChainSize{chainIndex}", ref sizePt, 1f))
                        {
                            sizePt = Math.Clamp(6f, sizePt, 18f);
                            this.fontChainEntries[chainIndex] = this.fontChainEntries[chainIndex] with
                            {
                                SizePx = sizePt * 4 / 3,
                            };
                            chainChanged = true;
                        }

                        var letterSpacing = this.fontChainEntries[chainIndex].LetterSpacing;
                        if (ImGui.InputFloat($"Letter Spacing##DalamudSettingsFontChainLetterSpacing{chainIndex}", ref letterSpacing, 1f))
                        {
                            this.fontChainEntries[chainIndex] = this.fontChainEntries[chainIndex] with
                            {
                                LetterSpacing = letterSpacing,
                            };
                            chainChanged = true;
                        }

                        var offsetX = this.fontChainEntries[chainIndex].OffsetX;
                        if (ImGui.InputFloat($"Offset X##DalamudSettingsFontChainOffsetX{chainIndex}", ref offsetX, 1f))
                        {
                            this.fontChainEntries[chainIndex] = this.fontChainEntries[chainIndex] with
                            {
                                OffsetX = offsetX,
                            };
                            chainChanged = true;
                        }

                        var offsetY = this.fontChainEntries[chainIndex].OffsetY;
                        if (ImGui.InputFloat($"Offset Y##DalamudSettingsFontChainOffsetY{chainIndex}", ref offsetY, 1f))
                        {
                            this.fontChainEntries[chainIndex] = this.fontChainEntries[chainIndex] with
                            {
                                OffsetY = offsetY,
                            };
                            chainChanged = true;
                        }
                    }
                }

                for (var i = 1; i < this.fontChainEntries.Count; i++)
                {
                    if (this.fontChainEntries[i].Ident == default)
                    {
                        chainChanged = true;
                        this.fontChainEntries.RemoveAt(i);
                        this.fontIndices.RemoveAt(i);
                        --i;
                    }
                }

                if (chainChanged)
                {
                    interfaceManager.Font.FontChainOverride = this.ChainBeingConfigured;
                    interfaceManager.RebuildFonts();
                }

                break;
            }

            case TaskStatus.Canceled:
            case TaskStatus.Faulted:
            {
                // there probably is no point localizing this
                ImGui.TextUnformatted($"Failed to load font list: {this.fontListTask.Exception}");
                break;
            }

            default:
            {
                var zero = 0;
                ImGui.BeginDisabled();
                foreach (var chainIndex in Enumerable.Range(0, this.fontChainEntries.Count + 1))
                {
                    if (chainIndex > 0)
                        ImGui.TextUnformatted("+");

                    ImGui.Combo("##DalamudSettingsFontFamilyCombo", ref zero, Array.Empty<string>(), 0);
                    ImGui.Combo("##DalamudSettingsFontVariantCombo", ref zero, Array.Empty<string>(), 0);
                }

                ImGui.EndDisabled();
                break;
            }
        }

        if (interfaceManager.Font.CustomDefaultFontLoadFailed)
        {
            ImGuiHelpers.SafeTextColoredWrapped(
                ImGuiColors.DalamudRed,
                Loc.Localize(
                    "DalamudSettingsFontFamilyAndVariantLoadFailed",
                    "Cannot use the selected font. Choose another."));
        }

        ImGuiHelpers.SafeTextColoredWrapped(
            ImGuiColors.DalamudGrey,
            Loc.Localize(
                "DalamudSettingsFontFamilyAndVariantHint",
                "Adding more fonts will add characters missing from previous fonts.\nIf the characters required by the game are not present in the selected font, they will be pulled from Noto Sans."));

        ImGuiHelpers.ScaledDummy(5);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + pad.Y);
        ImGui.Text(Loc.Localize("DalamudSettingsDefaultLineHeight", "Line Height"));
        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - pad.Y);
        if (ImGui.Button(Loc.Localize("DalamudSettingsIndividualConfigResetToDefaultValue", "Reset") +
                         "##DalamudSettingsLineHeightReset")
            && Math.Abs(this.fontChainLineHeight - 1f) > float.Epsilon)
        {
            this.fontChainLineHeight = 1f;
            interfaceManager.Font.FontChainOverride = this.ChainBeingConfigured;
            interfaceManager.RebuildFonts();
        }

        var lineHeightPercent = (int)Math.Clamp(Math.Round(this.fontChainLineHeight * 100), 100, 200);
        if (ImGui.DragInt("##DalamudSettingsDefaultLineHeightDrag", ref lineHeightPercent, 10, 100, 200, "%d%%",
                            ImGuiSliderFlags.AlwaysClamp))
        {
            this.fontChainLineHeight = lineHeightPercent / 100f;
            interfaceManager.Font.FontChainOverride = this.ChainBeingConfigured;
            interfaceManager.RebuildFonts();
        }

        ImGuiHelpers.ScaledDummy(5);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + pad.Y);
        ImGui.Text(Loc.Localize("DalamudSettingsGlobalUiScale", "Global Font Scale"));

        var buttonSize = GlobalUiScalePresets
                         .Select(x => ImGui.CalcTextSize(x.Item1, 0, x.Item1.IndexOf('#')) + (pad * 2))
                         .Aggregate(Vector2.Zero, (a, b) => new(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
        foreach (var (buttonLabel, scale) in GlobalUiScalePresets)
        {
            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - pad.Y);
            if (ImGui.Button(buttonLabel, buttonSize) && Math.Abs(this.globalUiScale - scale) > float.Epsilon)
            {
                ImGui.GetIO().FontGlobalScale = this.globalUiScale = scale;
                interfaceManager.RebuildFonts();
            }
        }

        var globalUiScaleInPt = 12f * this.globalUiScale;
        if (ImGui.DragFloat("##DalamudSettingsGlobalUiScaleDrag", ref globalUiScaleInPt, 0.1f, 9.6f, 36f, "%.1fpt",
                            ImGuiSliderFlags.AlwaysClamp))
        {
            this.globalUiScale = globalUiScaleInPt / 12f;
            ImGui.GetIO().FontGlobalScale = this.globalUiScale;
            interfaceManager.RebuildFonts();
        }

        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey,
                                            Loc.Localize("DalamudSettingsGlobalUiScaleHint",
                                                         "Scale text in all XIVLauncher UI elements - this is useful for 4K displays."));

        ImGuiHelpers.ScaledDummy(5);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - pad.Y);
        ImGui.Text(Loc.Localize("DalamudSettingsFontGamma", "Font Gamma"));
        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - pad.Y);
        if (ImGui.Button(Loc.Localize("DalamudSettingsIndividualConfigResetToDefaultValue", "Reset") +
                         "##DalamudSettingsFontGammaReset"))
        {
            this.fontGamma = 1.4f;
            interfaceManager.Font.GammaOverride = this.fontGamma;
            interfaceManager.RebuildFonts();
        }

        if (ImGui.DragFloat("##DalamudSettingsFontGammaDrag", ref this.fontGamma, 0.005f, 0.3f, 3f, "%.2f",
                            ImGuiSliderFlags.AlwaysClamp))
        {
            interfaceManager.Font.GammaOverride = this.fontGamma;
            interfaceManager.RebuildFonts();
        }

        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey,
                                            Loc.Localize("DalamudSettingsFontGammaHint",
                                                         "Changes the thickness of text."));

        base.Draw();
    }

    public override void Load()
    {
        var dconf = Service<DalamudConfiguration>.Get();
        this.globalUiScale = dconf.GlobalUiScale;
        this.fontChainEntries.Clear();
        this.fontChainEntries.AddRange(dconf.DefaultFontChain.Fonts);
        this.fontChainLineHeight = dconf.DefaultFontChain.LineHeight;
        this.fontIndices.Clear();
        this.fontGamma = dconf.FontGammaLevel;

        base.Load();
    }

    public override void Save()
    {
        var dconf = Service<DalamudConfiguration>.Get();
        dconf.GlobalUiScale = this.globalUiScale;
        dconf.DefaultFontChain = this.ChainBeingConfigured;
        dconf.FontGammaLevel = this.fontGamma;
        base.Save();
    }

    public override void OnOpen()
    {
        this.RefreshFontList(false);
        base.OnOpen();
    }

    public override void OnClose()
    {
        this.cancellationTokenSource?.Cancel();
        base.OnClose();
    }

    private void RefreshFontList(bool refreshSystem)
    {
        this.cancellationTokenSource?.Cancel();
        this.cancellationTokenSource = new();
        var cancellationToken = this.cancellationTokenSource.Token;
        this.fontListTask =
            EasyFontUtils.GetSystemFontsAsync(refreshSystem: refreshSystem, cancellationToken: cancellationToken)
            .ContinueWith(
            r => new ParsedFontCollection(
                r.Result.Select(x => x.Name).ToArray(),
                r.Result.Select(x => x.LocalizedNames.Values.ToArray()).ToArray(),
                r.Result.Select(x => x.Variants).ToArray()),
            this.cancellationTokenSource.Token);
    }
    
    private class ParsedFontCollection
    {
        public ParsedFontCollection(string[] names, string[][] localizedNames, FontIdent[][] variants)
        {
            this.Names = names;
            this.LocalizedNames = localizedNames;
            this.Variants = variants;
            this.NamesForImGui = "(AXIS)\0(Noto Sans JP)\0" + string.Join("\0", this.Names) + "\0";
            this.OptionalNamesForImGui = 
                Loc.Localize(
                    "DalamudSettingsFontFamilyAndVariantChooseMore",
                    "(choose a font, if additional scripts are desired)")
                + "\0" + string.Join("\0", this.Names) + "\0";
            this.VariantsForImGui =
                variants.Select(x => string.Join(
                                         "\0",
                                         x.Select(y => y.System.GetValueOrDefault().Variant.ToStringLocalized())) + "\0")
                        .ToArray();
        }

        public string[] Names { get; }

        public string[][] LocalizedNames { get; }

        public FontIdent[][] Variants { get; }

        public string NamesForImGui { get; }

        public string OptionalNamesForImGui { get; }

        public string[] VariantsForImGui { get; }
    }
}
