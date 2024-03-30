using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Styles;
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

    private readonly ISpannableMeasurement?[] iconMeasurements = new ISpannableMeasurement?[IconSlotCount];

    // Properties
    private ISpannable? spannableText;
    private ISpannableMeasurementOptions? spannableTextOptions;
    private BorderVector4 textMargin;
    private Vector2 alignment;

    // States
    private TextSpannableBuilder? textSpannableBuilder;
    private MemoryStream? lastLink = new();

    private ISpannableMeasurement? activeSpannableMeasurement;

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

    /// <summary>Occurs when the property <see cref="SpannableText"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannable?>? SpannableTextChange;

    /// <summary>Occurs when the property <see cref="SpannableText"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannableMeasurementOptions?>?
        SpannableTextOptionsChange;

    /// <summary>Occurs when the property <see cref="TextMargin"/> is changing.</summary>
    public event PropertyChangeEventHandler<BorderVector4>? TextMarginChange;

    /// <summary>Occurs when the property <see cref="Alignment"/> is changing.</summary>
    public event PropertyChangeEventHandler<Vector2>? AlignmentChange;

    /// <summary>Occurs when the property <see cref="LeftIcon"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannable?>? LeftIconChange;

    /// <summary>Occurs when the property <see cref="TopIcon"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannable?>? TopIconChange;

    /// <summary>Occurs when the property <see cref="RightIcon"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannable?>? RightIconChange;

    /// <summary>Occurs when the property <see cref="BottomIcon"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannable?>? BottomIconChange;

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

    /// <summary>Gets or sets options for <see cref="SpannableText"/>.</summary>
    /// <remarks>
    /// <para><see cref="ControlSpannable.TextStyle"/> will override this.</para>
    /// <para>The change will not be reflected if the referenced object is the same. Unset and set this property if
    /// the innards of the reference object got changed.</para>
    /// </remarks>
    public ISpannableMeasurementOptions? SpannableTextOptions
    {
        get => this.spannableTextOptions;
        set => this.HandlePropertyChange(
            nameof(this.SpannableTextOptions),
            ref this.spannableTextOptions,
            value,
            this.OnSpannableTextOptionsChange);
    }

    /// <summary>Gets or sets the text margin, which is the gap between icons and the text.</summary>
    /// <remarks>The value will be scaled by <see cref="ControlSpannable.EffectiveRenderScale"/>.</remarks>
    public BorderVector4 TextMargin
    {
        get => this.textMargin;
        set => this.HandlePropertyChange(nameof(this.TextMargin), ref this.textMargin, value, this.OnTextMarginChange);
    }

    /// <summary>Gets or sets the alignment of the content.</summary>
    /// <value>(0, ?) is left, (1, ?) is right, (?, 0) is top, and (?, 1) is bottom.</value>
    public Vector2 Alignment
    {
        get => this.alignment;
        set => this.HandlePropertyChange(nameof(this.Alignment), ref this.alignment, value, this.OnAlignmentChange);
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

            this.AllSpannables[this.childrenSlotText] = value;
        }
    }

    /// <summary>Gets the spannables used as icons.</summary>
    private ReadOnlySpan<ISpannable?> IconSpannables =>
        CollectionsMarshal.AsSpan(this.AllSpannables).Slice(this.childrenSlotIconBase, IconSlotCount);

    /// <inheritdoc/>
    public override ISpannableMeasurement? FindChildMeasurementAt(Vector2 screenOffset)
    {
        foreach (var m in this.iconMeasurements)
        {
            if (m is null)
                continue;
            if (m.Boundary.Contains(m.PointToClient(screenOffset)))
                return m;
        }

        if (this.activeSpannableMeasurement is { } asm)
        {
            if (!Matrix4x4.Invert(asm.FullTransformation, out var inv)
                && asm.Boundary.Contains(Vector2.Transform(screenOffset, inv)))
                return asm;
        }

        return base.FindChildMeasurementAt(screenOffset);
    }

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
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    protected override RectVector4 MeasureContentBox(Vector2 suggestedSize)
    {
        if (this.IsDisposed)
            return RectVector4.InvertedExtrema;

        for (var i = 0; i < IconSlotCount; i++)
        {
            if (this.IconSpannables[i] is not { } sp)
                continue;
            ref var im = ref this.iconMeasurements[i];
            if (!ReferenceEquals(im?.Spannable, sp))
            {
                im?.Spannable?.ReturnMeasurement(im);
                im = null;
            }

            im ??= sp.RentMeasurement(this.Renderer);
            im.RenderScale = this.EffectiveRenderScale;
            im.ImGuiGlobalId = this.GetGlobalIdFromInnerId(this.innerIdIconBase + i);
            im.Options.Size = suggestedSize - this.textMargin.Size;
            im.Options.VisibleSize = this.MeasurementOptions.VisibleSize - this.textMargin.Size;
            im.Measure();
        }

        var totalIconSize = this.textMargin.Size;
        totalIconSize.X += this.iconMeasurements[0]?.Boundary.Width ?? 0;
        totalIconSize.X += this.iconMeasurements[2]?.Boundary.Width ?? 0;
        totalIconSize.Y += this.iconMeasurements[1]?.Boundary.Height ?? 0;
        totalIconSize.Y += this.iconMeasurements[3]?.Boundary.Height ?? 0;

        var spannable = this.ActiveSpannable;
        if (!ReferenceEquals(this.activeSpannableMeasurement?.Spannable, this.ActiveSpannable))
        {
            this.activeSpannableMeasurement?.Spannable?.ReturnMeasurement(this.activeSpannableMeasurement);
            this.activeSpannableMeasurement = null;
        }

        if (this.activeSpannableMeasurement is null)
        {
            this.activeSpannableMeasurement = spannable.RentMeasurement(this.Renderer);

            if (this.spannableTextOptions is not null)
                this.activeSpannableMeasurement.Options.CopyFrom(this.spannableTextOptions);

            if (this.activeSpannableMeasurement.Options is TextSpannableBase.Options mo)
                mo.Style = this.TextStyle;
        }

        this.activeSpannableMeasurement.RenderScale = this.EffectiveRenderScale;
        this.activeSpannableMeasurement.ImGuiGlobalId = this.GetGlobalIdFromInnerId(this.innerIdText);
        this.activeSpannableMeasurement.Options.Size = suggestedSize - totalIconSize;
        this.activeSpannableMeasurement.Measure();

        var bb = RectVector4.Normalize(this.activeSpannableMeasurement.Boundary);
        var b = Vector2.Max(bb.Size, bb.RightBottom);
        b += totalIconSize;
        b.X = Math.Max(b.X, this.iconMeasurements[1]?.Boundary.Width ?? 0);
        b.X = Math.Max(b.X, this.iconMeasurements[3]?.Boundary.Width ?? 0);
        b.Y = Math.Max(b.Y, this.iconMeasurements[0]?.Boundary.Height ?? 0);
        b.Y = Math.Max(b.Y, this.iconMeasurements[2]?.Boundary.Height ?? 0);

        if (!this.IsWidthWrapContent && suggestedSize.X < float.PositiveInfinity)
            b.X = suggestedSize.X;
        if (!this.IsHeightWrapContent && suggestedSize.Y < float.PositiveInfinity)
            b.Y = suggestedSize.Y;
        return RectVector4.FromCoordAndSize(Vector2.Zero, b);
    }

    /// <inheritdoc/>
    protected override void OnUpdateTransformation(SpannableEventArgs args)
    {
        base.OnUpdateTransformation(args);

        if (this.IsDisposed || this.activeSpannableMeasurement is null)
            return;

        // Icons get all the space they want. They may overlap.
        for (var i = 0; i < IconSlotCount; i++)
        {
            if (this.iconMeasurements[i] is not { } rp)
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

            iconLt = iconLt.Round(1f / this.EffectiveRenderScale);
            rp.UpdateTransformation(Matrix4x4.CreateTranslation(new(iconLt, 0)), this.FullTransformation);
        }

        var lt =
            this.MeasuredContentBox.LeftTop
            + new Vector2(
                this.iconMeasurements[0]?.Boundary.Width ?? 0,
                this.iconMeasurements[1]?.Boundary.Height ?? 0)
            + this.textMargin.LeftTop;
        var rb =
            this.MeasuredContentBox.RightBottom
            - new Vector2(
                this.iconMeasurements[2]?.Boundary.Width ?? 0,
                this.iconMeasurements[3]?.Boundary.Height ?? 0)
            - this.textMargin.RightBottom;

        // If we don't have enough space for the text, try to center align it.
        var availTextSize = rb - lt;
        lt -= (this.activeSpannableMeasurement.Boundary.Size - availTextSize) * this.alignment;
        lt = lt.Round(1f / this.EffectiveRenderScale);
        this.activeSpannableMeasurement.UpdateTransformation(
            Matrix4x4.CreateTranslation(new(lt, 0)),
            this.FullTransformation);
    }

    /// <inheritdoc/>
    protected override void OnDraw(SpannableDrawEventArgs args)
    {
        base.OnDraw(args);

        if (this.IsDisposed || this.activeSpannableMeasurement is null)
            return;

        for (var i = 0; i < IconSlotCount; i++)
            this.iconMeasurements[i]?.Draw(args.DrawListPtr);

        this.activeSpannableMeasurement.Draw(args.DrawListPtr);
    }

    /// <inheritdoc/>
    protected override void OnHandleInteraction(SpannableEventArgs args)
    {
        base.OnHandleInteraction(args);

        if (this.IsDisposed)
            return;

        this.activeSpannableMeasurement?.HandleInteraction();

        for (var i = 0; i < IconSlotCount; i++)
            this.iconMeasurements[i]?.HandleInteraction();

        Debug.Assert(this.lastLink is not null, "LastLink must not be null if not disposed");

        SpannableMouseLinkEventArgs? e = null;
        if (this.activeSpannableMeasurement is not TextSpannableBase.Measurement tsmm)
        {
            if (this.lastLink.Length != 0)
            {
                e = SpannableEventArgsPool.Rent<SpannableMouseLinkEventArgs>();
                e.Sender = this;
                e.Link = this.lastLink.GetDataMemory();
                this.OnLinkMouseLeave(e);
                this.lastLink.Clear();
                SpannableEventArgsPool.Return(e);
            }

            return;
        }

        var issueClickEvent = false;
        switch (tsmm.GetInteractedLink(out var link))
        {
            case TextSpannableBase.LinkState.Clear:
            default:
                link = default;
                break;

            case TextSpannableBase.LinkState.Hovered:
            case TextSpannableBase.LinkState.Active:
                break;

            case TextSpannableBase.LinkState.Clicked:
                issueClickEvent = true;
                break;
        }

        if (!this.lastLink!.GetDataSpan().SequenceEqual(link))
        {
            e = SpannableEventArgsPool.Rent<SpannableMouseLinkEventArgs>();
            e.Sender = this;
            e.Link = this.lastLink.GetDataMemory();

            if (this.lastLink.Length != 0)
            {
                this.OnLinkMouseLeave(e);
                this.lastLink.Clear();
            }

            this.lastLink.Write(link);
            e.Link = this.lastLink.GetDataMemory();
            this.OnLinkMouseEnter(e);
        }

        if (issueClickEvent)
        {
            e ??= SpannableEventArgsPool.Rent<SpannableMouseLinkEventArgs>();
            e.Sender = this;
            e.Link = this.lastLink.GetDataMemory();
            this.OnLinkMouseClick(e);
        }

        SpannableEventArgsPool.Return(e);
    }

    /// <summary>Raises the <see cref="LinkMouseEnter"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
    protected virtual void OnLinkMouseEnter(SpannableMouseLinkEventArgs args) =>
        this.LinkMouseEnter?.Invoke(args);

    /// <summary>Raises the <see cref="LinkMouseLeave"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
    protected virtual void OnLinkMouseLeave(SpannableMouseLinkEventArgs args) =>
        this.LinkMouseLeave?.Invoke(args);

    /// <summary>Raises the <see cref="LinkMouseClick"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
    protected virtual void OnLinkMouseClick(SpannableMouseLinkEventArgs args) =>
        this.LinkMouseClick?.Invoke(args);

    /// <inheritdoc/>
    protected override void OnTextChange(PropertyChangeEventArgs<string?> args)
    {
        ObjectDisposedException.ThrowIf(this.textSpannableBuilder is null, this);

        if (args.State == PropertyChangeState.After)
        {
            this.textSpannableBuilder.Clear().Append(args.NewValue);
            this.ActiveSpannable = this.spannableText ?? this.textSpannableBuilder;
        }

        base.OnTextChange(args);
    }

    /// <inheritdoc/>
    protected override void OnTextStyleChange(PropertyChangeEventArgs<TextStyle> args)
    {
        if (args.State == PropertyChangeState.After
            && this.activeSpannableMeasurement?.Options is TextSpannableBase.Options mo)
            mo.Style = args.NewValue;
        base.OnTextStyleChange(args);
    }

    /// <summary>Raises the <see cref="SpannableTextChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnSpannableTextChange(PropertyChangeEventArgs<ISpannable?> args)
    {
        ObjectDisposedException.ThrowIf(this.textSpannableBuilder is null, this);

        if (args.State == PropertyChangeState.After)
            this.ActiveSpannable = this.spannableText ?? this.textSpannableBuilder;

        this.SpannableTextChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="SpannableTextOptionsChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnSpannableTextOptionsChange(
        PropertyChangeEventArgs<ISpannableMeasurementOptions?> args)
    {
        ObjectDisposedException.ThrowIf(this.textSpannableBuilder is null, this);

        if (args.State == PropertyChangeState.After)
        {
            if (args.NewValue is null)
                this.activeSpannableMeasurement?.Options.TryReset();
            else
                this.activeSpannableMeasurement?.Options.CopyFrom(args.NewValue);

            if (this.activeSpannableMeasurement?.Options is TextSpannableBase.Options mo)
                mo.Style = this.TextStyle;
        }

        this.SpannableTextOptionsChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="TextMarginChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnTextMarginChange(PropertyChangeEventArgs<BorderVector4> args) =>
        this.TextMarginChange?.Invoke(args);

    /// <summary>Raises the <see cref="AlignmentChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnAlignmentChange(PropertyChangeEventArgs<Vector2> args) =>
        this.AlignmentChange?.Invoke(args);

    /// <summary>Raises the <see cref="LeftIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnLeftIconChange(PropertyChangeEventArgs<ISpannable?> args) =>
        this.LeftIconChange?.Invoke(args);

    /// <summary>Raises the <see cref="TopIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnTopIconChange(PropertyChangeEventArgs<ISpannable?> args) =>
        this.TopIconChange?.Invoke(args);

    /// <summary>Raises the <see cref="RightIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnRightIconChange(PropertyChangeEventArgs<ISpannable?> args) =>
        this.RightIconChange?.Invoke(args);

    /// <summary>Raises the <see cref="BottomIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnBottomIconChange(PropertyChangeEventArgs<ISpannable?> args) =>
        this.BottomIconChange?.Invoke(args);
}
