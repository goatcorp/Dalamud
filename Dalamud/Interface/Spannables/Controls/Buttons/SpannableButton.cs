using System.Globalization;
using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Strings;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls.Buttons;

/// <summary>A button that is spannable.</summary>
public class SpannableButton : SpannableControl
{
    private readonly SpannedStringBuilder spannedStringBuilder = new();

    private ISpannableState? spannableState;
    private ISpannable? spannableText;
    private string? text;

    /// <summary>Initializes a new instance of the <see cref="SpannableButton"/> class.</summary>
    public SpannableButton()
    {
        this.Padding = new(8);
    }

    /// <summary>Gets or sets a spannable text.</summary>
    /// <remarks>Setting this property clears <see cref="Text"/>.</remarks>
    public ISpannable? SpannableText
    {
        get => this.spannableText;
        set
        {
            this.text = null;
            this.spannedStringBuilder.Clear();
            this.spannableText = value;
        }
    }

    /// <summary>Gets or sets a standard text.</summary>
    public string? Text
    {
        get => this.text;
        set
        {
            this.spannableText = null;
            this.text = value;
            this.spannedStringBuilder.Clear().Append(this.text);
        }
    }

    /// <inheritdoc/>
    public override void CommitMeasurement(SpannableCommitTransformationArgs args)
    {
        base.CommitMeasurement(args);

        if (this.spannableState is null)
            return;

        var spannable = this.spannableText ?? this.spannedStringBuilder;

        // TODO: is this necessary?
        // if (this.IsWidthWrapContent || this.IsHeightWrapContent)
        // {
        //     this.spannableState.RenderState.MaxSize = this.MeasuredContentBox.Size;
        //
        //     spannable.Measure(new(this.spannableState, this.MeasuredContentBox.Size));
        // }

        spannable.CommitMeasurement(
            new(
                this.spannableState,
                this.TransformToScreen(this.MeasuredContentBox.LeftTop),
                Vector2.Zero, // yes
                Trss.WithoutTranslation(this.Transformation)));
    }

    /// <inheritdoc/>
    public override void HandleInteraction(SpannableHandleInteractionArgs args, out SpannableLinkInteracted link)
    {
        if (this.spannableText is not null && this.spannableState is not null)
            this.spannableText.HandleInteraction(args with { State = this.spannableState }, out link);
        base.HandleInteraction(args, out link);
    }

    /// <inheritdoc/>
    public override void Draw(SpannableDrawArgs args)
    {
        var lt = this.TransformToScreen(this.MeasuredInteractiveBox.LeftTop);
        var rt = this.TransformToScreen(this.MeasuredInteractiveBox.RightTop);
        var rb = this.TransformToScreen(this.MeasuredInteractiveBox.RightBottom);
        var lb = this.TransformToScreen(this.MeasuredInteractiveBox.LeftBottom);
        args.SwitchToChannel(RenderChannel.BackChannel);
        if (this.IsMouseHovered && this.IsLeftMouseButtonDown)
            args.DrawListPtr.AddQuadFilled(lt, rt, rb, lb, ImGui.GetColorU32(ImGuiCol.ButtonActive));
        else if (this.IsMouseHovered)
            args.DrawListPtr.AddQuadFilled(lt, rt, rb, lb, ImGui.GetColorU32(ImGuiCol.ButtonHovered));
        else
            args.DrawListPtr.AddQuadFilled(lt, rt, rb, lb, ImGui.GetColorU32(ImGuiCol.Button));

        if (this.spannableState is not null)
        {
            var spannable = this.spannableText ?? this.spannedStringBuilder;
            spannable.Draw(args with { State = this.spannableState });
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
    protected override void InitializeFromArguments(string? args)
    {
        if (string.IsNullOrEmpty(args))
            return;

        if (SpannedString.TryParse(args, CultureInfo.InvariantCulture, out var parsed))
            this.SpannableText = parsed;
        else
            this.Text = args;
    }

    /// <inheritdoc/>
    protected override RectVector4 MeasureContentBox(SpannableMeasureArgs args, in RectVector4 availableContentBox)
    {
        var spannable = this.spannableText ?? this.spannedStringBuilder;
        this.spannableState = spannable.RentState(
            this.Renderer,
            args.State.GetGlobalIdFromInnerId(1),
            this.Scale,
            null,
            this.TextState with
            {
                InitialStyle = this.TextState.LastStyle,
                WordBreak = WordBreakType.KeepAll,
            });
        spannable.Measure(new(this.spannableState, availableContentBox.Size));

        var b = RectVector4.Normalize(this.spannableState.Boundary);

        var res = availableContentBox;
        if (this.IsWidthWrapContent)
            res.Right = res.Left + b.Right;
        if (this.IsHeightWrapContent)
            res.Bottom = res.Top + b.Bottom;
        return res;
    }
}
