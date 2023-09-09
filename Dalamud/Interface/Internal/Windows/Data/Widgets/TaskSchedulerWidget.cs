// ReSharper disable MethodSupportsCancellation // Using alternative method of cancelling tasks by throwing exceptions.
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Game;
using Dalamud.Interface.Colors;
using Dalamud.Logging.Internal;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.Internal.Windows.Data;

/// <summary>
/// Widget for displaying task scheduler test.
/// </summary>
internal class TaskSchedulerWidget : IDataWindowWidget
{
    private CancellationTokenSource taskSchedulerCancelSource = new();
    
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "tasksched", "taskscheduler" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Task Scheduler"; 

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        if (ImGui.Button("Clear list"))
        {
            TaskTracker.Clear();
        }

        ImGui.SameLine();
        ImGuiHelpers.ScaledDummy(10);
        ImGui.SameLine();

        if (ImGui.Button("Cancel using CancellationTokenSource"))
        {
            this.taskSchedulerCancelSource.Cancel();
            this.taskSchedulerCancelSource = new();
        }

        ImGui.Text("Run in any thread: ");
        ImGui.SameLine();

        if (ImGui.Button("Short Task.Run"))
        {
            Task.Run(() => { Thread.Sleep(500); });
        }

        ImGui.SameLine();

        if (ImGui.Button("Task in task(Delay)"))
        {
            var token = this.taskSchedulerCancelSource.Token;
            Task.Run(async () => await this.TestTaskInTaskDelay(token), token);
        }

        ImGui.SameLine();

        if (ImGui.Button("Task in task(Sleep)"))
        {
            Task.Run(async () => await this.TestTaskInTaskSleep());
        }

        ImGui.SameLine();

        if (ImGui.Button("Faulting task"))
        {
            Task.Run(() =>
            {
                Thread.Sleep(200);

                string a = null;
                a.Contains("dalamud"); // Intentional null exception.
            });
        }

        ImGui.Text("Run in Framework.Update: ");
        ImGui.SameLine();

        if (ImGui.Button("ASAP"))
        {
            Task.Run(async () => await Service<Framework>.Get().RunOnTick(() => { }, cancellationToken: this.taskSchedulerCancelSource.Token));
        }

        ImGui.SameLine();

        if (ImGui.Button("In 1s"))
        {
            Task.Run(async () => await Service<Framework>.Get().RunOnTick(() => { }, cancellationToken: this.taskSchedulerCancelSource.Token, delay: TimeSpan.FromSeconds(1)));
        }

        ImGui.SameLine();

        if (ImGui.Button("In 60f"))
        {
            Task.Run(async () => await Service<Framework>.Get().RunOnTick(() => { }, cancellationToken: this.taskSchedulerCancelSource.Token, delayTicks: 60));
        }

        ImGui.SameLine();

        if (ImGui.Button("Error in 1s"))
        {
            Task.Run(async () => await Service<Framework>.Get().RunOnTick(() => throw new Exception("Test Exception"), cancellationToken: this.taskSchedulerCancelSource.Token, delay: TimeSpan.FromSeconds(1)));
        }

        ImGui.SameLine();

        if (ImGui.Button("As long as it's in Framework Thread"))
        {
            Task.Run(async () => await Service<Framework>.Get().RunOnFrameworkThread(() => { Log.Information("Task dispatched from non-framework.update thread"); }));
            Service<Framework>.Get().RunOnFrameworkThread(() => { Log.Information("Task dispatched from framework.update thread"); }).Wait();
        }

        if (ImGui.Button("Drown in tasks"))
        {
            var token = this.taskSchedulerCancelSource.Token;
            Task.Run(
                () => 
                {
                    for (var i = 0; i < 100; i++)
                    {
                        token.ThrowIfCancellationRequested();
                        Task.Run(
                            () => 
                            {
                                for (var j = 0; j < 100; j++)
                                {
                                    token.ThrowIfCancellationRequested();
                                    Task.Run(
                                        () => 
                                        {
                                            for (var k = 0; k < 100; k++)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                Task.Run(
                                                    () => 
                                                    {
                                                        for (var l = 0; l < 100; l++)
                                                        {
                                                            token.ThrowIfCancellationRequested();
                                                            Task.Run(
                                                                async () => 
                                                                {
                                                                    for (var m = 0; m < 100; m++)
                                                                    {
                                                                        token.ThrowIfCancellationRequested();
                                                                        await Task.Delay(1, token);
                                                                    }
                                                                });
                                                        }
                                                    });
                                            }
                                        });
                                }
                            });
                    }
                });
        }
        
        ImGui.SameLine();

        ImGuiHelpers.ScaledDummy(20);

        // Needed to init the task tracker, if we're not on a debug build
        Service<TaskTracker>.Get().Enable();

        for (var i = 0; i < TaskTracker.Tasks.Count; i++)
        {
            var task = TaskTracker.Tasks[i];
            var subTime = DateTime.Now;
            if (task.Task == null)
                subTime = task.FinishTime;

            switch (task.Status)
            {
                case TaskStatus.Created:
                case TaskStatus.WaitingForActivation:
                case TaskStatus.WaitingToRun:
                    ImGui.PushStyleColor(ImGuiCol.Header, ImGuiColors.DalamudGrey);
                    break;
                case TaskStatus.Running:
                case TaskStatus.WaitingForChildrenToComplete:
                    ImGui.PushStyleColor(ImGuiCol.Header, ImGuiColors.ParsedBlue);
                    break;
                case TaskStatus.RanToCompletion:
                    ImGui.PushStyleColor(ImGuiCol.Header, ImGuiColors.ParsedGreen);
                    break;
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                    ImGui.PushStyleColor(ImGuiCol.Header, ImGuiColors.DalamudRed);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (ImGui.CollapsingHeader($"#{task.Id} - {task.Status} {(subTime - task.StartTime).TotalMilliseconds}ms###task{i}"))
            {
                task.IsBeingViewed = true;

                if (ImGui.Button("CANCEL (May not work)"))
                {
                    try
                    {
                        var cancelFunc =
                            typeof(Task).GetMethod("InternalCancel", BindingFlags.NonPublic | BindingFlags.Instance);
                        cancelFunc?.Invoke(task, null);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Could not cancel task");
                    }
                }

                ImGuiHelpers.ScaledDummy(10);

                ImGui.TextUnformatted(task.StackTrace?.ToString());

                if (task.Exception != null)
                {
                    ImGuiHelpers.ScaledDummy(15);
                    ImGui.TextColored(ImGuiColors.DalamudRed, "EXCEPTION:");
                    ImGui.TextUnformatted(task.Exception.ToString());
                }
            }
            else
            {
                task.IsBeingViewed = false;
            }

            ImGui.PopStyleColor(1);
        }
    }
    
    private async Task TestTaskInTaskDelay(CancellationToken token)
    {
        await Task.Delay(5000, token);
    }

#pragma warning disable 1998
    private async Task TestTaskInTaskSleep()
#pragma warning restore 1998
    {
        Thread.Sleep(5000);
    }
}
