using System.Runtime.CompilerServices;

namespace Dalamud.Interface;

internal static class StableInsertionSortExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void StableSort<T, TKey>(this IList<T> list, Func<T, TKey> selector)
    {
        var tmpList = new List<T>(list.Count);
        tmpList.AddRange(list.OrderBy(selector));
        for (var i = 0; i < tmpList.Count; ++i)
            list[i] = tmpList[i];
    }

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
