using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

using Dalamud.Game;
using Serilog;

namespace Dalamud.Logging.Internal
{
    /// <summary>
    /// Class responsible for tracking asynchronous tasks.
    /// </summary>
    internal class TaskTracker : IDisposable
    {
        private static readonly List<TaskInfo> TrackedTasksInternal = new();
        private static readonly ConcurrentQueue<TaskInfo> NewlyCreatedTasks = new();
        private static bool clearRequested = false;

        private MonoMod.RuntimeDetour.Hook? scheduleAndStartHook;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskTracker"/> class.
        /// </summary>
        public TaskTracker()
        {
            this.ApplyPatch();

            var framework = Service<Framework>.Get();
            framework.Update += this.FrameworkOnUpdate;
        }

        /// <summary>
        /// Gets a read-only list of tracked tasks.
        /// </summary>
        public static IReadOnlyList<TaskInfo> Tasks => TrackedTasksInternal.ToArray();

        /// <summary>
        /// Clear the list of tracked tasks.
        /// </summary>
        public static void Clear() => clearRequested = true;

        /// <summary>
        /// Update the tracked data.
        /// </summary>
        public static void UpdateData()
        {
            if (clearRequested)
            {
                TrackedTasksInternal.Clear();
                clearRequested = false;
            }

            while (NewlyCreatedTasks.TryDequeue(out var newTask))
            {
                TrackedTasksInternal.Add(newTask);
            }

            for (var i = 0; i < TrackedTasksInternal.Count; i++)
            {
                var taskInfo = TrackedTasksInternal[i];
                if (taskInfo.Task == null)
                    continue;

                taskInfo.IsCompleted = taskInfo.Task.IsCompleted;
                taskInfo.IsFaulted = taskInfo.Task.IsFaulted;
                taskInfo.IsCanceled = taskInfo.Task.IsCanceled;
                taskInfo.IsCompletedSuccessfully = taskInfo.Task.IsCompletedSuccessfully;
                taskInfo.Status = taskInfo.Task.Status;

                if (taskInfo.IsCompleted || taskInfo.IsFaulted || taskInfo.IsCanceled ||
                    taskInfo.IsCompletedSuccessfully)
                {
                    taskInfo.Exception = taskInfo.Task.Exception;

                    taskInfo.Task = null;
                    taskInfo.FinishTime = DateTime.Now;
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.scheduleAndStartHook?.Dispose();

            var framework = Service<Framework>.Get();
            framework.Update -= this.FrameworkOnUpdate;
        }

        private static bool AddToActiveTasksHook(Func<Task, bool> orig, Task self)
        {
            orig(self);

            var trace = new StackTrace();
            NewlyCreatedTasks.Enqueue(new TaskInfo
            {
                Task = self,
                Id = self.Id,
                StackTrace = trace,
            });

            return true;
        }

        private void FrameworkOnUpdate(Framework framework)
        {
            UpdateData();
        }

        private void ApplyPatch()
        {
            var targetType = typeof(Task);

            var debugField = targetType.GetField("s_asyncDebuggingEnabled", BindingFlags.Static | BindingFlags.NonPublic);
            debugField.SetValue(null, true);

            Log.Information("s_asyncDebuggingEnabled: {0}", debugField.GetValue(null));

            var targetMethod = targetType.GetMethod("AddToActiveTasks", BindingFlags.Static | BindingFlags.NonPublic);
            var patchMethod = typeof(TaskTracker).GetMethod(nameof(AddToActiveTasksHook), BindingFlags.NonPublic | BindingFlags.Static);

            if (targetMethod == null)
            {
                Log.Error("AddToActiveTasks TargetMethod null!");
                return;
            }

            if (patchMethod == null)
            {
                Log.Error("AddToActiveTasks PatchMethod null!");
                return;
            }

            this.scheduleAndStartHook = new MonoMod.RuntimeDetour.Hook(targetMethod, patchMethod);

            Log.Information("AddToActiveTasks Hooked!");
        }

        /// <summary>
        /// Class representing a tracked task.
        /// </summary>
        internal class TaskInfo
        {
            /// <summary>
            /// Gets or sets the tracked task.
            /// </summary>
            public Task? Task { get; set; }

            /// <summary>
            /// Gets or sets the ID of the task.
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Gets or sets the stack trace of where the task was started.
            /// </summary>
            public StackTrace? StackTrace { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether or not the task was completed.
            /// </summary>
            public bool IsCompleted { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether or not the task faulted.
            /// </summary>
            public bool IsFaulted { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether or not the task was canceled.
            /// </summary>
            public bool IsCanceled { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether or not the task was completed successfully.
            /// </summary>
            public bool IsCompletedSuccessfully { get; set; }

            /// <summary>
            /// Gets or sets the status of the task.
            /// </summary>
            public TaskStatus Status { get; set; }

            /// <summary>
            /// Gets the start time of the task.
            /// </summary>
            public DateTime StartTime { get; } = DateTime.Now;

            /// <summary>
            /// Gets or sets the end time of the task.
            /// </summary>
            public DateTime FinishTime { get; set; }

            /// <summary>
            /// Gets or sets the exception that occurred within the task.
            /// </summary>
            public AggregateException? Exception { get; set; }
        }
    }
}
