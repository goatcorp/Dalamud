using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Dalamud.Utility;

/// <summary>
/// An implementation of a weak concurrent set based on a <see cref="ConditionalWeakTable{TKey,TValue}"/>.
/// </summary>
/// <typeparam name="T">The type of object that we're tracking.</typeparam>
public class WeakConcurrentCollection<T> : ICollection<T> where T : class
{
    private readonly ConditionalWeakTable<T, object> cwt = new();
    
    /// <inheritdoc/>
    public int Count => this.cwt.Count();

    /// <inheritdoc/>
    public bool IsReadOnly => false;
    
    private IEnumerable<T> Keys => this.cwt.Select(pair => pair.Key);

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => this.cwt.Select(pair => pair.Key).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.cwt.Select(pair => pair.Key).GetEnumerator();

    /// <inheritdoc/>
    public void Add(T item) => this.cwt.AddOrUpdate(item, null);
    
    /// <inheritdoc/>
    public void Clear() => this.cwt.Clear();

    /// <inheritdoc/>
    public bool Contains(T item) => this.Keys.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex) => this.Keys.ToArray().CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public bool Remove(T item) => this.cwt.Remove(item);
}
