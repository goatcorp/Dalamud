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
        this.Background = new DisplayedStatePattern
        {
            NormalSpannable = new ShapePattern
            {
                Type = ShapePattern.Shape.RectFilled,
                ImGuiColor = ImGuiCol.Button,
            },
            HoveredSpannable = new ShapePattern
            {
                Type = ShapePattern.Shape.RectFilled,
                ImGuiColor = ImGuiCol.ButtonHovered,
            },
            ActiveSpannable = new ShapePattern
            {
                Type = ShapePattern.Shape.RectFilled,
                ImGuiColor = ImGuiCol.ButtonActive,
            },
            DisabledSpannable = new ShapePattern
            {
                Type = ShapePattern.Shape.RectFilled,
                ImGuiColor = ImGuiCol.Button,
                ColorMultiplier = new(1.4f, 1.4f, 1.4f, 0.6f),
            },
        };
    }
}
