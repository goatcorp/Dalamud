using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Dalamud.Utility.Timing;

/// <summary>
/// Class used for tracking a time interval taken.
/// </summary>
[DebuggerDisplay("{Name} - {Duration}")]
public sealed class TimingHandle : TimingEvent, IDisposable, IComparable<TimingHandle>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimingHandle"/> class.
    /// </summary>
    /// <param name="name">The name of this timing.</param>
    internal TimingHandle(string name)
        : base(name)
    {
        this.Stack = Timings.TaskTimingHandles;

        lock (this.Stack)
            this.Parent = this.Stack.LastOrDefault();

        if (this.Parent != null)
        {
            this.Parent.ChildCount++;
            this.IdChain = new long[this.Parent.IdChain.Length + 1];
            Array.Copy(this.Parent.IdChain, this.IdChain, this.Parent.IdChain.Length);
        }
        else
        {
            this.IdChain = new long[1];
        }

        this.IdChain[^1] = this.Id;
        this.EndTime = this.StartTime;
        this.IsMainThread = ThreadSafety.IsMainThread;

        lock (this.Stack)
            this.Stack.Add(this);
    }

    /// <summary>
    /// Gets the id chain.
    /// </summary>
    public long[] IdChain { get; init; }

    /// <summary>
    /// Gets the time this timing ended.
    /// </summary>
    public double EndTime { get; private set; }

    /// <summary>
    /// Gets the duration of this timing.
    /// </summary>
    public double Duration => Math.Floor(this.EndTime - this.StartTime);

    /// <summary>
    /// Gets the attached timing handle stack.
    /// </summary>
    public List<TimingHandle> Stack { get; private set; }

    /// <summary>
    /// Gets the parent timing.
    /// </summary>
    public TimingHandle? Parent { get; private set; }

    /// <summary>
    /// Gets a value indicating whether or not this timing was started on the main thread.
    /// </summary>
    public bool IsMainThread { get; private set; }

    /// <summary>
    /// Gets the number of child timings.
    /// </summary>
    public uint ChildCount { get; private set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.EndTime = Timings.Stopwatch.Elapsed.TotalMilliseconds;

        lock (this.Stack)
            this.Stack.Remove(this);

        if (this.Duration > 1 || this.ChildCount > 0)
        {
            lock (Timings.AllTimings)
            {
                Timings.AllTimings.Add(this, this);
            }
        }
    }

    /// <inheritdoc/>
    public int CompareTo(TimingHandle? other)
    {
        if (other == null)
            return -1;

        var i = 0;
        for (; i < this.IdChain.Length && i < other.IdChain.Length; i++)
        {
            if (this.IdChain[i] < other.IdChain[i])
                return -1;
            if (this.IdChain[i] > other.IdChain[i])
                return 1;
        }

        if (this.IdChain.Length < other.IdChain.Length)
            return -1;
        if (this.IdChain.Length > other.IdChain.Length)
            return 1;
        return 0;
    }
}
