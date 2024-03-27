using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.RenderPassMethodArgs;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Helpers;

/// <summary>Extension methods for everything under <see cref="Spannables"/>.</summary>
public static class SpannableExtensions
{
    /// <summary>Gets a global ID from an inner ID.</summary>
    /// <param name="renderPass">The spannable render pass.</param>
    /// <param name="innerId">The inner ID.</param>
    /// <returns>The global ID.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe uint GetGlobalIdFromInnerId(this ISpannableRenderPass renderPass, int innerId) =>
        ImGuiNative.igGetID_Ptr((void*)(((ulong)renderPass.ImGuiGlobalId << 32) | (uint)innerId));

    /// <summary>Transforms spannable-local coordiantes to screen coordinates.</summary>
    /// <param name="renderPass">The spannable render pass.</param>
    /// <param name="p">The point to transform.</param>
    /// <returns>The transformed coordinates.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 PointToScreen(this ISpannableRenderPass renderPass, Vector2 p) =>
        Vector2.Transform(p, renderPass.TransformationFromAncestors);

    /// <summary>Transforms screen coordiantes to spannable-local coordinates.</summary>
    /// <param name="renderPass">The spannable render pass.</param>
    /// <param name="p">The point to transform.</param>
    /// <returns>The transformed coordinates.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 PointToClient(this ISpannableRenderPass renderPass, Vector2 p) =>
        Vector2.Transform(
            p,
            Matrix4x4.Invert(renderPass.TransformationFromAncestors, out var inverted)
                ? inverted
                : Matrix4x4.Identity);

