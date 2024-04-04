using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Patterns;

/// <summary>A spannable that renders a shape.</summary>
/// <remarks>See <c>RenderCheckMark</c> in <c>imgui_draw.cpp</c>.</remarks>
public class ShapePattern : AbstractPattern
{
    private Shape type;
    private Rgba32 color;
    private ImGuiCol? imGuiColor;
    private Vector4 colorMultiplier = Vector4.One;
    private float thickness = 1;
    private float rounding;
    private ImDrawFlags roundingFlags = ImDrawFlags.RoundCornersDefault;
    private BorderVector4 margin;
    private float rotation;

    /// <summary>Occurs when the property <see cref="Type"/> is changing.</summary>
    public event PropertyChangeEventHandler<Shape>? TypeChange;

    /// <summary>Occurs when the property <see cref="Color"/> is changing.</summary>
    public event PropertyChangeEventHandler<Rgba32>? ColorChange;

    /// <summary>Occurs when the property <see cref="ImGuiColor"/> is changing.</summary>
    public event PropertyChangeEventHandler<ImGuiCol?>? ImGuiColorChange;

    /// <summary>Occurs when the property <see cref="ColorMultiplier"/> is changing.</summary>
    public event PropertyChangeEventHandler<Vector4>? ColorMultiplierChange;

    /// <summary>Occurs when the property <see cref="Thickness"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? ThicknessChange;

    /// <summary>Occurs when the property <see cref="Rounding"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? RoundingChange;

    /// <summary>Occurs when the property <see cref="RoundingFlags"/> is changing.</summary>
    public event PropertyChangeEventHandler<ImDrawFlags>? RoundingFlagsChange;

    /// <summary>Occurs when the property <see cref="Margin"/> is changing.</summary>
    public event PropertyChangeEventHandler<BorderVector4>? MarginChange;

    /// <summary>Occurs when the property <see cref="Rotation"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? RotationChange;

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

        /// <summary>An equilateral triangle.</summary>
        EquilateralTriangle,

