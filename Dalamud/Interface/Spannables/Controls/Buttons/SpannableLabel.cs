using System.IO;

using Dalamud.Interface.Spannables.Controls.EventHandlerDelegates;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Strings;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;

using FFXIVClientStructs.FFXIV.Common.Math;

namespace Dalamud.Interface.Spannables.Controls.Buttons;

/// <summary>A label that is spannable.</summary>
public class SpannableLabel : ControlSpannable
{
    private readonly int childrenSlotText;
    private readonly int innerIdSlotText;

    private SpannedStringBuilder? spannedStringBuilder;
    private MemoryStream? lastLink = new();

    private ISpannable? spannableText;

    private ISpannable? activeSpannable;
    private ISpannableRenderPass? activeSpannablePass;

    /// <summary>Initializes a new instance of the <see cref="SpannableLabel"/> class.</summary>
    public SpannableLabel()
    {
        this.activeSpannable = this.spannedStringBuilder = new();
        this.childrenSlotText = this.AllChildrenAvailableSlot++;
        this.AllChildren.Add(this.activeSpannable);
        this.innerIdSlotText = this.InnerIdAvailableSlot++;
    }

    /// <summary>Occurs when the mouse pointer enters a link in the control.</summary>
    public event ControlLinkEventHandler? LinkMouseEnter;

    /// <summary>Occurs when the mouse pointer leaves a link in the control.</summary>
    public event ControlLinkEventHandler? LinkMouseLeave;

    /// <summary>Occurs when a link in the control is clicked by the mouse.</summary>
    public event ControlLinkEventHandler? LinkMouseClick;

    /// <summary>Occurs when the property <see cref="SpannableText"/> has been changed.</summary>
    public event PropertyChangedEventHandler<ISpannable?>? SpannableTextChanged;

    /// <summary>Gets or sets a spannable text.</summary>
    /// <remarks>Setting this property clears <see cref="ControlSpannable.Text"/>.</remarks>
    public ISpannable? SpannableText
    {
        get => this.spannableText;
        set => this.HandlePropertyChange(
            nameof(this.SpannableText),
            ref this.spannableText,
            value,
            this.OnSpannableTextChanged);
    }

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
            this.activeSpannable?.Dispose();
            this.activeSpannable = null;
            this.spannedStringBuilder?.Dispose();
            this.spannedStringBuilder = null!;
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    protected override RectVector4 MeasureContentBox(SpannableMeasureArgs args, in RectVector4 availableContentBox)
    {
        var spannable = this.spannableText ?? this.spannedStringBuilder;
        if (this.activeSpannable is null || spannable is null)
            return RectVector4.InvertedExtrema;

        if (!ReferenceEquals(spannable, this.activeSpannable))
        {
            this.activeSpannable.ReturnRenderPass(this.activeSpannablePass);
            this.activeSpannablePass = null;
            this.activeSpannable = spannable;
        }

        this.activeSpannablePass ??= spannable.RentRenderPass(new(this.Renderer));
        this.activeSpannablePass.MeasureSpannable(
            new(
                spannable,
                this.activeSpannablePass,
                availableContentBox.Size,
                this.Scale,
                this.TextState with
                {
                    InitialStyle = this.TextState.LastStyle,
                }));

        var b = RectVector4.Normalize(this.activeSpannablePass.Boundary);

        var res = availableContentBox;
        if (this.IsWidthWrapContent)
            res.Right = res.Left + b.Right;
        if (this.IsHeightWrapContent)
            res.Bottom = res.Top + b.Bottom;
        return res;
    }

    /// <inheritdoc/>
    protected override void OnCommitMeasurement(ControlCommitMeasurementArgs args)
    {
        base.OnCommitMeasurement(args);

        if (this.activeSpannable is null || this.activeSpannablePass is null)
            return;

        args.MeasureArgs.NotifyChild(
            this.activeSpannable,
            this.activeSpannablePass,
            this.MeasuredContentBox.LeftTop,
            Matrix4x4.Identity);
    }

    /// <inheritdoc/>
    protected override void OnHandleInteraction(
        ControlHandleInteractionArgs args,
        out SpannableLinkInteracted link)
    {
        base.OnHandleInteraction(args, out link);

        if (!link.IsEmpty)
        {
            if (this.spannableText is not null && this.activeSpannablePass is not null && this.Enabled)
            {
                args.HandleInteractionArgs.NotifyChild(
                    this.spannableText,
                    this.activeSpannablePass,
                    this.innerIdSlotText,
                    out _);
            }
        }
        else if (this.spannableText is not null && this.activeSpannablePass is not null && this.Enabled)
        {
            args.HandleInteractionArgs.NotifyChild(
                this.spannableText,
                this.activeSpannablePass,
                this.innerIdSlotText,
                out link);
        }

        if (this.lastLink is null)
            return;

        if (!this.lastLink.GetDataSpan().SequenceEqual(link.Link))
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

    /// <inheritdoc/>
    protected override void OnDraw(ControlDrawArgs args)
    {
        base.OnDraw(args);

        if (this.activeSpannable is null || this.activeSpannablePass is null)
            return;

        args.DrawArgs.NotifyChild(this.activeSpannable, this.activeSpannablePass);
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
    protected override void OnTextChanged(PropertyChangedEventArgs<string?> args)
    {
        if (this.spannedStringBuilder is null)
            return;

        this.SpannableText = null;
        this.spannedStringBuilder.Clear().Append(args.NewValue);
        this.AllChildren[this.childrenSlotText] = this.spannedStringBuilder;
        base.OnTextChanged(args);
    }

    /// <summary>Raises the <see cref="SpannableTextChanged"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnSpannableTextChanged(PropertyChangedEventArgs<ISpannable?> args)
    {
        if (this.spannedStringBuilder is null)
            return;

        this.Text = null;
        this.spannedStringBuilder.Clear();
        this.AllChildren[this.childrenSlotText] = this.spannableText ?? this.spannedStringBuilder;
        this.SpannableTextChanged?.Invoke(args);
    }
}