    /// <summary>Notifies a child <see cref="ISpannable"/> with transformed arguments.</summary>
    /// <param name="sourceArgs">The original arguments.</param>
    /// <param name="childRenderPass">Render pass for the child spannable.</param>
    /// <param name="childInnerId">Inner child ID.</param>
    /// <param name="childMeasureArgs">Measure arguments for the child.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotifyChild(
        this scoped in SpannableMeasureArgs sourceArgs,
        ISpannableRenderPass childRenderPass,
        int childInnerId,
        in SpannableMeasureArgs childMeasureArgs) =>
        childRenderPass.MeasureSpannable(
            childMeasureArgs with
            {
                RenderPass = childRenderPass,
                ImGuiGlobalId = sourceArgs.ImGuiGlobalId == 0 || childInnerId == -1
                                    ? 0
                                    : sourceArgs.RenderPass.GetGlobalIdFromInnerId(childInnerId),
            });

    /// <summary>Notifies a child <see cref="ISpannable"/> with transformed arguments.</summary>
    /// <param name="sourceArgs">The original arguments.</param>
    /// <param name="childRenderPass">The child state.</param>
    /// <param name="childCommitMeasurementArgs">Commit measurement arguments for the child.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotifyChild(
        this scoped in SpannableCommitMeasurementArgs sourceArgs,
        ISpannableRenderPass childRenderPass,
        in SpannableCommitMeasurementArgs childCommitMeasurementArgs) =>
        childRenderPass.CommitSpannableMeasurement(childCommitMeasurementArgs with
        {
            RenderPass = childRenderPass,
            TransformationFromParent = Matrix4x4.Identity,
        });
    
    /// <summary>Notifies a child <see cref="ISpannable"/> with transformed arguments.</summary>
    /// <param name="sourceArgs">The original arguments.</param>
    /// <param name="childRenderPass">The child state.</param>
    /// <param name="childCommitMeasurementArgs">Commit measurement arguments for the child.</param>
    /// <param name="transformation">Optional transformation to apply to the child.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotifyChild(
        this scoped in SpannableCommitMeasurementArgs sourceArgs,
        ISpannableRenderPass childRenderPass,
        in SpannableCommitMeasurementArgs childCommitMeasurementArgs,
        in Matrix4x4 transformation) =>
        childRenderPass.CommitSpannableMeasurement(childCommitMeasurementArgs with
        {
            RenderPass = childRenderPass,
            TransformationFromParent = transformation,
            TransformationFromAncestors = Matrix4x4.Multiply(transformation, sourceArgs.TransformationFromAncestors),
        });

    /// <summary>Notifies a child <see cref="ISpannable"/> with transformed arguments.</summary>
    /// <param name="sourceArgs">The original arguments.</param>
    /// <param name="childRenderPass">The child state.</param>
    /// <param name="childCommitMeasurementArgs">Commit measurement arguments for the child.</param>
    /// <param name="transformation1">Optional 1st transformation to apply to the child.</param>
    /// <param name="transformation2">Optional 2nd transformation to apply to the child.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotifyChild(
        this scoped in SpannableCommitMeasurementArgs sourceArgs,
        ISpannableRenderPass childRenderPass,
        in SpannableCommitMeasurementArgs childCommitMeasurementArgs,
        in Matrix4x4 transformation1,
        in Matrix4x4 transformation2) =>
        sourceArgs.NotifyChild(
            childRenderPass,
            childCommitMeasurementArgs,
            Matrix4x4.Multiply(transformation1, transformation2));

    /// <summary>Notifies a child <see cref="ISpannable"/> with transformed arguments.</summary>
    /// <param name="sourceArgs">The original arguments.</param>
    /// <param name="childRenderPass">Render pass for the child spannable.</param>
    /// <param name="childHandleInteractionArgs">Handle interaction arguments for the child.</param>
    /// <param name="link">The interacted link, if the child processed the event.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotifyChild(
        this scoped in SpannableHandleInteractionArgs sourceArgs,
        ISpannableRenderPass childRenderPass,
        scoped in SpannableHandleInteractionArgs childHandleInteractionArgs,
        out SpannableLinkInteracted link) =>
        childRenderPass.HandleSpannableInteraction(
            childHandleInteractionArgs with
            {
                RenderPass = childRenderPass,
                MouseLocalLocation = Vector2.Transform(
                    sourceArgs.MouseLocalLocation,
                    Matrix4x4.Invert(childRenderPass.TransformationFromParent, out var inverted)
                        ? inverted
                        : Matrix4x4.Identity),
            },
            out link);

    /// <summary>Notifies a child <see cref="ISpannable"/> with transformed arguments.</summary>
    /// <param name="sourceArgs">The original arguments.</param>
    /// <param name="childRenderPass">The child state.</param>
    /// <param name="childDrawArgs">Draw arguments for the child.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotifyChild(
        this scoped in SpannableDrawArgs sourceArgs,
        ISpannableRenderPass childRenderPass,
        in SpannableDrawArgs childDrawArgs) =>
        childRenderPass.DrawSpannable(childDrawArgs with { RenderPass = childRenderPass });

    /// <summary>Copies the this value to an out parameter, for extracting a temporary value in a inline list or
    /// dictionary definition.</summary>
    /// <param name="value">Source value.</param>
    /// <param name="outValue">Value copy destination.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>The same <paramref name="value"/> for method chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetAsOut<T>(this T value, out T outValue)
        where T : ISpannable => outValue = value;

    /// <summary>Enumerates all items in the hierarchy under a spannable, including itself.</summary>
    /// <param name="root">The root item to enumerate.</param>
    /// <returns>An enumerable that enumerates through all spannables under the root, including itself.</returns>
    public static IEnumerable<ISpannable> EnumerateHierarchy(this ISpannable root)
    {
        yield return root;
        foreach (var s in root.GetAllChildSpannables())
        {
            if (s is null)
                continue;

            foreach (var child in s.EnumerateHierarchy())
                yield return child;
        }
    }

    /// <summary>Enumerates all items in the hierarchy under a spannable, including itself.</summary>
    /// <param name="root">The root item to enumerate.</param>
    /// <typeparam name="T">The type of spannable interested.</typeparam>
    /// <returns>An enumerable that enumerates through all spannables under the root, including itself.</returns>
    public static IEnumerable<T> EnumerateHierarchy<T>(this ISpannable root) where T : ISpannable
    {
        if (root is T roott)
            yield return roott;
        foreach (var s in root.GetAllChildSpannables())
        {
            if (s is null)
                continue;

            foreach (var child in s.EnumerateHierarchy<T>())
                yield return child;
        }
    }
}
