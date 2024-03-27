using System.Numerics;
using System.Runtime.CompilerServices;

namespace Dalamud.Interface.Spannables.RenderPassMethodArgs;

/// <summary>Arguments for use with <see cref="ISpannableRenderPass.CommitSpannableMeasurement"/>.</summary>
public struct SpannableCommitMeasurementArgs
{
    /// <summary>The state obtained from <see cref="ISpannable.RentRenderPass"/>.</summary>
    public ISpannableRenderPass RenderPass;

    /// <inheritdoc cref="ISpannableRenderPass.InnerOrigin"/>
    public Vector2 InnerOrigin;

    /// <inheritdoc cref="ISpannableRenderPass.TransformationFromParent"/>
    public Matrix4x4 TransformationFromParent;

    /// <inheritdoc cref="ISpannableRenderPass.TransformationFromAncestors"/>
    public Matrix4x4 TransformationFromAncestors;

    /// <summary>Initializes a new instance of the <see cref="SpannableCommitMeasurementArgs"/> struct.</summary>
    /// <param name="renderPass">The state for the spannable.</param>
    /// <param name="innerOrigin">The transformation origin.</param>
    /// <param name="transformationFromParent">The transformation matrix from the direct parent.</param>
    /// <param name="transformationFromAncestors">The transformation matrix from all ancestors.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpannableCommitMeasurementArgs(
        ISpannableRenderPass renderPass,
        Vector2 innerOrigin,
        in Matrix4x4 transformationFromParent,
        in Matrix4x4 transformationFromAncestors)
    {
        this.TransformationFromParent = transformationFromParent;
        this.TransformationFromAncestors = transformationFromAncestors;
        this.RenderPass = renderPass;
        this.InnerOrigin = innerOrigin;
    }
}
