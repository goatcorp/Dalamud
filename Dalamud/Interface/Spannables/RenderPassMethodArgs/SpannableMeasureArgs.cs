using System.Numerics;

using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Rendering;

namespace Dalamud.Interface.Spannables.RenderPassMethodArgs;

/// <summary>Arguments for use with <see cref="ISpannableRenderPass.MeasureSpannable"/>.</summary>
public ref struct SpannableMeasureArgs
{
    /// <summary>The associated spannable.</summary>
    public ISpannable Sender;

    /// <summary>The state obtained from <see cref="ISpannable.RentRenderPass"/>.</summary>
    public ISpannableRenderPass RenderPass;

    /// <summary>The initial text state.</summary>
    public TextState TextState;

    /// <summary>The maximum size available for the spannable.</summary>
    public Vector2 MaxSize;

    /// <summary>The render scale to use.</summary>
    public float Scale;

    /// <summary>The allocated ImGui global ID.</summary>
    public uint ImGuiGlobalId;

    /// <summary>Initializes a new instance of the <see cref="SpannableMeasureArgs"/> struct.</summary>
    /// <param name="sender">The associated spannable.</param>
    /// <param name="renderPass">The state for the spannable.</param>
    /// <param name="maxSize">The maximum size.</param>
    /// <param name="scale">The render scale to use.</param>
    /// <param name="textState">The initial text state.</param>
    /// <param name="imGuiGlobalId">The ImGui global ID for the spannable, or 0 interactivity is not used.</param>
    public SpannableMeasureArgs(
        ISpannable sender,
        ISpannableRenderPass renderPass,
        Vector2 maxSize,
        float scale,
        TextState textState,
        uint imGuiGlobalId)
    {
        this.Sender = sender;
        this.RenderPass = renderPass;
        this.MaxSize = maxSize;
        this.Scale = scale;
        this.TextState = textState;
        this.ImGuiGlobalId = imGuiGlobalId;
    }

    /// <summary>Notifies a child <see cref="ISpannable"/> with transformed arguments.</summary>
    /// <param name="child">A child to notify the event.</param>
    /// <param name="childRenderPass">The child state.</param>
    /// <param name="childInnerId">The inner ID of the child. <c>-1</c> to disable.</param>
    /// <param name="maxSize">The maximum size for the child.</param>
    /// <param name="textState">The initial text state for the child.</param>
    public readonly void NotifyChild(
        ISpannable child,
        ISpannableRenderPass childRenderPass,
        int childInnerId,
        Vector2 maxSize,
        in TextState textState) =>
        childRenderPass.MeasureSpannable(
            this with
            {
                Sender = child,
                RenderPass = childRenderPass,
                ImGuiGlobalId = this.RenderPass.ImGuiGlobalId == 0 || childInnerId == -1
                                    ? 0
                                    : this.RenderPass.GetGlobalIdFromInnerId(childInnerId),
                MaxSize = maxSize,
                TextState = textState with { InitialStyle = textState.LastStyle },
            });

    /// <summary>Notifies a child <see cref="ISpannable"/> with transformed arguments.</summary>
    /// <param name="child">A child to notify the event.</param>
    /// <param name="childRenderPass">The child state.</param>
    /// <param name="childInnerId">The inner ID of the child. <c>-1</c> to disable.</param>
    /// <param name="maxSize">The maximum size for the child.</param>
    public readonly void NotifyChild(
        ISpannable child,
        ISpannableRenderPass childRenderPass,
        int childInnerId,
        Vector2 maxSize) => this.NotifyChild(child, childRenderPass, childInnerId, maxSize, this.RenderPass.ActiveTextState);
}
