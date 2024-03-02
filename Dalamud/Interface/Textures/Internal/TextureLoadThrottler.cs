using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>
/// Service for managing texture loads.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class TextureLoadThrottler : IServiceType, IDisposable
{
    private readonly CancellationTokenSource disposeCancellationTokenSource = new();
    private readonly Task adderTask;
    private readonly Task[] workerTasks;

    private readonly Channel<WorkItem> newItemChannel;
    private readonly Channel<object?> workTokenChannel;
    private readonly List<WorkItem> workItemPending = new();

    private bool disposing;

    [ServiceManager.ServiceConstructor]
    private TextureLoadThrottler()
    {
        this.newItemChannel = Channel.CreateUnbounded<WorkItem>(new() { SingleReader = true });
        this.workTokenChannel = Channel.CreateUnbounded<object?>(new() { SingleWriter = true });

        this.adderTask = Task.Run(this.LoopAddWorkItemAsync);
        this.workerTasks = new Task[Math.Max(1, Environment.ProcessorCount - 1)];
        foreach (ref var task in this.workerTasks.AsSpan())
            task = Task.Run(this.LoopProcessWorkItemAsync);
    }

    /// <summary>Basis for throttling. Values may be changed anytime.</summary>
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

    /// <summary>Loads a texture according to some order.</summary>
    /// <param name="basis">The throttle basis.</param>
    /// <param name="immediateLoadFunction">The immediate load function.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    public Task<IDalamudTextureWrap> LoadTextureAsync(
        IThrottleBasisProvider basis,
        Func<CancellationToken, Task<IDalamudTextureWrap>> immediateLoadFunction,
        CancellationToken cancellationToken)
    {
        var work = new WorkItem(basis, immediateLoadFunction, cancellationToken);

        return
            this.newItemChannel.Writer.TryWrite(work)
                ? work.Task
                : Task.FromException<IDalamudTextureWrap>(new ObjectDisposedException(nameof(TextureLoadThrottler)));
    }

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
            return last.CancelAsRequested() ? null : last;
        }
    }

    /// <summary>
    /// A read-only implementation of <see cref="IThrottleBasisProvider"/>.
    /// </summary>
    public class ReadOnlyThrottleBasisProvider : IThrottleBasisProvider
    {
        /// <inheritdoc/>
        public bool IsOpportunistic { get; init; } = false;

        /// <inheritdoc/>
        public long FirstRequestedTick { get; init; } = Environment.TickCount64;

        /// <inheritdoc/>
        public long LatestRequestedTick { get; init; } = Environment.TickCount64;
    }

    private class WorkItem : IComparable<WorkItem>
    {
        private readonly TaskCompletionSource<IDalamudTextureWrap> taskCompletionSource;
        private readonly IThrottleBasisProvider basis;
        private readonly CancellationToken cancellationToken;
        private readonly Func<CancellationToken, Task<IDalamudTextureWrap>> immediateLoadFunction;

        public WorkItem(
            IThrottleBasisProvider basis,
            Func<CancellationToken, Task<IDalamudTextureWrap>> immediateLoadFunction,
            CancellationToken cancellationToken)
        {
            this.taskCompletionSource = new();
            this.basis = basis;
            this.cancellationToken = cancellationToken;
            this.immediateLoadFunction = immediateLoadFunction;
        }

        public Task<IDalamudTextureWrap> Task => this.taskCompletionSource.Task;

        public int CompareTo(WorkItem other)
        {
            if (this.basis.IsOpportunistic != other.basis.IsOpportunistic)
                return this.basis.IsOpportunistic ? 1 : -1;
            if (this.basis.IsOpportunistic)
                return -this.basis.LatestRequestedTick.CompareTo(other.basis.LatestRequestedTick);
            return this.basis.FirstRequestedTick.CompareTo(other.basis.FirstRequestedTick);
        }

        public bool CancelAsRequested()
        {
            if (!this.cancellationToken.IsCancellationRequested)
                return false;

            // Cancel the load task and move on.
            this.taskCompletionSource.TrySetCanceled(this.cancellationToken);

            // Suppress the OperationCanceledException caused from the above.
            _ = this.taskCompletionSource.Task.Exception;

            return true;
        }

        public async ValueTask Process(CancellationToken serviceDisposeToken)
        {
            try
            {
                IDalamudTextureWrap wrap;
                if (this.cancellationToken.CanBeCanceled)
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                        serviceDisposeToken,
                        this.cancellationToken);
                    wrap = await this.immediateLoadFunction(cts.Token);
                }
                else
                {
                    wrap = await this.immediateLoadFunction(serviceDisposeToken);
                }

                if (!this.taskCompletionSource.TrySetResult(wrap))
                    wrap.Dispose();
            }
            catch (Exception e)
            {
                this.taskCompletionSource.TrySetException(e);
                _ = this.taskCompletionSource.Task.Exception;
            }
        }
    }
}
