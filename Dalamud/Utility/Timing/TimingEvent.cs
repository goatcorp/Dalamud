using System.Threading;

namespace Dalamud.Utility.Timing;

public class TimingEvent
{
    private static long IdCounter = 0;

    /// <summary>
    /// Id of this timing event.
    /// </summary>
    public readonly long Id = Interlocked.Increment(ref IdCounter);

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
