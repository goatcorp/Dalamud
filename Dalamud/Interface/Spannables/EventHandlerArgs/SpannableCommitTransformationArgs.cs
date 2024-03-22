using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.EventHandlerArgs;

/// <summary>Arguments for use with <see cref="ISpannable.CommitMeasurement"/>.</summary>
public struct SpannableCommitTransformationArgs
{
    /// <summary>The state obtained from <see cref="ISpannable.RentState"/>.</summary>
    public ISpannableState State;

    /// <inheritdoc cref="ISpannableState.ScreenOffset"/>
    public Vector2 ScreenOffset;

    /// <inheritdoc cref="ISpannableState.TransformationOrigin"/>
    public Vector2 TransformationOrigin;

    /// <inheritdoc cref="ISpannableState.Transformation"/>
    public Trss Transformation;

    /// <summary>Initializes a new instance of the <see cref="SpannableCommitTransformationArgs"/> struct.</summary>
    /// <param name="state">The state for the spannable.</param>
    /// <param name="screenOffset">The screen offset.</param>
    /// <param name="transformationOrigin">The transformation origin.</param>
    /// <param name="transformation">The transformation matrix.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpannableCommitTransformationArgs(
        ISpannableState state,
        Vector2 screenOffset,
        Vector2 transformationOrigin,
        Trss transformation)
    {
        this.State = state;
        this.ScreenOffset = screenOffset;
        this.Transformation = transformation;
        this.TransformationOrigin = transformationOrigin;
    }
}
