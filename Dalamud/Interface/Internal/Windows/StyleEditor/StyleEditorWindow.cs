using System.Linq;
using System.Numerics;
using System.Reflection;

using CheapLoc;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Game.Player;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.DesignSystem;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;

using Serilog;

namespace Dalamud.Interface.Internal.Windows.StyleEditor;

/// <summary>
/// Window for the Dalamud style editor.
/// </summary>
public class StyleEditorWindow : Window
{
    private ImGuiColorEditFlags alphaFlags = ImGuiColorEditFlags.AlphaPreviewHalf;

    private int currentSel = 0;
    private string initialStyle = string.Empty;
    private bool didSave = false;
    private bool anyChanges = false;

    private string renameText = string.Empty;
    private bool renameModalDrawing = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="StyleEditorWindow"/> class.
    /// </summary>
    public StyleEditorWindow()
        : base("Dalamud Style Editor")
    {
        this.IsOpen = true;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(890, 560),
        };
    }

    /// <inheritdoc />
    public override void OnOpen()
    {
        this.didSave = false;

        var config = Service<DalamudConfiguration>.Get();
        config.SavedStyles ??= [];
        this.currentSel = config.SavedStyles.FindIndex(x => x.Name == config.ChosenStyle);

        this.initialStyle = config.ChosenStyle;

        base.OnOpen();
    }

    /// <inheritdoc />
    public override void OnClose()
    {
        if (!this.didSave)
        {
            var config = Service<DalamudConfiguration>.Get();
            var newStyle = config.SavedStyles.FirstOrDefault(x => x.Name == this.initialStyle);
            newStyle?.Apply();
            if (this.anyChanges)
            {
                Service<InterfaceManager>.Get().InvokeStyleChanged();
            }
        }

        base.OnClose();
    }

    /// <inheritdoc />
    public override void Draw()
    {
        var config = Service<DalamudConfiguration>.Get();
        var renameModalTitle = Loc.Localize("RenameStyleModalTitle", "Rename Style");

        var workStyle = config.SavedStyles[this.currentSel];
        workStyle.BuiltInColors ??= StyleModelV1.DalamudStandard.BuiltInColors;

        var isBuiltinStyle = this.currentSel < 3;
        var appliedThisFrame = false;

        var styleAry = config.SavedStyles.Select(x => x.Name).ToArray();
        ImGui.Text(Loc.Localize("StyleEditorChooseStyle", "Choose Style:"));
        if (ImGui.Combo("###styleChooserCombo", ref this.currentSel, styleAry))
        {
            var newStyle = config.SavedStyles[this.currentSel];
            newStyle.Apply();
            this.Change();
            appliedThisFrame = true;
        }

        ImGui.SameLine();
        ImGuiHelpers.ScaledDummy(10);
        ImGui.SameLine();

        if (ImGui.Button(Loc.Localize("StyleEditorAddNew", "Add new style")))
        {
            this.SaveStyle();

            var newStyle = StyleModelV1.Get();
            newStyle.Name = Util.GetRandomName();
            config.SavedStyles.Add(newStyle);

            this.currentSel = config.SavedStyles.Count - 1;

            newStyle.Apply();
            this.Change();
            appliedThisFrame = true;

            config.QueueSave();
        }

        ImGui.SameLine();

        if (isBuiltinStyle)
            ImGui.BeginDisabled();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash) && this.currentSel != 0)
        {
            var deletingStyle = config.SavedStyles[this.currentSel];
            var deletingChosenStyle = config.ChosenStyle == deletingStyle.Name;

            // Reset assignments
            foreach (var assignment in config.CharacterStyleAssignments.Where(a => a.StyleName == deletingStyle.Name))
                assignment.StyleName = null;

            this.currentSel--;
            var newStyle = config.SavedStyles[this.currentSel];
            newStyle.Apply();
            this.Change();
            appliedThisFrame = true;

            config.SavedStyles.RemoveAt(this.currentSel + 1);

            if (deletingChosenStyle)
                config.ChosenStyle = newStyle.Name;

            config.QueueSave();
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Loc.Localize("StyleEditorDeleteStyle", "Delete current style"));

        ImGui.SameLine();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Pen) && this.currentSel != 0)
        {
            var newStyle = config.SavedStyles[this.currentSel];
            this.renameText = newStyle.Name;

            this.renameModalDrawing = true;
            ImGui.OpenPopup(renameModalTitle);
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Loc.Localize("StyleEditorRenameStyle", "Rename style"));

        ImGui.SameLine();

        ImGuiHelpers.ScaledDummy(5);
        ImGui.SameLine();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.FileExport))
        {
            var selectedStyle = config.SavedStyles[this.currentSel];
            var newStyle = StyleModelV1.Get();
            newStyle.Name = selectedStyle.Name;
            ImGui.SetClipboardText(newStyle.Serialize());
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Loc.Localize("StyleEditorCopy", "Copy style to clipboard for sharing"));

        if (isBuiltinStyle)
            ImGui.EndDisabled();

        ImGui.SameLine();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.FileImport))
        {
            this.SaveStyle();

            var styleJson = ImGui.GetClipboardText();

            try
            {
                var newStyle = StyleModel.Deserialize(styleJson);

                newStyle.Name ??= Util.GetRandomName();

                if (config.SavedStyles.Any(x => x.Name == newStyle.Name))
                {
                    newStyle.Name = $"{newStyle.Name} ({Util.GetRandomName()} Mix)";
                }

                config.SavedStyles.Add(newStyle);
                newStyle.Apply();
                this.Change();
                appliedThisFrame = true;

                this.currentSel = config.SavedStyles.Count - 1;

                config.QueueSave();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not import style");
            }
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Loc.Localize("StyleEditorImport", "Import style from clipboard"));

        ImGui.Separator();

        ImGui.PushItemWidth(ImGui.GetWindowWidth() * 0.50f);

        if (appliedThisFrame)
        {
            ImGui.Text(Loc.Localize("StyleEditorApplying", "Applying style..."));
        }
        else if (ImGui.BeginTabBar("StyleEditorTabs"u8))
        {
            var style = ImGui.GetStyle();
            var changes = false;
            if (ImGui.BeginTabItem(Loc.Localize("StyleEditorVariables", "Variables")))
            {
                this.DrawBuiltinWarning(isBuiltinStyle);
                using var disabled = ImRaii.Disabled(isBuiltinStyle);
                if (ImGui.BeginChild($"ScrollingVars", ImGuiHelpers.ScaledVector2(0, 0), true, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoBackground))
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5);

                    changes |= ImGui.SliderFloat2("WindowPadding", ref style.WindowPadding, 0.0f, 20.0f, "%.0f");
                    changes |= ImGui.SliderFloat2("FramePadding", ref style.FramePadding, 0.0f, 20.0f, "%.0f");
                    changes |= ImGui.SliderFloat2("CellPadding", ref style.CellPadding, 0.0f, 20.0f, "%.0f");
                    changes |= ImGui.SliderFloat2("ItemSpacing", ref style.ItemSpacing, 0.0f, 20.0f, "%.0f");
                    changes |= ImGui.SliderFloat2("ItemInnerSpacing", ref style.ItemInnerSpacing, 0.0f, 20.0f, "%.0f");
                    changes |= ImGui.SliderFloat2("TouchExtraPadding", ref style.TouchExtraPadding, 0.0f, 10.0f, "%.0f");
                    changes |= ImGui.SliderFloat("IndentSpacing"u8, ref style.IndentSpacing, 0.0f, 30.0f, "%.0f"u8);
                    changes |= ImGui.SliderFloat("ScrollbarSize"u8, ref style.ScrollbarSize, 1.0f, 20.0f, "%.0f"u8);
                    changes |= ImGui.SliderFloat("GrabMinSize"u8, ref style.GrabMinSize, 1.0f, 20.0f, "%.0f"u8);
                    ImGui.Text("Borders"u8);
                    changes |= ImGui.SliderFloat("WindowBorderSize"u8, ref style.WindowBorderSize, 0.0f, 1.0f, "%.0f"u8);
                    changes |= ImGui.SliderFloat("ChildBorderSize"u8, ref style.ChildBorderSize, 0.0f, 1.0f, "%.0f"u8);
                    changes |= ImGui.SliderFloat("PopupBorderSize"u8, ref style.PopupBorderSize, 0.0f, 1.0f, "%.0f"u8);
                    changes |= ImGui.SliderFloat("FrameBorderSize"u8, ref style.FrameBorderSize, 0.0f, 1.0f, "%.0f"u8);
                    changes |= ImGui.SliderFloat("TabBorderSize"u8, ref style.TabBorderSize, 0.0f, 1.0f, "%.0f"u8);
                    ImGui.Text("Rounding"u8);
                    changes |= ImGui.SliderFloat("WindowRounding"u8, ref style.WindowRounding, 0.0f, 12.0f, "%.0f"u8);
                    changes |= ImGui.SliderFloat("ChildRounding"u8, ref style.ChildRounding, 0.0f, 12.0f, "%.0f"u8);
                    changes |= ImGui.SliderFloat("FrameRounding"u8, ref style.FrameRounding, 0.0f, 12.0f, "%.0f"u8);
                    changes |= ImGui.SliderFloat("PopupRounding"u8, ref style.PopupRounding, 0.0f, 12.0f, "%.0f"u8);
                    changes |= ImGui.SliderFloat("ScrollbarRounding"u8, ref style.ScrollbarRounding, 0.0f, 12.0f, "%.0f"u8);
                    changes |= ImGui.SliderFloat("GrabRounding"u8, ref style.GrabRounding, 0.0f, 12.0f, "%.0f"u8);
                    changes |= ImGui.SliderFloat("LogSliderDeadzone"u8, ref style.LogSliderDeadzone, 0.0f, 12.0f, "%.0f"u8);
                    changes |= ImGui.SliderFloat("TabRounding"u8, ref style.TabRounding, 0.0f, 12.0f, "%.0f"u8);
                    ImGui.Text("Alignment"u8);
                    changes |= ImGui.SliderFloat2("WindowTitleAlign", ref style.WindowTitleAlign, 0.0f, 1.0f, "%.2f");
                    var windowMenuButtonPosition = (int)style.WindowMenuButtonPosition + 1;
                    if (ImGui.Combo("WindowMenuButtonPosition"u8, ref windowMenuButtonPosition, ["None", "Left", "Right"]))
                    {
                        style.WindowMenuButtonPosition = (ImGuiDir)(windowMenuButtonPosition - 1);
                        changes = true;
                    }

                    changes |= ImGui.SliderFloat2("ButtonTextAlign", ref style.ButtonTextAlign, 0.0f, 1.0f, "%.2f");
                    ImGui.SameLine();
                    ImGuiComponents.HelpMarker("Alignment applies when a button is larger than its text content.");
                    changes |= ImGui.SliderFloat2("SelectableTextAlign", ref style.SelectableTextAlign, 0.0f, 1.0f, "%.2f");
                    ImGui.SameLine();
                    ImGuiComponents.HelpMarker("Alignment applies when a selectable is larger than its text content.");
                    changes |= ImGui.SliderFloat2("DisplaySafeAreaPadding", ref style.DisplaySafeAreaPadding, 0.0f, 30.0f, "%.0f");
                    ImGui.SameLine();
                    ImGuiComponents.HelpMarker(
                        "Adjust if you cannot see the edges of your screen (e.g. on a TV where scaling has not been configured).");

                    ImGui.EndChild();
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.Localize("StyleEditorColors", "Colors")))
            {
                this.DrawBuiltinWarning(isBuiltinStyle);
                using var disabled = ImRaii.Disabled(isBuiltinStyle);
                if (ImGui.BeginChild("ScrollingColors"u8, ImGuiHelpers.ScaledVector2(0, 0), true, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoBackground))
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5);

                    if (ImGui.RadioButton("Opaque"u8, this.alphaFlags == ImGuiColorEditFlags.None))
                        this.alphaFlags = ImGuiColorEditFlags.None;
                    ImGui.SameLine();
                    if (ImGui.RadioButton("Alpha"u8, this.alphaFlags == ImGuiColorEditFlags.AlphaPreview))
                        this.alphaFlags = ImGuiColorEditFlags.AlphaPreview;
                    ImGui.SameLine();
                    if (ImGui.RadioButton("Both"u8, this.alphaFlags == ImGuiColorEditFlags.AlphaPreviewHalf))
                        this.alphaFlags = ImGuiColorEditFlags.AlphaPreviewHalf;
                    ImGui.SameLine();

                    ImGuiComponents.HelpMarker(
                        "In the color list:\n" +
                        "Left-click on color square to open color picker,\n" +
                        "Right-click to open edit options menu.");

                    foreach (var imGuiCol in Enum.GetValues<ImGuiCol>())
                    {
                        if (imGuiCol == ImGuiCol.Count)
                            continue;

                        ImGui.PushID(imGuiCol.ToString());

                        changes |= ImGui.ColorEdit4("##color", ref style.Colors[(int)imGuiCol], ImGuiColorEditFlags.AlphaBar | this.alphaFlags);

                        ImGui.SameLine(0.0f, style.ItemInnerSpacing.X);
                        ImGui.Text(imGuiCol.ToString());

                        ImGui.PopID();
                    }

                    ImGui.Separator();

                    foreach (var property in typeof(DalamudColors).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        ImGui.PushID(property.Name);

                        var colorVal = property.GetValue(workStyle.BuiltInColors);
                        if (colorVal == null)
                        {
                            colorVal = property.GetValue(StyleModelV1.DalamudStandard.BuiltInColors);
                            property.SetValue(workStyle.BuiltInColors, colorVal);
                        }

                        var color = (Vector4)colorVal;

                        if (ImGui.ColorEdit4("##color", ref color, ImGuiColorEditFlags.AlphaBar | this.alphaFlags))
                        {
                            property.SetValue(workStyle.BuiltInColors, color);
                            workStyle.BuiltInColors?.Apply();
                            changes = true;
                        }

                        ImGui.SameLine(0.0f, style.ItemInnerSpacing.X);
                        ImGui.Text(property.Name);

                        ImGui.PopID();
                    }

                    ImGui.EndChild();
                }

                ImGui.EndTabItem();
            }

            if (workStyle is StyleModelV1 workStyleV1 && ImGui.BeginTabItem(Loc.Localize("StyleEditorBlur", "Blur")))
            {
                this.DrawBuiltinWarning(isBuiltinStyle);
                using var disabledBlur = ImRaii.Disabled(isBuiltinStyle);
                ImGui.TextWrapped(Loc.Localize("StyleEditorWindowBgBlur", "Background Blur strength"));

                var v = workStyleV1.WindowBlurStrength * 100f;
                if (ImGui.SliderFloat($"###blurStrength", ref v, 0f, 100f, "%.1f%%"))
                {
                    workStyleV1.WindowBlurStrength = v / 100f;
                    WindowSystem.DefaultBackgroundBlurStrength = workStyleV1.WindowBlurStrength;
                    changes = true;
                }

                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
                ImGui.TextWrapped(
                    Loc.Localize(
                        "DalamudSettingBackgroundBlurHint",
                        "This will allow you to set the strength of the blur effect for plugin windows.\n" +
                        "Set to 0% to disable the blur effect. This may not be supported by all of your plugins. Contact the plugin author if you want them to support this feature."));
                ImGui.PopStyleColor();

                ImGuiHelpers.ScaledDummy(5);

                ImGui.TextWrapped(Loc.Localize("StyleEditorWindowBgBlurTint", "Background Blur Tint"));
                var tint = workStyleV1.WindowBlurTint;
                if (ImGui.ColorEdit4(
                        $"###blurTint",
                        ref tint,
                        ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf))
                {
                    workStyleV1.WindowBlurTint = tint;
                    WindowSystem.DefaultBackgroundBlurTint = workStyleV1.WindowBlurTint;
                }

                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
                ImGui.TextWrapped(
                    Loc.Localize(
                        "DalamudSettingBackgroundBlurTintHint",
                        "This will allow you to tint the background blur for inactive windows."));
                ImGui.PopStyleColor();

                ImGuiHelpers.ScaledDummy(5);

                ImGui.TextWrapped(Loc.Localize("StyleEditorWindowBgBlurTint", "Background Blur Tint (Active Window)"));
                var tintActive = workStyleV1.WindowBlurTintActive;
                if (ImGui.ColorEdit4(
                        $"###blurTintActive",
                        ref tintActive,
                        ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf))
                {
                    workStyleV1.WindowBlurTintActive = tintActive;
                    WindowSystem.DefaultBackgroundBlurTintActive = workStyleV1.WindowBlurTintActive;
                }

                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
                ImGui.TextWrapped(
                    Loc.Localize(
                        "DalamudSettingBackgroundBlurTintActiveHint",
                        "This will allow you to tint the background blur for active windows."));
                ImGui.PopStyleColor();

                ImGuiHelpers.ScaledDummy(5);

                ImGui.TextWrapped(Loc.Localize("StyleEditorWindowBgBlurTint", "Background Blur Luminosity"));
                var luminosity = workStyleV1.WindowBlurLuminosity;
                if (ImGui.ColorEdit4(
                        $"###blurLuminosity",
                        ref luminosity,
                        ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf))
                {
                    workStyleV1.WindowBlurLuminosity = luminosity;
                    WindowSystem.DefaultBackgroundBlurLuminosity = workStyleV1.WindowBlurLuminosity;
                }

                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
                ImGui.TextWrapped(
                    Loc.Localize(
                        "DalamudSettingBackgroundBlurLuminosityHint",
                        "Luminosity target color (RGB) and luminosity blend strength (Alpha).\n" +
                        "Reduces contrast of the blurred background by replacing the blurred image's lightness with the target colors' lightness (keeping the blurred background's hue and saturation)."));
                ImGui.PopStyleColor();

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.Localize("StyleEditorCharacterAssignments", "Character Assignments")))
            {
                this.DrawCharacterAssignmentsTab(config);
                ImGui.EndTabItem();
            }

            if (changes)
            {
                this.Change();
            }

            ImGui.EndTabBar();
        }

        ImGui.PopItemWidth();

        if (DalamudComponents.DrawFloatingSaveDiscardButtons(out var saveClicked))
            this.IsOpen = false;

        if (saveClicked)
        {
            this.SaveStyle();

            config.ChosenStyle = config.SavedStyles[this.currentSel].Name;
            Log.Verbose("ChosenStyle = {ChosenStyle}", config.ChosenStyle);

            this.didSave = true;
        }

        if (ImGui.BeginPopupModal(renameModalTitle, ref this.renameModalDrawing, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.Text(Loc.Localize("StyleEditorEnterName", "Please enter the new name for this style."));
            ImGui.Spacing();

            ImGui.InputText("###renameModalInput"u8, ref this.renameText, 255);

            const float buttonWidth = 120f;
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - buttonWidth) / 2);

            if (ImGui.Button("OK"u8, new Vector2(buttonWidth, 40)))
            {
                config.SavedStyles[this.currentSel].Name = this.renameText;
                config.QueueSave();

                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private void DrawBuiltinWarning(bool isBuiltinStyle)
    {
        if (!isBuiltinStyle)
            return;

        ImGui.TextColored(ImGuiColors.AttentionForeground, Loc.Localize("StyleEditorNotAllowed", "You cannot edit built-in styles. Please add a new style first."));
        ImGuiHelpers.ScaledDummy(3);
    }

    private void DrawCharacterAssignmentsTab(DalamudConfiguration config)
    {
        ImGui.TextWrapped(Loc.Localize(
            "StyleEditorCharacterAssignmentsHint",
            "Assign a style to each character. When that character logs in, the assigned style will be applied automatically."));
        ImGuiHelpers.ScaledDummy(5);

        var styleNames = config.SavedStyles?.Select(x => x.Name).ToArray() ?? [];

        var comboItems = new string[styleNames.Length + 1];
        comboItems[0] = Loc.Localize("StyleEditorCharacterAssignmentsLastSelected", "Last selected");
        for (var i = 0; i < styleNames.Length; i++)
            comboItems[i + 1] = styleNames[i];

        ulong? wantRemoveContentId = null;

        var comboWidth = ImGuiHelpers.GlobalScale * 300;

        using var child = ImRaii.Child("###characterAssignmentsScroll"u8);
        if (child)
        {
            if (config.CharacterStyleAssignments.Count == 0)
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize(
                    "StyleEditorNoCharacterAssignments",
                    "No character assignments yet. Add your current character using the button below."));
            }
            else if (ImGui.BeginTable(
                         "###charStyleTable",
                         3,
                         ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV))
            {
                ImGui.TableSetupColumn("###remove"u8, ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("###charname"u8, ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("###style"u8, ImGuiTableColumnFlags.WidthFixed, comboWidth);

                foreach (var entry in config.CharacterStyleAssignments.ToArray())
                {
                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    if (ImGuiComponents.IconButton($"###removeCharStyle{entry.ContentId}", FontAwesomeIcon.Trash))
                        wantRemoveContentId = entry.ContentId;

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(Loc.Localize("StyleEditorRemoveCharacterAssignment", "Remove this character assignment"));

                    ImGui.TableSetColumnIndex(1);
                    string characterDisplay;
                    if (!string.IsNullOrEmpty(entry.DisplayName) && !string.IsNullOrEmpty(entry.ServerName))
                        characterDisplay = $"{entry.DisplayName} <icon({(int)BitmapFontIcon.CrossWorld})> {entry.ServerName}";
                    else
                        characterDisplay = entry.ContentId.ToString();

                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ImGui.GetFrameHeight() / 2f) - (ImGui.GetTextLineHeight() / 2f));
                    ImGuiHelpers.CompileSeStringWrapped(characterDisplay);

                    ImGui.TableSetColumnIndex(2);
                    var currentStyleIdx = string.IsNullOrEmpty(entry.StyleName) || !styleNames.Contains(entry.StyleName)
                        ? 0
                        : Array.IndexOf(styleNames, entry.StyleName) + 1;
                    if (currentStyleIdx < 0) currentStyleIdx = 0;

                    ImGui.SetNextItemWidth(comboWidth);
                    if (ImGui.Combo($"###styleCombo{entry.ContentId}", ref currentStyleIdx, comboItems, comboItems.Length))
                    {
                        entry.StyleName = currentStyleIdx == 0 ? null : styleNames[currentStyleIdx - 1];
                        config.QueueSave();
                    }
                }

                ImGui.EndTable();
            }
        }

        if (wantRemoveContentId != null)
        {
            var toRemove = config.CharacterStyleAssignments.FirstOrDefault(x => x.ContentId == wantRemoveContentId.Value);
            if (toRemove != null)
            {
                config.CharacterStyleAssignments.Remove(toRemove);
                config.QueueSave();
            }
        }

        ImGuiHelpers.ScaledDummy(5);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5);

        var player = Service<PlayerState>.Get();
        if (player.IsLoaded)
        {
            using var disabled = ImRaii.Disabled(config.CharacterStyleAssignments.Any(x => x.ContentId == player.ContentId));
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Plus, string.Format(
                    Loc.Localize("StyleEditorAddCurrentCharacter", "Add current character: {0}"),
                    player.CharacterName)))
            {
                var serverName = player.HomeWorld.Value.Name.ExtractText();
                config.CharacterStyleAssignments.Add(new CharacterStyleAssignment(player.CharacterName, player.ContentId, serverName));
                config.QueueSave();
            }
        }
        else
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize(
                "StyleEditorCharacterNotLoaded",
                "You must be logged in to add your current character."));
        }
    }

    private void SaveStyle()
    {
        if (this.currentSel < 3)
            return;

        var config = Service<DalamudConfiguration>.Get();

        var newStyle = StyleModelV1.Get();
        newStyle.Name = config.SavedStyles[this.currentSel].Name;
        config.SavedStyles[this.currentSel] = newStyle;
        newStyle.Apply();

        config.QueueSave();
    }

    private void Change()
    {
        this.anyChanges = true;
        Service<InterfaceManager>.Get().InvokeStyleChanged();
    }
}
