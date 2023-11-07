using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dalamud.Utility;

/// <summary>
/// Utility class for managing finalizing stuff.
/// </summary>
public sealed class DisposeStack : IDisposable, IAsyncDisposable
{
    private readonly Stack<object> stack = new();

    /// <inheritdoc cref="Stack{T}.EnsureCapacity"/>
    public void EnsureCapacity(int capacity) => this.stack.EnsureCapacity(capacity);

    /// <inheritdoc cref="Stack{T}.Push"/>
    /// <returns>The parameter.</returns>
    [return: NotNullIfNotNull(nameof(d))]
    public T? Add<T>(T? d) where T : IDisposable
    {
        if (d is not null)
            this.stack.Push(d);
        return d;
    }

    /// <inheritdoc cref="Stack{T}.Push"/>
    public Action Add(Action d)
    {
        this.stack.Push(d);
        return d;
    }

    /// <inheritdoc cref="Stack{T}.Push"/>
    public Func<Task> Add(Func<Task> d)
    {
        this.stack.Push(d);
        return d;
    }

    /// <inheritdoc cref="Stack{T}.Push"/>
    public GCHandle Add(GCHandle d)
    {
        if (d != default)
            this.stack.Push(d);
        return d;
    }

    /// <summary>
    /// Queue all the given <see cref="IDisposable"/> to be disposed later.
    /// </summary>
    /// <param name="ds">Disposables.</param>
    public void AddRange(IEnumerable<IDisposable> ds)
    {
        foreach (var d in ds)
            this.stack.Push(d);
    }

    /// <summary>
    /// Queue all the given <see cref="IDisposable"/> to be run later.
    /// </summary>
    /// <param name="ds">Actions.</param>
    public void AddRange(IEnumerable<Action> ds)
    {
        foreach (var d in ds)
            this.stack.Push(d);
    }

    /// <summary>
    /// Queue all the given <see cref="Func{T}"/> returning <see cref="Task"/> to be run later.
    /// </summary>
    /// <param name="ds">Func{Task}s.</param>
    public void AddRange(IEnumerable<Func<Task>> ds)
    {
        foreach (var d in ds)
            this.stack.Push(d);
    }

    /// <summary>
    /// Queue all the given <see cref="GCHandle"/> to be disposed later.
    /// </summary>
    /// <param name="ds">GCHandles.</param>
    public void AddRange(IEnumerable<GCHandle> ds)
    {
        foreach (var d in ds)
            this.stack.Push(d);
    }

    /// <summary>
    /// Cancel all pending disposals.
    /// </summary>
    /// <remarks>Use this after successful initialization of multiple disposables.</remarks>
    public void Cancel() => this.stack.Clear();

    /// <inheritdoc cref="Stack{T}.EnsureCapacity"/>
    /// <returns>This for method chaining.</returns>
    public DisposeStack WithEnsureCapacity(int capacity)
    {
        this.EnsureCapacity(capacity);
        return this;
    }

    /// <inheritdoc cref="Add{T}"/>
    /// <returns>This for method chaining.</returns>
    public DisposeStack With(IDisposable d)
    {
        this.Add(d);
        return this;
    }

    /// <inheritdoc cref="Add(Action)"/>
    /// <returns>This for method chaining.</returns>
    public DisposeStack With(Action d)
    {
        this.Add(d);
        return this;
    }

    /// <inheritdoc cref="Add(Func{Task})"/>
    /// <returns>This for method chaining.</returns>
    public DisposeStack With(Func<Task> d)
    {
        this.Add(d);
        return this;
    }

    /// <inheritdoc cref="Add(GCHandle)"/>
    /// <returns>This for method chaining.</returns>
    public DisposeStack With(GCHandle d)
    {
        this.Add(d);
        return this;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        List<Exception>? excs = null;
        while (this.stack.TryPop(out var o))
        {
            try
            {
                switch (o)
                {
                    case IDisposable x:
                        x.Dispose();
                        break;
                    case Action a:
                        a.Invoke();
                        break;
                    case Func<Task> a:
                        a.Invoke().Wait();
                        break;
                    case GCHandle a:
                        a.Free();
                        break;
                }
            }
            catch (Exception ex)
            {
                excs ??= new();
                excs.Add(ex);
            }
        }

        if (excs is not null)
            throw excs.Count == 1 ? excs[0] : new AggregateException(excs);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        List<Exception>? excs = null;
        while (this.stack.TryPop(out var o))
        {
            try
            {
                switch (o)
                {
                    case IAsyncDisposable x:
                        await x.DisposeAsync();
                        break;
                    case IDisposable x:
                        x.Dispose();
                        break;
                    case Func<Task> a:
                        await a.Invoke();
                        break;
                    case Action a:
                        a.Invoke();
                        break;
                    case GCHandle a:
                        a.Free();
                        break;
                }
            }
            catch (Exception ex)
            {
                excs ??= new();
                excs.Add(ex);
            }
        }

        if (excs is not null)
            throw excs.Count == 1 ? excs[0] : new AggregateException(excs);
    }
}
