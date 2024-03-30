using System.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls;

/// <summary>Extension methods for <see cref="ControlSpannable"/>.</summary>
public static class ControlSpannableExtensions
{
    /// <inheritdoc cref="ISpannableMeasurement.Measure"/>
    public static bool ExplicitMeasure(this ControlSpannable cs) => ((ISpannableMeasurement)cs).Measure();

    /// <inheritdoc cref="ISpannableMeasurement.HandleInteraction"/>
    public static bool ExplicitHandleInteraction(this ControlSpannable cs) => ((ISpannableMeasurement)cs).HandleInteraction();

    /// <inheritdoc cref="ISpannableMeasurement.UpdateTransformation"/>
    public static void ExplicitUpdateTransformation(
        this ControlSpannable cs,
        scoped in Matrix4x4 local,
        scoped in Matrix4x4 ancestral) =>
        ((ISpannableMeasurement)cs).UpdateTransformation(local, ancestral);

    /// <inheritdoc cref="ISpannableMeasurement.Draw"/>
    public static void ExplicitDraw(this ControlSpannable cs, ImDrawListPtr drawListPtr) =>
        ((ISpannableMeasurement)cs).Draw(drawListPtr);
}
