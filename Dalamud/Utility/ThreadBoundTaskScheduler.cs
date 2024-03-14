using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dalamud.Utility;

/// <summary>
/// A task scheduler that runs tasks on a specific thread.
/// </summary>
internal class ThreadBoundTaskScheduler : TaskScheduler
{
    private const byte Scheduled = 0;
    private const byte Running = 1;

    private readonly ConcurrentDictionary<Task, byte> scheduledTasks = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreadBoundTaskScheduler"/> class.
    /// </summary>
    /// <param name="boundThread">The thread to bind this task scheduelr to.</param>
    public ThreadBoundTaskScheduler(Thread? boundThread = null)
    {
        this.BoundThread = boundThread;
    }

    /// <summary>
    /// Gets or sets the thread this task scheduler is bound to.
    /// </summary>
    public Thread? BoundThread { get; set; }

    /// <summary>
    /// Gets a value indicating whether we're on the bound thread.
    /// </summary>
    public bool IsOnBoundThread => Thread.CurrentThread == this.BoundThread;

    /// <summary>
    /// Runs queued tasks.
    /// </summary>
    public void Run()
    {
        foreach (var task in this.scheduledTasks.Keys)
        {
            if (!this.scheduledTasks.TryUpdate(task, Running, Scheduled))
                continue;

            _ = this.TryExecuteTask(task);
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<Task> GetScheduledTasks()
    {
        return this.scheduledTasks.Keys;
    }

    /// <inheritdoc/>
    protected override void QueueTask(Task task)
    {
        this.scheduledTasks[task] = Scheduled;
    }

    /// <inheritdoc/>
    protected override bool TryDequeue(Task task)
    {
        if (!this.scheduledTasks.TryRemove(task, out _))
            return false;
        return true;
    }

    /// <inheritdoc/>
    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        if (!this.IsOnBoundThread)
            return false;

        if (taskWasPreviouslyQueued && !this.scheduledTasks.TryUpdate(task, Running, Scheduled))
            return false;

        _ = this.TryExecuteTask(task);
        return true;
    }

    private new bool TryExecuteTask(Task task)
    {
        var r = base.TryExecuteTask(task);
        this.scheduledTasks.Remove(task, out _);
        return r;
    }
}
