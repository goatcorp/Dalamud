using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Plugin.Services;

namespace Dalamud.Interface.Spannables;

/// <summary>A spannable that draws into a rectangular region.</summary>
public interface ISpannable
{
    /// <summary>Rents a state.</summary>
    /// <param name="renderer">The associated renderer.</param>
    /// <param name="renderState">The initial render state.</param>
    /// <param name="args">Optional spannable-implementation defined arguments.</param>
    /// <returns>The rented state.</returns>
    ISpannableState RentState(ISpannableRenderer renderer, RenderState renderState, string? args);

    /// <summary>Returns a state.</summary>
    /// <param name="state">The state to return.</param>
    /// <remarks>If <paramref name="state"/> is null, the call is a no-op.</remarks>
    void ReturnState(ISpannableState? state);

    /// <summary>Measures this spannable.</summary>
    /// <param name="args">The arguments.</param>
    void Measure(SpannableMeasureArgs args);

    /// <summary>Interacts with this spannable.</summary>
    /// <param name="args">The arguments.</param>
    /// <param name="linkData">Data of the link currently being interacted. If none, set to default.</param>
    void InteractWith(SpannableInteractionArgs args, out ReadOnlySpan<byte> linkData);

    /// <summary>Draws this spannable.</summary>
    /// <param name="args">The arguments.</param>
    void Draw(SpannableDrawArgs args);
}
