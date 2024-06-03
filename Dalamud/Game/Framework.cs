using System.Collections.Concurrent;
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
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

namespace Dalamud.Game;

/// <summary>
/// This class represents the Framework of the native game client and grants access to various subsystems.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal sealed class Framework : IInternalDisposableService, IFramework
{
    private static readonly ModuleLog Log = new("Framework");

    private static readonly Stopwatch StatsStopwatch = new();

    private readonly Stopwatch updateStopwatch = new();
    private readonly HitchDetector hitchDetector;

    private readonly Hook<OnUpdateDetour> updateHook;
    private readonly Hook<OnRealDestroyDelegate> destroyHook;

    private readonly FrameworkAddressResolver addressResolver;
    
    [ServiceManager.ServiceDependency]
    private readonly GameLifecycle lifecycle = Service<GameLifecycle>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private readonly CancellationTokenSource frameworkDestroy;
    private readonly ThreadBoundTaskScheduler frameworkThreadTaskScheduler;

    private readonly ConcurrentDictionary<TaskCompletionSource, (ulong Expire, CancellationToken CancellationToken)>
        tickDelayedTaskCompletionSources = new();

    private ulong tickCounter; 

    [ServiceManager.ServiceConstructor]
    private Framework(TargetSigScanner sigScanner)
    {
        this.hitchDetector = new HitchDetector("FrameworkUpdate", this.configuration.FrameworkUpdateHitch);

        this.addressResolver = new FrameworkAddressResolver();
        this.addressResolver.Setup(sigScanner);

        this.frameworkDestroy = new();
        this.frameworkThreadTaskScheduler = new();
        this.FrameworkThreadTaskFactory = new(
            this.frameworkDestroy.Token,
            TaskCreationOptions.None,
            TaskContinuationOptions.None,
            this.frameworkThreadTaskScheduler);

        this.updateHook = Hook<OnUpdateDetour>.FromAddress(this.addressResolver.TickAddress, this.HandleFrameworkUpdate);
        this.destroyHook = Hook<OnRealDestroyDelegate>.FromAddress(this.addressResolver.DestroyAddress, this.HandleFrameworkDestroy);

        this.updateHook.Enable();
        this.destroyHook.Enable();
    }

    /// <summary>
    /// A delegate type used during the native Framework::destroy.
    /// </summary>
    /// <param name="framework">The native Framework address.</param>
    /// <returns>A value indicating if the call was successful.</returns>
    public delegate bool OnRealDestroyDelegate(IntPtr framework);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate bool OnUpdateDetour(IntPtr framework);

    /// <inheritdoc/>
    public event IFramework.OnUpdateDelegate? Update;

    /// <summary>
    /// Executes during FrameworkUpdate before all <see cref="Update"/> delegates.
    /// </summary>
    internal event IFramework.OnUpdateDelegate? BeforeUpdate;

    /// <summary>
    /// Gets or sets a value indicating whether the collection of stats is enabled.
    /// </summary>
    public static bool StatsEnabled { get; set; }

    /// <summary>
    /// Gets the stats history mapping.
    /// </summary>
    public static Dictionary<string, List<double>> StatsHistory { get; } = new();

    /// <inheritdoc/>
    public DateTime LastUpdate { get; private set; } = DateTime.MinValue;

    /// <inheritdoc/>
    public DateTime LastUpdateUTC { get; private set; } = DateTime.MinValue;

    /// <inheritdoc/>
    public TimeSpan UpdateDelta { get; private set; } = TimeSpan.Zero;

    /// <inheritdoc/>
    public bool IsInFrameworkUpdateThread => this.frameworkThreadTaskScheduler.IsOnBoundThread;

    /// <inheritdoc/>
    public bool IsFrameworkUnloading => this.frameworkDestroy.IsCancellationRequested;

    /// <summary>
    /// Gets the list of update sub-delegates that didn't get updated this frame.
    /// </summary>
    internal List<string> NonUpdatedSubDelegates { get; private set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to dispatch update events.
    /// </summary>
    internal bool DispatchUpdateEvents { get; set; } = true;

    private TaskFactory FrameworkThreadTaskFactory { get; }

    /// <inheritdoc/>
    public TaskFactory GetTaskFactory() => this.FrameworkThreadTaskFactory;

    /// <inheritdoc/>
    public Task DelayTicks(long numTicks, CancellationToken cancellationToken = default)
    {
        if (this.frameworkDestroy.IsCancellationRequested)
            return Task.FromCanceled(this.frameworkDestroy.Token);
        if (numTicks <= 0)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource();
        this.tickDelayedTaskCompletionSources[tcs] = (this.tickCounter + (ulong)numTicks, cancellationToken);
        return tcs.Task;
    }

    /// <inheritdoc/>
    public Task Run(Action action, CancellationToken cancellationToken = default)
    {
        if (cancellationToken == default)
            cancellationToken = this.FrameworkThreadTaskFactory.CancellationToken;
        return this.FrameworkThreadTaskFactory.StartNew(action, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<T> Run<T>(Func<T> action, CancellationToken cancellationToken = default)
    {
        if (cancellationToken == default)
            cancellationToken = this.FrameworkThreadTaskFactory.CancellationToken;
        return this.FrameworkThreadTaskFactory.StartNew(action, cancellationToken);
    }

    /// <inheritdoc/>
    public Task Run(Func<Task> action, CancellationToken cancellationToken = default)
    {
        if (cancellationToken == default)
            cancellationToken = this.FrameworkThreadTaskFactory.CancellationToken;
        return this.FrameworkThreadTaskFactory.StartNew(action, cancellationToken).Unwrap();
    }

    /// <inheritdoc/>
    public Task<T> Run<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        if (cancellationToken == default)
            cancellationToken = this.FrameworkThreadTaskFactory.CancellationToken;
        return this.FrameworkThreadTaskFactory.StartNew(action, cancellationToken).Unwrap();
    }

    /// <inheritdoc/>
    public Task<T> RunOnFrameworkThread<T>(Func<T> func) =>
        this.IsInFrameworkUpdateThread || this.IsFrameworkUnloading ? Task.FromResult(func()) : this.RunOnTick(func);

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public Task<T> RunOnFrameworkThread<T>(Func<Task<T>> func) =>
        this.IsInFrameworkUpdateThread || this.IsFrameworkUnloading ? func() : this.RunOnTick(func);

    /// <inheritdoc/>
    public Task RunOnFrameworkThread(Func<Task> func) =>
        this.IsInFrameworkUpdateThread || this.IsFrameworkUnloading ? func() : this.RunOnTick(func);

    /// <inheritdoc/>
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

        if (cancellationToken == default)
            cancellationToken = this.FrameworkThreadTaskFactory.CancellationToken;
        return this.FrameworkThreadTaskFactory.ContinueWhenAll(
            new[]
            {
                Task.Delay(delay, cancellationToken),
                this.DelayTicks(delayTicks, cancellationToken),
            },
            _ => func(),
            cancellationToken,
            TaskContinuationOptions.HideScheduler,
            this.frameworkThreadTaskScheduler);
    }

    /// <inheritdoc/>
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

        if (cancellationToken == default)
            cancellationToken = this.FrameworkThreadTaskFactory.CancellationToken;
        return this.FrameworkThreadTaskFactory.ContinueWhenAll(
            new[]
            {
                Task.Delay(delay, cancellationToken),
                this.DelayTicks(delayTicks, cancellationToken),
            },
            _ => action(),
            cancellationToken,
            TaskContinuationOptions.HideScheduler,
            this.frameworkThreadTaskScheduler);
    }

    /// <inheritdoc/>
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

        if (cancellationToken == default)
            cancellationToken = this.FrameworkThreadTaskFactory.CancellationToken;
        return this.FrameworkThreadTaskFactory.ContinueWhenAll(
            new[]
            {
                Task.Delay(delay, cancellationToken),
                this.DelayTicks(delayTicks, cancellationToken),
            },
            _ => func(),
            cancellationToken,
            TaskContinuationOptions.HideScheduler,
            this.frameworkThreadTaskScheduler).Unwrap();
    }

    /// <inheritdoc/>
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

        if (cancellationToken == default)
            cancellationToken = this.FrameworkThreadTaskFactory.CancellationToken;
        return this.FrameworkThreadTaskFactory.ContinueWhenAll(
            new[]
            {
                Task.Delay(delay, cancellationToken),
                this.DelayTicks(delayTicks, cancellationToken),
            },
            _ => func(),
            cancellationToken,
            TaskContinuationOptions.HideScheduler,
            this.frameworkThreadTaskScheduler).Unwrap();
    }

    /// <summary>
    /// Dispose of managed and unmanaged resources.
    /// </summary>
    void IInternalDisposableService.DisposeService()
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
        StatsStopwatch.Reset();
    }

    /// <summary>
    /// Adds a update time to the stats history.
    /// </summary>
    /// <param name="key">Delegate Name.</param>
    /// <param name="ms">Runtime.</param>
    internal static void AddToStats(string key, double ms)
    {
        if (!StatsHistory.ContainsKey(key))
            StatsHistory.Add(key, new List<double>());

        StatsHistory[key].Add(ms);

        if (StatsHistory[key].Count > 1000)
        {
            StatsHistory[key].RemoveRange(0, StatsHistory[key].Count - 1000);
        }
    }

    /// <summary>
    /// Profiles each sub-delegate in the eventDelegate and logs to StatsHistory.
    /// </summary>
    /// <param name="eventDelegate">The Delegate to Profile.</param>
    /// <param name="frameworkInstance">The Framework Instance to pass to delegate.</param>
    internal void ProfileAndInvoke(IFramework.OnUpdateDelegate? eventDelegate, IFramework frameworkInstance)
    {
        if (eventDelegate is null) return;

        var invokeList = eventDelegate.GetInvocationList();

        // Individually invoke OnUpdate handlers and time them.
        foreach (var d in invokeList)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                d.Method.Invoke(d.Target, new object[] { frameworkInstance });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception while dispatching Framework::Update event.");
            }

            stopwatch.Stop();

            var key = $"{d.Target}::{d.Method.Name}";
            if (this.NonUpdatedSubDelegates.Contains(key))
                this.NonUpdatedSubDelegates.Remove(key);

            AddToStats(key, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private bool HandleFrameworkUpdate(IntPtr framework)
    {
        this.frameworkThreadTaskScheduler.BoundThread ??= Thread.CurrentThread;

        ThreadSafety.MarkMainThread();

        this.BeforeUpdate?.InvokeSafely(this);

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
            this.tickCounter++;
            foreach (var (k, (expiry, ct)) in this.tickDelayedTaskCompletionSources)
            {
                if (ct.IsCancellationRequested)
                    k.SetCanceled(ct);
                else if (expiry <= this.tickCounter)
                    k.SetResult();
                else
                    continue;

                this.tickDelayedTaskCompletionSources.Remove(k, out _);
            }

            if (StatsEnabled)
            {
                StatsStopwatch.Restart();
                this.frameworkThreadTaskScheduler.Run();
                StatsStopwatch.Stop();

                AddToStats(nameof(this.frameworkThreadTaskScheduler), StatsStopwatch.Elapsed.TotalMilliseconds);
            }
            else
            {
                this.frameworkThreadTaskScheduler.Run();
            }

            if (StatsEnabled && this.Update != null)
            {
                // Stat Tracking for Framework Updates
                this.NonUpdatedSubDelegates = StatsHistory.Keys.ToList();
                this.ProfileAndInvoke(this.Update, this);

                // Cleanup handlers that are no longer being called
                foreach (var key in this.NonUpdatedSubDelegates)
                {
                    if (key == nameof(this.FrameworkThreadTaskFactory))
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
        this.frameworkDestroy.Cancel();
        this.DispatchUpdateEvents = false;
        foreach (var k in this.tickDelayedTaskCompletionSources.Keys)
            k.SetCanceled(this.frameworkDestroy.Token);
        this.tickDelayedTaskCompletionSources.Clear();

        // All the same, for now...
        this.lifecycle.SetShuttingDown();
        this.lifecycle.SetUnloading();

        Log.Information("Framework::Destroy!");
        Service<Dalamud>.Get().Unload();
        this.frameworkThreadTaskScheduler.Run();
        ServiceManager.WaitForServiceUnload();
        Log.Information("Framework::Destroy OK!");

        return this.destroyHook.OriginalDisposeSafe(framework);
    }
}

/// <summary>
/// Plugin-scoped version of a Framework service.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IFramework>]
#pragma warning restore SA1015
internal class FrameworkPluginScoped : IInternalDisposableService, IFramework
{
    [ServiceManager.ServiceDependency]
    private readonly Framework frameworkService = Service<Framework>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameworkPluginScoped"/> class.
    /// </summary>
    internal FrameworkPluginScoped()
    {
        this.frameworkService.Update += this.OnUpdateForward;
    }

    /// <inheritdoc/>
    public event IFramework.OnUpdateDelegate? Update;

    /// <inheritdoc/>
    public DateTime LastUpdate => this.frameworkService.LastUpdate;

    /// <inheritdoc/>
    public DateTime LastUpdateUTC => this.frameworkService.LastUpdateUTC;

    /// <inheritdoc/>
    public TimeSpan UpdateDelta => this.frameworkService.UpdateDelta;

    /// <inheritdoc/>
    public bool IsInFrameworkUpdateThread => this.frameworkService.IsInFrameworkUpdateThread;

    /// <inheritdoc/>
    public bool IsFrameworkUnloading => this.frameworkService.IsFrameworkUnloading;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.frameworkService.Update -= this.OnUpdateForward;

        this.Update = null;
    }

    /// <inheritdoc/>
    public TaskFactory GetTaskFactory() => this.frameworkService.GetTaskFactory();

    /// <inheritdoc/>
    public Task DelayTicks(long numTicks, CancellationToken cancellationToken = default) =>
        this.frameworkService.DelayTicks(numTicks, cancellationToken);

    /// <inheritdoc/>
    public Task Run(Action action, CancellationToken cancellationToken = default) =>
        this.frameworkService.Run(action, cancellationToken);

    /// <inheritdoc/>
    public Task<T> Run<T>(Func<T> action, CancellationToken cancellationToken = default) =>
        this.frameworkService.Run(action, cancellationToken);

    /// <inheritdoc/>
    public Task Run(Func<Task> action, CancellationToken cancellationToken = default) =>
        this.frameworkService.Run(action, cancellationToken);

    /// <inheritdoc/>
    public Task<T> Run<T>(Func<Task<T>> action, CancellationToken cancellationToken = default) =>
        this.frameworkService.Run(action, cancellationToken);

    /// <inheritdoc/>
    public Task<T> RunOnFrameworkThread<T>(Func<T> func)
        => this.frameworkService.RunOnFrameworkThread(func);

    /// <inheritdoc/>
    public Task RunOnFrameworkThread(Action action)
        => this.frameworkService.RunOnFrameworkThread(action);

    /// <inheritdoc/>
    public Task<T> RunOnFrameworkThread<T>(Func<Task<T>> func)
        => this.frameworkService.RunOnFrameworkThread(func);

    /// <inheritdoc/>
    public Task RunOnFrameworkThread(Func<Task> func)
        => this.frameworkService.RunOnFrameworkThread(func);

    /// <inheritdoc/>
    public Task<T> RunOnTick<T>(Func<T> func, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default)
        => this.frameworkService.RunOnTick(func, delay, delayTicks, cancellationToken);

    /// <inheritdoc/>
    public Task RunOnTick(Action action, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default)
        => this.frameworkService.RunOnTick(action, delay, delayTicks, cancellationToken);

    /// <inheritdoc/>
    public Task<T> RunOnTick<T>(Func<Task<T>> func, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default)
        => this.frameworkService.RunOnTick(func, delay, delayTicks, cancellationToken);

    /// <inheritdoc/>
    public Task RunOnTick(Func<Task> func, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default)
        => this.frameworkService.RunOnTick(func, delay, delayTicks, cancellationToken);

    private void OnUpdateForward(IFramework framework)
    {
        if (Framework.StatsEnabled && this.Update != null)
        {
            this.frameworkService.ProfileAndInvoke(this.Update, framework);
        }
        else
        {
            this.Update?.Invoke(framework);
        }
    }
}
