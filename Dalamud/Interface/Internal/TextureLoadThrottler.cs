using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Dalamud.Interface.Internal;

/// <summary>
/// Service for managing texture loads.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class TextureLoadThrottler : IServiceType, IDisposable
{
    private readonly CancellationTokenSource disposeCancellationTokenSource = new();
    private readonly Task adderTask;
    private readonly Task[] workerTasks;

    private readonly object workListLock = new();
    private readonly Channel<WorkItem> newItemChannel = Channel.CreateUnbounded<WorkItem>();
    private readonly Channel<object?> workTokenChannel = Channel.CreateUnbounded<object?>();
    private readonly List<WorkItem> workItemPending = new();

    private bool disposing;

    [ServiceManager.ServiceConstructor]
    private TextureLoadThrottler()
    {
        this.adderTask = Task.Run(this.LoopAddWorkItemAsync);
        this.workerTasks = new Task[Math.Min(64, Environment.ProcessorCount)];
        foreach (ref var task in this.workerTasks.AsSpan())
            task = Task.Run(this.LoopProcessWorkItemAsync);
    }

    /// <summary>
    /// Basis for throttling.
    /// </summary>
    internal interface IThrottleBasisProvider
    {
        /// <summary>
        /// Gets a value indicating whether the resource is requested in an opportunistic way.
        /// </summary>
        bool IsOpportunistic { get; }

        /// <summary>
        /// Gets the first requested tick count from <see cref="Environment.TickCount64"/>.
        /// </summary>
        long FirstRequestedTick { get; }

        /// <summary>
        /// Gets the latest requested tick count from <see cref="Environment.TickCount64"/>.
        /// </summary>
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

    /// <summary>
    /// Creates a texture loader.
    /// </summary>
    /// <param name="basis">The throttle basis.</param>
    /// <param name="immediateLoadFunction">The immediate load function.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    public Task<IDalamudTextureWrap> CreateLoader(
        IThrottleBasisProvider basis,
        Func<CancellationToken, Task<IDalamudTextureWrap>> immediateLoadFunction,
        CancellationToken cancellationToken)
    {
        var work = new WorkItem
        {
            TaskCompletionSource = new(),
            Basis = basis,
            CancellationToken = cancellationToken,
            ImmediateLoadFunction = immediateLoadFunction,
        };

        return
            this.newItemChannel.Writer.TryWrite(work)
                ? work.TaskCompletionSource.Task
                : Task.FromException<IDalamudTextureWrap>(new ObjectDisposedException(nameof(TextureLoadThrottler)));
    }

    private async Task LoopAddWorkItemAsync()
    {
        var newWorkTemp = new List<WorkItem>();
        var reader = this.newItemChannel.Reader;
        while (!reader.Completion.IsCompleted)
        {
            await reader.WaitToReadAsync();

            newWorkTemp.EnsureCapacity(reader.Count);
            while (newWorkTemp.Count < newWorkTemp.Capacity && reader.TryRead(out var newWork))
                newWorkTemp.Add(newWork);
            lock (this.workListLock)
                this.workItemPending.AddRange(newWorkTemp);
            for (var i = newWorkTemp.Count; i > 0; i--)
                this.workTokenChannel.Writer.TryWrite(null);
            newWorkTemp.Clear();
        }
    }

    private async Task LoopProcessWorkItemAsync()
    {
        var reader = this.workTokenChannel.Reader;
        while (!reader.Completion.IsCompleted)
        {
            _ = await reader.ReadAsync();

            if (this.ExtractHighestPriorityWorkItem() is not { } work)
                continue;

            try
            {
                IDalamudTextureWrap wrap;
                if (work.CancellationToken.CanBeCanceled)
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                        this.disposeCancellationTokenSource.Token,
                        work.CancellationToken);
                    wrap = await work.ImmediateLoadFunction(cts.Token);
                }
                else
                {
                    wrap = await work.ImmediateLoadFunction(this.disposeCancellationTokenSource.Token);
                }

                work.TaskCompletionSource.SetResult(wrap);
            }
            catch (Exception e)
            {
                work.TaskCompletionSource.SetException(e);
                _ = work.TaskCompletionSource.Task.Exception;
            }
        }
    }

    private WorkItem? ExtractHighestPriorityWorkItem()
    {
        lock (this.workListLock)
        {
            WorkItem? highestPriorityWork = null;
            var highestPriorityIndex = -1;
            for (var i = 0; i < this.workItemPending.Count; i++)
            {
                var work = this.workItemPending[i];
                if (work.CancellationToken.IsCancellationRequested)
                {
                    work.TaskCompletionSource.SetCanceled(work.CancellationToken);
                    _ = work.TaskCompletionSource.Task.Exception;
                    this.RelocatePendingWorkItemToEndAndEraseUnsafe(i--);
                    continue;
                }

                if (highestPriorityIndex == -1 ||
                    work.CompareTo(this.workItemPending[highestPriorityIndex]) < 0)
                {
                    highestPriorityIndex = i;
                    highestPriorityWork = work;
                }
            }

            if (highestPriorityWork is null)
                return null;

            this.RelocatePendingWorkItemToEndAndEraseUnsafe(highestPriorityIndex);
            return highestPriorityWork;
        }
    }

    /// <summary>
    /// Remove an item in <see cref="workItemPending"/>, avoiding shifting.
    /// </summary>
    /// <param name="index">Index of the item to remove.</param>
    private void RelocatePendingWorkItemToEndAndEraseUnsafe(int index)
    {
        // Relocate the element to remove to the last.
        if (index != this.workItemPending.Count - 1)
        {
            (this.workItemPending[^1], this.workItemPending[index]) =
                (this.workItemPending[index], this.workItemPending[^1]);
        }

        this.workItemPending.RemoveAt(this.workItemPending.Count - 1);
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

    [SuppressMessage(
        "StyleCop.CSharp.OrderingRules",
        "SA1206:Declaration keywords should follow order",
        Justification = "no")]
    private record WorkItem : IComparable<WorkItem>
    {
        public required TaskCompletionSource<IDalamudTextureWrap> TaskCompletionSource { get; init; }

        public required IThrottleBasisProvider Basis { get; init; }

        public required CancellationToken CancellationToken { get; init; }

        public required Func<CancellationToken, Task<IDalamudTextureWrap>> ImmediateLoadFunction { get; init; }

        public int CompareTo(WorkItem other)
        {
            if (this.Basis.IsOpportunistic != other.Basis.IsOpportunistic)
                return this.Basis.IsOpportunistic ? 1 : -1;
            if (this.Basis.IsOpportunistic)
                return -this.Basis.LatestRequestedTick.CompareTo(other.Basis.LatestRequestedTick);
            return this.Basis.FirstRequestedTick.CompareTo(other.Basis.FirstRequestedTick);
        }
    }
}
