using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Dalamud.Utility.Timing;

/// <summary>
/// Class for measuring time taken in various subsystems.
/// </summary>
public static class Timings
{
    /// <summary>
    /// Stopwatch used for timing.
    /// </summary>
    internal static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

    /// <summary>
    /// All concluded timings.
    /// </summary>
    internal static readonly SortedList<TimingHandle, TimingHandle> AllTimings = new();

    internal static readonly List<TimingEvent> Events = new();

    private static readonly AsyncLocal<List<TimingHandle>> threadTimingsStackStorage = new();

    /// <summary>
    /// Gets all active timings of current thread.
    /// </summary>
    internal static List<TimingHandle> ThreadTimingsStack
    {
        get
        {
            threadTimingsStackStorage.Value ??= new List<TimingHandle>();
            return threadTimingsStackStorage.Value;
        }
    }

    /// <summary>
    /// Start a new timing.
    /// </summary>
    /// <param name="name">The name of the timing.</param>
    /// <param name="memberName">Name of the calling member.</param>
    /// <param name="sourceFilePath">Name of the calling file.</param>
    /// <param name="sourceLineNumber">Name of the calling line number.</param>
    /// <returns>Disposable that stops the timing once disposed.</returns>
    public static IDisposable Start(string name, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        return new TimingHandle(name)
        {
            MemberName = memberName,
            FileName = sourceFilePath,
            LineNumber = sourceLineNumber,
        };
    }

    /// <summary>
    /// Record a one-time event.
    /// </summary>
    /// <param name="name">The name of the timing.</param>
    /// <param name="memberName">Name of the calling member.</param>
    /// <param name="sourceFilePath">Name of the calling file.</param>
    /// <param name="sourceLineNumber">Name of the calling line number.</param>
    /// <returns>Disposable that stops the timing once disposed.</returns>
    public static void Event(string name, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
                             [CallerLineNumber] int sourceLineNumber = 0)
    {
        lock (Events)
        {
            Events.Add(new TimingEvent(name)
            {
                MemberName = memberName,
                FileName = sourceFilePath,
                LineNumber = sourceLineNumber,
            });
        }
    }
}
