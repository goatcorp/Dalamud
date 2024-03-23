using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Patterns;

/// <summary>A spannable that renders a solid color.</summary>
public sealed class SolidColorPattern : SpannablePattern
{
    /// <summary>Gets or sets the channel to render to.</summary>
    public RenderChannel TargetChannel { get; set; } = RenderChannel.BackChannel;

    /// <summary>Gets or sets the fill color.</summary>
    public Rgba32 Color { get; set; }

    /// <summary>Gets or sets the rounding.</summary>
    public float Rounding { get; set; } = 0;

    /// <summary>Gets or sets the rounding flags.</summary>
    public ImDrawFlags RoundingFlags { get; set; } = ImDrawFlags.RoundCornersDefault;

    /// <inheritdoc/>
    protected override PatternRenderPass CreateNewRenderPass() => new SolidColorRenderPass(this);

    /// <summary>A state for <see cref="LayeredPattern"/>.</summary>
    private class SolidColorRenderPass(SolidColorPattern owner) : PatternRenderPass
    {
        public override void DrawSpannable(SpannableDrawArgs args)
        {
            base.DrawSpannable(args);
            
            var lt = args.RenderPass.Boundary.LeftTop;
            var rb = args.RenderPass.Boundary.RightBottom;

            args.SwitchToChannel(owner.TargetChannel);

            using var st = ScopedTransformer.From(args, 1f);
            args.DrawListPtr.AddRectFilled(lt, rb, owner.Color, owner.Rounding, owner.RoundingFlags);
        }
    }
}
