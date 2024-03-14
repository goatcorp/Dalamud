using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dalamud.Utility;

/// <summary>
/// Utilities for disposing stuff.
/// </summary>
public static class DisposeSafety
{
    /// <summary>
    /// Interface that marks a disposable that it can call back on dispose.
    /// </summary>
    public interface IDisposeCallback : IDisposable
    {
        /// <summary>
        /// Event to be fired before object dispose. First parameter is the object iself.
        /// </summary>
        event Action<IDisposeCallback>? BeforeDispose;

        /// <summary>
        /// Event to be fired after object dispose. First parameter is the object iself.
        /// </summary>
        event Action<IDisposeCallback, Exception?>? AfterDispose;
    }

    /// <summary>
    /// Returns a proxy <see cref="IDisposable"/> that on dispose will dispose the result of the given
    /// <see cref="Task{T}"/>.<br />
    /// If any exception has occurred, it will be ignored.
    /// </summary>
    /// <param name="task">The task.</param>
    /// <typeparam name="T">A disposable type.</typeparam>
    /// <returns>The proxy <see cref="IDisposable"/>.</returns>
    public static IDisposable ToDisposableIgnoreExceptions<T>(this Task<T> task)
        where T : IDisposable
    {
        return Disposable.Create(
            () => task.ContinueWith(
                r =>
                {
                    _ = r.Exception;
                    if (r.IsCompleted)
                    {
                        try
                        {
                            r.Dispose();
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                }));
    }

    /// <summary>
    /// Transforms <paramref name="task"/> into a <see cref="Task"/>, disposing the content as necessary.
    /// </summary>
    /// <param name="task">The task.</param>
    /// <param name="ignoreAllExceptions">Ignore all exceptions.</param>
    /// <typeparam name="T">A disposable type.</typeparam>
    /// <returns>A wrapper for the task.</returns>
    public static Task ToContentDisposedTask<T>(this Task<T> task, bool ignoreAllExceptions = false)
        where T : IDisposable => task.ContinueWith(
        r =>
        {
            if (!r.IsCompletedSuccessfully)
            {
                if (ignoreAllExceptions)
                {
                    _ = r.Exception;
                    return Task.CompletedTask;
                }

                return r;
            }

            try
            {
                r.Result.Dispose();
            }
            catch (Exception e)
            {
                if (!ignoreAllExceptions)
                {
                    return Task.FromException(
                        new AggregateException(
                            new[] { e }.Concat(
                                (IEnumerable<Exception>)r.Exception?.InnerExceptions
                                ?? new[] { new OperationCanceledException() })));
                }
            }

            return Task.CompletedTask;
        }).Unwrap();

    /// <summary>
    /// Returns a proxy <see cref="IDisposable"/> that on dispose will dispose all the elements of the given
    /// <see cref="IEnumerable{T}"/> of <typeparamref name="T"/>s.
    /// </summary>
    /// <param name="disposables">The disposables.</param>
    /// <typeparam name="T">The disposable types.</typeparam>
    /// <returns>The proxy <see cref="IDisposable"/>.</returns>
    /// <exception cref="AggregateException">Error.</exception>
    public static IDisposable AggregateToDisposable<T>(this IEnumerable<T>? disposables)
        where T : IDisposable
    {
        if (disposables is not T[] array)
            array = disposables?.ToArray() ?? Array.Empty<T>();

        return Disposable.Create(
            () =>
            {
                List<Exception?> exceptions = null;
                foreach (var d in array)
                {
                    try
                    {
                        d?.Dispose();
                    }
                    catch (Exception de)
                    {
                        exceptions ??= new();
                        exceptions.Add(de);
                    }
                }

                if (exceptions is not null)
                    throw new AggregateException(exceptions);
            });
    }

    /// <summary>
    /// Utility class for managing finalizing stuff.
    /// </summary>
    public class ScopedFinalizer : IDisposeCallback, IAsyncDisposable
    {
        private readonly List<object> objects = new();

        /// <inheritdoc/>
        public event Action<IDisposeCallback>? BeforeDispose;

        /// <inheritdoc/>
        public event Action<IDisposeCallback, Exception?>? AfterDispose;

        /// <inheritdoc cref="Stack{T}.EnsureCapacity"/>
        public void EnsureCapacity(int capacity)
        {
            lock (this.objects)
                this.objects.EnsureCapacity(capacity);
        }

        /// <inheritdoc cref="Stack{T}.Push"/>
        /// <returns>The parameter.</returns>
        [return: NotNullIfNotNull(nameof(d))]
        public T? Add<T>(T? d) where T : IDisposable
        {
            if (d is not null)
            {
                lock (this.objects)
                    this.objects.Add(this.CheckAdd(d));
            }

            return d;
        }

        /// <inheritdoc cref="Stack{T}.Push"/>
        [return: NotNullIfNotNull(nameof(d))]
        public Action? Add(Action? d)
        {
            if (d is not null)
            {
                lock (this.objects)
                    this.objects.Add(this.CheckAdd(d));
            }

            return d;
        }

        /// <inheritdoc cref="Stack{T}.Push"/>
        [return: NotNullIfNotNull(nameof(d))]
        public Func<Task>? Add(Func<Task>? d)
        {
            if (d is not null)
            {
                lock (this.objects)
                    this.objects.Add(this.CheckAdd(d));
            }

            return d;
        }

        /// <inheritdoc cref="Stack{T}.Push"/>
        public GCHandle Add(GCHandle d)
        {
            if (d != default)
            {
                lock (this.objects)
                    this.objects.Add(this.CheckAdd(d));
            }

            return d;
        }

        /// <summary>
        /// Queue all the given <see cref="IDisposable"/> to be disposed later.
        /// </summary>
        /// <param name="ds">Disposables.</param>
        public void AddRange(IEnumerable<IDisposable?> ds)
        {
            lock (this.objects)
                this.objects.AddRange(ds.Where(d => d is not null).Select(d => (object)this.CheckAdd(d)));
        }

        /// <summary>
        /// Queue all the given <see cref="IDisposable"/> to be run later.
        /// </summary>
        /// <param name="ds">Actions.</param>
        public void AddRange(IEnumerable<Action?> ds)
        {
            lock (this.objects)
                this.objects.AddRange(ds.Where(d => d is not null).Select(d => (object)this.CheckAdd(d)));
        }

        /// <summary>
        /// Queue all the given <see cref="Func{T}"/> returning <see cref="Task"/> to be run later.
        /// </summary>
        /// <param name="ds">Func{Task}s.</param>
        public void AddRange(IEnumerable<Func<Task>?> ds)
        {
            lock (this.objects)
                this.objects.AddRange(ds.Where(d => d is not null).Select(d => (object)this.CheckAdd(d)));
        }

        /// <summary>
        /// Queue all the given <see cref="GCHandle"/> to be disposed later.
        /// </summary>
        /// <param name="ds">GCHandles.</param>
        public void AddRange(IEnumerable<GCHandle> ds)
        {
            lock (this.objects)
                this.objects.AddRange(ds.Select(d => (object)this.CheckAdd(d)));
        }

        /// <summary>
        /// Cancel all pending disposals.
        /// </summary>
        /// <remarks>Use this after successful initialization of multiple disposables.</remarks>
        public void Cancel()
        {
            lock (this.objects)
            {
                foreach (var o in this.objects)
                    this.CheckRemove(o);
                this.objects.Clear();
            }
        }

        /// <inheritdoc cref="Stack{T}.EnsureCapacity"/>
        /// <returns>This for method chaining.</returns>
        public ScopedFinalizer WithEnsureCapacity(int capacity)
        {
            this.EnsureCapacity(capacity);
            return this;
        }

        /// <inheritdoc cref="Add{T}"/>
        /// <returns>This for method chaining.</returns>
        public ScopedFinalizer With(IDisposable d)
        {
            this.Add(d);
            return this;
        }

        /// <inheritdoc cref="Add(Action)"/>
        /// <returns>This for method chaining.</returns>
        public ScopedFinalizer With(Action d)
        {
            this.Add(d);
            return this;
        }

        /// <inheritdoc cref="Add(Func{Task})"/>
        /// <returns>This for method chaining.</returns>
        public ScopedFinalizer With(Func<Task> d)
        {
            this.Add(d);
            return this;
        }

        /// <inheritdoc cref="Add(GCHandle)"/>
        /// <returns>This for method chaining.</returns>
        public ScopedFinalizer With(GCHandle d)
        {
            this.Add(d);
            return this;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.BeforeDispose?.InvokeSafely(this);

            List<Exception>? exceptions = null;
            while (true)
            {
                object obj;
                lock (this.objects)
                {
                    if (this.objects.Count == 0)
                        break;
                    obj = this.objects[^1];
                    this.objects.RemoveAt(this.objects.Count - 1);
                }

                try
                {
                    switch (obj)
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
                    exceptions ??= new();
                    exceptions.Add(ex);
                }
            }

            lock (this.objects)
                this.objects.TrimExcess();

            if (exceptions is not null)
            {
                var exs = exceptions.Count == 1 ? exceptions[0] : new AggregateException(exceptions);
                try
                {
                    this.AfterDispose?.Invoke(this, exs);
                }
                catch
                {
                    // whatever
                }

                throw exs;
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            this.BeforeDispose?.InvokeSafely(this);

            List<Exception>? exceptions = null;
            while (true)
            {
                object obj;
                lock (this.objects)
                {
                    if (this.objects.Count == 0)
                        break;
                    obj = this.objects[^1];
                    this.objects.RemoveAt(this.objects.Count - 1);
                }

                try
                {
                    switch (obj)
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
                    exceptions ??= new();
                    exceptions.Add(ex);
                }
            }

            lock (this.objects)
                this.objects.TrimExcess();

            if (exceptions is not null)
            {
                var exs = exceptions.Count == 1 ? exceptions[0] : new AggregateException(exceptions);
                try
                {
                    this.AfterDispose?.Invoke(this, exs);
                }
                catch
                {
                    // whatever
                }

                throw exs;
            }
        }

        private T CheckAdd<T>(T item)
        {
            if (item is IDisposeCallback dc)
                dc.BeforeDispose += this.OnItemDisposed;

            return item;
        }

        private void CheckRemove(object item)
        {
            if (item is IDisposeCallback dc)
                dc.BeforeDispose -= this.OnItemDisposed;
        }

        private void OnItemDisposed(IDisposeCallback obj)
        {
            obj.BeforeDispose -= this.OnItemDisposed;
            lock (this.objects)
                this.objects.Remove(obj);
        }
    }
}
