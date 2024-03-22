using System.IO;

using Dalamud.Interface.Spannables.Controls.EventHandlerDelegates;
using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Strings;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;

using FFXIVClientStructs.FFXIV.Common.Math;

namespace Dalamud.Interface.Spannables.Controls.Buttons;

/// <summary>A label that is spannable.</summary>
public class SpannableLabel : SpannableControl
{
    private readonly SpannedStringBuilder spannedStringBuilder = new();
    private readonly MemoryStream lastLink = new();

    private ISpannableState? spannableState;
    private ISpannable? spannableText;

    /// <summary>Occurs when the mouse pointer enters a link in the control.</summary>
    public event SpannableControlLinkEventHandler? LinkMouseEnter;

    /// <summary>Occurs when the mouse pointer leaves a link in the control.</summary>
    public event SpannableControlLinkEventHandler? LinkMouseLeave;

    /// <summary>Occurs when a link in the control is clicked by the mouse.</summary>
    public event SpannableControlLinkEventHandler? LinkMouseClick;

    /// <summary>Gets or sets a spannable text.</summary>
    /// <remarks>Setting this property clears <see cref="SpannableControl.Text"/>.</remarks>
    public ISpannable? SpannableText
    {
        get => this.spannableText;
        set
        {
            this.Text = null;
            this.spannedStringBuilder.Clear();
            this.spannableText = value;
        }
    }

    /// <inheritdoc/>
    public override void ReturnState(ISpannableState? state)
    {
        var spannable = this.spannableText ?? this.spannedStringBuilder;
        spannable.ReturnState(this.spannableState);
        this.spannableState = null;

        base.ReturnState(state);
    }

    /// <inheritdoc/>
    protected override RectVector4 MeasureContentBox(SpannableMeasureArgs args, in RectVector4 availableContentBox)
    {
        var spannable = this.spannableText ?? this.spannedStringBuilder;
        this.spannableState = spannable.RentState(
            new(
                this.Renderer,
                args.State.GetGlobalIdFromInnerId(1),
                this.Scale,
                this.TextState with
                {
                    InitialStyle = this.TextState.LastStyle,
                    WordBreak = WordBreakType.KeepAll,
                }));
        spannable.MeasureSpannable(new(this.spannableState, availableContentBox.Size));

        var b = RectVector4.Normalize(this.spannableState.Boundary);

        var res = availableContentBox;
        if (this.IsWidthWrapContent)
            res.Right = res.Left + b.Right;
        if (this.IsHeightWrapContent)
            res.Bottom = res.Top + b.Bottom;
        return res;
    }

    /// <inheritdoc/>
    protected override void OnCommitMeasurement(SpannableControlCommitMeasurementArgs args)
    {
        base.OnCommitMeasurement(args);

        if (this.spannableState is null)
            return;

        args.MeasureArgs.NotifyChild(
            this.spannableText ?? this.spannedStringBuilder,
            this.spannableState,
            this.MeasuredContentBox.LeftTop,
            Matrix4x4.Identity);
    }

    /// <inheritdoc/>
    protected override void OnHandleInteraction(
        SpannableControlHandleInteractionArgs args,
        out SpannableLinkInteracted link)
    {
        base.OnHandleInteraction(args, out link);
        if (!link.IsEmpty)
        {
            if (this.spannableText is not null && this.spannableState is not null && this.Enabled)
                args.HandleInteractionArgs.NotifyChild(this.spannableText, this.spannableState, out _);
        }
        else if (this.spannableText is not null && this.spannableState is not null && this.Enabled)
        {
            args.HandleInteractionArgs.NotifyChild(this.spannableText, this.spannableState, out link);
        }

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
    protected override void OnDraw(SpannableControlDrawArgs args)
    {
        base.OnDraw(args);
        if (this.spannableState is not null)
            args.DrawArgs.NotifyChild(this.spannableText ?? this.spannedStringBuilder, this.spannableState);
    }

    /// <summary>Raises the <see cref="LinkMouseEnter"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlMouseLinkEventArgs"/> that contains the event data.</param>
    protected virtual void OnLinkMouseEnter(SpannableControlMouseLinkEventArgs args) =>
        this.LinkMouseEnter?.Invoke(args);

    /// <summary>Raises the <see cref="LinkMouseLeave"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlMouseLinkEventArgs"/> that contains the event data.</param>
    protected virtual void OnLinkMouseLeave(SpannableControlMouseLinkEventArgs args) =>
        this.LinkMouseLeave?.Invoke(args);

    /// <summary>Raises the <see cref="LinkMouseClick"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlMouseLinkEventArgs"/> that contains the event data.</param>
    protected virtual void OnLinkMouseClick(SpannableControlMouseLinkEventArgs args) =>
        this.LinkMouseClick?.Invoke(args);

    /// <inheritdoc/>
    protected override void OnTextChanged(SpannableControlPropertyChangedEventArgs<string?> args)
    {
        this.spannableText = null;
        this.spannedStringBuilder.Clear().Append(args.NewValue);
        base.OnTextChanged(args);
    }
}
