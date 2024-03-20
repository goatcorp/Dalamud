using Dalamud.Interface.SpannedStrings.Rendering;

namespace Dalamud.Interface.SpannedStrings.Spannables;

/// <summary>Render state interface for use with <see cref="ISpannable.RentState"/>.</summary>
public interface ISpannableState
{
    /// <summary>Gets the mutalbe reference to the render state.</summary>
    public ref RenderState RenderState { get; }

    /// <summary>Gets the current renderer.</summary>
    public ISpannableRenderer Renderer { get; }
}
