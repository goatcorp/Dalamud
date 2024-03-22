using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Utility;

using ImGuiNET;

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

/// <summary>A spannable that renders a solid color from ImGui style values.</summary>
public sealed class ImGuiColorSolidBrushDrawable : BrushSpannable
{
    /// <summary>Gets or sets the channel to render to.</summary>
    public RenderChannel TargetChannel { get; set; } = RenderChannel.BackChannel;

    /// <summary>Gets or sets the ImGui style with a color value.</summary>
    public ImGuiCol Color { get; set; }

    /// <summary>Gets or sets the color multiplier, including the alpha channel.</summary>
    public Vector4 ColorMultiplier { get; set; } = Vector4.One;

    /// <inheritdoc/>
    public override unsafe void DrawSpannable(SpannableDrawArgs args)
    {
        var lt = args.State.TransformToScreen(args.State.Boundary.LeftTop);
        var rt = args.State.TransformToScreen(args.State.Boundary.RightTop);
        var rb = args.State.TransformToScreen(args.State.Boundary.RightBottom);
        var lb = args.State.TransformToScreen(args.State.Boundary.LeftBottom);
        args.SwitchToChannel(this.TargetChannel);

        var color4 = new Rgba32(*ImGui.GetStyleColorVec4(this.Color) * this.ColorMultiplier);
        args.DrawListPtr.AddQuadFilled(lt, rt, rb, lb, color4);
    }
}
