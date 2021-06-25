using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// Color Demo Window to view custom ImGui colors.
    /// </summary>
    internal sealed class ColorDemoWindow : Window
    {
        private readonly List<(string Name, Vector4 Color)> colors;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorDemoWindow"/> class.
        /// </summary>
        public ColorDemoWindow()
            : base("Dalamud Colors Demo")
        {
            this.Size = new Vector2(600, 500);
            this.SizeCondition = ImGuiCond.FirstUseEver;

            this.colors = new List<(string Name, Vector4 Color)>()
            {
                ("White", ImGuiColors.White),
                ("DalamudRed", ImGuiColors.DalamudRed),
                ("DalamudGrey", ImGuiColors.DalamudGrey),
                ("DalamudGrey2", ImGuiColors.DalamudGrey2),
                ("DalamudGrey3", ImGuiColors.DalamudGrey3),
                ("DalamudWhite", ImGuiColors.DalamudWhite),
                ("DalamudWhite2", ImGuiColors.DalamudWhite2),
                ("DalamudOrange", ImGuiColors.DalamudOrange),
                ("TankBlue", ImGuiColors.TankBlue),
                ("HealerGreen", ImGuiColors.HealerGreen),
                ("DPSRed", ImGuiColors.DPSRed),
            }.OrderBy(colorDemo => colorDemo.Name).ToList();
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            ImGui.Text("This is a collection of UI colors you can use in your plugin.");

            ImGui.Separator();

            foreach (var (name, color) in this.colors)
            {
                ImGui.TextColored(color, name);
            }
        }
    }
}
