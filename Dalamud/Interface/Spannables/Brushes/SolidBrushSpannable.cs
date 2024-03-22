using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Utility;

namespace Dalamud.Interface.Spannables.Brushes;

/// <summary>A spannable that renders a solid color.</summary>
public sealed class SolidBrushSpannable : BrushSpannable
{
    /// <summary>Gets or sets the channel to render to.</summary>
    public RenderChannel TargetChannel { get; set; } = RenderChannel.BackChannel;

    /// <summary>Gets or sets the fill color.</summary>
    public Rgba32 Color { get; set; }

    /// <inheritdoc/>
    public override void DrawSpannable(SpannableDrawArgs args)
    {
        var lt = args.State.TransformToScreen(args.State.Boundary.LeftTop);
        var rt = args.State.TransformToScreen(args.State.Boundary.RightTop);
        var rb = args.State.TransformToScreen(args.State.Boundary.RightBottom);
        var lb = args.State.TransformToScreen(args.State.Boundary.LeftBottom);
        args.SwitchToChannel(this.TargetChannel);
        args.DrawListPtr.AddQuadFilled(lt, rt, rb, lb, this.Color);
    }
}
