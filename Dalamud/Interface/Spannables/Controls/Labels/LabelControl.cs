using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.RenderPassMethodArgs;
using Dalamud.Interface.Spannables.Strings;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls.Labels;

/// <summary>A label that is spannable.</summary>
public class LabelControl : ControlSpannable
{
    private const int IconSlotCount = 4;

    private readonly int childrenSlotText;
    private readonly int innerIdText;

    private readonly int childrenSlotIconBase;
    private readonly int innerIdIconBase;

    private readonly ISpannableRenderPass?[] iconSpannableRenderPasses = new ISpannableRenderPass?[IconSlotCount];

    private SpannedStringBuilder? spannedStringBuilder;
    private MemoryStream? lastLink = new();

    private ISpannable? spannableText;

    private ISpannableRenderPass? activeSpannableRenderPass;

    /// <summary>Initializes a new instance of the <see cref="LabelControl"/> class.</summary>
    public LabelControl()
    {
        this.childrenSlotText = this.AllSpannablesAvailableSlot++;
        this.innerIdText = this.InnerIdAvailableSlot++;
        this.AllSpannables.Add(this.spannedStringBuilder = new());

        this.childrenSlotIconBase = this.AllSpannablesAvailableSlot;
        this.innerIdIconBase = this.InnerIdAvailableSlot;
        this.AllSpannablesAvailableSlot += IconSlotCount;
        this.InnerIdAvailableSlot += IconSlotCount;
        for (var i = 0; i < IconSlotCount; i++)
            this.AllSpannables.Add(null);
    }

    /// <summary>Occurs when the mouse pointer enters a link in the control.</summary>
    public event ControlMouseLinkEventHandler? LinkMouseEnter;

    /// <summary>Occurs when the mouse pointer leaves a link in the control.</summary>
    public event ControlMouseLinkEventHandler? LinkMouseLeave;

    /// <summary>Occurs when a link in the control is clicked by the mouse.</summary>
    public event ControlMouseLinkEventHandler? LinkMouseClick;

    /// <summary>Occurs when the property <see cref="SpannableText"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, ISpannable?>? SpannableTextChange;

    /// <summary>Occurs when the property <see cref="LeftIcon"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, ISpannable?>? LeftIconChange;

    /// <summary>Occurs when the property <see cref="TopIcon"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, ISpannable?>? TopIconChange;

    /// <summary>Occurs when the property <see cref="RightIcon"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, ISpannable?>? RightIconChange;

    /// <summary>Occurs when the property <see cref="BottomIcon"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, ISpannable?>? BottomIconChange;

    /// <summary>Gets or sets a spannable text.</summary>
    /// <remarks>Having this property set takes priority over <see cref="ControlSpannable.Text"/>.</remarks>
    public ISpannable? SpannableText
    {
        get => this.spannableText;
        set => this.HandlePropertyChange(
            nameof(this.SpannableText),
            ref this.spannableText,
            value,
            this.OnSpannableTextChange);
    }

    /// <summary>Gets or sets the icon size.</summary>
    public Vector2 IconSize { get; set; }

    /// <summary>Gets or sets the spannable to display on the left side.</summary>
    /// <remarks>Primary use case is for displaying an icon.</remarks>
    public ISpannable? LeftIcon
    {
        get => this.AllSpannables[this.childrenSlotIconBase + 0];
        set => this.HandlePropertyChange(
            nameof(this.LeftIcon),
            ref CollectionsMarshal.AsSpan(this.AllSpannables)[this.childrenSlotIconBase + 0],
            value,
            this.OnLeftIconChange);
    }

    /// <summary>Gets or sets the spannable to display on the top side.</summary>
    /// <remarks>Primary use case is for displaying an icon.</remarks>
    public ISpannable? TopIcon
    {
        get => this.AllSpannables[this.childrenSlotIconBase + 1];
        set => this.HandlePropertyChange(
            nameof(this.TopIcon),
            ref CollectionsMarshal.AsSpan(this.AllSpannables)[this.childrenSlotIconBase + 1],
            value,
            this.OnTopIconChange);
    }

    /// <summary>Gets or sets the spannable to display on the right side.</summary>
    /// <remarks>Primary use case is for displaying an icon.</remarks>
    public ISpannable? RightIcon
    {
        get => this.AllSpannables[this.childrenSlotIconBase + 2];
        set => this.HandlePropertyChange(
            nameof(this.RightIcon),
            ref CollectionsMarshal.AsSpan(this.AllSpannables)[this.childrenSlotIconBase + 2],
            value,
            this.OnRightIconChange);
    }

