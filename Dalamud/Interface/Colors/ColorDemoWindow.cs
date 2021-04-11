using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dalamud.Interface.Colors
{
    /// <summary>
    /// color Demo Window to view custom ImGui colors.
    /// </summary>
    internal class ColorDemoWindow : Window
    {
        private readonly List<KeyValuePair<string, Vector4>> colors;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorDemoWindow"/> class.
        /// </summary>
        public ColorDemoWindow()
            : base("Dalamud Colors Demo")
        {
            this.Size = new Vector2(600, 500);
            this.SizeCondition = ImGuiCond.FirstUseEver;
            this.colors = new List<KeyValuePair<string, Vector4>>
            {
                Demo("White", ImGuiColors.White),
                Demo("DalamudRed", ImGuiColors.DalamudRed),
                Demo("DalamudGrey", ImGuiColors.DalamudGrey),
                Demo("DalamudGrey2", ImGuiColors.DalamudGrey2),
                Demo("DalamudGrey3", ImGuiColors.DalamudGrey3),
                Demo("DalamudWhite", ImGuiColors.DalamudWhite),
                Demo("DalamudWhite2", ImGuiColors.DalamudWhite2),
                Demo("DalamudOrange", ImGuiColors.DalamudOrange),
                Demo("TankBlue", ImGuiColors.TankBlue),
                Demo("HealerGreen", ImGuiColors.HealerGreen),
                Demo("DPSRed", ImGuiColors.DPSRed),
            };
            this.colors = this.colors.OrderBy(colorDemo => colorDemo.Key).ToList();
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            ImGui.BeginChild("color_scrolling", new Vector2(0, 0), false, ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.HorizontalScrollbar);
            ImGui.Text("This is a collection of UI colors you can use in your plugin.");
            ImGui.Separator();
            foreach (var color in this.colors)
            {
                ImGui.TextColored(color.Value, color.Key);
            }

            ImGui.EndChild();
        }

        private static KeyValuePair<string, Vector4> Demo(string name, Vector4 color)
        {
            return new KeyValuePair<string, Vector4>(name, color);
        }
    }
}
