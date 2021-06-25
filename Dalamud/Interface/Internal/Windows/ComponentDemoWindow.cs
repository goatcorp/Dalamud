using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// Component Demo Window to view custom ImGui components.
    /// </summary>
    internal sealed class ComponentDemoWindow : Window
    {
        private readonly List<(string Name, Action Demo)> componentDemos;
        private Vector4 defaultColor = ImGuiColors.DalamudOrange;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentDemoWindow"/> class.
        /// </summary>
        public ComponentDemoWindow()
            : base("Dalamud Components Demo")
        {
            this.Size = new Vector2(600, 500);
            this.SizeCondition = ImGuiCond.FirstUseEver;

            this.componentDemos = new()
            {
                ("Test", ImGuiComponents.Test),
                ("HelpMarker", HelpMarkerDemo),
                ("IconButton", IconButtonDemo),
                ("TextWithLabel", TextWithLabelDemo),
                ("ColorPickerWithPalette", this.ColorPickerWithPaletteDemo),
            };
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            ImGui.Text("This is a collection of UI components you can use in your plugin.");

            for (var i = 0; i < this.componentDemos.Count; i++)
            {
                var componentDemo = this.componentDemos[i];

                if (ImGui.CollapsingHeader($"{componentDemo.Name}###comp{i}"))
                {
                    componentDemo.Demo();
                }
            }
        }

        private static void HelpMarkerDemo()
        {
            ImGui.Text("Hover over the icon to learn more.");
            ImGuiComponents.HelpMarker("help me!");
        }

        private static void IconButtonDemo()
        {
            ImGui.Text("Click on the icon to use as a button.");
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(1, FontAwesomeIcon.Carrot))
            {
                ImGui.OpenPopup("IconButtonDemoPopup");
            }

            if (ImGui.BeginPopup("IconButtonDemoPopup"))
            {
                ImGui.Text("You clicked!");
                ImGui.EndPopup();
            }
        }

        private static void TextWithLabelDemo()
        {
            ImGuiComponents.TextWithLabel("Label", "Hover to see more", "more");
        }

        private void ColorPickerWithPaletteDemo()
        {
            ImGui.Text("Click on the color button to use the picker.");
            ImGui.SameLine();
            this.defaultColor = ImGuiComponents.ColorPickerWithPalette(1, "ColorPickerWithPalette Demo", this.defaultColor);
        }
    }
}