    /// <summary>Gets or sets the spannable to display on the bottom side.</summary>
    /// <remarks>Primary use case is for displaying an icon.</remarks>
    public ISpannable? BottomIcon
    {
        get => this.AllSpannables[this.childrenSlotIconBase + 3];
        set => this.HandlePropertyChange(
            nameof(this.BottomIcon),
            ref CollectionsMarshal.AsSpan(this.AllSpannables)[this.childrenSlotIconBase + 3],
            value,
            this.OnBottomIconChange);
    }

    /// <summary>Gets or sets the currently active spannable between <see cref="spannableText"/> and
    /// <see cref="spannedStringBuilder"/>.</summary>
    private ISpannable ActiveSpannable
    {
        get => this.AllSpannables[this.childrenSlotText] ?? throw new ObjectDisposedException(this.GetType().Name);
        set
        {
            if (ReferenceEquals(value, this.AllSpannables[this.childrenSlotText]))
                return;

            this.AllSpannables[this.childrenSlotText]?.ReturnRenderPass(this.activeSpannableRenderPass);
            this.AllSpannables[this.childrenSlotText] = value;
        }
    }

    /// <summary>Gets the spannables used as icons.</summary>
    private ReadOnlySpan<ISpannable?> IconSpannables =>
        CollectionsMarshal.AsSpan(this.AllSpannables).Slice(this.childrenSlotIconBase, IconSlotCount);

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.spannedStringBuilder?.Dispose();
            this.spannedStringBuilder = null;
            this.lastLink?.Dispose();
            this.lastLink = null;
            this.spannableText?.Dispose();
            this.spannableText = null;
            this.spannedStringBuilder?.Dispose();
            this.spannedStringBuilder = null!;
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    protected override RectVector4 MeasureContentBox(SpannableMeasureArgs args, in RectVector4 availableContentBox)
    {
        if (this.IsDisposed)
            return RectVector4.InvertedExtrema;

        for (var i = 0; i < IconSlotCount; i++)
        {
            if (this.IconSpannables[i] is not { } sp)
                continue;
            ref var rp = ref this.iconSpannableRenderPasses[i];
            rp ??= sp.RentRenderPass(this.Renderer);
            args.NotifyChild(
                sp,
                rp,
                this.innerIdIconBase + i,
                availableContentBox.Size,
                this.ActiveTextState);
        }

        var spannable = this.ActiveSpannable;
        this.activeSpannableRenderPass ??= spannable.RentRenderPass(this.Renderer);
        args.NotifyChild(
            spannable,
            this.activeSpannableRenderPass,
            this.innerIdText,
            new(
                availableContentBox.Right >= float.PositiveInfinity ||
                availableContentBox.Left <= float.NegativeInfinity
                    ? float.PositiveInfinity
                    : Math.Max(
                        0,
                        availableContentBox.Width
                        - (this.iconSpannableRenderPasses[0]?.Boundary.Width ?? 0)
                        - (this.iconSpannableRenderPasses[2]?.Boundary.Width ?? 0)),
                availableContentBox.Bottom >= float.PositiveInfinity ||
                availableContentBox.Top <= float.NegativeInfinity
                    ? float.PositiveInfinity
                    : Math.Max(
                        0,
                        availableContentBox.Height
                        - (this.iconSpannableRenderPasses[1]?.Boundary.Height ?? 0)
                        - (this.iconSpannableRenderPasses[3]?.Boundary.Height ?? 0))),
            this.ActiveTextState);

        var b = RectVector4.Normalize(this.activeSpannableRenderPass.Boundary);
        b.Right += this.iconSpannableRenderPasses[0]?.Boundary.Width ?? 0;
        b.Right += this.iconSpannableRenderPasses[2]?.Boundary.Width ?? 0;
        b.Right = Math.Max(b.Right, this.iconSpannableRenderPasses[1]?.Boundary.Width ?? 0);
        b.Right = Math.Max(b.Right, this.iconSpannableRenderPasses[3]?.Boundary.Width ?? 0);
        b.Bottom += this.iconSpannableRenderPasses[1]?.Boundary.Height ?? 0;
        b.Bottom += this.iconSpannableRenderPasses[3]?.Boundary.Height ?? 0;
        b.Bottom = Math.Max(b.Bottom, this.iconSpannableRenderPasses[0]?.Boundary.Height ?? 0);
        b.Bottom = Math.Max(b.Bottom, this.iconSpannableRenderPasses[2]?.Boundary.Height ?? 0);

        var res = availableContentBox;
        if (this.IsWidthWrapContent)
            res.Right = res.Left + b.Right;
        if (this.IsHeightWrapContent)
            res.Bottom = res.Top + b.Bottom;
        return res;
    }

