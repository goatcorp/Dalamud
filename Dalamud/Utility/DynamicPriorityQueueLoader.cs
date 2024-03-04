using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Dalamud.Utility;

/// <summary>Base class for loading resources in dynamic order.</summary>
internal class DynamicPriorityQueueLoader : IDisposable
{
    private readonly CancellationTokenSource disposeCancellationTokenSource = new();
    private readonly Task adderTask;
    private readonly Task[] workerTasks;

    private readonly Channel<WorkItem> newItemChannel;
    private readonly Channel<object?> workTokenChannel;
    private readonly List<WorkItem> workItemPending = new();

    private bool disposing;

    /// <summary>Initializes a new instance of the <see cref="DynamicPriorityQueueLoader"/> class.</summary>
    /// <param name="concurrency">Maximum number of concurrent load tasks.</param>
    public DynamicPriorityQueueLoader(int concurrency)
    {
        this.newItemChannel = Channel.CreateUnbounded<WorkItem>(new() { SingleReader = true });
        this.workTokenChannel = Channel.CreateUnbounded<object?>(new() { SingleWriter = true });

        this.adderTask = Task.Run(this.LoopAddWorkItemAsync);
        this.workerTasks = new Task[concurrency];
        foreach (ref var task in this.workerTasks.AsSpan())
            task = Task.Run(this.LoopProcessWorkItemAsync);
    }

    /// <summary>Provider for priority metrics.</summary>
    internal interface IThrottleBasisProvider
    {
        /// <summary>Gets a value indicating whether the resource is requested in an opportunistic way.</summary>
        bool IsOpportunistic { get; }

        /// <summary>Gets the first requested tick count from <see cref="Environment.TickCount64"/>.</summary>
        long FirstRequestedTick { get; }

        /// <summary>Gets the latest requested tick count from <see cref="Environment.TickCount64"/>.</summary>
        long LatestRequestedTick { get; }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.disposing)
            return;

        this.disposing = true;
        this.newItemChannel.Writer.Complete();
        this.workTokenChannel.Writer.Complete();
        this.disposeCancellationTokenSource.Cancel();

        this.adderTask.Wait();
        Task.WaitAll(this.workerTasks);

