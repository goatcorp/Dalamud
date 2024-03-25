using System.Numerics;
using System.Runtime.CompilerServices;

namespace Dalamud.Interface.Spannables.RenderPassMethodArgs;

/// <summary>Arguments for use with <see cref="ISpannableRenderPass.CommitSpannableMeasurement"/>.</summary>
public struct SpannableCommitTransformationArgs
{
    /// <summary>The associated spannable.</summary>
    public ISpannable Sender;

    /// <summary>The state obtained from <see cref="ISpannable.RentRenderPass"/>.</summary>
    public ISpannableRenderPass RenderPass;

    /// <inheritdoc cref="ISpannableRenderPass.InnerOrigin"/>
    public Vector2 InnerOrigin;

    /// <inheritdoc cref="ISpannableRenderPass.TransformationFromParent"/>
    public Matrix4x4 TransformationFromParent;

    /// <inheritdoc cref="ISpannableRenderPass.TransformationFromAncestors"/>
    public Matrix4x4 TransformationFromAncestors;

    /// <summary>Initializes a new instance of the <see cref="SpannableCommitTransformationArgs"/> struct.</summary>
    /// <param name="sender">The associated spannable.</param>
    /// <param name="renderPass">The state for the spannable.</param>
    /// <param name="innerOrigin">The transformation origin.</param>
    /// <param name="transformationFromParent">The transformation matrix from the direct parent.</param>
    /// <param name="transformationFromAncestors">The transformation matrix from all ancestors.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpannableCommitTransformationArgs(
        ISpannable sender,
        ISpannableRenderPass renderPass,
        Vector2 innerOrigin,
        Matrix4x4 transformationFromParent,
        Matrix4x4 transformationFromAncestors)
    {
        this.Sender = sender;
        this.RenderPass = renderPass;
        this.TransformationFromParent = transformationFromParent;
        this.TransformationFromAncestors = transformationFromAncestors;
        this.InnerOrigin = innerOrigin;
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
        in Matrix4x4 extraTransformation)
    {
        var mtx = Matrix4x4.Identity;
        mtx = Matrix4x4.Multiply(mtx, Matrix4x4.CreateTranslation(new(childOffset, 0)));
        mtx = Matrix4x4.Multiply(mtx, extraTransformation);
        childRenderPass.CommitSpannableMeasurement(
            new(
                child,
                childRenderPass,
                this.InnerOrigin,
                mtx,
                Matrix4x4.Multiply(mtx, this.TransformationFromAncestors)));
    }
}
