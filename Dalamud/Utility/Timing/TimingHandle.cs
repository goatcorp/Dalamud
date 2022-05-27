using System;
using System.Diagnostics;
using System.Linq;

namespace Dalamud.Utility.Timing;

/// <summary>
/// Class used for tracking a time interval taken.
/// </summary>
[DebuggerDisplay("{Name} - {Duration}")]
public sealed class TimingHandle : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimingHandle"/> class.
    /// </summary>
    /// <param name="name">The name of this timing.</param>
    internal TimingHandle(string name)
    {
        this.Name = name;

        this.Parent = Timings.Current.Value;
        Timings.Current.Value = this;

        lock (Timings.AllTimings)
        {
            if (this.Parent != null)
            {
                this.ChildCount++;
            }

            this.StartTime = Timings.Stopwatch.Elapsed.TotalMilliseconds;
            this.EndTime = this.StartTime;
            this.IsMainThread = ThreadSafety.IsMainThread;

            if (Timings.ActiveTimings.Count > 0)
            {
                this.Depth = Timings.ActiveTimings.Max(x => x.Depth) + 1;
            }

            Timings.ActiveTimings.Add(this);
        }
    }

    /// <summary>
    /// Gets the time this timing started.
    /// </summary>
    public double StartTime { get; private set; }

    /// <summary>
    /// Gets the time this timing ended.
    /// </summary>
    public double EndTime { get; private set; }

    /// <summary>
    /// Gets the duration of this timing.
    /// </summary>
    public double Duration => Math.Floor(this.EndTime - this.StartTime);

    /// <summary>
    /// Gets the parent timing.
    /// </summary>
    public TimingHandle? Parent { get; private set; }

    /// <summary>
    /// Gets a value indicating whether or not this timing has already returned to its parent.
    /// </summary>
    public bool Returned { get; private set; }

    /// <summary>
    /// Gets a value indicating whether or not this timing was started on the main thread.
    /// </summary>
    public bool IsMainThread { get; private set; }

    /// <summary>
    /// Gets the number of child timings.
    /// </summary>
    public uint ChildCount { get; private set; }

    /// <summary>
    /// Gets the depth of this timing.
    /// </summary>
    public uint Depth { get; private set; }

    /// <summary>
    /// Gets the name of the timing.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the member that created this timing.
    /// </summary>
    public string? MemberName { get; init; }

    /// <summary>
    /// Gets the file name that created this timing.
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// Gets the line number that created this timing.
    /// </summary>
    public int LineNumber { get; init; }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.EndTime = Timings.Stopwatch.Elapsed.TotalMilliseconds;
        Timings.Current.Value = this.Parent;

        lock (Timings.AllTimings)
        {
            if (this.Duration > 1 || this.ChildCount > 0)
            {
                Timings.AllTimings.Add(this);
                this.Returned = this.Parent != null && Timings.ActiveTimings.Contains(this.Parent);
            }

            Timings.ActiveTimings.Remove(this);
        }
    }
}
