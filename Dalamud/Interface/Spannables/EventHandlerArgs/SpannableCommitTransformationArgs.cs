using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.EventHandlerArgs;

/// <summary>Arguments for use with <see cref="ISpannableRenderPass.CommitSpannableMeasurement"/>.</summary>
public struct SpannableCommitTransformationArgs
{
    /// <summary>The associated spannable.</summary>
    public ISpannable Sender;

    /// <summary>The state obtained from <see cref="ISpannable.RentRenderPass"/>.</summary>
    public ISpannableRenderPass RenderPass;

    /// <inheritdoc cref="ISpannableRenderPass.ScreenOffset"/>
    public Vector2 ScreenOffset;

    /// <inheritdoc cref="ISpannableRenderPass.TransformationOrigin"/>
    public Vector2 TransformationOrigin;

    /// <inheritdoc cref="ISpannableRenderPass.Transformation"/>
    public Matrix4x4 Transformation;

    /// <summary>Initializes a new instance of the <see cref="SpannableCommitTransformationArgs"/> struct.</summary>
    /// <param name="sender">The associated spannable.</param>
    /// <param name="renderPass">The state for the spannable.</param>
    /// <param name="screenOffset">The screen offset.</param>
    /// <param name="transformationOrigin">The transformation origin.</param>
    /// <param name="transformation">The transformation matrix.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpannableCommitTransformationArgs(
        ISpannable sender, ISpannableRenderPass renderPass,
        Vector2 screenOffset,
        Vector2 transformationOrigin,
        Matrix4x4 transformation)
    {
        this.Sender = sender;
        this.RenderPass = renderPass;
        this.ScreenOffset = screenOffset;
        this.Transformation = transformation;
        this.TransformationOrigin = transformationOrigin;
    }

    /// <summary>Notifies a child <see cref="ISpannable"/> with transformed arguments.</summary>
    /// <param name="child">A child to notify the event.</param>
    /// <param name="childRenderPass">The child state.</param>
    /// <param name="childOffset">The child offset within this spannable.</param>
    /// <param name="extraTransformation">Any extra transformation for the child.</param>
    public readonly void NotifyChild(
        ISpannable child,
        ISpannableRenderPass childRenderPass,
        Vector2 childOffset,
        in Matrix4x4 extraTransformation) =>
        childRenderPass.CommitSpannableMeasurement(
            new(
                child,
                childRenderPass,
                this.RenderPass.TransformToScreen(childOffset),
                Vector2.Zero,
                Matrix4x4.Multiply(extraTransformation, this.Transformation.WithoutTranslation())));
}
