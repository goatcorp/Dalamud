using System.Collections.Generic;
using System.Runtime.CompilerServices;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Helpers;

/// <summary>Extension methods for everything under <see cref="Spannables"/>.</summary>
public static class SpannableExtensions
{
    /// <summary>Copies the this value to an out parameter, for extracting a temporary value in a inline list or
    /// dictionary definition.</summary>
    /// <param name="value">Source value.</param>
    /// <param name="outValue">Value copy destination.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>The same <paramref name="value"/> for method chaining.</returns>
    public static T GetAsOut<T>(this T value, out T outValue)
        where T : ISpannable => outValue = value;

    /// <summary>Gets a global ID from an inner ID.</summary>
    /// <param name="renderPass">The spannable state.</param>
    /// <param name="innerId">The inner ID.</param>
    /// <returns>The global ID.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe uint GetGlobalIdFromInnerId(this ISpannableRenderPass renderPass, int innerId) =>
        ImGuiNative.igGetID_Ptr((void*)(((ulong)renderPass.ImGuiGlobalId << 32) | (uint)innerId));

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
