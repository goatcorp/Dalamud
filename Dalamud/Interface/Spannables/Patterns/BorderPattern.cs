using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Patterns;

/// <summary>A spannable that renders borders.</summary>
public class BorderPattern : AbstractPattern.AbstractSpannable<BorderPattern.BorderOptions>
{
    /// <summary>Initializes a new instance of the <see cref="BorderPattern"/> class.</summary>
    /// <param name="options">Border options.</param>
    /// <param name="sourceTemplate">The source template.</param>
    public BorderPattern(BorderOptions options, ISpannableTemplate? sourceTemplate = null)
        : base(options, sourceTemplate)
    {
    }

    /// <inheritdoc/>
    protected override void OnDrawInside(SpannableDrawEventArgs args)
    {
        var color =
            this.Options.ImGuiColor is not null
                ? new(ImGui.GetColorU32(this.Options.ImGuiColor.Value))
                : this.Options.Color;
        color = (Vector4)color * this.Options.ColorMultiplier;

        var lt = this.Options.Margin.LeftTop;
        var sz = Vector2.Max(this.Boundary.Size - this.Options.Margin.Size, Vector2.Zero);
        if (sz.X is >= float.PositiveInfinity or <= 0 || sz.Y is >= float.PositiveInfinity or <= 0)
            return;

        lt *= this.Options.RenderScale;
        sz *= this.Options.RenderScale;
        var rb = lt + sz;
        using var st = new ScopedTransformer(
            args.DrawListPtr,
            Matrix4x4.Identity,
            new(1 / this.Options.RenderScale),
            1f);

        if (this.Options.DrawLeft)
            args.DrawListPtr.AddLine(lt, new(lt.X, rb.Y), color, this.Options.Thickness);
        if (this.Options.DrawTop)
            args.DrawListPtr.AddLine(lt, new(rb.X, lt.Y), color, this.Options.Thickness);
        if (this.Options.DrawRight)
            args.DrawListPtr.AddLine(new(rb.X, lt.Y), rb, color, this.Options.Thickness);
        if (this.Options.DrawBottom)
            args.DrawListPtr.AddLine(new(lt.X, rb.Y), rb, color, this.Options.Thickness);
    }

    /// <summary>Options for <see cref="BorderPattern"/>.</summary>
    public class BorderOptions : AbstractPattern.PatternOptions
    {
        private bool drawLeft;
        private bool drawTop;
        private bool drawRight;
        private bool drawBottom;
        private Rgba32 color;
        private ImGuiCol? imGuiColor;
        private Vector4 colorMultiplier = Vector4.One;
        private float thickness = 1;
        private BorderVector4 margin;

        /// <summary>Gets or sets a value indicating whether to draw border on the left side.</summary>
        public bool DrawLeft
        {
            get => this.drawLeft;
            set => this.UpdateProperty(nameof(this.DrawLeft), ref this.drawLeft, value, this.drawLeft == value);
        }

        /// <summary>Gets or sets a value indicating whether to draw border on the top side.</summary>
        public bool DrawTop
        {
            get => this.drawTop;
            set => this.UpdateProperty(nameof(this.DrawTop), ref this.drawTop, value, this.drawTop == value);
        }

        /// <summary>Gets or sets a value indicating whether to draw border on the right side.</summary>
        public bool DrawRight
        {
            get => this.drawRight;
            set => this.UpdateProperty(nameof(this.DrawRight), ref this.drawRight, value, this.drawRight == value);
        }

        /// <summary>Gets or sets a value indicating whether to draw border on the bottom side.</summary>
        public bool DrawBottom
        {
            get => this.drawBottom;
            set => this.UpdateProperty(nameof(this.DrawBottom), ref this.drawBottom, value, this.drawBottom == value);
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

        /// <summary>Gets or sets the margin.</summary>
        public BorderVector4 Margin
        {
            get => this.margin;
            set => this.UpdateProperty(nameof(this.Margin), ref this.margin, value, this.margin == value);
        }

        /// <inheritdoc/>
        public override bool TryReset()
        {
            this.drawLeft = default;
            this.drawTop = default;
            this.drawRight = default;
            this.drawBottom = default;
            this.color = default;
            this.imGuiColor = default;
            this.colorMultiplier = Vector4.One;
            this.thickness = 1;
            this.margin = default;
            return base.TryReset();
        }

        /// <inheritdoc/>
        public override void CopyFrom(SpannableOptions source)
        {
            base.CopyFrom(source);
            if (source is BorderOptions s)
            {
                this.DrawLeft = s.DrawLeft;
                this.DrawTop = s.DrawTop;
                this.DrawRight = s.DrawRight;
                this.DrawBottom = s.DrawBottom;
                this.Color = s.Color;
                this.ImGuiColor = s.ImGuiColor;
                this.ColorMultiplier = s.ColorMultiplier;
                this.Thickness = s.Thickness;
                this.Margin = s.Margin;
            }
        }
    }

    /// <summary>A spannable that renders borders.</summary>
    public class Template(BorderOptions options) : AbstractPattern.AbstractTemplate<BorderOptions>(options)
    {
        /// <inheritdoc/>
        public override Spannable CreateSpannable() => new BorderPattern(this.Options, this);
    }
}
