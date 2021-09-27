using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.Windows.StyleEditor;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using JetBrains.Annotations;
using Serilog;

namespace Dalamud.Interface.Internal.Windows
{
    public class StyleEditorWindow : Window
    {
        private ImGuiColorEditFlags alphaFlags = ImGuiColorEditFlags.None;
        private StyleModel workStyle = StyleModel.DalamudStandard;

        private int currentSel = 0;
        private string initialStyle;

        public StyleEditorWindow()
            : base("Dalamud Style Editor")
        {
            this.IsOpen = true;

            var config = Service<DalamudConfiguration>.Get();
            config.SavedStyles ??= new List<StyleModel>();
            this.currentSel = config.SavedStyles.FindIndex(x => x.Name == config.ChosenStyle);

            this.initialStyle = config.ChosenStyle;
        }

        public override void Draw()
        {
            var config = Service<DalamudConfiguration>.Get();

            var style = ImGui.GetStyle();

            ImGui.Text("Choose Style:");
            if (ImGui.Combo("###styleChooserCombo", ref this.currentSel, config.SavedStyles.Select(x => x.Name).ToArray(), 1))
            {
                var newStyle = config.SavedStyles[this.currentSel];
                newStyle.Apply();
            }

            if (ImGui.Button("Add new style"))
            {
                var newStyle = StyleModel.DalamudStandard;
                newStyle.Name = "New Style";
                config.SavedStyles.Add(newStyle);

                this.currentSel = config.SavedStyles.Count - 1;

                config.Save();
            }

            ImGui.SameLine();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash) && this.currentSel != 0)
            {
                this.currentSel--;
                var newStyle = config.SavedStyles[this.currentSel];
                newStyle.Apply();

                config.SavedStyles.RemoveAt(this.currentSel + 1);

                config.Save();
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Delete current style");

            ImGui.SameLine();

            ImGuiHelpers.ScaledDummy(5);
            ImGui.SameLine();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.FileExport))
            {
                ImGui.SetClipboardText(StyleModel.Get().ToJsonEncoded());
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Copy style to clipboard for sharing");

            ImGui.SameLine();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.FileImport))
            {
                var styleJson = ImGui.GetClipboardText();

                try
                {
                    var newStyle = StyleModel.FromJsonEncoded(styleJson);

                    config.SavedStyles.Add(newStyle);
                    newStyle.Apply();

                    this.currentSel = config.SavedStyles.Count - 1;

                    config.Save();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Could not import style");
                }
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Import style from clipboard");

            ImGui.Separator();

            ImGui.PushItemWidth(ImGui.GetWindowWidth() * 0.50f);

            if (this.currentSel == 0)
            {
                ImGui.TextColored(ImGuiColors.DalamudRed, "You cannot edit the \"Dalamud Standard\" style. Please add a new style first.");
            }
            else if (ImGui.BeginTabBar("StyleEditorTabs"))
            {
                if (ImGui.BeginTabItem("Variables"))
                {
                    ImGui.BeginChild($"ScrollingVars", ImGuiHelpers.ScaledVector2(0, -32), true, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoBackground);

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
                    var window_menu_button_position = (int)style.WindowMenuButtonPosition + 1;
                    if (ImGui.Combo("WindowMenuButtonPosition", ref window_menu_button_position, "None\0Left\0Right\0"))
                        style.WindowMenuButtonPosition = (ImGuiDir)(window_menu_button_position - 1);
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
                    ImGui.EndTabItem();

                    ImGui.EndChild();

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Colors"))
                {
                    ImGui.BeginChild("ScrollingColors", ImGuiHelpers.ScaledVector2(0, -30), true, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoBackground);

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

                    ImGui.EndChild();

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.PopItemWidth();

            ImGui.Separator();

            if (ImGui.Button("Close"))
            {
                var newStyle = config.SavedStyles.FirstOrDefault(x => x.Name == this.initialStyle);
                newStyle?.Apply();

                this.IsOpen = false;
            }

            ImGui.SameLine();

            if (ImGui.Button("Save and Close"))
            {
                config.ChosenStyle = config.SavedStyles[this.currentSel].Name;

                var newStyle = StyleModel.Get();
                newStyle.Name = config.ChosenStyle;
                config.SavedStyles[this.currentSel] = newStyle;

                config.Save();
            }
        }
    }
}
