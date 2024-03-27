using System.Numerics;

using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.RenderPassMethodArgs;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables;

/// <summary>Render state interface for use with <see cref="ISpannable.RentRenderPass"/>.</summary>
/// <remarks>Implement this interface to store information for use across <see cref="MeasureSpannable"/>,
/// <see cref="HandleSpannableInteraction"/>, and <see cref="DrawSpannable"/> calls.</remarks>
public interface ISpannableRenderPass
{
    /// <summary>Gets the object that created this render pass.</summary>
    ISpannable RenderPassCreator { get; }
    
    /// <summary>Gets the mutable reference to the render state.</summary>
    ref TextState ActiveTextState { get; }

    /// <summary>Gets the global ImGui ID from <see cref="HandleSpannableInteraction"/>.</summary>
    /// <remarks><c>0</c> if no ID is assigned.</remarks>
    uint ImGuiGlobalId { get; }

    /// <summary>Gets the measured boundary from <see cref="MeasureSpannable"/>.</summary>
    ref readonly RectVector4 Boundary { get; }

    /// <summary>Gets the origin, in the offset ratio of <see cref="Boundary"/>.</summary>
    Vector2 InnerOrigin { get; }

    /// <summary>Gets the direct transformation matrix from the parent, from <see cref="CommitSpannableMeasurement"/>.
    /// </summary>
    ref readonly Matrix4x4 TransformationFromParent { get; }

    /// <summary>Gets the full transformation matrix from all the ancestors, from
    /// <see cref="CommitSpannableMeasurement"/>.</summary>
    ref readonly Matrix4x4 TransformationFromAncestors { get; }

    /// <summary>Gets the current renderer.</summary>
    ISpannableRenderer Renderer { get; }

    /// <summary>Measures this spannable, given the constraints set via <see cref="ActiveTextState"/>.</summary>
    /// <param name="args">The arguments.</param>
    void MeasureSpannable(scoped in SpannableMeasureArgs args);

    /// <summary>Commits the calculated transformation values. </summary>
    /// <param name="args">The arguments.</param>
    void CommitSpannableMeasurement(scoped in SpannableCommitMeasurementArgs args);

    /// <summary>Draws this spannable.</summary>
    /// <param name="args">The arguments.</param>
    void DrawSpannable(SpannableDrawArgs args);

    /// <summary>Interacts with this spannable.</summary>
    /// <param name="args">The arguments.</param>
    /// <param name="link">The interacted link.</param>
    void HandleSpannableInteraction(scoped in SpannableHandleInteractionArgs args, out SpannableLinkInteracted link);
}
