using System.Numerics;

namespace Dalamud.Interface.Spannables.EventHandlerArgs;

/// <summary>Arguments for use with <see cref="ISpannable.MeasureSpannable"/>.</summary>
public struct SpannableMeasureArgs
{
    /// <summary>The state obtained from <see cref="ISpannable.RentState"/>.</summary>
    public readonly ISpannableState State;

    /// <summary>The maximum size available for the spannable.</summary>
    public readonly Vector2 MaxSize;

    /// <summary>Initializes a new instance of the <see cref="SpannableMeasureArgs"/> struct.</summary>
    /// <param name="state">The state for the spannable.</param>
    /// <param name="maxSize">The maximum size.</param>
    public SpannableMeasureArgs(ISpannableState state, Vector2 maxSize)
    {
        this.State = state;
        this.MaxSize = maxSize;
    }
}
