using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Patterns;

/// <summary>A spannable that renders a shape.</summary>
/// <remarks>See <c>RenderCheckMark</c> in <c>imgui_draw.cpp</c>.</remarks>
public class ShapePattern : AbstractPattern.AbstractSpannable<ShapePattern.ShapeOptions>
{
    /// <summary>Initializes a new instance of the <see cref="ShapePattern"/> class.</summary>
    /// <param name="options">Shape options.</param>
    /// <param name="sourceTemplate">The source template.</param>
    public ShapePattern(ShapeOptions options, ISpannableTemplate? sourceTemplate = null)
        : base(options, sourceTemplate)
    {
    }

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

    /// <inheritdoc/>
    protected override void OnDrawInside(SpannableDrawEventArgs args)
    {
        var color =
            this.Options.ImGuiColor is not null
                ? new(ImGui.GetColorU32(this.Options.ImGuiColor.Value))
                : this.Options.Color;
        color = (Vector4)color * this.Options.ColorMultiplier;

        var pos = this.Options.Margin.LeftTop;
        var sz = Vector2.Max(this.Boundary.Size - this.Options.Margin.Size, Vector2.Zero);
        var sz1 = Math.Min(sz.X, sz.Y);
        if (sz1 is >= float.PositiveInfinity or <= 0)
            return;

        var rs = this.Options.RenderScale;
        pos *= rs;
        sz *= rs;
        sz1 *= rs;
        var rd = this.Options.Rounding * rs;
        using var st = new ScopedTransformer(args.DrawListPtr, Matrix4x4.Identity, new(1 / rs), 1f);

        switch (this.Options.Shape)
        {
            case Shape.Rect:
                args.DrawListPtr.AddRect(
                    pos,
                    pos + sz,
                    color,
                    rd,
                    this.Options.RoundingFlags,
                    this.Options.Thickness);
                break;
            case Shape.RectFilled:
                args.DrawListPtr.AddRectFilled(pos, pos + sz, color, rd, this.Options.RoundingFlags);
                break;
            case Shape.Square:
                args.DrawListPtr.AddRect(
                    pos + ((sz - new Vector2(sz1)) / 2f),
                    pos + ((sz + new Vector2(sz1)) / 2f),
                    color,
                    rd,
                    this.Options.RoundingFlags,
                    this.Options.Thickness);
                break;
            case Shape.SquareFilled:
                args.DrawListPtr.AddRectFilled(
                    pos + ((sz - new Vector2(sz1)) / 2f),
                    pos + ((sz + new Vector2(sz1)) / 2f),
                    color,
                    rd,
                    this.Options.RoundingFlags);
                break;
            case Shape.Circle:
                args.DrawListPtr.AddCircle(pos + (sz / 2), sz1 / 2, color, 0, this.Options.Thickness);
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

    /// <summary>Options for <see cref="ShapePattern"/>.</summary>
    public class ShapeOptions : AbstractPattern.PatternOptions
    {
        private Shape shape;
        private Rgba32 color;
        private ImGuiCol? imGuiColor;
        private Vector4 colorMultiplier = Vector4.One;
        private float thickness = 1;
        private float rounding;
        private ImDrawFlags roundingFlags = ImDrawFlags.RoundCornersDefault;
        private BorderVector4 margin;

        /// <summary>Gets or sets a shape.</summary>
        public Shape Shape
        {
            get => this.shape;
            set => this.UpdateProperty(nameof(this.Shape), ref this.shape, value, this.shape == value);
        }

        /// <summary>Gets or sets the fill color.</summary>
        /// <remarks>If <see cref="ImGuiColor"/> is set, then that will be used.</remarks>
        public Rgba32 Color
        {
            get => this.color;
            set => this.UpdateProperty(nameof(this.Color), ref this.color, value, this.color == value);
        }

        /// <summary>Gets or sets the fill color.</summary>
        public ImGuiCol? ImGuiColor
        {
            get => this.imGuiColor;
            set => this.UpdateProperty(nameof(this.ImGuiColor), ref this.imGuiColor, value, this.imGuiColor == value);
        }

        /// <summary>Gets or sets the color multiplier, including the alpha channel.</summary>
        public Vector4 ColorMultiplier
        {
            get => this.colorMultiplier;
            set => this.UpdateProperty(
                nameof(this.ColorMultiplier),
                ref this.colorMultiplier,
                value,
                this.colorMultiplier == value);
        }

        /// <summary>Gets or sets the thickness.</summary>
        /// <remarks>Applicable to select shapes.</remarks>
        public float Thickness
        {
            get => this.thickness;
            set => this.UpdateProperty(nameof(this.Thickness), ref this.thickness, value, this.thickness - value == 0f);
        }

        /// <summary>Gets or sets the rounding.</summary>
        /// <remarks>Applicable to select shapes.</remarks>
        public float Rounding
        {
            get => this.rounding;
            set => this.UpdateProperty(nameof(this.Rounding), ref this.rounding, value, this.rounding - value == 0f);
        }

        /// <summary>Gets or sets the rounding flags.</summary>
        /// <remarks>Applicable to select shapes.</remarks>
        public ImDrawFlags RoundingFlags
        {
            get => this.roundingFlags;
            set => this.UpdateProperty(
                nameof(this.RoundingFlags),
                ref this.roundingFlags,
                value,
                this.roundingFlags - value == 0f);
        }

        /// <summary>Gets or sets the margin.</summary>
        public BorderVector4 Margin
        {
            get => this.margin;
            set => this.UpdateProperty(nameof(this.Margin), ref this.margin, value, this.margin == value);
        }

        /// <inheritdoc/>
        public override bool TryReset()
        {
            this.shape = default;
            this.color = default;
            this.imGuiColor = default;
            this.colorMultiplier = Vector4.One;
            this.thickness = 1;
            this.rounding = 0;
            this.roundingFlags = ImDrawFlags.RoundCornersDefault;
            this.margin = BorderVector4.Zero;
            return base.TryReset();
        }

        /// <inheritdoc/>
        public override void CopyFrom(SpannableOptions source)
        {
            base.CopyFrom(source);
            if (source is ShapeOptions s)
            {
                this.Shape = s.Shape;
                this.Color = s.Color;
                this.ImGuiColor = s.ImGuiColor;
                this.ColorMultiplier = s.ColorMultiplier;
                this.Thickness = s.Thickness;
                this.Rounding = s.Rounding;
                this.RoundingFlags = s.RoundingFlags;
                this.Margin = s.Margin;
            }
        }
    }

    /// <summary>A spannable that renders shapes.</summary>
    public class Template(ShapeOptions options) : AbstractPattern.AbstractTemplate<ShapeOptions>(options)
    {
        /// <inheritdoc/>
        public override Spannable CreateSpannable() => new ShapePattern(this.Options, this);
    }
}
