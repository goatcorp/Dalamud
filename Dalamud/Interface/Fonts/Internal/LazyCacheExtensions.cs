using System.Collections.Concurrent;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Extensions for caches of lazily made values.</summary>
internal static class LazyCacheExtensions
{
    /// <summary>Gets the value of a lazy from a cache. A lazy that throws is removed, so that the next try makes it
    /// again.</summary>
    /// <param name="cache">The cache.</param>
    /// <param name="key">The key of the lazy.</param>
    /// <param name="lazy">The lazy in the cache.</param>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    /// <returns>The value.</returns>
    public static TValue ValueOrRemove<TKey, TValue>(
        this ConcurrentDictionary<TKey, Lazy<TValue>> cache, TKey key, Lazy<TValue> lazy)
        where TKey : notnull
    {
        try
        {
            return lazy.Value;
        }
        catch
        {
            cache.TryRemove(new(key, lazy));
            throw;
        }
    }
}
