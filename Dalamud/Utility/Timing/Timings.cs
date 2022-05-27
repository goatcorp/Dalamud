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
    internal static readonly List<TimingHandle> AllTimings = new();

    /// <summary>
    /// All active timings.
    /// </summary>
    internal static readonly List<TimingHandle> ActiveTimings = new();

    /// <summary>
    /// Current active timing entry.
    /// </summary>
    internal static readonly AsyncLocal<TimingHandle> Current = new();

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
}
