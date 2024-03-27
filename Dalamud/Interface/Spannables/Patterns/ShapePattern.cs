using System.Numerics;

using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.RenderPassMethodArgs;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Patterns;

/// <summary>A spannable that renders a shape.</summary>
/// <remarks>See <c>RenderCheckMark</c> in <c>imgui_draw.cpp</c>.</remarks>
public class ShapePattern : PatternSpannable
{
    /// <summary>Available shapes.</summary>
    public enum Shape
    {
        /// <summary>A hollow rectangle.</summary>
        Rect,

        /// <summary>A filled rectangle.</summary>
        RectFilled,

        /// <summary>A hollow square.</summary>
        Square,

        /// <summary>A filled square.</summary>
        SquareFilled,

        /// <summary>A hollow circle.</summary>
        Circle,

        /// <summary>A filled circle.</summary>
        CircleFilled,

        /// <summary>A checkmark.</summary>
        Checkmark,
    }

    /// <summary>Gets or sets a shape.</summary>
    public Shape Type { get; set; }

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

    /// <summary>Gets or sets the rounding.</summary>
    /// <remarks>Applicable to select shapes.</remarks>
    public float Rounding { get; set; } = 0;

    /// <summary>Gets or sets the rounding flags.</summary>
    /// <remarks>Applicable to select shapes.</remarks>
    public ImDrawFlags RoundingFlags { get; set; } = ImDrawFlags.RoundCornersDefault;

    /// <summary>Gets or sets the margin.</summary>
    public BorderVector4 Margin { get; set; }

    /// <inheritdoc/>
    protected override PatternRenderPass CreateNewRenderPass() => new CheckmarkRenderPass(this);

    /// <summary>A state for <see cref="LayeredPattern"/>.</summary>
    private class CheckmarkRenderPass(ShapePattern owner) : PatternRenderPass(owner)
    {
        protected override void DrawUntransformed(SpannableDrawArgs args)
        {
            var color =
                owner.ImGuiColor is not null
                    ? new(ImGui.GetColorU32(owner.ImGuiColor.Value))
                    : owner.Color;
            color = (Vector4)color * owner.ColorMultiplier;

            var pos = owner.Margin.LeftTop;
            var sz = Vector2.Max(this.Boundary.Size - owner.Margin.Size, Vector2.Zero);
            var sz1 = Math.Min(sz.X, sz.Y);
            if (sz1 is >= float.PositiveInfinity or <= 0)
                return;

            pos *= this.Scale;
            sz *= this.Scale;
            sz1 *= this.Scale;
            using var st = new ScopedTransformer(args.DrawListPtr, Matrix4x4.Identity, new(1 / this.Scale), 1f);

            switch (owner.Type)
            {
                case Shape.Rect:
                    args.DrawListPtr.AddRect(
                        pos,
                        pos + sz,
                        color,
                        owner.Rounding,
                        owner.RoundingFlags,
                        owner.Thickness);
                    break;
                case Shape.RectFilled:
                    args.DrawListPtr.AddRectFilled(pos, pos + sz, color, owner.Rounding, owner.RoundingFlags);
                    break;
                case Shape.Square:
                    args.DrawListPtr.AddRect(
                        pos + ((sz - new Vector2(sz1)) / 2f),
                        pos + ((sz + new Vector2(sz1)) / 2f),
                        color,
                        owner.Rounding,
                        owner.RoundingFlags,
                        owner.Thickness);
                    break;
                case Shape.SquareFilled:
                    args.DrawListPtr.AddRectFilled(
                        pos + ((sz - new Vector2(sz1)) / 2f),
                        pos + ((sz + new Vector2(sz1)) / 2f),
                        color,
                        owner.Rounding,
                        owner.RoundingFlags);
                    break;
                case Shape.Circle:
                    args.DrawListPtr.AddCircle(pos + (sz / 2), sz1 / 2, color, 0, owner.Thickness);
                    break;
                case Shape.CircleFilled:
                    args.DrawListPtr.AddCircleFilled(pos + (sz / 2), sz1 / 2, color);
                    break;
                case Shape.Checkmark:
                {
                    var thickness = Math.Max(sz1 / 5.0f, 1.0f);
                    sz1 -= thickness * 0.5f;
                    pos += new Vector2(thickness * 0.25f, thickness * 0.25f);

                    if (sz.X > sz.Y)
                        pos.X += (sz.X - sz.Y) / 2f;
                    else if (sz.Y > sz.X)
                        pos.Y += (sz.Y - sz.X) / 2f;

                    var third = sz1 / 3.0f;
                    var bx = pos.X + third;
                    var by = (pos.Y + sz1) - (third * 0.5f);

                    args.DrawListPtr.PathLineTo(new(bx - third, by - third));
                    args.DrawListPtr.PathLineTo(new(bx, by));
                    args.DrawListPtr.PathLineTo(new(bx + (third * 2.0f), by - (third * 2.0f)));
                    args.DrawListPtr.PathStroke(color, 0, thickness);
                    break;
                }
            }
        }
    }
}
