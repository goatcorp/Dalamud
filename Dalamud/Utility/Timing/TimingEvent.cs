using System.Threading;

namespace Dalamud.Utility.Timing;

/// <summary>
/// Class representing a timing event.
/// </summary>
public class TimingEvent
{
    /// <summary>
    /// Id of this timing event.
    /// </summary>
#pragma warning disable SA1401
    public readonly long Id = Interlocked.Increment(ref idCounter);
#pragma warning restore SA1401

    private static long idCounter = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimingEvent"/> class.
    /// </summary>
    /// <param name="name">Name of the event.</param>
    internal TimingEvent(string name)
    {
        this.Name = name;
        this.StartTime = Timings.Stopwatch.Elapsed.TotalMilliseconds;
    }

    /// <summary>
    /// Gets the time this timing started.
    /// </summary>
    public double StartTime { get; private set; }

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
}