    /// <inheritdoc/>
    protected override void OnCommitMeasurement(ControlCommitMeasurementEventArgs args)
    {
        base.OnCommitMeasurement(args);

        if (this.IsDisposed || this.activeSpannableRenderPass is null)
            return;

        // Icons get all the space they want. They may overlap.
        for (var i = 0; i < IconSlotCount; i++)
        {
            if (this.IconSpannables[i] is not { } sp || this.iconSpannableRenderPasses[i] is not { } rp)
                continue;
            args.SpannableArgs.NotifyChild(
                sp,
                rp,
                i switch
                {
                    0 => this.MeasuredContentBox.LeftTop,
                    1 => this.MeasuredContentBox.LeftTop,
                    2 => this.MeasuredContentBox.RightTop - new Vector2(rp.Boundary.Width, 0),
                    3 => this.MeasuredContentBox.LeftBottom - new Vector2(0, rp.Boundary.Height),
                    _ => Vector2.Zero,
                },
                Matrix4x4.Identity);
        }

        var lt =
            this.MeasuredContentBox.LeftTop
            + new Vector2(
                this.iconSpannableRenderPasses[0]?.Boundary.Width ?? 0,
                this.iconSpannableRenderPasses[1]?.Boundary.Height ?? 0);

        // If we don't have enough space for the text, try to center align it.
        var availTextSize =
            this.MeasuredContentBox.Size
            - new Vector2(
                this.iconSpannableRenderPasses[0]?.Boundary.Width ?? 0
                + this.iconSpannableRenderPasses[2]?.Boundary.Width ?? 0,
                this.iconSpannableRenderPasses[1]?.Boundary.Height ?? 0
                + this.iconSpannableRenderPasses[3]?.Boundary.Height ?? 0);
        lt.X -= MathF.Round((this.activeSpannableRenderPass.Boundary.Width - availTextSize.X) / 2f);
        lt.Y -= MathF.Round((this.activeSpannableRenderPass.Boundary.Height - availTextSize.Y) / 2f);

        args.SpannableArgs.NotifyChild(this.ActiveSpannable, this.activeSpannableRenderPass, lt, Matrix4x4.Identity);
    }

    /// <inheritdoc/>
    protected override void OnDraw(ControlDrawEventArgs args)
    {
        base.OnDraw(args);

        if (this.IsDisposed || this.activeSpannableRenderPass is null)
            return;

        for (var i = 0; i < IconSlotCount; i++)
        {
            if (this.IconSpannables[i] is not { } sp || this.iconSpannableRenderPasses[i] is not { } rp)
                continue;
            args.SpannableArgs.NotifyChild(sp, rp);
        }

        args.SpannableArgs.NotifyChild(this.ActiveSpannable, this.activeSpannableRenderPass);
    }

    /// <inheritdoc/>
    protected override void OnHandleInteraction(
        ControlHandleInteractionEventArgs args,
        out SpannableLinkInteracted link)
    {
        base.OnHandleInteraction(args, out link);

        if (this.IsDisposed)
            return;

        if (!link.IsEmpty)
        {
            if (this.activeSpannableRenderPass is not null && this.Enabled)
            {
                args.SpannableArgs.NotifyChild(
                    this.ActiveSpannable,
                    this.activeSpannableRenderPass,
                    out _);
            }
        }
        else if (this.activeSpannableRenderPass is not null && this.Enabled)
        {
            args.SpannableArgs.NotifyChild(
                this.ActiveSpannable,
                this.activeSpannableRenderPass,
                out link);
        }

        for (var i = 0; i < IconSlotCount; i++)
        {
            if (this.IconSpannables[i] is not { } sp || this.iconSpannableRenderPasses[i] is not { } rp)
                continue;
            if (!link.IsEmpty)
                args.SpannableArgs.NotifyChild(sp, rp, out link);
            else
                args.SpannableArgs.NotifyChild(sp, rp, out _);
        }

        Debug.Assert(this.lastLink is not null, "LastLink must not be null if not disposed");

        if (!this.lastLink!.GetDataSpan().SequenceEqual(link.Link))
        {
            if (this.lastLink.Length != 0)
            {
                this.OnLinkMouseLeave(new() { Sender = this, Link = this.lastLink.GetDataSpan() });
                this.lastLink.Clear();
            }

            this.lastLink.Write(link.Link);
            this.OnLinkMouseEnter(new() { Sender = this, Link = this.lastLink.GetDataSpan() });
        }

        if (link.IsMouseClicked)
        {
            this.OnLinkMouseClick(
                new()
                {
                    Sender = this,
                    Button = link.ClickedMouseButton,
                    Link = this.lastLink.GetDataSpan(),
                });
        }
    }

