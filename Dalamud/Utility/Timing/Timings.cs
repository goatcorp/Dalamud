using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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

    /// <summary>
    /// List of all timing events.
    /// </summary>
    internal static readonly List<TimingEvent> Events = new();

    private static readonly AsyncLocal<Tuple<int?, List<TimingHandle>>> TaskTimingHandleStorage = new();

    /// <summary>
    /// Gets or sets all active timings of current thread.
    /// </summary>
    internal static List<TimingHandle> TaskTimingHandles
    {
        get
        {
            if (TaskTimingHandleStorage.Value == null || TaskTimingHandleStorage.Value.Item1 != Task.CurrentId)
                TaskTimingHandleStorage.Value = Tuple.Create<int?, List<TimingHandle>>(Task.CurrentId, new());
            return TaskTimingHandleStorage.Value!.Item2!;
        }
        set => TaskTimingHandleStorage.Value = Tuple.Create(Task.CurrentId, value);
    }

    /// <summary>
    /// Attaches timing handle to a Func{T}.
    /// </summary>
    /// <param name="task">Task to attach.</param>
    /// <typeparam name="T">Return type.</typeparam>
    /// <returns>Attached task.</returns>
    public static Func<T> AttachTimingHandle<T>(Func<T> task)
    {
        var outerTimingHandle = TaskTimingHandles;
        return () =>
        {
            T res = default(T);
            var prev = TaskTimingHandles;
            TaskTimingHandles = outerTimingHandle;
            try
            {
                res = task();
            }
            finally
            {
                TaskTimingHandles = prev;
            }

            return res;
        };
    }

    /// <summary>
    /// Attaches timing handle to an Action.
    /// </summary>
    /// <param name="task">Task to attach.</param>
    /// <returns>Attached task.</returns>
    public static Action AttachTimingHandle(Action task)
    {
        var outerTimingHandle = TaskTimingHandles;
        return () =>
        {
            var prev = TaskTimingHandles;
            TaskTimingHandles = outerTimingHandle;
            try
            {
                task();
            }
            finally
            {
                TaskTimingHandles = prev;
            }
        };
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
    public static void Event(
        string name,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
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
