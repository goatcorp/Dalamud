using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables;

/// <summary>Render state interface for use with <see cref="ISpannable.RentRenderPass"/>.</summary>
/// <remarks>Implement this interface to store information for use across <see cref="MeasureSpannable"/>,
/// <see cref="HandleSpannableInteraction"/>, and <see cref="DrawSpannable"/> calls.</remarks>
public interface ISpannableRenderPass
{
    /// <summary>Gets the mutable reference to the render state.</summary>
    ref TextState TextState { get; }

    /// <summary>Gets the measured boundary from <see cref="MeasureSpannable"/>.</summary>
    ref readonly RectVector4 Boundary { get; }

    /// <summary>Gets the screen offset of the left top, pre-transformed, from
    /// <see cref="CommitSpannableMeasurement"/>.</summary>
    Vector2 ScreenOffset { get; }

    /// <summary>Gets the transformation origin, in the offset ratio of <see cref="Boundary"/>.</summary>
    /// <remarks>If (0, 0) is set, then the transformation will happen with left top as the origin.<br />
    /// If (1, 1) is set, then the transformation will happen with right bottom as the origin.<br />
    /// If (0.5, 0) is set, then the transformation will happen with top center as the origin.</remarks>
    Vector2 TransformationOrigin { get; }

    /// <summary>Gets the transformation matrix from <see cref="CommitSpannableMeasurement"/>.</summary>
    ref readonly Matrix4x4 Transformation { get; }

    /// <summary>Gets the global ImGui ID from <see cref="HandleSpannableInteraction"/>.</summary>
    /// <remarks><c>0</c> if no ID is assigned.</remarks>
    uint ImGuiGlobalId { get; }

    /// <summary>Gets the current renderer.</summary>
    ISpannableRenderer Renderer { get; }

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
