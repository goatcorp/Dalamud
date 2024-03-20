using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Dalamud.Utility;

/// <summary>
/// Extensions methods providing stable insertion sorts for IList.
/// </summary>
internal static class StableInsertionSortExtension
{
    /// <summary>
    /// Perform a stable sort on a list.
    /// </summary>
    /// <param name="list">The list to sort.</param>
    /// <param name="selector">Selector to order by.</param>
    /// <typeparam name="T">Element type.</typeparam>
    /// <typeparam name="TKey">Selected type.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void StableSort<T, TKey>(this IList<T> list, Func<T, TKey> selector)
    {
        var tmpList = new List<T>(list.Count);
        tmpList.AddRange(list.OrderBy(selector));
        for (var i = 0; i < tmpList.Count; ++i)
            list[i] = tmpList[i];
    }

    /// <summary>
    /// Perform a stable sort on a list.
    /// </summary>
    /// <param name="list">The list to sort.</param>
    /// <param name="comparer">Comparer to use when comparing items.</param>
    /// <typeparam name="T">Element type.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void StableSort<T>(this IList<T> list, Comparison<T> comparer)
    {
        var tmpList = new List<(T, int)>(list.Count);
        tmpList.AddRange(list.WithIndex());
        tmpList.Sort((a, b) =>
        {
            var ret = comparer(a.Item1, b.Item1);
            return ret != 0 ? ret : a.Item2.CompareTo(b.Item2);
        });
        for (var i = 0; i < tmpList.Count; ++i)
            list[i] = tmpList[i].Item1;
    }
}
