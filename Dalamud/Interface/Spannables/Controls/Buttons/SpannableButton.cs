using System.Globalization;
using System.Numerics;

using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Strings;
using Dalamud.Interface.Spannables.Styles;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls.Buttons;

/// <summary>A button that is spannable.</summary>
public class SpannableButton : SpannableControl
{
    private ISpannableState? spannableState;
    private ISpannable? spannableText;
    private SpannedStringBuilder spannedStringBuilder = new();
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
    public override void InteractWith(SpannableInteractionArgs args, out ReadOnlySpan<byte> linkData)
    {
        if (this.spannableText is not null && this.spannableState is not null)
            this.spannableText.InteractWith(new(this.spannableState), out linkData);
        base.InteractWith(args, out linkData);
    }

    /// <inheritdoc/>
    public override void Draw(SpannableDrawArgs args)
    {
        var lt = args.State.RenderState.StartScreenOffset + args.State.RenderState.Transform(this.MeasuredInteractiveBox.LeftTop);
        var rt = args.State.RenderState.StartScreenOffset + args.State.RenderState.Transform(this.MeasuredInteractiveBox.RightTop);
        var rb = args.State.RenderState.StartScreenOffset + args.State.RenderState.Transform(this.MeasuredInteractiveBox.RightBottom);
        var lb = args.State.RenderState.StartScreenOffset + args.State.RenderState.Transform(this.MeasuredInteractiveBox.LeftBottom);
        args.SwitchToChannel(RenderChannel.BackChannel);
        if (this.IsMouseHovered && this.IsLeftMouseButtonDown)
            this.RenderState.DrawListPtr.AddQuadFilled(lt, rt, rb, lb, ImGui.GetColorU32(ImGuiCol.ButtonActive));
        else if (this.IsMouseHovered)
            this.RenderState.DrawListPtr.AddQuadFilled(lt, rt, rb, lb, ImGui.GetColorU32(ImGuiCol.ButtonHovered));
        else
            this.RenderState.DrawListPtr.AddQuadFilled(lt, rt, rb, lb, ImGui.GetColorU32(ImGuiCol.Button));

        if (this.spannableState is not null)
        {
            var spannable = this.spannableText ?? this.spannedStringBuilder;
            spannable.Draw(args.WithState(this.spannableState));
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
        this.RenderState.Offset = Vector2.Zero;

        var rs2 = (this.RenderState with
                      {
                          Offset = Vector2.Zero,
                          Boundary = RectVector4.InvertedExtrema,
                          MaxSize = availableContentBox.Size,
                          InitialStyle = this.RenderState.LastStyle,
                          WordBreak = WordBreakType.KeepAll,
                          ImGuiGlobalId = this.RenderState.GetGlobalIdFromInnerId(1),
                      }).WithTransformation(
            Matrix4x4.Multiply(
                Matrix4x4.CreateTranslation(
                    new(Vector2.Transform(this.RenderState.Offset, this.RenderState.Transformation), 0)),
                this.RenderState.Transformation));
        
        var spannable = this.spannableText ?? this.spannedStringBuilder;
        this.spannableState = spannable.RentState(this.Renderer, rs2, null);
        spannable.Measure(new(this.spannableState));

        this.spannableState.RenderState.Boundary = RectVector4.Normalize(this.spannableState.RenderState.Boundary);

        var res = availableContentBox;
        if (this.IsWidthWrapContent)
            res.Right = Math.Min(res.Right, res.Left + this.spannableState.RenderState.Boundary.Right);
        if (this.IsHeightWrapContent)
            res.Bottom = Math.Min(res.Bottom, res.Top + this.spannableState.RenderState.Boundary.Bottom);
        return res;
    }

    /// <inheritdoc/>
    protected override void OnMeasureEnd(SpannableMeasureArgs args)
    {
        if (this.spannableState is null)
            return;

        if (Matrix4x4.Decompose(this.RenderState.Transformation, out var scale, out var rot, out _))
        {
            var m = Matrix4x4.CreateFromQuaternion(rot);
            m = Matrix4x4.Multiply(m, Matrix4x4.CreateScale(scale));

            this.spannableState.RenderState = this.spannableState.RenderState.WithTransformation(m);
        }

        this.spannableState.RenderState.StartScreenOffset =
            this.RenderState.StartScreenOffset + this.RenderState.Transform(this.MeasuredContentBox.LeftTop);
            
        if (this.IsWidthWrapContent || this.IsHeightWrapContent)
        {
            this.spannableState.RenderState.MaxSize = this.MeasuredContentBox.Size;

            var spannable = this.spannableText ?? this.spannedStringBuilder;
            spannable.Measure(new(this.spannableState));
        }

        base.OnMeasureEnd(args);
    }
}
