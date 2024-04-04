using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Patterns;

/// <summary>A spannable that renders borders.</summary>
public class BorderPattern : AbstractPattern
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

    /// <summary>Occurs when the property <see cref="DrawLeftChanged"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? DrawLeftChanged;

    /// <summary>Occurs when the property <see cref="DrawTopChanged"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? DrawTopChanged;

    /// <summary>Occurs when the property <see cref="DrawRightChanged"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? DrawRightChanged;

    /// <summary>Occurs when the property <see cref="DrawBottomChanged"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? DrawBottomChanged;

    /// <summary>Occurs when the property <see cref="ColorChanged"/> is changing.</summary>
    public event PropertyChangeEventHandler<Rgba32>? ColorChanged;

    /// <summary>Occurs when the property <see cref="ImGuiColorChanged"/> is changing.</summary>
    public event PropertyChangeEventHandler<ImGuiCol?>? ImGuiColorChanged;

    /// <summary>Occurs when the property <see cref="ColorMultiplierChanged"/> is changing.</summary>
    public event PropertyChangeEventHandler<Vector4>? ColorMultiplierChanged;

    /// <summary>Occurs when the property <see cref="ThicknessChanged"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? ThicknessChanged;

    /// <summary>Occurs when the property <see cref="MarginChanged"/> is changing.</summary>
    public event PropertyChangeEventHandler<BorderVector4>? MarginChanged;

    /// <summary>Gets or sets a value indicating whether to draw border on the left side.</summary>
    public bool DrawLeft
    {
        get => this.drawLeft;
        set => this.HandlePropertyChange(
            nameof(this.DrawLeft),
            ref this.drawLeft,
            value,
            this.drawLeft == value,
            this.OnDrawLeftChanged);
    }

    /// <summary>Gets or sets a value indicating whether to draw border on the top side.</summary>
    public bool DrawTop
    {
        get => this.drawTop;
        set => this.HandlePropertyChange(
            nameof(this.DrawTop),
            ref this.drawTop,
            value,
            this.drawTop == value,
            this.OnDrawTopChanged);
    }

    /// <summary>Gets or sets a value indicating whether to draw border on the right side.</summary>
    public bool DrawRight
    {
        get => this.drawRight;
        set => this.HandlePropertyChange(
            nameof(this.DrawRight),
            ref this.drawRight,
            value,
            this.drawRight == value,
            this.OnDrawRightChanged);
    }

    /// <summary>Gets or sets a value indicating whether to draw border on the bottom side.</summary>
    public bool DrawBottom
    {
        get => this.drawBottom;
        set => this.HandlePropertyChange(
            nameof(this.DrawBottom),
            ref this.drawBottom,
            value,
            this.drawBottom == value,
            this.OnDrawBottomChanged);
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
            this.OnColorChanged);
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
            this.OnImGuiColorChanged);
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
            this.OnColorMultiplierChanged);
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
            this.OnThicknessChanged);
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
            this.OnMarginChanged);
    }

    /// <inheritdoc/>
    protected override void OnDrawInside(SpannableDrawEventArgs args)
    {
        var c =
            this.ImGuiColor is not null
                ? new(ImGui.GetColorU32(this.ImGuiColor.Value))
                : this.Color;
        c = (Vector4)c * this.ColorMultiplier;

        var lt = this.Margin.LeftTop;
        var sz = Vector2.Max(this.Boundary.Size - this.Margin.Size, Vector2.Zero);
        if (sz.X is >= float.PositiveInfinity or <= 0 || sz.Y is >= float.PositiveInfinity or <= 0)
            return;

        lt *= this.EffectiveRenderScale;
        sz *= this.EffectiveRenderScale;
        var rb = lt + sz;
        using var st = new ScopedTransformer(
            args.DrawListPtr,
            Matrix4x4.Identity,
            new(1 / this.EffectiveRenderScale),
            1f);

        if (this.DrawLeft)
            args.DrawListPtr.AddLine(lt, new(lt.X, rb.Y), c, this.Thickness);
        if (this.DrawTop)
            args.DrawListPtr.AddLine(lt, new(rb.X, lt.Y), c, this.Thickness);
        if (this.DrawRight)
            args.DrawListPtr.AddLine(new(rb.X, lt.Y), rb, c, this.Thickness);
        if (this.DrawBottom)
            args.DrawListPtr.AddLine(new(lt.X, rb.Y), rb, c, this.Thickness);
    }

    /// <summary>Raises the <see cref="DrawLeftChanged"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnDrawLeftChanged(PropertyChangeEventArgs<bool> args) => this.DrawLeftChanged?.Invoke(args);

    /// <summary>Raises the <see cref="DrawTopChanged"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnDrawTopChanged(PropertyChangeEventArgs<bool> args) => this.DrawTopChanged?.Invoke(args);

    /// <summary>Raises the <see cref="DrawRightChanged"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnDrawRightChanged(PropertyChangeEventArgs<bool> args) =>
        this.DrawRightChanged?.Invoke(args);

    /// <summary>Raises the <see cref="DrawBottomChanged"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnDrawBottomChanged(PropertyChangeEventArgs<bool> args) =>
        this.DrawBottomChanged?.Invoke(args);

    /// <summary>Raises the <see cref="ColorChanged"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnColorChanged(PropertyChangeEventArgs<Rgba32> args) => this.ColorChanged?.Invoke(args);

    /// <summary>Raises the <see cref="ImGuiColorChanged"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnImGuiColorChanged(PropertyChangeEventArgs<ImGuiCol?> args) =>
        this.ImGuiColorChanged?.Invoke(args);

    /// <summary>Raises the <see cref="ColorMultiplierChanged"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnColorMultiplierChanged(PropertyChangeEventArgs<Vector4> args) =>
        this.ColorMultiplierChanged?.Invoke(args);

    /// <summary>Raises the <see cref="ThicknessChanged"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnThicknessChanged(PropertyChangeEventArgs<float> args) =>
        this.ThicknessChanged?.Invoke(args);

    /// <summary>Raises the <see cref="MarginChanged"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnMarginChanged(PropertyChangeEventArgs<BorderVector4> args) =>
        this.MarginChanged?.Invoke(args);
}
