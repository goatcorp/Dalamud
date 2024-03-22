using System.Numerics;

using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables;

/// <summary>Render state interface for use with <see cref="ISpannable.RentState"/>.</summary>
/// <remarks>Implement this interface to store information for use across <see cref="ISpannable.MeasureSpannable"/>,
/// <see cref="ISpannable.HandleSpannableInteraction"/>, and <see cref="ISpannable.DrawSpannable"/> calls.</remarks>
public interface ISpannableState
{
    /// <summary>Gets the mutable reference to the render state.</summary>
    ref TextState TextState { get; }

    /// <summary>Gets the global ImGui ID for this spannable.</summary>
    /// <remarks><c>0</c> if no ID is assigned.</remarks>
    uint ImGuiGlobalId { get; }

    /// <summary>Gets the measured boundary from <see cref="ISpannable.MeasureSpannable"/>.</summary>
    ref readonly RectVector4 Boundary { get; }
    
    /// <summary>Gets the screen offset of the left top, pre-transformed, from
    /// <see cref="ISpannable.CommitSpannableMeasurement"/>.</summary>
    Vector2 ScreenOffset { get; }

    /// <summary>Gets the transformation origin, in the offset ratio of <see cref="Boundary"/>.</summary>
    /// <remarks>If (0, 0) is set, then the transformation will happen with left top as the origin.<br />
    /// If (1, 1) is set, then the transformation will happen with right bottom as the origin.<br />
    /// If (0.5, 0) is set, then the transformation will happen with top center as the origin.</remarks>
    Vector2 TransformationOrigin { get; }

    /// <summary>Gets the transformation matrix from <see cref="ISpannable.CommitSpannableMeasurement"/>.</summary>
    ref readonly Trss Transformation { get; }

    /// <summary>Gets the current renderer.</summary>
    ISpannableRenderer Renderer { get; }
}
