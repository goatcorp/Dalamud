using System.Numerics;

using Dalamud.Interface.Spannables.Rendering;

namespace Dalamud.Interface.Spannables.EventHandlerArgs;

/// <summary>Arguments for use with <see cref="ISpannableRenderPass.MeasureSpannable"/>.</summary>
public ref struct SpannableMeasureArgs
{
    /// <summary>The associated spannable.</summary>
    public ISpannable Sender;

    /// <summary>The state obtained from <see cref="ISpannable.RentRenderPass"/>.</summary>
    public ISpannableRenderPass RenderPass;

    /// <summary>The maximum size available for the spannable.</summary>
    public Vector2 MaxSize;

    /// <summary>The render scale to use.</summary>
    public float Scale;

    /// <summary>The initial text state.</summary>
    public TextState TextState;

    /// <summary>Initializes a new instance of the <see cref="SpannableMeasureArgs"/> struct.</summary>
    /// <param name="sender">The associated spannable.</param>
    /// <param name="renderPass">The state for the spannable.</param>
    /// <param name="maxSize">The maximum size.</param>
    /// <param name="scale">The render scale to use.</param>
    /// <param name="textState">The initial text state.</param>
    public SpannableMeasureArgs(
        ISpannable sender,
        ISpannableRenderPass renderPass,
        Vector2 maxSize,
        float scale,
        TextState textState)
    {
        this.Sender = sender;
        this.RenderPass = renderPass;
        this.MaxSize = maxSize;
        this.Scale = scale;
        this.TextState = textState;
    }

    /// <summary>Notifies a child <see cref="ISpannable"/> with transformed arguments.</summary>
    /// <param name="child">A child to notify the event.</param>
    /// <param name="childRenderPass">The child state.</param>
    /// <param name="maxSize">The maximum size for the child.</param>
    /// <param name="textState">The initial text state for the child.</param>
    public readonly void NotifyChild(
        ISpannable child,
        ISpannableRenderPass childRenderPass,
        Vector2 maxSize,
        in TextState textState) =>
        childRenderPass.MeasureSpannable(
            this with
            {
                Sender = child,
                RenderPass = childRenderPass,
                MaxSize = maxSize,
                TextState = textState,
            });
}
