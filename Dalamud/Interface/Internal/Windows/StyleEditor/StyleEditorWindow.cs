using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;

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
        config.SavedStyles ??= new List<StyleModel>();
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

        var isBuiltinStyle = this.currentSel < 2;
        var appliedThisFrame = false;

        var styleAry = config.SavedStyles.Select(x => x.Name).ToArray();
        ImGui.Text(Loc.Localize("StyleEditorChooseStyle", "Choose Style:"));
        if (ImGui.Combo("###styleChooserCombo", ref this.currentSel, styleAry, styleAry.Length))
        {
            var newStyle = config.SavedStyles[this.currentSel];
            newStyle.Apply();
            appliedThisFrame = true;
        }

        if (ImGui.Button(Loc.Localize("StyleEditorAddNew", "Add new style")))
        {
            this.SaveStyle();

            var newStyle = StyleModelV1.DalamudStandard;
            newStyle.Name = Util.GetRandomName();
            config.SavedStyles.Add(newStyle);

            this.currentSel = config.SavedStyles.Count - 1;

            newStyle.Apply();
            appliedThisFrame = true;

            config.QueueSave();
        }

        ImGui.SameLine();

        if (isBuiltinStyle)
            ImGui.BeginDisabled();
        
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash) && this.currentSel != 0)
        {
            this.currentSel--;
            var newStyle = config.SavedStyles[this.currentSel];
            newStyle.Apply();
            appliedThisFrame = true;

            config.SavedStyles.RemoveAt(this.currentSel + 1);

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

        if (isBuiltinStyle)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, Loc.Localize("StyleEditorNotAllowed", "You cannot edit built-in styles. Please add a new style first."));
        }
        else if (appliedThisFrame)
        {
            ImGui.Text(Loc.Localize("StyleEditorApplying", "Applying style..."));
        }
        else if (ImGui.BeginTabBar("StyleEditorTabs"))
        {
            var style = ImGui.GetStyle();

            if (ImGui.BeginTabItem(Loc.Localize("StyleEditorVariables", "Variables")))
            {
                if (ImGui.BeginChild($"ScrollingVars", ImGuiHelpers.ScaledVector2(0, -32), true, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoBackground))
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5);

                    ImGui.SliderFloat2("WindowPadding", ref style.WindowPadding, 0.0f, 20.0f, "%.0f");
                    ImGui.SliderFloat2("FramePadding", ref style.FramePadding, 0.0f, 20.0f, "%.0f");
                    ImGui.SliderFloat2("CellPadding", ref style.CellPadding, 0.0f, 20.0f, "%.0f");
                    ImGui.SliderFloat2("ItemSpacing", ref style.ItemSpacing, 0.0f, 20.0f, "%.0f");
                    ImGui.SliderFloat2("ItemInnerSpacing", ref style.ItemInnerSpacing, 0.0f, 20.0f, "%.0f");
                    ImGui.SliderFloat2("TouchExtraPadding", ref style.TouchExtraPadding, 0.0f, 10.0f, "%.0f");
                    ImGui.SliderFloat("IndentSpacing", ref style.IndentSpacing, 0.0f, 30.0f, "%.0f");
                    ImGui.SliderFloat("ScrollbarSize", ref style.ScrollbarSize, 1.0f, 20.0f, "%.0f");
                    ImGui.SliderFloat("GrabMinSize", ref style.GrabMinSize, 1.0f, 20.0f, "%.0f");
                    ImGui.Text("Borders");
                    ImGui.SliderFloat("WindowBorderSize", ref style.WindowBorderSize, 0.0f, 1.0f, "%.0f");
                    ImGui.SliderFloat("ChildBorderSize", ref style.ChildBorderSize, 0.0f, 1.0f, "%.0f");
                    ImGui.SliderFloat("PopupBorderSize", ref style.PopupBorderSize, 0.0f, 1.0f, "%.0f");
                    ImGui.SliderFloat("FrameBorderSize", ref style.FrameBorderSize, 0.0f, 1.0f, "%.0f");
                    ImGui.SliderFloat("TabBorderSize", ref style.TabBorderSize, 0.0f, 1.0f, "%.0f");
                    ImGui.Text("Rounding");
                    ImGui.SliderFloat("WindowRounding", ref style.WindowRounding, 0.0f, 12.0f, "%.0f");
                    ImGui.SliderFloat("ChildRounding", ref style.ChildRounding, 0.0f, 12.0f, "%.0f");
                    ImGui.SliderFloat("FrameRounding", ref style.FrameRounding, 0.0f, 12.0f, "%.0f");
                    ImGui.SliderFloat("PopupRounding", ref style.PopupRounding, 0.0f, 12.0f, "%.0f");
                    ImGui.SliderFloat("ScrollbarRounding", ref style.ScrollbarRounding, 0.0f, 12.0f, "%.0f");
                    ImGui.SliderFloat("GrabRounding", ref style.GrabRounding, 0.0f, 12.0f, "%.0f");
                    ImGui.SliderFloat("LogSliderDeadzone", ref style.LogSliderDeadzone, 0.0f, 12.0f, "%.0f");
                    ImGui.SliderFloat("TabRounding", ref style.TabRounding, 0.0f, 12.0f, "%.0f");
                    ImGui.Text("Alignment");
                    ImGui.SliderFloat2("WindowTitleAlign", ref style.WindowTitleAlign, 0.0f, 1.0f, "%.2f");
                    var windowMenuButtonPosition = (int)style.WindowMenuButtonPosition + 1;
                    if (ImGui.Combo("WindowMenuButtonPosition", ref windowMenuButtonPosition, "None\0Left\0Right\0"))
                        style.WindowMenuButtonPosition = (ImGuiDir)(windowMenuButtonPosition - 1);
                    ImGui.SliderFloat2("ButtonTextAlign", ref style.ButtonTextAlign, 0.0f, 1.0f, "%.2f");
                    ImGui.SameLine();
                    ImGuiComponents.HelpMarker("Alignment applies when a button is larger than its text content.");
                    ImGui.SliderFloat2("SelectableTextAlign", ref style.SelectableTextAlign, 0.0f, 1.0f, "%.2f");
                    ImGui.SameLine();
                    ImGuiComponents.HelpMarker("Alignment applies when a selectable is larger than its text content.");
                    ImGui.SliderFloat2("DisplaySafeAreaPadding", ref style.DisplaySafeAreaPadding, 0.0f, 30.0f, "%.0f");
                    ImGui.SameLine();
                    ImGuiComponents.HelpMarker(
                        "Adjust if you cannot see the edges of your screen (e.g. on a TV where scaling has not been configured).");

                    ImGui.EndChild();
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.Localize("StyleEditorColors", "Colors")))
            {
                if (ImGui.BeginChild("ScrollingColors", ImGuiHelpers.ScaledVector2(0, -30), true, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoBackground))
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5);

                    if (ImGui.RadioButton("Opaque", this.alphaFlags == ImGuiColorEditFlags.None))
                        this.alphaFlags = ImGuiColorEditFlags.None;
                    ImGui.SameLine();
                    if (ImGui.RadioButton("Alpha", this.alphaFlags == ImGuiColorEditFlags.AlphaPreview))
                        this.alphaFlags = ImGuiColorEditFlags.AlphaPreview;
                    ImGui.SameLine();
                    if (ImGui.RadioButton("Both", this.alphaFlags == ImGuiColorEditFlags.AlphaPreviewHalf))
                        this.alphaFlags = ImGuiColorEditFlags.AlphaPreviewHalf;
                    ImGui.SameLine();

                    ImGuiComponents.HelpMarker(
                        "In the color list:\n" +
                        "Left-click on color square to open color picker,\n" +
                        "Right-click to open edit options menu.");

                    foreach (var imGuiCol in Enum.GetValues<ImGuiCol>())
                    {
                        if (imGuiCol == ImGuiCol.COUNT)
                            continue;

                        ImGui.PushID(imGuiCol.ToString());

                        ImGui.ColorEdit4("##color", ref style.Colors[(int)imGuiCol], ImGuiColorEditFlags.AlphaBar | this.alphaFlags);

                        ImGui.SameLine(0.0f, style.ItemInnerSpacing.X);
                        ImGui.TextUnformatted(imGuiCol.ToString());

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
                        }

                        ImGui.SameLine(0.0f, style.ItemInnerSpacing.X);
                        ImGui.TextUnformatted(property.Name);

                        ImGui.PopID();
                    }

                    ImGui.EndChild();
                }

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.PopItemWidth();

        ImGui.Separator();

        if (ImGui.Button(Loc.Localize("Close", "Close")))
        {
            this.IsOpen = false;
        }

        ImGui.SameLine();

        if (ImGui.Button(Loc.Localize("SaveAndClose", "Save and Close")))
        {
            this.SaveStyle();

            config.ChosenStyle = config.SavedStyles[this.currentSel].Name;
            Log.Verbose("ChosenStyle = {ChosenStyle}", config.ChosenStyle);

            this.didSave = true;

            this.IsOpen = false;
        }

        if (ImGui.BeginPopupModal(renameModalTitle, ref this.renameModalDrawing, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.Text(Loc.Localize("StyleEditorEnterName", "Please enter the new name for this style."));
            ImGui.Spacing();

            ImGui.InputText("###renameModalInput", ref this.renameText, 255);

            const float buttonWidth = 120f;
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - buttonWidth) / 2);

            if (ImGui.Button("OK", new Vector2(buttonWidth, 40)))
            {
                config.SavedStyles[this.currentSel].Name = this.renameText;
                config.QueueSave();

                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private void SaveStyle()
    {
        if (this.currentSel < 2)
            return;

        var config = Service<DalamudConfiguration>.Get();

        var newStyle = StyleModelV1.Get();
        newStyle.Name = config.SavedStyles[this.currentSel].Name;
        config.SavedStyles[this.currentSel] = newStyle;
        newStyle.Apply();

        config.QueueSave();
    }
}
