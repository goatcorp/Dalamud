using System.Runtime.CompilerServices;

namespace Dalamud.Utility;

/// <summary>
/// Helpers for working with thread safety.
/// </summary>
public static class ThreadSafety
{
    [ThreadStatic]
    private static bool threadStaticIsMainThread;

    [ThreadStatic]
    private static bool threadStaticIsInPresent;

    /// <summary>
    /// Gets a value indicating whether the current thread is the main thread.
    /// </summary>
    public static bool IsMainThread => threadStaticIsMainThread;

    /// <summary>
    /// Gets a value indicating whether the current thread is currently executing inside the DXGI Present detour.
    /// </summary>
    /// <remarks>
    /// This is a frame-phase guard, distinct from <see cref="IsMainThread"/>. Driving the shared D3D11 immediate
    /// context (e.g. via <c>DrawListTextureWrap</c>) is only safe inside the Present detour. Because frame-generation
    /// layers such as NVIDIA Smooth Motion can invoke Present from a non-main thread, this flag is tracked
    /// per-thread and set only for the duration of the Present detour on whichever thread is presenting.
    /// </remarks>
    public static bool IsInPresent => threadStaticIsInPresent;

    /// <summary>
    /// Throws an exception when the current thread is not the main thread.
    /// </summary>
    /// <param name="message">The message to be passed into the exception, if one is to be thrown.</param>
    /// <exception cref="InvalidOperationException">Thrown when the current thread is not the main thread.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertMainThread(string? message = null)
    {
        if (!threadStaticIsMainThread)
        {
            throw new InvalidOperationException(message ?? "Not on main thread!");
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
    /// Throws an exception when the current thread is not executing inside the DXGI Present detour.
    /// </summary>
    /// <param name="message">The message to be passed into the exception, if one is to be thrown.</param>
    /// <exception cref="InvalidOperationException">Thrown when the current thread is not inside the Present detour.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertInPresent(string? message = null)
    {
        if (!threadStaticIsInPresent)
        {
            throw new InvalidOperationException(
                message ??
                "Not inside the Present detour! Driving the D3D11 immediate context is only safe during Present.");
        }
    }

    /// <summary><see cref="AssertInPresent"/>, but only on debug compilation mode.</summary>
    /// <param name="message">The message to be passed into the exception, if one is to be thrown.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DebugAssertInPresent(string? message = null)
    {
#if DEBUG
        AssertInPresent(message);
#endif
    }

    /// <summary>
    /// Marks a thread as the main thread.
    /// </summary>
    internal static void MarkMainThread()
    {
        threadStaticIsMainThread = true;
    }

    /// <summary>
    /// Marks the current thread as executing inside the DXGI Present detour, for the duration of the returned scope.
    /// </summary>
    /// <returns>A scope that clears the flag (restoring the previous value) when disposed.</returns>
    internal static InPresentScope EnterPresent() => new(true);

    /// <summary>
    /// A disposable scope that sets <see cref="IsInPresent"/> for the current thread and restores its previous
    /// value when disposed.
    /// </summary>
    internal readonly ref struct InPresentScope
    {
        private readonly bool previous;

        /// <summary>Initializes a new instance of the <see cref="InPresentScope"/> struct.</summary>
        /// <param name="value">The value to set <see cref="IsInPresent"/> to for the current thread.</param>
        public InPresentScope(bool value)
        {
            this.previous = threadStaticIsInPresent;
            threadStaticIsInPresent = value;
        }

        /// <summary>Restores the previous <see cref="IsInPresent"/> value for the current thread.</summary>
        public void Dispose() => threadStaticIsInPresent = this.previous;
    }
}
