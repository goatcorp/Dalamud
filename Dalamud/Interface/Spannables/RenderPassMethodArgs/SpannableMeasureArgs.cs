using System.Numerics;

using Dalamud.Interface.Spannables.Rendering;

namespace Dalamud.Interface.Spannables.RenderPassMethodArgs;

/// <summary>Arguments for use with <see cref="ISpannableRenderPass.MeasureSpannable"/>.</summary>
public struct SpannableMeasureArgs
{
    /// <summary>The state obtained from <see cref="ISpannable.RentRenderPass"/>.</summary>
    public ISpannableRenderPass RenderPass;

    /// <summary>The initial text state.</summary>
    public TextState TextState;

    /// <summary>The minimum size that the spannable should be.</summary>
    public Vector2 MinSize;

    /// <summary>The suggested size that the spannable should be.</summary>
    public Vector2 SuggestedSize;

    /// <summary>The maximum size available for the spannable.</summary>
    public Vector2 MaxSize;

    /// <summary>The render scale to use.</summary>
    public float Scale;

    /// <summary>The allocated ImGui global ID.</summary>
    public uint ImGuiGlobalId;

    /// <summary>Initializes a new instance of the <see cref="SpannableMeasureArgs"/> struct.</summary>
    /// <param name="renderPass">The state for the spannable.</param>
    /// <param name="minSize">The minimum size.</param>
    /// <param name="maxSize">The maximum size.</param>
    /// <param name="scale">The render scale to use.</param>
    /// <param name="textState">The initial text state.</param>
    /// <param name="imGuiGlobalId">The ImGui global ID for the spannable, or 0 interactivity is not used.</param>
    public SpannableMeasureArgs(
        ISpannableRenderPass renderPass,
        Vector2 minSize,
        Vector2 maxSize,
        float scale,
        TextState textState,
        uint imGuiGlobalId)
    {
        this.RenderPass = renderPass;
        this.MaxSize = maxSize;
        this.Scale = scale;
        this.TextState = textState;
        this.ImGuiGlobalId = imGuiGlobalId;
        this.MinSize = minSize;
    }
}
