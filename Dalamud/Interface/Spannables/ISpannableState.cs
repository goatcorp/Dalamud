using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Plugin.Services;

namespace Dalamud.Interface.Spannables;

/// <summary>Render state interface for use with <see cref="ISpannable.RentState"/>.</summary>
/// <remarks>Implement this interface to store information for use across <see cref="ISpannable.Measure"/>,
/// <see cref="ISpannable.InteractWith"/>, and <see cref="ISpannable.Draw"/> calls.</remarks>
public interface ISpannableState
{
    /// <summary>Gets the mutalbe reference to the render state.</summary>
    ref RenderState RenderState { get; }

    /// <summary>Gets the current renderer.</summary>
    ISpannableRenderer Renderer { get; }
}
