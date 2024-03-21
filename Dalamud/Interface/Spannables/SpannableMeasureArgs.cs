namespace Dalamud.Interface.Spannables;

/// <summary>Arguments for use with <see cref="ISpannable.Measure"/>.</summary>
public struct SpannableMeasureArgs
{
    /// <summary>The state obtained from <see cref="ISpannable.RentState"/>.</summary>
    public readonly ISpannableState State;

    /// <summary>Initializes a new instance of the <see cref="SpannableMeasureArgs"/> struct.</summary>
    /// <param name="state">The state for the spannable.</param>
    public SpannableMeasureArgs(ISpannableState state) => this.State = state;
}
