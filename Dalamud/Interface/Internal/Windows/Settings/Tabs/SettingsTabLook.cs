using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Windows.PluginInstaller;
using Dalamud.Interface.Internal.Windows.Settings.Widgets;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using ImGuiNET;
using Serilog;
using SharpDX.DirectWrite;

using FontStretch = SharpDX.DirectWrite.FontStretch;
using FontStyle = SharpDX.DirectWrite.FontStyle;
using FontWeight = SharpDX.DirectWrite.FontWeight;

namespace Dalamud.Interface.Internal.Windows.Settings.Tabs;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented",
                 Justification = "Internals")]
public class SettingsTabLook : SettingsTab
{
    private const int FontIndexAxis = 0;
    private const int FontIndexNotoSans = 1;
    private const int SpecialFontCount = 2;

    private readonly List<(int FamilyIndex, int VariantIndex)> fontIndices = new();
    private List<FontFamilyAndVariant> fontChain = null!;
    private bool useAxis;
    private float globalUiScale;
    private float fontGamma;

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

    public override void Draw()
    {
        var interfaceManager = Service<InterfaceManager>.Get();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);
        ImGui.Text(Loc.Localize("DalamudSettingsFontFamilyAndVariant", "Font Family and Variant"));
        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3);
        ImGui.PushFont(InterfaceManager.MonoFont);
        if (ImGui.Button(Loc.Localize("DalamudSettingsIndividualConfigResetToDefaultValue", "Reset") +
                         "##DalamudSettingsFontFamilyAndVariantReset"))
        {
            this.useAxis = true;
            this.fontChain.Clear();
            this.fontIndices.Clear();
            interfaceManager.Font.UseAxisOverride = true;
            interfaceManager.Font.FontChainOverride = this.fontChain;
            interfaceManager.RebuildFonts();
        }

        ImGui.PopFont();

        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3);
        if (ImGui.Button(
                Loc.Localize("DalamudSettingsIndividualConfigRefresh", "Refresh") +
                "##DalamudSettingsFontFamilyAndVariantRefresh"))
        {
            this.RefreshFontList(true);
        }

        switch (this.fontListTask.Status)
        {
            case TaskStatus.RanToCompletion:
            {
                const StringComparison ccic = StringComparison.CurrentCultureIgnoreCase;

                var fonts = this.fontListTask.Result;
                
                // sanity check
                if (!this.fontChain.Any())
                    this.fontChain.Add(FontFamilyAndVariant.Empty);

                var chainChanged = false;
                foreach (var chainIndex in Enumerable.Range(0, this.fontChain.Count + 1))
                {
                    if (chainIndex > 0)
                        ImGui.TextUnformatted("+");

                    if (this.fontIndices.Count <= chainIndex)
                        this.fontIndices.Add(new(-1, -1));

                    var (familyIndex, variantIndex) = this.fontIndices[chainIndex];
                    var chainItem = this.fontChain.Count <= chainIndex
                                        ? FontFamilyAndVariant.Empty
                                        : this.fontChain[chainIndex];

                    var familyOffset = chainIndex == 0 ? SpecialFontCount : 1;
                    var imguiNames = chainIndex == 0 ? fonts.NamesForImGui : fonts.OptionalNamesForImGui;

                    familyIndex = chainIndex switch
                    {
                        0 when this.useAxis => FontIndexAxis,
                        0 when string.IsNullOrWhiteSpace(chainItem.Name) => FontIndexNotoSans,
                        > 0 when string.IsNullOrWhiteSpace(chainItem.Name) => 0,
                        _ => familyIndex,
                    };

                    if (familyIndex == -1)
                    {
                        familyIndex = fonts.Names.IndexOf(x => string.Equals(x, chainItem.Name, ccic));
                        if (familyIndex != -1)
                            familyIndex += familyOffset;
                    }

                    if (familyIndex == -1)
                    {
                        familyIndex = fonts.LocalizedNames
                                           .IndexOf(x => x.Any(y => string.Equals(y, chainItem.Name, ccic)));
                        if (familyIndex != -1)
                            familyIndex += familyOffset;
                    }

                    if (familyIndex == -1)
                        familyIndex = 0;

                    if (ImGui.Combo($"##DalamudSettingsFontFamilyCombo{chainIndex}", ref familyIndex, imguiNames))
                    {
                        chainChanged = true;
                        variantIndex = -1;
                    }

                    string variantNames;
                    FontFamilyAndVariant[] variants;
                    if (familyIndex >= familyOffset)
                    {
                        variantNames = fonts.VariantsForImGui[familyIndex - familyOffset];
                        variants = fonts.Variants[familyIndex - familyOffset];
                        if (variantIndex == -1 || variantIndex >= variants.Length)
                            variantIndex = variants.IndexOf(x => chainItem.VariantEquals(x));
                        if (variantIndex == -1)
                        {
                            // Note: numeric operation on "FontStyle" makes no sense, but it has only 3 values and we want
                            //       the lowest valid value of Normal = 0.
                            variantIndex =
                                variants
                                    .Select((x, i) => (x, i))
                                    .OrderBy(x => Math.Abs((int)x.x.Weight - (int)FontWeight.Normal))
                                    .ThenBy(x => Math.Abs((int)x.x.Stretch - (int)FontStretch.Normal))
                                    .ThenBy(x => Math.Abs((int)x.x.Style - (int)FontStyle.Normal))
                                    .First()
                                    .i;
                        }
                    }
                    else
                    {
                        variantNames = "-";
                        variants = new[] { FontFamilyAndVariant.Empty };
                        variantIndex = 0;
                    }

                    if (variants.Length < 2)
                        ImGui.BeginDisabled();
                    if (ImGui.Combo($"##DalamudSettingsFontVariantCombo{chainIndex}", ref variantIndex, variantNames))
                        chainChanged = true;
                    if (variants.Length < 2)
                        ImGui.EndDisabled();

                    if (chainIndex < this.fontChain.Count)
                        this.fontChain[chainIndex] = variants[variantIndex];
                    else if (variants[variantIndex] != FontFamilyAndVariant.Empty)
                        this.fontChain.Add(variants[variantIndex]);
                    else
                        Debug.Assert(chainIndex == this.fontChain.Count, "chainIndex == this.fontChain.Count");
                    this.fontIndices[chainIndex] = (familyIndex, variantIndex);
                }

                this.useAxis = this.fontIndices[0].FamilyIndex == FontIndexAxis;
                for (var i = 1; i < this.fontChain.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(this.fontChain[i].Name))
                    {
                        chainChanged = true;
                        this.fontChain.RemoveAt(i);
                        this.fontIndices.RemoveAt(i);
                        --i;
                    }
                }

                if (chainChanged)
                {
                    // TODO: override
                    interfaceManager.Font.FontChainOverride = this.fontChain;
                    interfaceManager.Font.UseAxisOverride = this.useAxis;
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
                foreach (var chainIndex in Enumerable.Range(0, this.fontChain.Count + 1))
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
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3);
        if (ImGui.Button("12pt##DalamudSettingsGlobalUiScaleReset12"))
        {
            this.globalUiScale = 1.0f;
            ImGui.GetIO().FontGlobalScale = this.globalUiScale;
            interfaceManager.RebuildFonts();
        }

        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3);
        if (ImGui.Button("14pt##DalamudSettingsGlobalUiScaleReset14"))
        {
            this.globalUiScale = 14.0f / 12.0f;
            ImGui.GetIO().FontGlobalScale = this.globalUiScale;
            interfaceManager.RebuildFonts();
        }

        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3);
        if (ImGui.Button("18pt##DalamudSettingsGlobalUiScaleReset18"))
        {
            this.globalUiScale = 18.0f / 12.0f;
            ImGui.GetIO().FontGlobalScale = this.globalUiScale;
            interfaceManager.RebuildFonts();
        }

        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3);
        if (ImGui.Button("24pt##DalamudSettingsGlobalUiScaleReset24"))
        {
            this.globalUiScale = 24.0f / 12.0f;
            ImGui.GetIO().FontGlobalScale = this.globalUiScale;
            interfaceManager.RebuildFonts();
        }

        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3);
        if (ImGui.Button("36pt##DalamudSettingsGlobalUiScaleReset36"))
        {
            this.globalUiScale = 36.0f / 12.0f;
            ImGui.GetIO().FontGlobalScale = this.globalUiScale;
            interfaceManager.RebuildFonts();
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

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);
        ImGui.Text(Loc.Localize("DalamudSettingsFontGamma", "Font Gamma"));
        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3);
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
        this.fontChain = dconf.DefaultFontChain.ToList(); // cloning
        this.fontIndices.Clear();
        this.fontGamma = dconf.FontGammaLevel;
        this.useAxis = dconf.UseAxisFontsFromGame;

        base.Load();
    }

    public override void Save()
    {
        var dconf = Service<DalamudConfiguration>.Get();
        dconf.GlobalUiScale = this.globalUiScale;
        dconf.DefaultFontChain = this.fontChain.ToList(); // cloning
        dconf.FontGammaLevel = this.fontGamma;
        dconf.UseAxisFontsFromGame = this.useAxis;
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
        this.fontListTask = Task.Run(
            () =>
            {
                var names = new List<string>();
                var localizedNames = new List<string[]>();
                var variants = new List<FontFamilyAndVariant[]>();
                var tempLocalizedNames = new List<string>();
                var tempVariantNames = new List<string>();
                var tempVariants = new List<FontFamilyAndVariant>();

                using var factory = new SharpDX.DirectWrite.Factory();
                using var collection = factory.GetSystemFontCollection(refreshSystem);

                names.EnsureCapacity(collection.FontFamilyCount);
                localizedNames.EnsureCapacity(collection.FontFamilyCount);
                variants.EnsureCapacity(collection.FontFamilyCount);

                var languageNamePrefixes = new[] { string.Empty, "en", string.Empty };
                var languageNames = new List<string>();
                foreach (var familyIndex in Enumerable.Range(0, collection.FontFamilyCount))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using var family = collection.GetFontFamily(familyIndex);
                    if (family.FontCount == 0)
                        continue;

                    using var familyNames = family.FamilyNames;
                    tempLocalizedNames.EnsureCapacity(familyNames.Count);
                    tempLocalizedNames.Clear();
                    tempLocalizedNames.AddRange(
                        Enumerable.Range(0, familyNames.Count)
                                  .Select(x => familyNames.GetString(x)));
                    languageNames.EnsureCapacity(familyNames.Count);
                    languageNames.Clear();
                    languageNames.AddRange(
                        Enumerable.Range(0, familyNames.Count)
                                  .Select(x => familyNames.GetLocaleName(x).ToLowerInvariant()));

                    languageNamePrefixes[0] = Service<DalamudConfiguration>.Get().EffectiveLanguage.ToLowerInvariant();
                    string? name = null;
                    foreach (var languageNamePrefix in languageNamePrefixes)
                    {
                        var localeNameIndex = languageNames.IndexOf(x => x.StartsWith(languageNamePrefix));
                        if (localeNameIndex != -1)
                        {
                            name = tempLocalizedNames[localeNameIndex];
                            break;
                        }
                    }

                    if (name is null)
                        continue;

                    tempVariantNames.Clear();
                    tempVariants.Clear();
                    tempVariantNames.EnsureCapacity(family.FontCount);
                    tempVariants.EnsureCapacity(family.FontCount);
                    foreach (var fontIndex in Enumerable.Range(0, family.FontCount))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        using var font = family.GetFont(fontIndex);

                        // we can't handle faux italic/bold at the moment; skip them
                        if (font.Simulations != FontSimulations.None)
                            continue;

                        // imgui crashes on some fonts; following is NOT an exhaustive check of causes
                        if (!font.HasCharacter('A') || !font.HasCharacter('0') || !font.HasCharacter('?'))
                            continue;

                        tempVariants.Add(new(name, font.Weight, font.Stretch, font.Style));
                        tempVariantNames.Add(tempVariants.Last().GetLocalizedVariantDescription());
                    }

                    if (!tempVariants.Any())
                        continue;

                    names.Add(name);
                    localizedNames.Add(tempLocalizedNames.ToArray());
                    variants.Add(tempVariants.ToArray());
                }

                var newFontFamilyIndices =
                    names
                        .Select((x, i) => (x, i))
                        .OrderBy(x => x.x, StringComparer.CurrentCultureIgnoreCase)
                        .Select(x => x.i)
                        .ToArray();

                return new ParsedFontCollection(
                           newFontFamilyIndices.Select(x => names[x]).ToArray(),
                           newFontFamilyIndices.Select(x => localizedNames[x]).ToArray(),
                           newFontFamilyIndices.Select(x => variants[x]).ToArray());
            },
            this.cancellationTokenSource.Token);
    }
    
    private class ParsedFontCollection
    {
        public ParsedFontCollection(string[] names, string[][] localizedNames, FontFamilyAndVariant[][] variants)
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
                variants.Select(x => string.Join("\0", x.Select(y => y.GetLocalizedVariantDescription())) + "\0")
                        .ToArray();
        }

        public string[] Names { get; }

        public string[][] LocalizedNames { get; }

        public FontFamilyAndVariant[][] Variants { get; }

        public string NamesForImGui { get; }

        public string OptionalNamesForImGui { get; }

        public string[] VariantsForImGui { get; }
    }
}
