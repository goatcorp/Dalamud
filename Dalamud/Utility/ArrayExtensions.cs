using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Dalamud.Utility;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1618:Generic type parameters should be documented", Justification = "Reviewed,")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Reviewed,")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1615:Element return value should be documented", Justification = "Reviewed,")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1611:Element parameters should be documented", Justification = "Reviewed,")]
internal static class ArrayExtensions
{
    /// <summary> Iterate over enumerables with additional index. </summary>
    public static IEnumerable<(T Value, int Index)> WithIndex<T>(this IEnumerable<T> list)
        => list.Select((x, i) => (x, i));

    /// <summary> Remove an added index from an indexed enumerable. </summary>
    public static IEnumerable<T> WithoutIndex<T>(this IEnumerable<(T Value, int Index)> list)
        => list.Select(x => x.Value);

    /// <summary> Remove the value and only keep the index from an indexed enumerable. </summary>
    public static IEnumerable<int> WithoutValue<T>(this IEnumerable<(T Value, int Index)> list)
        => list.Select(x => x.Index);

    // Find the index of the first object fulfilling predicate's criteria in the given list.
    // Returns -1 if no such object is found.
    public static int IndexOf<T>(this IEnumerable<T> array, Predicate<T> predicate)
    {
        var i = 0;
        foreach (var obj in array)
        {
            if (predicate(obj))
                return i;

            ++i;
        }

        return -1;
    }

    // Find the index of the first occurrence of needle in the given list.
    // Returns -1 if needle is not contained in the list.
    public static int IndexOf<T>(this IEnumerable<T> array, T needle) where T : notnull
    {
        var i = 0;
        foreach (var obj in array)
        {
            if (needle.Equals(obj))
                return i;

            ++i;
        }

        return -1;
    }

    // Find the first object fulfilling predicate's criteria in the given list, if one exists.
    // Returns true if an object is found, false otherwise.
    public static bool FindFirst<T>(this IEnumerable<T> array, Predicate<T> predicate, [NotNullWhen(true)] out T? result)
    {
        foreach (var obj in array)
        {
            if (predicate(obj))
            {
                result = obj!;
                return true;
            }
        }

        result = default;
        return false;
    }

    // Find the first occurrence of needle in the given list and return the value contained in the list in result.
    // Returns true if an object is found, false otherwise.
    public static bool FindFirst<T>(this IEnumerable<T> array, T needle, [NotNullWhen(true)] out T? result) where T : notnull
    {
        foreach (var obj in array)
        {
            if (obj.Equals(needle))
            {
                result = obj;
                return true;
            }
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Interprets the given array as an <see cref="IReadOnlyCollection{T}"/>, so that you can enumerate it multiple
    /// times, and know the number of elements within.
    /// </summary>
    /// <param name="array">The enumerable.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns><paramref name="array"/> casted as a <see cref="IReadOnlyCollection{T}"/> if it is one; otherwise the result of <see cref="Enumerable.ToArray{TSource}"/>.</returns>
    public static IReadOnlyCollection<T> AsReadOnlyCollection<T>(this IEnumerable<T> array) =>
        array as IReadOnlyCollection<T> ?? array.ToArray();

    /// <inheritdoc cref="List{T}.FindIndex(System.Predicate{T})"/>
    public static int FindIndex<T>(this IReadOnlyList<T> list, Predicate<T> match)
        => list.FindIndex(0, list.Count, match);

    /// <inheritdoc cref="List{T}.FindIndex(int,System.Predicate{T})"/>
    public static int FindIndex<T>(this IReadOnlyList<T> list, int startIndex, Predicate<T> match)
        => list.FindIndex(startIndex, list.Count - startIndex, match);

    /// <inheritdoc cref="List{T}.FindIndex(int,int,System.Predicate{T})"/>
    public static int FindIndex<T>(this IReadOnlyList<T> list, int startIndex, int count, Predicate<T> match)
    {
        if ((uint)startIndex > (uint)list.Count)
            throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, null);

        if (count < 0 || startIndex > list.Count - count)
            throw new ArgumentOutOfRangeException(nameof(count), count, null);

        if (match == null)
            throw new ArgumentNullException(nameof(match));

        var endIndex = startIndex + count;
        for (var i = startIndex; i < endIndex; i++)
        {
            if (match(list[i])) return i;
        }

        return -1;
    }

    /// <inheritdoc cref="List{T}.FindLastIndex(System.Predicate{T})"/>
    public static int FindLastIndex<T>(this IReadOnlyList<T> list, Predicate<T> match)
        => list.FindLastIndex(list.Count - 1, list.Count, match);

    /// <inheritdoc cref="List{T}.FindLastIndex(int,System.Predicate{T})"/>
    public static int FindLastIndex<T>(this IReadOnlyList<T> list, int startIndex, Predicate<T> match)
        => list.FindLastIndex(startIndex, startIndex + 1, match);

    /// <inheritdoc cref="List{T}.FindLastIndex(int,int,System.Predicate{T})"/>
    public static int FindLastIndex<T>(this IReadOnlyList<T> list, int startIndex, int count, Predicate<T> match)
    {
        if (match == null)
            throw new ArgumentNullException(nameof(match));

        if (list.Count == 0)
        {
            // Special case for 0 length List
            if (startIndex != -1)
                throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, null);
        }
        else
        {
            // Make sure we're not out of range
            if ((uint)startIndex >= (uint)list.Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, null);
        }

        // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
        if (count < 0 || startIndex - count + 1 < 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, null);

        var endIndex = startIndex - count;
        for (var i = startIndex; i > endIndex; i--)
        {
            if (match(list[i]))
            {
                return i;
            }
        }

        return -1;
    }
}
