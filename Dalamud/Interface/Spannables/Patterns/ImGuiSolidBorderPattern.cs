using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Patterns;

/// <summary>A spannable that renders a solid colored border from ImGui style values.</summary>
public sealed class ImGuiSolidBorderPattern : PatternSpannable
{
    /// <summary>Gets or sets the channel to render to.</summary>
    public RenderChannel TargetChannel { get; set; } = RenderChannel.BackChannel;

    /// <summary>Gets or sets the ImGui style with a color value.</summary>
    public ImGuiCol Color { get; set; }

    /// <summary>Gets or sets the color multiplier, including the alpha channel.</summary>
    public Vector4 ColorMultiplier { get; set; } = Vector4.One;

    /// <summary>Gets or sets the thickness.</summary>
    public float Thickness { get; set; } = 1;

    /// <summary>Gets or sets the rounding.</summary>
    public float Rounding { get; set; } = 0;

    /// <summary>Gets or sets the rounding flags.</summary>
    public ImDrawFlags RoundingFlags { get; set; } = ImDrawFlags.RoundCornersDefault;

    /// <inheritdoc/>
    protected override PatternRenderPass CreateNewRenderPass() => new ImGuiColorBorderRenderPass(this);

    /// <summary>A state for <see cref="LayeredPattern"/>.</summary>
    private class ImGuiColorBorderRenderPass(ImGuiSolidBorderPattern owner) : PatternRenderPass
    {
        public override unsafe void DrawSpannable(SpannableDrawArgs args)
        {
            base.DrawSpannable(args);

            var lt = args.RenderPass.Boundary.LeftTop;
            var rb = args.RenderPass.Boundary.RightBottom;
            var color4 = new Rgba32(*ImGui.GetStyleColorVec4(owner.Color) * owner.ColorMultiplier);

            args.SwitchToChannel(owner.TargetChannel);

            using var st = ScopedTransformer.From(args, 1f);
            args.DrawListPtr.AddRect(
                lt,
                rb,
                color4,
                owner.Rounding,
                owner.RoundingFlags,
                owner.Thickness);
        }
    }
}
