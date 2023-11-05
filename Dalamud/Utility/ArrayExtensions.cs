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

    /// <summary>
    /// Dispose all items in the given enumerable.
    /// </summary>
    /// <param name="array">The enumerable.</param>
    /// <typeparam name="T">Disposable type.</typeparam>
    public static void DisposeItems<T>(this IEnumerable<T> array)
        where T : IDisposable 
    {
        List<Exception>? excs = null;
        foreach (var x in array)
        {
            try
            {
                x.Dispose();
            }
            catch (Exception ex)
            {
                (excs ??= new()).Add(ex);
            }
        }

        if (excs is not null)
            throw excs.Count == 1 ? excs[0] : new AggregateException(excs);
    }

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
}
