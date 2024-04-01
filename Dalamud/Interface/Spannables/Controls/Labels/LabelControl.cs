using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Interface.Spannables.Text;
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

    private readonly ISpannableTemplate?[] iconTemplates = new ISpannableTemplate?[IconSlotCount];

    // Properties
    private ISpannableTemplate? spannableText;
    private SpannableOptions? spannableTextOptions;
    private BorderVector4 textMargin;
    private Vector2 alignment;

    // States
    private MemoryStream? lastLink = new();

    /// <summary>Initializes a new instance of the <see cref="LabelControl"/> class.</summary>
    public LabelControl()
    {
        this.childrenSlotText = this.AllSpannablesAvailableSlot++;
        this.innerIdText = this.InnerIdAvailableSlot++;
        this.AllSpannables.Add(null);

        this.childrenSlotIconBase = this.AllSpannablesAvailableSlot;
        this.innerIdIconBase = this.InnerIdAvailableSlot;
        this.AllSpannablesAvailableSlot += IconSlotCount;
        this.InnerIdAvailableSlot += IconSlotCount;
        for (var i = 0; i < IconSlotCount; i++)
            this.AllSpannables.Add(null);
    }

    /// <summary>Occurs when the mouse pointer enters a link in the control.</summary>
    public event SpannableMouseLinkEventHandler? LinkMouseEnter;

    /// <summary>Occurs when the mouse pointer leaves a link in the control.</summary>
    public event SpannableMouseLinkEventHandler? LinkMouseLeave;

    /// <summary>Occurs when a link in the control just got held down.</summary>
    public event SpannableMouseLinkEventHandler? LinkMouseDown;

    /// <summary>Occurs when a link in the control just got released.</summary>
    public event SpannableMouseLinkEventHandler? LinkMouseUp;

    /// <summary>Occurs when a link in the control is clicked by the mouse.</summary>
    public event SpannableMouseLinkEventHandler? LinkMouseClick;

    /// <summary>Occurs when the property <see cref="SpannableText"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannableTemplate?>? SpannableTextChange;

    /// <summary>Occurs when the property <see cref="SpannableText"/> is changing.</summary>
    public event PropertyChangeEventHandler<SpannableOptions?>?
        SpannableTextOptionsChange;

    /// <summary>Occurs when the property <see cref="TextMargin"/> is changing.</summary>
    public event PropertyChangeEventHandler<BorderVector4>? TextMarginChange;

    /// <summary>Occurs when the property <see cref="Alignment"/> is changing.</summary>
    public event PropertyChangeEventHandler<Vector2>? AlignmentChange;

    /// <summary>Occurs when the property <see cref="LeftIcon"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannableTemplate?>? LeftIconChange;

    /// <summary>Occurs when the property <see cref="TopIcon"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannableTemplate?>? TopIconChange;

    /// <summary>Occurs when the property <see cref="RightIcon"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannableTemplate?>? RightIconChange;

    /// <summary>Occurs when the property <see cref="BottomIcon"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannableTemplate?>? BottomIconChange;

    /// <summary>Gets or sets a spannable text.</summary>
    /// <remarks>Having this property set takes priority over <see cref="ControlSpannable.Text"/>.</remarks>
    public ISpannableTemplate? SpannableText
    {
        get => this.spannableText;
        set => this.HandlePropertyChange(
            nameof(this.SpannableText),
            ref this.spannableText,
            value,
            ReferenceEquals(this.spannableText, value),
            this.OnSpannableTextChange);
    }

    /// <summary>Gets or sets options for <see cref="SpannableText"/>.</summary>
    /// <remarks>
    /// <para><see cref="ControlSpannable.TextStyle"/> will override this.</para>
    /// <para>The change will not be reflected if the referenced object is the same. Unset and set this property if
    /// the innards of the reference object got changed.</para>
    /// </remarks>
    public SpannableOptions? SpannableTextOptions
    {
        get => this.spannableTextOptions;
        set => this.HandlePropertyChange(
            nameof(this.SpannableTextOptions),
            ref this.spannableTextOptions,
            value,
            this.spannableTextOptions == value,
            this.OnSpannableTextOptionsChange);
    }

    /// <summary>Gets or sets the text margin, which is the gap between icons and the text.</summary>
    /// <remarks>The value will be scaled by <see cref="ControlSpannable.EffectiveRenderScale"/>.</remarks>
    public BorderVector4 TextMargin
    {
        get => this.textMargin;
        set => this.HandlePropertyChange(
            nameof(this.TextMargin),
            ref this.textMargin,
            value,
            this.textMargin == value,
            this.OnTextMarginChange);
    }

    /// <summary>Gets or sets the alignment of the content.</summary>
    /// <value>(0, ?) is left, (1, ?) is right, (?, 0) is top, and (?, 1) is bottom.</value>
    public Vector2 Alignment
    {
        get => this.alignment;
        set => this.HandlePropertyChange(
            nameof(this.Alignment),
            ref this.alignment,
            value,
            this.alignment == value,
            this.OnAlignmentChange);
    }

    /// <summary>Gets or sets the spannable to display on the left side.</summary>
    /// <remarks>Primary use case is for displaying an icon.</remarks>
    public ISpannableTemplate? LeftIcon
    {
        get => this.iconTemplates[0];
        set => this.HandlePropertyChange(
            nameof(this.LeftIcon),
            ref this.iconTemplates[0],
            value,
            ReferenceEquals(this.iconTemplates[0], value),
            this.OnLeftIconChange);
    }

    /// <summary>Gets or sets the spannable to display on the top side.</summary>
    /// <remarks>Primary use case is for displaying an icon.</remarks>
    public ISpannableTemplate? TopIcon
    {
        get => this.iconTemplates[1];
        set => this.HandlePropertyChange(
            nameof(this.TopIcon),
            ref this.iconTemplates[1],
            value,
            ReferenceEquals(this.iconTemplates[1], value),
            this.OnTopIconChange);
    }

    /// <summary>Gets or sets the spannable to display on the right side.</summary>
    /// <remarks>Primary use case is for displaying an icon.</remarks>
    public ISpannableTemplate? RightIcon
    {
        get => this.iconTemplates[2];
        set => this.HandlePropertyChange(
            nameof(this.RightIcon),
            ref this.iconTemplates[2],
            value,
            ReferenceEquals(this.iconTemplates[2], value),
            this.OnRightIconChange);
    }

    /// <summary>Gets or sets the spannable to display on the bottom side.</summary>
    /// <remarks>Primary use case is for displaying an icon.</remarks>
    public ISpannableTemplate? BottomIcon
    {
        get => this.iconTemplates[3];
        set => this.HandlePropertyChange(
            nameof(this.BottomIcon),
            ref this.iconTemplates[3],
            value,
            ReferenceEquals(this.iconTemplates[3], value),
            this.OnBottomIconChange);
    }

    /// <summary>Gets or sets the currently active spannable.</summary>
    private Spannable? ActiveSpannable
    {
        get => this.AllSpannables[this.childrenSlotText];
        set
        {
            if (ReferenceEquals(value, this.AllSpannables[this.childrenSlotText]))
                return;

            if (this.spannableTextOptions is not null)
                value?.Options.CopyFrom(this.spannableTextOptions);

            if (value?.Options is AbstractStyledText.Options mo)
                mo.Style = this.TextStyle;

            if (this.AllSpannables[this.childrenSlotText] is AbstractStyledText.TextSpannable prev)
            {
                prev.LinkMouseEnter -= this.PrevOnLinkMouseEnter;
                prev.LinkMouseLeave -= this.PrevOnLinkMouseLeave;
                prev.LinkMouseUp -= this.PrevOnLinkMouseUp;
                prev.LinkMouseDown -= this.PrevOnLinkMouseDown;
                prev.LinkMouseClick -= this.PrevOnLinkMouseClick;
            }

            this.AllSpannables[this.childrenSlotText] = value;
            
            if (value is AbstractStyledText.TextSpannable curr)
            {
                curr.LinkMouseEnter += this.PrevOnLinkMouseEnter;
                curr.LinkMouseLeave += this.PrevOnLinkMouseLeave;
                curr.LinkMouseUp += this.PrevOnLinkMouseUp;
                curr.LinkMouseDown += this.PrevOnLinkMouseDown;
                curr.LinkMouseClick += this.PrevOnLinkMouseClick;
            }

            this.RequestMeasure();
        }
    }

    /// <summary>Gets the spannables used as icons.</summary>
    private Span<Spannable?> IconSpannables =>
        CollectionsMarshal.AsSpan(this.AllSpannables).Slice(this.childrenSlotIconBase, IconSlotCount);

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
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
            if (this.iconTemplates[i] is not { } sp)
                continue;
            ref var im = ref this.IconSpannables[i];
            if (!ReferenceEquals(im?.SourceTemplate, sp))
            {
                var recycling = im;
                im = null;
                recycling?.SourceTemplate?.RecycleSpannable(recycling);
            }

            im ??= sp.CreateSpannable();
            im.Renderer = this.Renderer;
            im.ImGuiGlobalId = this.GetGlobalIdFromInnerId(this.innerIdIconBase + i);
            im.Options.RenderScale = this.EffectiveRenderScale;
            im.Options.VisibleSize = this.Options.VisibleSize - this.textMargin.Size;
            im.Options.PreferredSize = im.Options.VisibleSize;
            im.RenderPassMeasure();
        }

        var totalIconSize = this.textMargin.Size;
        totalIconSize.X += this.IconSpannables[0]?.Boundary.Width ?? 0;
        totalIconSize.X += this.IconSpannables[2]?.Boundary.Width ?? 0;
        totalIconSize.Y += this.IconSpannables[1]?.Boundary.Height ?? 0;
        totalIconSize.Y += this.IconSpannables[3]?.Boundary.Height ?? 0;

        Vector2 b;
        if (this.ActiveSpannable is null)
        {
            b = Vector2.Zero;
        }
        else
        {
            this.ActiveSpannable.Renderer = this.Renderer;
            this.ActiveSpannable.ImGuiGlobalId = this.GetGlobalIdFromInnerId(this.innerIdText);
            this.ActiveSpannable.Options.RenderScale = this.EffectiveRenderScale;
            this.ActiveSpannable.Options.VisibleSize = suggestedSize - totalIconSize;
            this.ActiveSpannable.Options.PreferredSize = this.ActiveSpannable.Options.VisibleSize;
            this.ActiveSpannable.RenderPassMeasure();
            b = RectVector4.Normalize(this.ActiveSpannable.Boundary).Size;
        }

        b += totalIconSize;
        b.X = Math.Max(b.X, this.IconSpannables[1]?.Boundary.Width ?? 0);
        b.X = Math.Max(b.X, this.IconSpannables[3]?.Boundary.Width ?? 0);
        b.Y = Math.Max(b.Y, this.IconSpannables[0]?.Boundary.Height ?? 0);
        b.Y = Math.Max(b.Y, this.IconSpannables[2]?.Boundary.Height ?? 0);

        if (!this.IsWidthWrapContent && suggestedSize.X < float.PositiveInfinity)
            b.X = suggestedSize.X;
        if (!this.IsHeightWrapContent && suggestedSize.Y < float.PositiveInfinity)
            b.Y = suggestedSize.Y;
        return RectVector4.FromCoordAndSize(Vector2.Zero, b);
    }

    /// <inheritdoc/>
    protected override void OnPlace(SpannableEventArgs args)
    {
        base.OnPlace(args);

        if (this.IsDisposed)
            return;

        // Icons get all the space they want. They may overlap.
        for (var i = 0; i < IconSlotCount; i++)
        {
            if (this.IconSpannables[i] is not { } rp)
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
            rp.RenderPassPlace(Matrix4x4.CreateTranslation(new(iconLt, 0)), this.FullTransformation);
        }

        if (this.ActiveSpannable is not null)
        {
            var lt =
                this.MeasuredContentBox.LeftTop
                + new Vector2(
                    this.IconSpannables[0]?.Boundary.Width ?? 0,
                    this.IconSpannables[1]?.Boundary.Height ?? 0)
                + this.textMargin.LeftTop;
            var rb =
                this.MeasuredContentBox.RightBottom
                - new Vector2(
                    this.IconSpannables[2]?.Boundary.Width ?? 0,
                    this.IconSpannables[3]?.Boundary.Height ?? 0)
                - this.textMargin.RightBottom;

            // If we don't have enough space for the text, try to center align it.
            var availTextSize = rb - lt;
            lt -= (this.ActiveSpannable.Boundary.Size - availTextSize) * this.alignment;
            lt = lt.Round(1f / this.EffectiveRenderScale);
            this.ActiveSpannable.RenderPassPlace(
                Matrix4x4.CreateTranslation(new(lt, 0)),
                this.FullTransformation);
        }
    }

    /// <inheritdoc/>
    protected override void OnDrawInside(SpannableDrawEventArgs args)
    {
        base.OnDrawInside(args);

        if (this.IsDisposed)
            return;

        for (var i = 0; i < IconSlotCount; i++)
            this.IconSpannables[i]?.RenderPassDraw(args.DrawListPtr);

        this.ActiveSpannable?.RenderPassDraw(args.DrawListPtr);
    }

    /// <summary>Raises the <see cref="LinkMouseEnter"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
    protected virtual void OnLinkMouseEnter(SpannableMouseLinkEventArgs args) =>
        this.LinkMouseEnter?.Invoke(args);

    /// <summary>Raises the <see cref="LinkMouseLeave"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
    protected virtual void OnLinkMouseLeave(SpannableMouseLinkEventArgs args) =>
        this.LinkMouseLeave?.Invoke(args);

    /// <summary>Raises the <see cref="LinkMouseDown"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
    protected virtual void OnLinkMouseDown(SpannableMouseLinkEventArgs args) =>
        this.LinkMouseDown?.Invoke(args);

    /// <summary>Raises the <see cref="LinkMouseUp"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
    protected virtual void OnLinkMouseUp(SpannableMouseLinkEventArgs args) =>
        this.LinkMouseUp?.Invoke(args);

    /// <summary>Raises the <see cref="LinkMouseClick"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
    protected virtual void OnLinkMouseClick(SpannableMouseLinkEventArgs args) =>
        this.LinkMouseClick?.Invoke(args);

    /// <inheritdoc/>
    protected override void OnTextChange(PropertyChangeEventArgs<string?> args)
    {
        if (args.State == PropertyChangeState.After)
        {
            var old = this.ActiveSpannable;
            this.ActiveSpannable = new StyledText(args.NewValue).CreateSpannable();
            if (old?.SourceTemplate is { } st)
                st.RecycleSpannable(old);
            else
                old?.Dispose();
        }

        base.OnTextChange(args);
    }

    /// <inheritdoc/>
    protected override void OnTextStyleChange(PropertyChangeEventArgs<TextStyle> args)
    {
        if (args.State == PropertyChangeState.After
            && this.ActiveSpannable?.Options is AbstractStyledText.Options mo)
            mo.Style = args.NewValue;
        base.OnTextStyleChange(args);
    }

    /// <summary>Raises the <see cref="SpannableTextChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnSpannableTextChange(PropertyChangeEventArgs<ISpannableTemplate?> args)
    {
        if (args.State == PropertyChangeState.After)
        {
            var old = this.ActiveSpannable;
            this.ActiveSpannable = args.NewValue?.CreateSpannable();
            if (old?.SourceTemplate is { } st)
                st.RecycleSpannable(old);
            else
                old?.Dispose();
        }

        this.SpannableTextChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="SpannableTextOptionsChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnSpannableTextOptionsChange(PropertyChangeEventArgs<SpannableOptions?> args)
    {
        if (args.State == PropertyChangeState.After)
        {
            if (args.NewValue is null)
                this.ActiveSpannable?.Options.TryReset();
            else
                this.ActiveSpannable?.Options.CopyFrom(args.NewValue);

            if (this.ActiveSpannable?.Options is AbstractStyledText.Options mo)
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
    protected virtual void OnLeftIconChange(PropertyChangeEventArgs<ISpannableTemplate?> args) =>
        this.LeftIconChange?.Invoke(args);

    /// <summary>Raises the <see cref="TopIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnTopIconChange(PropertyChangeEventArgs<ISpannableTemplate?> args) =>
        this.TopIconChange?.Invoke(args);

    /// <summary>Raises the <see cref="RightIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnRightIconChange(PropertyChangeEventArgs<ISpannableTemplate?> args) =>
        this.RightIconChange?.Invoke(args);

    /// <summary>Raises the <see cref="BottomIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnBottomIconChange(PropertyChangeEventArgs<ISpannableTemplate?> args) =>
        this.BottomIconChange?.Invoke(args);

    private void PrevOnLinkMouseLeave(SpannableMouseLinkEventArgs args)
    {
        args.Initialize(this, SpannableEventStep.DirectTarget);
        this.OnLinkMouseLeave(args);
    }

    private void PrevOnLinkMouseUp(SpannableMouseLinkEventArgs args)
    {
        args.Initialize(this, SpannableEventStep.DirectTarget);
        this.OnLinkMouseUp(args);
    }

    private void PrevOnLinkMouseEnter(SpannableMouseLinkEventArgs args)
    {
        args.Initialize(this, SpannableEventStep.DirectTarget);
        this.OnLinkMouseEnter(args);
    }

    private void PrevOnLinkMouseDown(SpannableMouseLinkEventArgs args)
    {
        args.Initialize(this, SpannableEventStep.DirectTarget);
        this.OnLinkMouseDown(args);
    }

    private void PrevOnLinkMouseClick(SpannableMouseLinkEventArgs args)
    {
        args.Initialize(this, SpannableEventStep.DirectTarget);
        this.OnLinkMouseClick(args);
    }
}
