using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Plugin.Services;

namespace Dalamud.Interface.Spannables;

/// <summary>A spannable that draws into a rectangular region.</summary>
public interface ISpannable
{
    /// <summary>Rents a state.</summary>
    /// <param name="renderer">The associated renderer.</param>
    /// <param name="imGuiGlobalId">The allocated ImGui global ID. <c>0</c> to disable interaction.</param>
    /// <param name="scale">The render scale to use.</param>
    /// <param name="args">Optional spannable-implementation defined arguments.</param>
    /// <param name="textState">The initial text state.</param>
    /// <returns>The rented state.</returns>
    ISpannableState RentState(
        ISpannableRenderer renderer,
        uint imGuiGlobalId,
        float scale,
        string? args,
        in TextState textState);

    /// <summary>Returns a state.</summary>
    /// <param name="state">The state to return.</param>
    /// <remarks>If <paramref name="state"/> is null, the call is a no-op.</remarks>
    void ReturnState(ISpannableState? state);

    /// <summary>Measures this spannable, given the constraints set via <see cref="TextState"/>.</summary>
    /// <param name="args">The arguments.</param>
    void Measure(SpannableMeasureArgs args);

    /// <summary>Commits the calculated transformation values. </summary>
    /// <param name="args">The arguments.</param>
    void CommitMeasurement(SpannableCommitTransformationArgs args);

    /// <summary>Interacts with this spannable.</summary>
    /// <param name="args">The arguments.</param>
    /// <param name="link">The interacted link.</param>
    void HandleInteraction(SpannableHandleInteractionArgs args, out SpannableLinkInteracted link);

    /// <summary>Draws this spannable.</summary>
    /// <param name="args">The arguments.</param>
    void Draw(SpannableDrawArgs args);
}
