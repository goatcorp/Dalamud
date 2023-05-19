using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Toast;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Utility;
using Serilog;

namespace Dalamud.Game;

/// <summary>
/// This class represents the Framework of the native game client and grants access to various subsystems.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
public sealed class Framework : IDisposable, IServiceType
{
    private readonly GameLifecycle lifecycle;

    private static Stopwatch statsStopwatch = new();

    private readonly Stopwatch updateStopwatch = new();
    private readonly HitchDetector hitchDetector;

    private readonly Hook<OnUpdateDetour> updateHook;
    private readonly Hook<OnRealDestroyDelegate> destroyHook;

    private readonly object runOnNextTickTaskListSync = new();
    private List<RunOnNextTickTaskBase> runOnNextTickTaskList = new();
    private List<RunOnNextTickTaskBase> runOnNextTickTaskList2 = new();

    private Thread? frameworkUpdateThread;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    [ServiceManager.ServiceConstructor]
    private Framework(SigScanner sigScanner, GameLifecycle lifecycle)
    {
        this.lifecycle = lifecycle;
        this.hitchDetector = new HitchDetector("FrameworkUpdate", this.configuration.FrameworkUpdateHitch);

        this.Address = new FrameworkAddressResolver();
        this.Address.Setup(sigScanner);

        this.updateHook = Hook<OnUpdateDetour>.FromAddress(this.Address.TickAddress, this.HandleFrameworkUpdate);
        this.destroyHook = Hook<OnRealDestroyDelegate>.FromAddress(this.Address.DestroyAddress, this.HandleFrameworkDestroy);
    }

    /// <summary>
    /// A delegate type used with the <see cref="Update"/> event.
    /// </summary>
    /// <param name="framework">The Framework instance.</param>
    public delegate void OnUpdateDelegate(Framework framework);

    /// <summary>
    /// A delegate type used during the native Framework::destroy.
    /// </summary>
    /// <param name="framework">The native Framework address.</param>
    /// <returns>A value indicating if the call was successful.</returns>
    public delegate bool OnRealDestroyDelegate(IntPtr framework);

    /// <summary>
    /// A delegate type used during the native Framework::free.
    /// </summary>
    /// <returns>The native Framework address.</returns>
    public delegate IntPtr OnDestroyDelegate();

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate bool OnUpdateDetour(IntPtr framework);

    private delegate IntPtr OnDestroyDetour(); // OnDestroyDelegate

    /// <summary>
    /// Event that gets fired every time the game framework updates.
    /// </summary>
    public event OnUpdateDelegate Update;

    /// <summary>
    /// Gets or sets a value indicating whether the collection of stats is enabled.
    /// </summary>
    public static bool StatsEnabled { get; set; }

    /// <summary>
    /// Gets the stats history mapping.
    /// </summary>
    public static Dictionary<string, List<double>> StatsHistory { get; } = new();

    /// <summary>
    /// Gets a raw pointer to the instance of Client::Framework.
    /// </summary>
    public FrameworkAddressResolver Address { get; }

    /// <summary>
    /// Gets the last time that the Framework Update event was triggered.
    /// </summary>
    public DateTime LastUpdate { get; private set; } = DateTime.MinValue;

    /// <summary>
    /// Gets the last time in UTC that the Framework Update event was triggered.
    /// </summary>
    public DateTime LastUpdateUTC { get; private set; } = DateTime.MinValue;

