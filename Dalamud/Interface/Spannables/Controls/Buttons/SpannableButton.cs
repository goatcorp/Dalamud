using Dalamud.Interface.Spannables.Brushes;
using Dalamud.Interface.Spannables.Rendering;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls.Buttons;

/// <summary>A button that is spannable.</summary>
public class SpannableButton : SpannableLabel
{
    /// <summary>Initializes a new instance of the <see cref="SpannableButton"/> class.</summary>
    public SpannableButton()
    {
        this.Padding = new(8);
        this.NormalBackground = new ImGuiColorSolidBrushDrawable
        {
            TargetChannel = RenderChannel.BackChannel,
            Color = ImGuiCol.Button,
        };
        this.HoveredBackground = new ImGuiColorSolidBrushDrawable
        {
            TargetChannel = RenderChannel.BackChannel,
            Color = ImGuiCol.ButtonHovered,
        };
        this.ActiveBackground = new ImGuiColorSolidBrushDrawable
        {
            TargetChannel = RenderChannel.BackChannel,
            Color = ImGuiCol.ButtonActive,
        };
        this.DisabledBackground = new ImGuiColorSolidBrushDrawable
        {
            TargetChannel = RenderChannel.BackChannel,
            Color = ImGuiCol.Button,
            ColorMultiplier = new(1.4f, 1.4f, 1.4f, 0.6f),
        };
    }
}
