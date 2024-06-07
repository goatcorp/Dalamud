using System.Runtime.CompilerServices;

namespace Dalamud.Utility;

/// <summary>
/// Helpers for working with thread safety.
/// </summary>
public static class ThreadSafety
{
    [ThreadStatic]
    private static bool threadStaticIsMainThread;

    /// <summary>
    /// Gets a value indicating whether the current thread is the main thread.
    /// </summary>
    public static bool IsMainThread => threadStaticIsMainThread;

    /// <summary>
    /// Throws an exception when the current thread is not the main thread.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the current thread is not the main thread.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertMainThread()
    {
        if (!threadStaticIsMainThread)
        {
            throw new InvalidOperationException("Not on main thread!");
        }
    }

    /// <summary>
    /// Throws an exception when the current thread is the main thread.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the current thread is the main thread.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertNotMainThread()
    {
        if (threadStaticIsMainThread)
        {
            throw new InvalidOperationException("On main thread!");
        }
    }

    /// <summary><see cref="AssertMainThread"/>, but only on debug compilation mode.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DebugAssertMainThread()
    {
#if DEBUG
        AssertMainThread();
#endif
    }

    /// <summary>
    /// Marks a thread as the main thread.
    /// </summary>
    internal static void MarkMainThread()
    {
        threadStaticIsMainThread = true;
    }
}
