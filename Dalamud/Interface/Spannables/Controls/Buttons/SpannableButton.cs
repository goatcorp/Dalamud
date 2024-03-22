using Dalamud.Interface.Spannables.Brushes;
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
        this.NormalBackground = new ImGuiColorSolidBrushDrawable
        {
            TargetChannel = RenderChannel.BackChannel,
            Color = ImGuiCol.Button,
        };
        this.HoveredBackground = new ImGuiColorSolidBrushDrawable
        {
            TargetChannel = RenderChannel.BackChannel,
            Color = ImGuiCol.ButtonHovered,
        };
        this.ActiveBackground = new ImGuiColorSolidBrushDrawable
        {
            TargetChannel = RenderChannel.BackChannel,
            Color = ImGuiCol.ButtonActive,
        };
        this.DisabledBackground = new ImGuiColorSolidBrushDrawable
        {
            TargetChannel = RenderChannel.BackChannel,
            Color = ImGuiCol.Button,
            ColorMultiplier = new(1.4f, 1.4f, 1.4f, 0.6f),
        };
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

    /// <summary>Gets or sets the opacity of the text when the control is disabled.</summary>
    public float DisabledTextOpacity { get; set; } = 0.5f;

    /// <inheritdoc/>
    public override void CommitSpannableMeasurement(scoped in SpannableCommitTransformationArgs args)
    {
        base.CommitSpannableMeasurement(args);

        if (this.spannableState is null)
            return;

        args.NotifyChild(
            this.spannableText ?? this.spannedStringBuilder,
            this.spannableState,
            this.MeasuredContentBox.LeftTop,
            Trss.Identity);
    }

    /// <inheritdoc/>
    public override void HandleSpannableInteraction(
        scoped in SpannableHandleInteractionArgs args,
        out SpannableLinkInteracted link)
    {
        if (this.spannableText is not null && this.spannableState is not null && this.Enabled)
            args.NotifyChild(this.spannableText, this.spannableState, out link);
        base.HandleSpannableInteraction(args, out link);
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
    protected override unsafe void OnDraw(SpannableControlDrawArgs args)
    {
        if (this.spannableState is not null)
        {
            var numVertices = args.DrawArgs.DrawListPtr.VtxBuffer.Size;
            args.DrawArgs.NotifyChild(this.spannableText ?? this.spannedStringBuilder, this.spannableState);

            if (!this.Enabled)
            {
                var ptr = (ImDrawVert*)args.DrawArgs.DrawListPtr.VtxBuffer.Data + numVertices;
                for (var remaining = args.DrawArgs.DrawListPtr.VtxBuffer.Size - numVertices;
                     remaining > 0;
                     remaining--, ptr++)
                {
                    ref var a = ref ((byte*)&ptr->col)[3];
                    a = (byte)Math.Clamp(a * this.DisabledTextOpacity, 0, 255);
                }
            }
        }
    }
}