        _ = this.adderTask.Exception;
        foreach (var t in this.workerTasks)
            _ = t.Exception;
    }

    /// <summary>Loads a resource according to some order.</summary>
    /// <typeparam name="T">The type of resource.</typeparam>
    /// <param name="basis">The throttle basis. <c>null</c> may be used to create a new instance of
    /// <see cref="IThrottleBasisProvider"/> that is not opportunistic with time values of now.</param>
    /// <param name="immediateLoadFunction">The immediate load function.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="disposables">Disposables to dispose when the task completes.</param>
    /// <returns>The task.</returns>
    /// <remarks>
    /// <paramref name="immediateLoadFunction"/> may throw immediately without returning anything, or the returned
    /// <see cref="Task{TResult}"/> may complete in failure.
    /// </remarks>
    public Task<T> LoadAsync<T>(
        IThrottleBasisProvider? basis,
        Func<CancellationToken, Task<T>> immediateLoadFunction,
        CancellationToken cancellationToken,
        params IDisposable?[] disposables)
    {
        basis ??= new ReadOnlyThrottleBasisProvider();
        var work = new WorkItem<T>(basis, immediateLoadFunction, cancellationToken, disposables);

        if (this.newItemChannel.Writer.TryWrite(work))
            return work.Task;

        work.Dispose();
        return Task.FromException<T>(new ObjectDisposedException(this.GetType().Name));
    }

    /// <summary>Continuously transfers work items added from <see cref="LoadAsync{T}"/> to
    /// <see cref="workItemPending"/>, until all items are transferred and <see cref="Dispose"/> is called.</summary>
    private async Task LoopAddWorkItemAsync()
    {
        const int batchAddSize = 64;
        var newWorks = new List<WorkItem>(batchAddSize);
        var reader = this.newItemChannel.Reader;
        while (await reader.WaitToReadAsync())
        {
            while (newWorks.Count < batchAddSize && reader.TryRead(out var newWork))
                newWorks.Add(newWork);

            lock (this.workItemPending)
                this.workItemPending.AddRange(newWorks);

            for (var i = newWorks.Count; i > 0; i--)
                this.workTokenChannel.Writer.TryWrite(null);

            newWorks.Clear();
        }
    }

    /// <summary>Continuously processes work items in <see cref="workItemPending"/>, until all items are processed and
    /// <see cref="Dispose"/> is called.</summary>
    private async Task LoopProcessWorkItemAsync()
    {
        var reader = this.workTokenChannel.Reader;
        while (await reader.WaitToReadAsync())
        {
            if (!reader.TryRead(out _))
                continue;

            if (this.ExtractHighestPriorityWorkItem() is not { } work)
                continue;

            await work.Process(this.disposeCancellationTokenSource.Token);
            work.Dispose();
        }
    }

    /// <summary>Extracts the work item with the highest priority from <see cref="workItemPending"/>,
    /// and removes cancelled items, if any.</summary>
    /// <remarks>The order of items of <see cref="workItemPending"/> is undefined after this function.</remarks>
    private WorkItem? ExtractHighestPriorityWorkItem()
    {
        lock (this.workItemPending)
        {
            for (var startIndex = 0; startIndex < this.workItemPending.Count - 1;)
            {
                var span = CollectionsMarshal.AsSpan(this.workItemPending)[startIndex..];
                ref var lastRef = ref span[^1];
                foreach (ref var itemRef in span[..^1])
                {
                    if (itemRef.CancelAsRequested())
                    {
                        itemRef.Dispose();
                        itemRef = lastRef;
                        this.workItemPending.RemoveAt(this.workItemPending.Count - 1);
                        break;
                    }

                    if (itemRef.CompareTo(lastRef) < 0)
                        (itemRef, lastRef) = (lastRef, itemRef);
                    startIndex++;
                }
            }

            if (this.workItemPending.Count == 0)
                return null;

            var last = this.workItemPending[^1];
            this.workItemPending.RemoveAt(this.workItemPending.Count - 1);
            if (last.CancelAsRequested())
            {
                last.Dispose();
                return null;
            }

            return last;
        }
    }

    /// <summary>A read-only implementation of <see cref="IThrottleBasisProvider"/>.</summary>
    private class ReadOnlyThrottleBasisProvider : IThrottleBasisProvider
    {
        /// <inheritdoc/>
        public bool IsOpportunistic { get; init; } = false;

        /// <inheritdoc/>
        public long FirstRequestedTick { get; init; } = Environment.TickCount64;

        /// <inheritdoc/>
        public long LatestRequestedTick { get; init; } = Environment.TickCount64;
    }

    /// <summary>Represents a work item added from <see cref="LoadAsync{T}"/>.</summary>
    private abstract class WorkItem : IComparable<WorkItem>, IDisposable
    {
        private readonly IThrottleBasisProvider basis;
        private readonly IDisposable?[] disposables;

        protected WorkItem(
            IThrottleBasisProvider basis,
            CancellationToken cancellationToken,
            params IDisposable?[] disposables)
        {
            this.basis = basis;
            this.CancellationToken = cancellationToken;
            this.disposables = disposables;
        }

        protected CancellationToken CancellationToken { get; }

        public void Dispose()
        {
            foreach (ref var d in this.disposables.AsSpan())
                Interlocked.Exchange(ref d, null)?.Dispose();
        }

        public int CompareTo(WorkItem other)
        {
            if (this.basis.IsOpportunistic != other.basis.IsOpportunistic)
                return this.basis.IsOpportunistic ? 1 : -1;
            if (this.basis.IsOpportunistic)
                return -this.basis.LatestRequestedTick.CompareTo(other.basis.LatestRequestedTick);
            return this.basis.FirstRequestedTick.CompareTo(other.basis.FirstRequestedTick);
        }

        public abstract bool CancelAsRequested();

        public abstract ValueTask Process(CancellationToken serviceDisposeToken);
    }

    /// <summary>Typed version of <see cref="WorkItem"/>.</summary>
    private sealed class WorkItem<T> : WorkItem
    {
        private readonly TaskCompletionSource<T> taskCompletionSource;
        private readonly Func<CancellationToken, Task<T>> immediateLoadFunction;

        public WorkItem(
            IThrottleBasisProvider basis,
            Func<CancellationToken, Task<T>> immediateLoadFunction,
            CancellationToken cancellationToken,
            params IDisposable?[] disposables)
            : base(basis, cancellationToken, disposables)
        {
            this.taskCompletionSource = new();
            this.immediateLoadFunction = immediateLoadFunction;
        }

        public Task<T> Task => this.taskCompletionSource.Task;

        public override bool CancelAsRequested()
        {
            if (!this.CancellationToken.IsCancellationRequested)
                return false;

            // Cancel the load task and move on.
            this.taskCompletionSource.TrySetCanceled(this.CancellationToken);

            // Suppress the OperationCanceledException caused from the above.
            _ = this.taskCompletionSource.Task.Exception;

            return true;
        }

        public override async ValueTask Process(CancellationToken serviceDisposeToken)
        {
            try
            {
                T wrap;
                if (this.CancellationToken.CanBeCanceled)
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                        serviceDisposeToken,
                        this.CancellationToken);
                    wrap = await this.immediateLoadFunction(cts.Token);
                }
                else
                {
                    wrap = await this.immediateLoadFunction(serviceDisposeToken);
                }

                if (!this.taskCompletionSource.TrySetResult(wrap))
                    (wrap as IDisposable)?.Dispose();
            }
            catch (Exception e)
            {
                this.taskCompletionSource.TrySetException(e);
                _ = this.taskCompletionSource.Task.Exception;
            }
        }
    }
}
