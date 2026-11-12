using System.Runtime.CompilerServices;
using System.Threading;

using Dalamud.Configuration.Internal;

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
    /// Gets the lock that excludes worker rendering from native framework updates outside presentation.
    /// </summary>
    /// <remarks>
    /// The kernel swap-chain Present call temporarily releases all game-thread recursion levels so presentation
    /// workers can render. Presentation callbacks must use nonblocking acquisition: middleware can arrive holding
    /// the native device lock needed by a game thread that owns this lock. The managed framework tick runs before
    /// this lock is acquired.
    /// </remarks>
    internal static Lock NativeFrameworkRenderSyncRoot { get; } = new();

    /// <summary>
    /// Releases the calling thread's native-update lock ownership until disposal.
    /// </summary>
    /// <returns>A scope that restores every released recursion level when disposed on the calling thread.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static NativePresentationScope EnterNativePresentation() => new(NativeFrameworkRenderSyncRoot);

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
    /// Throws an exception when the current thread is not the main thread and DevMode is enabled.
    /// </summary>
    /// <param name="message">The message to be passed into the exception, if one is to be thrown.</param>
    /// <exception cref="InvalidOperationException">Thrown when the current thread is not the main thread.</exception>
    [Api16ToDo("Replace all calls with AssertMainThread")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void DevModeAssertMainThread(string? message = null)
    {
        if (!threadStaticIsMainThread && Service<DalamudConfiguration>.Get().DevMode == true)
        {
            throw new InvalidOperationException(message ?? "Not on main thread!");
        }
    }

    /// <summary>
    /// Marks a thread as the main thread.
    /// </summary>
    internal static void MarkMainThread()
    {
        threadStaticIsMainThread = true;
    }

    /// <summary>
    /// Suspends the calling thread's recursive native-update lock ownership.
    /// </summary>
    /// <remarks>Dispose once, on the creating thread, before resuming the original native framework update.</remarks>
    internal readonly ref struct NativePresentationScope
    {
        private readonly Lock syncRoot;
        private readonly int recursionDepth;

        /// <summary>Initializes a new instance of the <see cref="NativePresentationScope"/> struct.</summary>
        /// <param name="syncRoot">The native-update lock whose calling-thread ownership is suspended.</param>
        internal NativePresentationScope(Lock syncRoot)
        {
            this.syncRoot = syncRoot;

            var depth = 0;
            while (syncRoot.IsHeldByCurrentThread)
            {
                syncRoot.Exit();
                depth++;
            }

            this.recursionDepth = depth;
        }

        /// <summary>Restores every released recursion level.</summary>
        public void Dispose()
        {
            for (var depth = 0; depth < this.recursionDepth; depth++)
                this.syncRoot.Enter();
        }
    }
}
