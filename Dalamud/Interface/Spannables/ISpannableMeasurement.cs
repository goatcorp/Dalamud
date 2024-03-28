using System.Numerics;

using Dalamud.Plugin.Services;
using Dalamud.Utility.Numerics;

using ImGuiNET;

using Microsoft.Extensions.ObjectPool;

namespace Dalamud.Interface.Spannables;

/// <summary>Mutable measure state for a spannable.</summary>
public interface ISpannableMeasurement : IResettable
{
    /// <summary>Gets the associated spannable.</summary>
    ISpannable? Spannable { get; }

    /// <summary>Gets the renderer being used.</summary>
    ISpannableRenderer? Renderer { get; }

    /// <summary>Gets a value indicating whether the measured values stored as properties are valid.</summary>
    /// <value><c>true</c> if measurement values are valid and ready for use; <c>false</c> if a measurement pass has to
    /// be performed again.</value>
    bool IsMeasurementValid { get; }

    /// <summary>Gets the measured boundary.</summary>
    /// <remarks>Boundary may extend leftward or upward past zero.</remarks>
    RectVector4 Boundary { get; }

    /// <summary>Gets the mutable options for <see cref="Spannable"/>.</summary>
    ISpannableMeasurementOptions Options { get; }

    /// <summary>Gets a read-only reference to the full transformation matrix.</summary>
    ref readonly Matrix4x4 FullTransformation { get; }

    /// <summary>Gets or sets the ImGui global ID.</summary>
    uint ImGuiGlobalId { get; set; }

    /// <summary>Gets or sets the render scale.</summary>
    /// <remarks>Used only for loading underlying resources that will accommodate drawing without being blurry.
    /// Setting this property alone does not mean scaling the result.</remarks>
    float RenderScale { get; set; }

    /// <summary>Measures the spannable according to the parameters specified, and updates the result properties.
    /// </summary>
    /// <returns><c>true</c> if the spannable has just been measured; <c>false</c> if no measurement has happened
    /// because nothing changed.</returns>
    bool Measure();

    /// <summary>Handles interaction.</summary>
    /// <returns><c>true</c> if any processing is done; <c>false</c> if it was impossible because of reasons such as
    /// <see cref="ImGuiGlobalId"/> being 0 or no measurement being available.</returns>
    bool HandleInteraction();

    /// <summary>Updates the transformation for the measured data.</summary>
    /// <param name="local">The local transformation matrix.</param>
    /// <param name="ancestral">The ancestral transformation matrix.</param>
    void UpdateTransformation(scoped in Matrix4x4 local, scoped in Matrix4x4 ancestral);

    /// <summary>Draws from the measured data.</summary>
    /// <param name="drawListPtr">The target draw list.</param>
    void Draw(ImDrawListPtr drawListPtr);

    /// <summary>Returns this measurement to the pool owned by <see cref="Spannable"/>.</summary>
    void ReturnMeasurementToSpannable();
}