    /// <summary>Raises the <see cref="LinkMouseEnter"/> event.</summary>
    /// <param name="args">A <see cref="ControlMouseLinkEventArgs"/> that contains the event data.</param>
    protected virtual void OnLinkMouseEnter(ControlMouseLinkEventArgs args) =>
        this.LinkMouseEnter?.Invoke(args);

    /// <summary>Raises the <see cref="LinkMouseLeave"/> event.</summary>
    /// <param name="args">A <see cref="ControlMouseLinkEventArgs"/> that contains the event data.</param>
    protected virtual void OnLinkMouseLeave(ControlMouseLinkEventArgs args) =>
        this.LinkMouseLeave?.Invoke(args);

    /// <summary>Raises the <see cref="LinkMouseClick"/> event.</summary>
    /// <param name="args">A <see cref="ControlMouseLinkEventArgs"/> that contains the event data.</param>
    protected virtual void OnLinkMouseClick(ControlMouseLinkEventArgs args) =>
        this.LinkMouseClick?.Invoke(args);

    /// <inheritdoc/>
    protected override void OnTextChange(PropertyChangeEventArgs<ControlSpannable, string?> args)
    {
        if (this.spannedStringBuilder is null)
            return;

        this.spannedStringBuilder.Clear().Append(args.NewValue);
        this.ActiveSpannable = this.spannableText ?? this.spannedStringBuilder;
        base.OnTextChange(args);
    }

    /// <summary>Raises the <see cref="SpannableTextChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{TSender,T}"/> that contains the event data.</param>
    protected virtual void OnSpannableTextChange(PropertyChangeEventArgs<ControlSpannable, ISpannable?> args)
    {
        if (this.spannedStringBuilder is null)
            return;

        this.ActiveSpannable.ReturnRenderPass(this.activeSpannableRenderPass);
        this.activeSpannableRenderPass = null;

        this.ActiveSpannable = this.spannableText ?? this.spannedStringBuilder;
        this.SpannableTextChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="LeftIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{TSender,T}"/> that contains the event data.</param>
    protected virtual void OnLeftIconChange(PropertyChangeEventArgs<ControlSpannable, ISpannable?> args)
    {
        args.PreviousValue?.ReturnRenderPass(this.iconSpannableRenderPasses[0]);
        this.iconSpannableRenderPasses[0] = null;
        this.LeftIconChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="TopIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{TSender,T}"/> that contains the event data.</param>
    protected virtual void OnTopIconChange(PropertyChangeEventArgs<ControlSpannable, ISpannable?> args)
    {
        args.PreviousValue?.ReturnRenderPass(this.iconSpannableRenderPasses[1]);
        this.iconSpannableRenderPasses[1] = null;
        this.TopIconChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="RightIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{TSender,T}"/> that contains the event data.</param>
    protected virtual void OnRightIconChange(PropertyChangeEventArgs<ControlSpannable, ISpannable?> args)
    {
        args.PreviousValue?.ReturnRenderPass(this.iconSpannableRenderPasses[2]);
        this.iconSpannableRenderPasses[2] = null;
        this.RightIconChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="BottomIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{TSender,T}"/> that contains the event data.</param>
    protected virtual void OnBottomIconChange(PropertyChangeEventArgs<ControlSpannable, ISpannable?> args)
    {
        args.PreviousValue?.ReturnRenderPass(this.iconSpannableRenderPasses[3]);
        this.iconSpannableRenderPasses[3] = null;
        this.BottomIconChange?.Invoke(args);
    }
}
