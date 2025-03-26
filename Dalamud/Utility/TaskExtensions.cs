// Copyright (c) 2024 ppy Pty Ltd <contact@ppy.sh>.
// Copyright (c) 2024 XIVLauncher & Dalamud contributors.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Threading;
using System.Threading.Tasks;

namespace Dalamud.Utility;

/// <summary>
/// Extension methods to make working with <see cref="Task"/> easier.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Safe alternative to Task.Wait which ensures the calling thread is not a thread pool thread.
    /// </summary>
    /// <param name="task">The task to be awaited.</param>
    public static void WaitSafely(this Task task)
    {
        if (!IsWaitingValid(task))
            throw new InvalidOperationException($"Can't use {nameof(WaitSafely)} from inside an async operation.");

#pragma warning disable RS0030
        task.Wait();
#pragma warning restore RS0030
    }

    /// <summary>
    /// Safe alternative to Task.Result which ensures the calling thread is not a thread pool thread.
    /// </summary>
    /// <param name="task">The target task.</param>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <returns>The result.</returns>
    public static T GetResultSafely<T>(this Task<T> task)
    {
        // We commonly access `.Result` from within `ContinueWith`, which is a safe usage (the task is guaranteed to be completed).
        // Unfortunately, the only way to allow these usages is to check whether the task is completed or not here.
        // This does mean that there could be edge cases where this safety is skipped (ie. if the majority of executions complete
        // immediately).
        if (!task.IsCompleted && !IsWaitingValid(task))
            throw new InvalidOperationException($"Can't use {nameof(GetResultSafely)} from inside an async operation.");

#pragma warning disable RS0030
        return task.Result;
#pragma warning restore RS0030
    }

    /// <summary>Creates a new <see cref="Task"/> that resolves when <paramref name="task"/> completes, ignoring
    /// exceptions thrown from the task, if any.</summary>
    /// <param name="task">Task to await and ignore exceptions on failure.</param>
    /// <returns>A <see cref="Task"/> that completes successfully when <paramref name="task"/> completes in any state.
    /// </returns>
    /// <remarks>Awaiting the returned <see cref="Task"/> will always complete without exceptions, but awaiting
    /// <paramref name="task"/> will throw exceptions if it fails, even after this function is called.</remarks>
    /// <example>
    /// <para>
    /// <b>Wrong use of this function</b>
    /// <code>
    /// var task = TaskThrowingException();
    /// task.SuppressException();
    /// await TaskThrowingException(); // This line will throw.
    /// </code>
    /// </para>
    /// <para>
    /// <b>Correct use of this function, if waiting for the task</b>
    /// <code>await TaskThrowingException().SuppressException();</code>
    /// </para>
    /// <para>
    /// <b>Fire-and-forget</b><br />
    /// If not interested in the execution state of Task (fire-and-forget), simply calling this function will do.
    /// This function consumes the task's exception, so that it won't bubble up on later garbage collection.
    /// <code>TaskThrowingException().SuppressException();</code>
    /// </para>
    /// </example>
    public static Task SuppressException(this Task task) => task.ContinueWith(static r => r.Exception);

    private static bool IsWaitingValid(Task task)
    {
        // In the case the task has been started with the LongRunning flag, it will not be in the TPL thread pool and we can allow waiting regardless.
        if (task.CreationOptions.HasFlag(TaskCreationOptions.LongRunning))
            return true;

        // Otherwise only allow waiting from a non-TPL thread pool thread.
        return !Thread.CurrentThread.IsThreadPoolThread;
    }
}
