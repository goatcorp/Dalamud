using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Rendering;

namespace Dalamud.Interface.Spannables;

/// <summary>A spannable that draws into a rectangular region.</summary>
public interface ISpannable
{
    /// <summary>Rents a state.</summary>
    /// <returns>The rented state.</returns>
    /// <param name="args">The arguments.</param>
    ISpannableState RentState(scoped in SpannableRentStateArgs args);

    /// <summary>Returns a state.</summary>
    /// <param name="state">The state to return.</param>
    /// <remarks>If <paramref name="state"/> is null, the call is a no-op.</remarks>
    void ReturnState(ISpannableState? state);

    /// <summary>Measures this spannable, given the constraints set via <see cref="TextState"/>.</summary>
    /// <param name="args">The arguments.</param>
    void MeasureSpannable(scoped in SpannableMeasureArgs args);

    /// <summary>Commits the calculated transformation values. </summary>
    /// <param name="args">The arguments.</param>
    void CommitSpannableMeasurement(scoped in SpannableCommitTransformationArgs args);

    /// <summary>Interacts with this spannable.</summary>
    /// <param name="args">The arguments.</param>
    /// <param name="link">The interacted link.</param>
    void HandleSpannableInteraction(scoped in SpannableHandleInteractionArgs args, out SpannableLinkInteracted link);

    /// <summary>Draws this spannable.</summary>
    /// <param name="args">The arguments.</param>
    void DrawSpannable(SpannableDrawArgs args);
}
