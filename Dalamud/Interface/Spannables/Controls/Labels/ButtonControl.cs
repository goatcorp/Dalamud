using Dalamud.Interface.Spannables.Patterns;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls.Labels;

/// <summary>A button that is spannable.</summary>
public class ButtonControl : LabelControl
{
    /// <summary>Initializes a new instance of the <see cref="ButtonControl"/> class.</summary>
    public ButtonControl()
    {
        this.CaptureMouseOnMouseDown = true;
        this.Focusable = true;
        this.Padding = new(8);
        this.NormalBackground = new ShapePattern
        {
            Type = ShapePattern.Shape.RectFilled,
            ImGuiColor = ImGuiCol.Button,
        };
        this.HoveredBackground = new ShapePattern
        {
            Type = ShapePattern.Shape.RectFilled,
            ImGuiColor = ImGuiCol.ButtonHovered,
        };
        this.ActiveBackground = new ShapePattern
        {
            Type = ShapePattern.Shape.RectFilled,
            ImGuiColor = ImGuiCol.ButtonActive,
        };
        this.DisabledBackground = new ShapePattern
        {
            Type = ShapePattern.Shape.RectFilled,
            ImGuiColor = ImGuiCol.Button,
            ColorMultiplier = new(1.4f, 1.4f, 1.4f, 0.6f),
        };
    }
}