        /// <summary>A filled equilateral triangle.</summary>
        EquilateralTriangleFilled,
    }

    /// <summary>Gets or sets a shape.</summary>
    public Shape Type
    {
        get => this.type;
        set => this.HandlePropertyChange(
            nameof(this.Type),
            ref this.type,
            value,
            this.type == value,
            this.OnTypeChange);
    }

    /// <summary>Gets or sets the fill color.</summary>
    /// <remarks>If <see cref="ImGuiColor"/> is set, then that will be used.</remarks>
    public Rgba32 Color
    {
        get => this.color;
        set => this.HandlePropertyChange(
            nameof(this.Color),
            ref this.color,
            value,
            this.color == value,
            this.OnColorChange);
    }

    /// <summary>Gets or sets the fill color.</summary>
    public ImGuiCol? ImGuiColor
    {
        get => this.imGuiColor;
        set => this.HandlePropertyChange(
            nameof(this.ImGuiColor),
            ref this.imGuiColor,
            value,
            this.imGuiColor == value,
            this.OnImGuiColorChange);
    }

    /// <summary>Gets or sets the color multiplier, including the alpha channel.</summary>
    public Vector4 ColorMultiplier
    {
        get => this.colorMultiplier;
        set => this.HandlePropertyChange(
            nameof(this.ColorMultiplier),
            ref this.colorMultiplier,
            value,
            this.colorMultiplier == value,
            this.OnColorMultiplierChange);
    }

    /// <summary>Gets or sets the thickness.</summary>
    /// <remarks>Applicable to select shapes.</remarks>
    public float Thickness
    {
        get => this.thickness;
        set => this.HandlePropertyChange(
            nameof(this.Thickness),
            ref this.thickness,
            value,
            this.thickness - value == 0f,
            this.OnThicknessChange);
    }

    /// <summary>Gets or sets the rounding.</summary>
    /// <remarks>Applicable to select shapes.</remarks>
    public float Rounding
    {
        get => this.rounding;
        set => this.HandlePropertyChange(
            nameof(this.Rounding),
            ref this.rounding,
            value,
            this.rounding - value == 0f,
            this.OnRoundingChange);
    }

    /// <summary>Gets or sets the rounding flags.</summary>
    /// <remarks>Applicable to select shapes.</remarks>
    public ImDrawFlags RoundingFlags
    {
        get => this.roundingFlags;
        set => this.HandlePropertyChange(
            nameof(this.RoundingFlags),
            ref this.roundingFlags,
            value,
            this.roundingFlags == value,
            this.OnRoundingFlagsChange);
    }

    /// <summary>Gets or sets the margin.</summary>
    public BorderVector4 Margin
    {
        get => this.margin;
        set => this.HandlePropertyChange(
            nameof(this.Margin),
            ref this.margin,
            value,
            this.margin == value,
            this.OnMarginChange);
    }

    /// <summary>Gets or sets the clockwise rotation in radians.</summary>
    public float Rotation
    {
        get => this.rotation;
        set => this.HandlePropertyChange(
            nameof(this.Rotation),
            ref this.rotation,
            value,
            this.rotation - value == 0f,
            this.OnRotationChange);
    }

    /// <inheritdoc/>
    protected override void OnDrawInside(SpannableDrawEventArgs args)
    {
        var c =
            this.ImGuiColor is not null
                ? new(ImGui.GetColorU32(this.ImGuiColor.Value))
                : this.Color;
        c = (Vector4)c * this.ColorMultiplier;

        var pos = this.Margin.LeftTop;
        var sz = Vector2.Max(this.Boundary.Size - this.Margin.Size, Vector2.Zero);
        var sz1 = Math.Min(sz.X, sz.Y);
        if (sz1 is >= float.PositiveInfinity or <= 0)
            return;

        var rs = this.EffectiveRenderScale;
        var rd = this.Rounding * rs;
        using var st = new ScopedTransformer(
            args.DrawListPtr,
            Matrix4x4.Multiply(
                Matrix4x4.Multiply(
                    Matrix4x4.CreateScale(1f / rs),
                    Matrix4x4.CreateRotationZ(this.rotation)),
                Matrix4x4.CreateTranslation(new(pos + (sz / 2), 0))),
            Vector2.One,
            1f);

        pos *= rs;
        sz *= rs;
        sz1 *= rs;

        switch (this.type)
        {
            case Shape.Rect:
                args.DrawListPtr.AddRect(sz / -2, sz / +2, c, rd, this.RoundingFlags, this.Thickness);
                break;
            case Shape.RectFilled:
                args.DrawListPtr.AddRectFilled(sz / -2, sz / +2, c, rd, this.RoundingFlags);
                break;
            case Shape.Square:
                args.DrawListPtr.AddRect(
                    new Vector2(sz1) / -2f,
                    new Vector2(sz1) / +2f,
                    c,
                    rd,
                    this.RoundingFlags,
                    this.Thickness);
                break;
            case Shape.SquareFilled:
                args.DrawListPtr.AddRectFilled(
                    new Vector2(sz1) / -2f,
                    new Vector2(sz1) / +2f,
                    c,
                    rd,
                    this.RoundingFlags);
                break;
            case Shape.Circle:
                args.DrawListPtr.AddCircle(Vector2.Zero, sz1 / 2, c, 0, this.Thickness);
                break;
            case Shape.CircleFilled:
                args.DrawListPtr.AddCircleFilled(Vector2.Zero, sz1 / 2, c);
                break;
            case Shape.Checkmark:
            {
                var t = Math.Max(sz1 / 5.0f, 1.0f);
                sz1 -= t * 0.5f;
                pos = new((t * 0.25f) - (sz1 / 2f), (t * 0.25f) - (sz1 / 2f));

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
                args.DrawListPtr.PathStroke(c, 0, t);
                break;
            }

            case Shape.EquilateralTriangle:
            {
                var r = sz1 / 2f;
                var center = new Vector2(0, r / 3f);
                var p1 = center + new Vector2(0, -r);
                var p2 = center + (r * new Vector2(-MathF.Sin(MathF.PI / 3), MathF.Cos(MathF.PI / 3)));
                var p3 = center + (r * new Vector2(+MathF.Sin(MathF.PI / 3), MathF.Cos(MathF.PI / 3)));
                args.DrawListPtr.AddTriangle(p1, p2, p3, c, this.thickness);
                break;
            }

            case Shape.EquilateralTriangleFilled:
            {
                var r = sz1 / 2f;
                var center = new Vector2(0, r / 4f);
                var p1 = center + new Vector2(0, -r);
                var p2 = center + (r * new Vector2(-MathF.Sin(MathF.PI / 3), MathF.Cos(MathF.PI / 3)));
                var p3 = center + (r * new Vector2(+MathF.Sin(MathF.PI / 3), MathF.Cos(MathF.PI / 3)));
                args.DrawListPtr.AddTriangleFilled(p1, p2, p3, c);
                break;
            }
        }
    }

    /// <summary>Raises the <see cref="TypeChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnTypeChange(PropertyChangeEventArgs<Shape> args) => this.TypeChange?.Invoke(args);

    /// <summary>Raises the <see cref="ColorChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnColorChange(PropertyChangeEventArgs<Rgba32> args) => this.ColorChange?.Invoke(args);

    /// <summary>Raises the <see cref="ImGuiColorChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnImGuiColorChange(PropertyChangeEventArgs<ImGuiCol?> args) =>
        this.ImGuiColorChange?.Invoke(args);

    /// <summary>Raises the <see cref="ColorMultiplierChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnColorMultiplierChange(PropertyChangeEventArgs<Vector4> args) =>
        this.ColorMultiplierChange?.Invoke(args);

    /// <summary>Raises the <see cref="ThicknessChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnThicknessChange(PropertyChangeEventArgs<float> args) => this.ThicknessChange?.Invoke(args);

    /// <summary>Raises the <see cref="RoundingChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnRoundingChange(PropertyChangeEventArgs<float> args) => this.RoundingChange?.Invoke(args);

    /// <summary>Raises the <see cref="RoundingFlagsChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnRoundingFlagsChange(PropertyChangeEventArgs<ImDrawFlags> args) =>
        this.RoundingFlagsChange?.Invoke(args);

    /// <summary>Raises the <see cref="MarginChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnMarginChange(PropertyChangeEventArgs<BorderVector4> args) =>
        this.MarginChange?.Invoke(args);

    /// <summary>Raises the <see cref="RotationChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnRotationChange(PropertyChangeEventArgs<float> args) => this.RotationChange?.Invoke(args);
}
