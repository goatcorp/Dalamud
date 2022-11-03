using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows;

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
            ("DalamudRed", ImGuiColors.DalamudRed),
            ("DalamudGrey", ImGuiColors.DalamudGrey),
            ("DalamudGrey2", ImGuiColors.DalamudGrey2),
            ("DalamudGrey3", ImGuiColors.DalamudGrey3),
            ("DalamudWhite", ImGuiColors.DalamudWhite),
            ("DalamudWhite2", ImGuiColors.DalamudWhite2),
            ("DalamudOrange", ImGuiColors.DalamudOrange),
            ("DalamudYellow", ImGuiColors.DalamudYellow),
            ("DalamudViolet", ImGuiColors.DalamudViolet),
            ("TankBlue", ImGuiColors.TankBlue),
            ("HealerGreen", ImGuiColors.HealerGreen),
            ("DPSRed", ImGuiColors.DPSRed),
            ("ParsedGrey", ImGuiColors.ParsedGrey),
            ("ParsedGreen", ImGuiColors.ParsedGreen),
            ("ParsedBlue", ImGuiColors.ParsedBlue),
            ("ParsedPurple", ImGuiColors.ParsedPurple),
            ("ParsedOrange", ImGuiColors.ParsedOrange),
            ("ParsedPink", ImGuiColors.ParsedPink),
            ("ParsedGold", ImGuiColors.ParsedGold),
        }.OrderBy(colorDemo => colorDemo.Name).ToList();
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        ImGui.Text("This is a collection of UI colors you can use in your plugin.");

        ImGui.Separator();

        foreach (var property in typeof(ImGuiColors).GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            var color = (Vector4)property.GetValue(null);
            ImGui.TextColored(color, property.Name);
        }
    }
}
