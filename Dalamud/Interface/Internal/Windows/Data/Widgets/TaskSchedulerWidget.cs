// ReSharper disable MethodSupportsCancellation // Using alternative method of cancelling tasks by throwing exceptions.

using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Logging.Internal;
using Dalamud.Utility;
using Serilog;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying task scheduler test.
/// </summary>
internal class TaskSchedulerWidget : IDataWindowWidget
{
    private readonly FileDialogManager fileDialogManager = new();
    private string url = "https://geo.mirror.pkgbuild.com/iso/2024.01.01/archlinux-2024.01.01-x86_64.iso";
    private string localPath = string.Empty;

    private Task? downloadTask = null;
    private (long Downloaded, long Total, float Percentage) downloadState;
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
        var framework = Service<Framework>.Get();

        if (ImGui.Button("Clear list"u8))
        {
            TaskTracker.Clear();
        }

        ImGui.SameLine();
        ImGuiHelpers.ScaledDummy(10);
        ImGui.SameLine();

        if (ImGui.Button("Cancel using CancellationTokenSource"u8))
        {
            this.taskSchedulerCancelSource.Cancel();
            this.taskSchedulerCancelSource = new();
        }

        ImGui.Text("Run in any thread: "u8);
        ImGui.SameLine();

        if (ImGui.Button("Short Task.Run"u8))
        {
            Task.Run(() => { Thread.Sleep(500); });
        }

        ImGui.SameLine();

        if (ImGui.Button("Task in task(Delay)"u8))
        {
            var token = this.taskSchedulerCancelSource.Token;
            Task.Run(async () => await this.TestTaskInTaskDelay(token), token);
        }

        ImGui.SameLine();

        if (ImGui.Button("Task in task(Sleep)"u8))
        {
            Task.Run(async () => await this.TestTaskInTaskSleep());
        }

        ImGui.SameLine();

        if (ImGui.Button("Faulting task"u8))
        {
            Task.Run(() =>
            {
                Thread.Sleep(200);

                _ = ((string)null)!.Contains("dalamud"); // Intentional null exception.
            });
        }

        ImGui.Text("Run in Framework.Update: "u8);
        ImGui.SameLine();

        if (ImGui.Button("ASAP"u8))
        {
            _ = framework.RunOnTick(() => Log.Information("Framework.Update - ASAP"), cancellationToken: this.taskSchedulerCancelSource.Token);
        }

        ImGui.SameLine();

        if (ImGui.Button("In 1s"u8))
        {
            _ = framework.RunOnTick(() => Log.Information("Framework.Update - In 1s"), cancellationToken: this.taskSchedulerCancelSource.Token, delay: TimeSpan.FromSeconds(1));
        }

        ImGui.SameLine();

        if (ImGui.Button("In 60f"u8))
        {
            _ = framework.RunOnTick(() => Log.Information("Framework.Update - In 60f"), cancellationToken: this.taskSchedulerCancelSource.Token, delayTicks: 60);
        }

        ImGui.SameLine();

        if (ImGui.Button("In 1s+120f"u8))
        {
            _ = framework.RunOnTick(() => Log.Information("Framework.Update - In 1s+120f"), cancellationToken: this.taskSchedulerCancelSource.Token, delay: TimeSpan.FromSeconds(1), delayTicks: 120);
        }

        ImGui.SameLine();

        if (ImGui.Button("In 2s+60f"u8))
        {
            _ = framework.RunOnTick(() => Log.Information("Framework.Update - In 2s+60f"), cancellationToken: this.taskSchedulerCancelSource.Token, delay: TimeSpan.FromSeconds(2), delayTicks: 60);
        }

        if (ImGui.Button("Every 60f"u8))
        {
            _ = framework.RunOnTick(
                async () =>
                {
                    for (var i = 0L; ; i++)
                    {
                        Log.Information($"Loop #{i}; MainThread={ThreadSafety.IsMainThread}");
                        var it = i;
                        _ = Task.Factory.StartNew(() => Log.Information($" => Sub #{it}; MainThread={ThreadSafety.IsMainThread}"));
                        await framework.DelayTicks(60, this.taskSchedulerCancelSource.Token);
                    }
                },
                cancellationToken: this.taskSchedulerCancelSource.Token);
        }

        ImGui.SameLine();

        if (ImGui.Button("Every 1s"u8))
        {
            _ = framework.RunOnTick(
                async () =>
                {
                    for (var i = 0L; ; i++)
                    {
                        Log.Information($"Loop #{i}; MainThread={ThreadSafety.IsMainThread}");
                        var it = i;
                        _ = Task.Factory.StartNew(() => Log.Information($" => Sub #{it}; MainThread={ThreadSafety.IsMainThread}"));
                        await Task.Delay(TimeSpan.FromSeconds(1), this.taskSchedulerCancelSource.Token);
                    }
                },
                cancellationToken: this.taskSchedulerCancelSource.Token);
        }

        ImGui.SameLine();

        if (ImGui.Button("Every 60f (Await)"u8))
        {
            _ = framework.Run(
                async () =>
                {
                    for (var i = 0L; ; i++)
                    {
                        Log.Information($"Loop #{i}; MainThread={ThreadSafety.IsMainThread}");
                        var it = i;
                        _ = Task.Factory.StartNew(() => Log.Information($" => Sub #{it}; MainThread={ThreadSafety.IsMainThread}"));
                        await framework.DelayTicks(60, this.taskSchedulerCancelSource.Token);
                    }
                },
                this.taskSchedulerCancelSource.Token);
        }

