using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Dalamud.Interface.Internal.SharableTextures;

/// <summary>
/// Service for managing texture loads.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class TextureLoadThrottler : IServiceType
{
    private readonly object workListLock = new();
    private readonly List<WorkItem> pendingWorkList = new();
    private readonly List<WorkItem> activeWorkList = new();

    [ServiceManager.ServiceConstructor]
    private TextureLoadThrottler() =>
        this.MaxActiveWorkItems = Math.Min(64, Environment.ProcessorCount);

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

    private int MaxActiveWorkItems { get; }

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

        _ = Task.Run(() => this.ContinueWork(work), default);

        return work.TaskCompletionSource.Task;
    }

    private async Task ContinueWork(WorkItem? newItem)
    {
        while (true)
        {
            WorkItem? minWork = null;
            lock (this.workListLock)
            {
                if (newItem is not null)
                {
                    this.pendingWorkList.Add(newItem);
                    newItem = null;
                }

                if (this.activeWorkList.Count >= this.MaxActiveWorkItems)
                    return;

                var minIndex = -1;
                for (var i = 0; i < this.pendingWorkList.Count; i++)
                {
                    var work = this.pendingWorkList[i];
                    if (work.CancellationToken.IsCancellationRequested)
                    {
                        work.TaskCompletionSource.SetCanceled(work.CancellationToken);
                        _ = work.TaskCompletionSource.Task.Exception;
                        this.RelocatePendingWorkItemToEndAndEraseUnsafe(i--);
                        continue;
                    }

                    if (minIndex == -1 || work.CompareTo(this.pendingWorkList[minIndex]) < 0)
                    {
                        minIndex = i;
                        minWork = work;
                    }
                }

                if (minWork is null)
                    return;

                this.RelocatePendingWorkItemToEndAndEraseUnsafe(minIndex);

                this.activeWorkList.Add(minWork);
            }

            try
            {
                var r = await minWork.ImmediateLoadFunction(minWork.CancellationToken);
                minWork.TaskCompletionSource.SetResult(r);
            }
            catch (Exception e)
            {
                minWork.TaskCompletionSource.SetException(e);
                _ = minWork.TaskCompletionSource.Task.Exception;
            }

            lock (this.workListLock)
                this.activeWorkList.Remove(minWork);
        }
    }

    /// <summary>
    /// Remove an item in <see cref="pendingWorkList"/>, avoiding shifting.
    /// </summary>
    /// <param name="index">Index of the item to remove.</param>
    private void RelocatePendingWorkItemToEndAndEraseUnsafe(int index)
    {
        // Relocate the element to remove to the last.
        if (index != this.pendingWorkList.Count - 1)
        {
            (this.pendingWorkList[^1], this.pendingWorkList[index]) =
                (this.pendingWorkList[index], this.pendingWorkList[^1]);
        }

        this.pendingWorkList.RemoveAt(this.pendingWorkList.Count - 1);
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
