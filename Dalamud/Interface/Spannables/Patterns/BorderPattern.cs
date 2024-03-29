using System.Numerics;

using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Patterns;

/// <summary>A spannable that renders borders.</summary>
public class BorderPattern : PatternSpannable
{
    /// <summary>Gets or sets a value indicating whether to draw border on the left side.</summary>
    public bool DrawLeft { get; set; }

    /// <summary>Gets or sets a value indicating whether to draw border on the top side.</summary>
    public bool DrawTop { get; set; }

    /// <summary>Gets or sets a value indicating whether to draw border on the right side.</summary>
    public bool DrawRight { get; set; }

    /// <summary>Gets or sets a value indicating whether to draw border on the bottom side.</summary>
    public bool DrawBottom { get; set; }

    /// <summary>Gets or sets the fill color.</summary>
    /// <remarks>If <see cref="ImGuiColor"/> is set, then that will be used.</remarks>
    public Rgba32 Color { get; set; }

    /// <summary>Gets or sets the fill color.</summary>
    public ImGuiCol? ImGuiColor { get; set; }

    /// <summary>Gets or sets the color multiplier, including the alpha channel.</summary>
    public Vector4 ColorMultiplier { get; set; } = Vector4.One;

    /// <summary>Gets or sets the thickness.</summary>
    /// <remarks>Applicable to select shapes.</remarks>
    public float Thickness { get; set; } = 1;

    /// <summary>Gets or sets the margin.</summary>
    public BorderVector4 Margin { get; set; }

    /// <inheritdoc/>
    protected override PatternSpannableMeasurement CreateNewRenderPass() => new BorderPatternMeasurement(this, new());

    /// <summary>A state for <see cref="LayeredPattern"/>.</summary>
    private class BorderPatternMeasurement(BorderPattern owner, SpannableMeasurementOptions options)
        : PatternSpannableMeasurement(owner, options)
    {
        protected override void DrawUntransformed(ImDrawListPtr drawListPtr)
        {
            var color =
                owner.ImGuiColor is not null
                    ? new(ImGui.GetColorU32(owner.ImGuiColor.Value))
                    : owner.Color;
            color = (Vector4)color * owner.ColorMultiplier;

            var lt = owner.Margin.LeftTop;
            var sz = Vector2.Max(this.Boundary.Size - owner.Margin.Size, Vector2.Zero);
            if (sz.X is >= float.PositiveInfinity or <= 0 || sz.Y is >= float.PositiveInfinity or <= 0)
                return;

            lt *= this.RenderScale;
            sz *= this.RenderScale;
            var rb = lt + sz;
            using var st = new ScopedTransformer(drawListPtr, Matrix4x4.Identity, new(1 / this.RenderScale), 1f);

            if (owner.DrawLeft)
                drawListPtr.AddLine(lt, new(lt.X, rb.Y), color, owner.Thickness);
            if (owner.DrawTop)
                drawListPtr.AddLine(lt, new(rb.X, lt.Y), color, owner.Thickness);
            if (owner.DrawRight)
                drawListPtr.AddLine(new(rb.X, lt.Y), rb, color, owner.Thickness);
            if (owner.DrawBottom)
                drawListPtr.AddLine(new(lt.X, rb.Y), rb, color, owner.Thickness);
        }
    }
}
