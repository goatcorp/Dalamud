using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.RenderPassMethodArgs;
using Dalamud.Interface.Spannables.Text;
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

    // Properties
    private ISpannable? spannableText;
    private BorderVector4 textMargin;

    // States
    private TextSpannableBuilder? textSpannableBuilder;
    private MemoryStream? lastLink = new();

    private ISpannableRenderPass? activeSpannableRenderPass;

    /// <summary>Initializes a new instance of the <see cref="LabelControl"/> class.</summary>
    public LabelControl()
    {
        this.childrenSlotText = this.AllSpannablesAvailableSlot++;
        this.innerIdText = this.InnerIdAvailableSlot++;
        this.AllSpannables.Add(this.textSpannableBuilder = new());

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

    /// <summary>Occurs when the property <see cref="TextMargin"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, BorderVector4>? TextMarginChange;

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

    /// <summary>Gets or sets the text margin, which is the gap between icons and the text.</summary>
    /// <remarks>The value will be scaled by <see cref="ControlSpannable.Scale"/>.</remarks>
    public BorderVector4 TextMargin
    {
        get => this.textMargin;
        set => this.HandlePropertyChange(nameof(this.TextMargin), ref this.textMargin, value, this.OnTextMarginChange);
    }

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
    /// <see cref="textSpannableBuilder"/>.</summary>
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
            this.textSpannableBuilder?.Dispose();
            this.textSpannableBuilder = null;
            this.lastLink?.Dispose();
            this.lastLink = null;
            this.spannableText?.Dispose();
            this.spannableText = null;
            this.textSpannableBuilder?.Dispose();
            this.textSpannableBuilder = null!;
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    protected override RectVector4 MeasureContentBox(SpannableMeasureArgs args)
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
                Vector2.Zero,
                args.MaxSize - this.TextMargin.Size,
                this.ActiveTextState);
        }

        var totalIconSize = this.TextMargin.Size;
        totalIconSize.X += this.iconSpannableRenderPasses[0]?.Boundary.Width ?? 0;
        totalIconSize.X += this.iconSpannableRenderPasses[2]?.Boundary.Width ?? 0;
        totalIconSize.Y += this.iconSpannableRenderPasses[1]?.Boundary.Height ?? 0;
        totalIconSize.Y += this.iconSpannableRenderPasses[3]?.Boundary.Height ?? 0;

        var spannable = this.ActiveSpannable;
        this.activeSpannableRenderPass ??= spannable.RentRenderPass(this.Renderer);
        args.NotifyChild(
            spannable,
            this.activeSpannableRenderPass,
            this.innerIdText,
            Vector2.Max(Vector2.Zero, args.MinSize - totalIconSize),
            Vector2.Max(Vector2.Zero, args.MaxSize - totalIconSize),
            this.ActiveTextState);

        var b = RectVector4.Normalize(this.activeSpannableRenderPass.Boundary).RightBottom;
        b += totalIconSize;
        b.X = Math.Max(b.X, this.iconSpannableRenderPasses[1]?.Boundary.Width ?? 0);
        b.X = Math.Max(b.X, this.iconSpannableRenderPasses[3]?.Boundary.Width ?? 0);
        b.Y = Math.Max(b.Y, this.iconSpannableRenderPasses[0]?.Boundary.Height ?? 0);
        b.Y = Math.Max(b.Y, this.iconSpannableRenderPasses[2]?.Boundary.Height ?? 0);

        if (!this.IsWidthWrapContent && args.SuggestedSize.X < float.MaxValue)
            b.X = args.SuggestedSize.X;
        if (!this.IsHeightWrapContent && args.SuggestedSize.Y < float.MaxValue)
            b.Y = args.SuggestedSize.Y;
        return RectVector4.FromCoordAndSize(Vector2.Zero, Vector2.Clamp(b, args.MinSize, args.MaxSize));
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
            var iconLt = Vector2.Zero;
            switch (i)
            {
                case 0:
                    iconLt = this.MeasuredContentBox.LeftTop;
                    break;
                case 1:
                    iconLt = this.MeasuredContentBox.LeftTop;
                    break;
                case 2:
                    iconLt = this.MeasuredContentBox.RightTop - new Vector2(rp.Boundary.Width, 0);
                    break;
                case 3:
                    iconLt = this.MeasuredContentBox.LeftBottom - new Vector2(0, rp.Boundary.Height);
                    break;
            }

            if ((i & 1) == 0)
                iconLt.Y += (this.MeasuredContentBox.Height - rp.Boundary.Height) / 2f;
            else
                iconLt.X += (this.MeasuredContentBox.Width - rp.Boundary.Width) / 2f;

            args.SpannableArgs.NotifyChild(sp, rp, iconLt, Matrix4x4.Identity);
        }

        var lt =
            this.MeasuredContentBox.LeftTop
            + new Vector2(
                this.iconSpannableRenderPasses[0]?.Boundary.Width ?? 0,
                this.iconSpannableRenderPasses[1]?.Boundary.Height ?? 0)
            + this.textMargin.LeftTop;
        var rb =
            this.MeasuredContentBox.RightBottom
            - new Vector2(
                this.iconSpannableRenderPasses[2]?.Boundary.Width ?? 0,
                this.iconSpannableRenderPasses[3]?.Boundary.Height ?? 0)
            - this.TextMargin.RightBottom;

        // If we don't have enough space for the text, try to center align it.
        var availTextSize = rb - lt;
        if (this.activeSpannableRenderPass.Boundary.Width > availTextSize.X)
            lt.X -= MathF.Round((this.activeSpannableRenderPass.Boundary.Width - availTextSize.X) / 2f);
        if (this.activeSpannableRenderPass.Boundary.Height > availTextSize.Y)
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

        ControlMouseLinkEventArgs? e = null;
        if (!this.lastLink!.GetDataSpan().SequenceEqual(link.Link))
        {
            e = ControlEventArgsPool.Rent<ControlMouseLinkEventArgs>();
            e.Sender = this;
            e.Link = this.lastLink.GetDataMemory();
            
            if (this.lastLink.Length != 0)
            {
                this.OnLinkMouseLeave(e);
                this.lastLink.Clear();
            }

            this.lastLink.Write(link.Link);
            e.Link = this.lastLink.GetDataMemory();
            this.OnLinkMouseEnter(e);
        }

        if (link.IsMouseClicked)
        {
            e ??= ControlEventArgsPool.Rent<ControlMouseLinkEventArgs>();
            e.Sender = this;
            e.Link = this.lastLink.GetDataMemory();
            this.OnLinkMouseClick(e);
        }
            
        ControlEventArgsPool.Return(e);
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
        if (this.textSpannableBuilder is null)
            return;

        this.textSpannableBuilder.Clear().Append(args.NewValue);
        this.ActiveSpannable = this.spannableText ?? this.textSpannableBuilder;
        base.OnTextChange(args);
    }

    /// <summary>Raises the <see cref="SpannableTextChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{TSender,T}"/> that contains the event data.</param>
    protected virtual void OnSpannableTextChange(PropertyChangeEventArgs<ControlSpannable, ISpannable?> args)
    {
        if (this.textSpannableBuilder is null)
            return;

        this.ActiveSpannable.ReturnRenderPass(this.activeSpannableRenderPass);
        this.activeSpannableRenderPass = null;

        this.ActiveSpannable = this.spannableText ?? this.textSpannableBuilder;
        this.SpannableTextChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="TextMarginChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{TSender,T}"/> that contains the event data.</param>
    protected virtual void OnTextMarginChange(PropertyChangeEventArgs<ControlSpannable, BorderVector4> args) =>
        this.TextMarginChange?.Invoke(args);

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