    /// <summary>
    /// Gets the delta between the last Framework Update and the currently executing one.
    /// </summary>
    public TimeSpan UpdateDelta { get; private set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets a value indicating whether currently executing code is running in the game's framework update thread.
    /// </summary>
    public bool IsInFrameworkUpdateThread => Thread.CurrentThread == this.frameworkUpdateThread;

    /// <summary>
    /// Gets a value indicating whether game Framework is unloading.
    /// </summary>
    public bool IsFrameworkUnloading { get; internal set; }

    /// <summary>
    /// Gets or sets a value indicating whether to dispatch update events.
    /// </summary>
    internal bool DispatchUpdateEvents { get; set; } = true;

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="func">Function to call.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    public Task<T> RunOnFrameworkThread<T>(Func<T> func) =>
        this.IsInFrameworkUpdateThread || this.IsFrameworkUnloading ? Task.FromResult(func()) : this.RunOnTick(func);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <param name="action">Function to call.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    public Task RunOnFrameworkThread(Action action)
    {
        if (this.IsInFrameworkUpdateThread || this.IsFrameworkUnloading)
        {
            try
            {
                action();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        else
        {
            return this.RunOnTick(action);
        }
    }

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="func">Function to call.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    public Task<T> RunOnFrameworkThread<T>(Func<Task<T>> func) =>
        this.IsInFrameworkUpdateThread || this.IsFrameworkUnloading ? func() : this.RunOnTick(func);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <param name="func">Function to call.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    public Task RunOnFrameworkThread(Func<Task> func) =>
        this.IsInFrameworkUpdateThread || this.IsFrameworkUnloading ? func() : this.RunOnTick(func);

    /// <summary>
    /// Run given function in upcoming Framework.Tick call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="func">Function to call.</param>
    /// <param name="delay">Wait for given timespan before calling this function.</param>
    /// <param name="delayTicks">Count given number of Framework.Tick calls before calling this function. This takes precedence over delay parameter.</param>
    /// <param name="cancellationToken">Cancellation token which will prevent the execution of this function if wait conditions are not met.</param>
    /// <returns>Task representing the pending function.</returns>
    public Task<T> RunOnTick<T>(Func<T> func, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default)
    {
        if (this.IsFrameworkUnloading)
        {
            if (delay == default && delayTicks == default)
                return this.RunOnFrameworkThread(func);

            var cts = new CancellationTokenSource();
            cts.Cancel();
            return Task.FromCanceled<T>(cts.Token);
        }

        var tcs = new TaskCompletionSource<T>();
        lock (this.runOnNextTickTaskListSync)
        {
            this.runOnNextTickTaskList.Add(new RunOnNextTickTaskFunc<T>()
            {
                RemainingTicks = delayTicks,
                RunAfterTickCount = Environment.TickCount64 + (long)Math.Ceiling(delay.TotalMilliseconds),
                CancellationToken = cancellationToken,
                TaskCompletionSource = tcs,
                Func = func,
            });
        }

        return tcs.Task;
    }

    /// <summary>
    /// Run given function in upcoming Framework.Tick call.
    /// </summary>
    /// <param name="action">Function to call.</param>
    /// <param name="delay">Wait for given timespan before calling this function.</param>
    /// <param name="delayTicks">Count given number of Framework.Tick calls before calling this function. This takes precedence over delay parameter.</param>
    /// <param name="cancellationToken">Cancellation token which will prevent the execution of this function if wait conditions are not met.</param>
    /// <returns>Task representing the pending function.</returns>
    public Task RunOnTick(Action action, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default)
    {
        if (this.IsFrameworkUnloading)
        {
            if (delay == default && delayTicks == default)
                return this.RunOnFrameworkThread(action);

            var cts = new CancellationTokenSource();
            cts.Cancel();
            return Task.FromCanceled(cts.Token);
        }

        var tcs = new TaskCompletionSource();
        lock (this.runOnNextTickTaskListSync)
        {
            this.runOnNextTickTaskList.Add(new RunOnNextTickTaskAction()
            {
                RemainingTicks = delayTicks,
                RunAfterTickCount = Environment.TickCount64 + (long)Math.Ceiling(delay.TotalMilliseconds),
                CancellationToken = cancellationToken,
                TaskCompletionSource = tcs,
                Action = action,
            });
        }

        return tcs.Task;
    }

    /// <summary>
    /// Run given function in upcoming Framework.Tick call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="func">Function to call.</param>
    /// <param name="delay">Wait for given timespan before calling this function.</param>
    /// <param name="delayTicks">Count given number of Framework.Tick calls before calling this function. This takes precedence over delay parameter.</param>
    /// <param name="cancellationToken">Cancellation token which will prevent the execution of this function if wait conditions are not met.</param>
    /// <returns>Task representing the pending function.</returns>
    public Task<T> RunOnTick<T>(Func<Task<T>> func, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default)
    {
        if (this.IsFrameworkUnloading)
        {
            if (delay == default && delayTicks == default)
                return this.RunOnFrameworkThread(func);

            var cts = new CancellationTokenSource();
            cts.Cancel();
            return Task.FromCanceled<T>(cts.Token);
        }

        var tcs = new TaskCompletionSource<Task<T>>();
        lock (this.runOnNextTickTaskListSync)
        {
            this.runOnNextTickTaskList.Add(new RunOnNextTickTaskFunc<Task<T>>()
            {
                RemainingTicks = delayTicks,
                RunAfterTickCount = Environment.TickCount64 + (long)Math.Ceiling(delay.TotalMilliseconds),
                CancellationToken = cancellationToken,
                TaskCompletionSource = tcs,
                Func = func,
            });
        }

        return tcs.Task.ContinueWith(x => x.Result, cancellationToken).Unwrap();
    }

    /// <summary>
    /// Run given function in upcoming Framework.Tick call.
    /// </summary>
    /// <param name="func">Function to call.</param>
    /// <param name="delay">Wait for given timespan before calling this function.</param>
    /// <param name="delayTicks">Count given number of Framework.Tick calls before calling this function. This takes precedence over delay parameter.</param>
    /// <param name="cancellationToken">Cancellation token which will prevent the execution of this function if wait conditions are not met.</param>
    /// <returns>Task representing the pending function.</returns>
    public Task RunOnTick(Func<Task> func, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default)
    {
        if (this.IsFrameworkUnloading)
        {
            if (delay == default && delayTicks == default)
                return this.RunOnFrameworkThread(func);

            var cts = new CancellationTokenSource();
            cts.Cancel();
            return Task.FromCanceled(cts.Token);
        }

        var tcs = new TaskCompletionSource<Task>();
        lock (this.runOnNextTickTaskListSync)
        {
            this.runOnNextTickTaskList.Add(new RunOnNextTickTaskFunc<Task>()
            {
                RemainingTicks = delayTicks,
                RunAfterTickCount = Environment.TickCount64 + (long)Math.Ceiling(delay.TotalMilliseconds),
                CancellationToken = cancellationToken,
                TaskCompletionSource = tcs,
                Func = func,
            });
        }

        return tcs.Task.ContinueWith(x => x.Result, cancellationToken).Unwrap();
    }

    /// <summary>
    /// Dispose of managed and unmanaged resources.
    /// </summary>
    void IDisposable.Dispose()
    {
        this.RunOnFrameworkThread(() =>
        {
            // ReSharper disable once AccessToDisposedClosure
            this.updateHook.Disable();

            // ReSharper disable once AccessToDisposedClosure
            this.destroyHook.Disable();
        }).Wait();

        this.updateHook.Dispose();
        this.destroyHook.Dispose();

        this.updateStopwatch.Reset();
        statsStopwatch.Reset();
    }

    [ServiceManager.CallWhenServicesReady]
    private void ContinueConstruction()
    {
        this.updateHook.Enable();
        this.destroyHook.Enable();
    }

    private void RunPendingTickTasks()
    {
        if (this.runOnNextTickTaskList.Count == 0 && this.runOnNextTickTaskList2.Count == 0)
            return;

        for (var i = 0; i < 2; i++)
        {
            lock (this.runOnNextTickTaskListSync)
                (this.runOnNextTickTaskList, this.runOnNextTickTaskList2) = (this.runOnNextTickTaskList2, this.runOnNextTickTaskList);

            this.runOnNextTickTaskList2.RemoveAll(x => x.Run());
        }
    }

    private bool HandleFrameworkUpdate(IntPtr framework)
    {
        this.frameworkUpdateThread ??= Thread.CurrentThread;

        ThreadSafety.MarkMainThread();

        this.hitchDetector.Start();

        try
        {
            var chatGui = Service<ChatGui>.GetNullable();
            var toastGui = Service<ToastGui>.GetNullable();
            var config = Service<DalamudConfiguration>.GetNullable();
            if (chatGui == null || toastGui == null)
                goto original;

            chatGui.UpdateQueue();
            toastGui.UpdateQueue();

            config?.Update();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception while handling Framework::Update hook.");
        }

        if (this.DispatchUpdateEvents)
        {
            this.updateStopwatch.Stop();
            this.UpdateDelta = TimeSpan.FromMilliseconds(this.updateStopwatch.ElapsedMilliseconds);
            this.updateStopwatch.Restart();

            this.LastUpdate = DateTime.Now;
            this.LastUpdateUTC = DateTime.UtcNow;

            void AddToStats(string key, double ms)
            {
                if (!StatsHistory.ContainsKey(key))
                    StatsHistory.Add(key, new List<double>());

                StatsHistory[key].Add(ms);

                if (StatsHistory[key].Count > 1000)
                {
                    StatsHistory[key].RemoveRange(0, StatsHistory[key].Count - 1000);
                }
            }

            if (StatsEnabled)
            {
                statsStopwatch.Restart();
                this.RunPendingTickTasks();
                statsStopwatch.Stop();

                AddToStats(nameof(this.RunPendingTickTasks), statsStopwatch.Elapsed.TotalMilliseconds);
            }
            else
            {
                this.RunPendingTickTasks();
            }

            if (StatsEnabled && this.Update != null)
            {
                // Stat Tracking for Framework Updates
                var invokeList = this.Update.GetInvocationList();
                var notUpdated = StatsHistory.Keys.ToList();

                // Individually invoke OnUpdate handlers and time them.
                foreach (var d in invokeList)
                {
                    statsStopwatch.Restart();
                    try
                    {
                        d.Method.Invoke(d.Target, new object[] { this });
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Exception while dispatching Framework::Update event.");
                    }

                    statsStopwatch.Stop();

                    var key = $"{d.Target}::{d.Method.Name}";
                    if (notUpdated.Contains(key))
                        notUpdated.Remove(key);

                    AddToStats(key, statsStopwatch.Elapsed.TotalMilliseconds);
                }

                // Cleanup handlers that are no longer being called
                foreach (var key in notUpdated)
                {
                    if (key == nameof(this.RunPendingTickTasks))
                        continue;

                    if (StatsHistory[key].Count > 0)
                    {
                        StatsHistory[key].RemoveAt(0);
                    }
                    else
                    {
                        StatsHistory.Remove(key);
                    }
                }
            }
            else
            {
                this.Update?.InvokeSafely(this);
            }
        }

        this.hitchDetector.Stop();

        original:
        return this.updateHook.OriginalDisposeSafe(framework);
    }

    private bool HandleFrameworkDestroy(IntPtr framework)
    {
        this.IsFrameworkUnloading = true;
        this.DispatchUpdateEvents = false;

        // All the same, for now...
        this.lifecycle.SetShuttingDown();
        this.lifecycle.SetUnloading();

        Log.Information("Framework::Destroy!");
        Service<Dalamud>.Get().Unload();
        this.RunPendingTickTasks();
        ServiceManager.WaitForServiceUnload();
        Log.Information("Framework::Destroy OK!");

        return this.destroyHook.OriginalDisposeSafe(framework);
    }

    private abstract class RunOnNextTickTaskBase
    {
        internal int RemainingTicks { get; set; }

        internal long RunAfterTickCount { get; init; }

        internal CancellationToken CancellationToken { get; init; }

        internal bool Run()
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                this.CancelImpl();
                return true;
            }

            if (this.RemainingTicks > 0)
                this.RemainingTicks -= 1;
            if (this.RemainingTicks > 0)
                return false;

            if (this.RunAfterTickCount > Environment.TickCount64)
                return false;

            this.RunImpl();

            return true;
        }

        protected abstract void RunImpl();

        protected abstract void CancelImpl();
    }

    private class RunOnNextTickTaskFunc<T> : RunOnNextTickTaskBase
    {
        internal TaskCompletionSource<T> TaskCompletionSource { get; init; }

        internal Func<T> Func { get; init; }

        protected override void RunImpl()
        {
            try
            {
                this.TaskCompletionSource.SetResult(this.Func());
            }
            catch (Exception ex)
            {
                this.TaskCompletionSource.SetException(ex);
            }
        }

        protected override void CancelImpl()
        {
            this.TaskCompletionSource.SetCanceled();
        }
    }

    private class RunOnNextTickTaskAction : RunOnNextTickTaskBase
    {
        internal TaskCompletionSource TaskCompletionSource { get; init; }

        internal Action Action { get; init; }

        protected override void RunImpl()
        {
            try
            {
                this.Action();
                this.TaskCompletionSource.SetResult();
            }
            catch (Exception ex)
            {
                this.TaskCompletionSource.SetException(ex);
            }
        }

        protected override void CancelImpl()
        {
            this.TaskCompletionSource.SetCanceled();
        }
    }
}