        ImGui.SameLine();

        if (ImGui.Button("Every 1s (Await)"u8))
        {
            _ = framework.Run(
                async () =>
                {
                    for (var i = 0L; ; i++)
                    {
                        Log.Information($"Loop #{i}; MainThread={ThreadSafety.IsMainThread}");
                        var it = i;
                        _ = Task.Factory.StartNew(() => Log.Information($" => Sub #{it}; MainThread={ThreadSafety.IsMainThread}"));
                        await Task.Delay(TimeSpan.FromSeconds(1), this.taskSchedulerCancelSource.Token);
                    }
                },
                this.taskSchedulerCancelSource.Token);
        }

        ImGui.SameLine();

        if (ImGui.Button("As long as it's in Framework Thread"u8))
        {
            Task.Run(async () => await framework.RunOnFrameworkThread(() => { Log.Information("Task dispatched from non-framework.update thread"); }));
            framework.RunOnFrameworkThread(() => { Log.Information("Task dispatched from framework.update thread"); }).Wait();
        }

        ImGui.SameLine();

        if (ImGui.Button("Error in 1s"u8))
        {
            _ = framework.RunOnTick(() => throw new Exception("Test Exception"), cancellationToken: this.taskSchedulerCancelSource.Token, delay: TimeSpan.FromSeconds(1));
        }

        ImGui.SameLine();

        if (ImGui.Button("Freeze 1s"u8))
        {
            _ = framework.RunOnFrameworkThread(() => Helper().Wait());
            static async Task Helper() => await Task.Delay(1000);
        }

        ImGui.SameLine();

        if (ImGui.Button("Freeze Completely"u8))
        {
            _ = framework.Run(() => Helper().Wait());
            static async Task Helper() => await Task.Delay(1000);
        }

        if (ImGui.CollapsingHeader("Download"u8))
        {
            ImGui.InputText("URL"u8, ref this.url);
            ImGui.InputText("Local Path"u8, ref this.localPath);
            ImGui.SameLine();

            if (ImGuiComponents.IconButton("##localpathpicker", FontAwesomeIcon.File))
            {
                var defaultFileName = this.url.Split('\0', 2)[0].Split('/').Last();
                this.fileDialogManager.SaveFileDialog(
                    "Choose a local path",
                    "*",
                    defaultFileName,
                    string.Empty,
                    (accept, newPath) =>
                    {
                        if (accept)
                        {
                            this.localPath = newPath;
                        }
                    });
            }

            ImGui.Text($"{this.downloadState.Downloaded:##,###}/{this.downloadState.Total:##,###} ({this.downloadState.Percentage:0.00}%)");

            using var disabled =
                ImRaii.Disabled(this.downloadTask?.IsCompleted is false || this.localPath[0] == 0);
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Download"u8);
            ImGui.SameLine();
            var downloadUsingGlobalScheduler = ImGui.Button("using default scheduler"u8);
            ImGui.SameLine();
            var downloadUsingFramework = ImGui.Button("using Framework.Update"u8);
            if (downloadUsingGlobalScheduler || downloadUsingFramework)
            {
                var ct = this.taskSchedulerCancelSource.Token;
                this.downloadState = default;
                var factory = downloadUsingGlobalScheduler
                                  ? Task.Factory
                                  : framework.GetTaskFactory();
                this.downloadState = default;
                this.downloadTask = factory.StartNew(
                    async () =>
                    {
                        try
                        {
                            await using var to = File.Create(this.localPath);
                            using var client = new HttpClient();
                            using var conn = await client.GetAsync(this.url, HttpCompletionOption.ResponseHeadersRead, ct);
                            this.downloadState.Total = conn.Content.Headers.ContentLength ?? -1L;
                            await using var from = conn.Content.ReadAsStream(ct);
                            var buffer = new byte[8192];
                            while (true)
                            {
                                if (downloadUsingFramework)
                                    ThreadSafety.AssertMainThread();
                                if (downloadUsingGlobalScheduler)
                                    ThreadSafety.AssertNotMainThread();
                                var len = await from.ReadAsync(buffer, ct);
                                if (len == 0)
                                    break;
                                await to.WriteAsync(buffer.AsMemory(0, len), ct);
                                this.downloadState.Downloaded += len;
                                if (this.downloadState.Total >= 0)
                                {
                                    this.downloadState.Percentage =
                                        (100f * this.downloadState.Downloaded) / this.downloadState.Total;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error(e, "Failed to download {from} to {to}.", this.url, this.localPath);
                            try
                            {
                                File.Delete(this.localPath);
                            }
                            catch
                            {
                                // ignore
                            }
                        }
                    },
                    cancellationToken: ct).Unwrap();
            }
        }

        if (ImGui.Button("Drown in tasks"u8))
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

                if (ImGui.Button("CANCEL (May not work)"u8))
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

                ImGui.Text(task.StackTrace?.ToString());

                if (task.Exception != null)
                {
                    ImGuiHelpers.ScaledDummy(15);
                    ImGui.TextColored(ImGuiColors.DalamudRed, "EXCEPTION:"u8);
                    ImGui.Text(task.Exception.ToString());
                }
            }
            else
            {
                task.IsBeingViewed = false;
            }

            ImGui.PopStyleColor(1);
        }

        this.fileDialogManager.Draw();
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
