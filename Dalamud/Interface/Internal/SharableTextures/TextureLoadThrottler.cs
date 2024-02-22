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
    private readonly List<WorkItem> workList = new();
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

        _ = Task.Run(
            () =>
            {
                lock (this.workList)
                {
                    this.workList.Add(work);
                    if (this.activeWorkList.Count >= this.MaxActiveWorkItems)
                        return;
                }

                this.ContinueWork();
            },
            default);

        return work.TaskCompletionSource.Task;
    }

    private void ContinueWork()
    {
        WorkItem minWork;
        lock (this.workList)
        {
            if (this.workList.Count == 0)
                return;

            if (this.activeWorkList.Count >= this.MaxActiveWorkItems)
                return;

            var minIndex = 0;
            for (var i = 1; i < this.workList.Count; i++)
            {
                if (this.workList[i].CompareTo(this.workList[minIndex]) < 0)
                    minIndex = i;
            }

            minWork = this.workList[minIndex];
            // Avoid shifting; relocate the element to remove to the last
            if (minIndex != this.workList.Count - 1)
                (this.workList[^1], this.workList[minIndex]) = (this.workList[minIndex], this.workList[^1]);
            this.workList.RemoveAt(this.workList.Count - 1);

            this.activeWorkList.Add(minWork);
        }

        try
        {
            minWork.CancellationToken.ThrowIfCancellationRequested();
            minWork.InnerTask = minWork.ImmediateLoadFunction(minWork.CancellationToken);
        }
        catch (Exception e)
        {
            minWork.InnerTask = Task.FromException<IDalamudTextureWrap>(e);
        }

        minWork.InnerTask.ContinueWith(
            r =>
            {
                // Swallow exception, if any
                _ = r.Exception;

                lock (this.workList)
                    this.activeWorkList.Remove(minWork);
                if (r.IsCompletedSuccessfully)
                    minWork.TaskCompletionSource.SetResult(r.Result);
                else if (r.Exception is not null)
                    minWork.TaskCompletionSource.SetException(r.Exception);
                else if (r.IsCanceled)
                    minWork.TaskCompletionSource.SetCanceled();
                else
                    minWork.TaskCompletionSource.SetException(new Exception("??"));
                this.ContinueWork();
            });
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

        public Task<IDalamudTextureWrap>? InnerTask { get; set; }

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
