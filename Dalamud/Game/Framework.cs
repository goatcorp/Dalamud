using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using CSFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace Dalamud.Game;

/// <summary>
/// This class represents the Framework of the native game client and grants access to various subsystems.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed class Framework : IInternalDisposableService, IFramework
{
    private static readonly ModuleLog Log = ModuleLog.Create<Framework>();

    private static readonly Stopwatch StatsStopwatch = new();

    private readonly Stopwatch updateStopwatch = new();

    private readonly Hook<CSFramework.Delegates.Tick> updateHook;

    [ServiceManager.ServiceDependency]
    private readonly GameLifecycle lifecycle = Service<GameLifecycle>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private readonly CancellationTokenSource frameworkDestroy;
    private readonly CancellationTokenSource frameworkDestroyed;
    private readonly ThreadBoundTaskScheduler frameworkThreadTaskScheduler;

    private readonly ConcurrentDictionary<TaskCompletionSource, (ulong Expire, CancellationToken CancellationToken)>
        tickDelayedTaskCompletionSources = new();

    private ulong tickCounter;

    [ServiceManager.ServiceConstructor]
    private unsafe Framework()
    {
        this.frameworkDestroy = new CancellationTokenSource();
        this.frameworkDestroyed = new CancellationTokenSource();
        this.frameworkThreadTaskScheduler = new ThreadBoundTaskScheduler();
        this.FrameworkThreadTaskFactory = new TaskFactory(
            this.frameworkDestroyed.Token,
            TaskCreationOptions.None,
            TaskContinuationOptions.None,
            this.frameworkThreadTaskScheduler);

        this.updateHook = Hook<CSFramework.Delegates.Tick>.FromAddress((nint)CSFramework.StaticVirtualTablePointer->Tick, this.HandleFrameworkUpdate);

        this.updateHook.Enable();
    }

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
    public static Dictionary<string, List<double>> StatsHistory { get; } = [];

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
    internal List<string> NonUpdatedSubDelegates { get; private set; } = [];

    /// <summary>
    /// Gets the dictionary of delegates and hitch log time.
    /// </summary>
    internal Dictionary<string, DateTime> HitchLogHistory { get; private set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether to dispatch update events.
    /// </summary>
    internal bool DispatchUpdateEvents { get; set; } = true;

    private TaskFactory FrameworkThreadTaskFactory { get; }

    /// <inheritdoc/>
    public TaskFactory GetTaskFactory() => this.FrameworkThreadTaskFactory;

    /// <inheritdoc/>
    public async Task DelayTicks(long numTicks, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.frameworkDestroyed.Token);

        // Cancellation has already been requested either by provided token, or by framework token,
        // as this function is async this will return a Task.FromCancelled(...) automatically.
        linkedCts.Token.ThrowIfCancellationRequested();

        // Nonsense or before first tick
        if (numTicks <= 0 || this.frameworkThreadTaskScheduler.BoundThread == null)
        {
            return;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        this.tickDelayedTaskCompletionSources[tcs] = (this.tickCounter + (ulong)numTicks, linkedCts.Token);

        await tcs.Task;
    }

    /// <inheritdoc/>
    public async Task Run(Action action, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.frameworkDestroyed.Token);

        linkedCts.Token.ThrowIfCancellationRequested();

        if (this.IsInFrameworkUpdateThread)
        {
            action();
        }
        else
        {
            await this.FrameworkThreadTaskFactory.StartNew(action, linkedCts.Token);
        }
    }

    /// <inheritdoc/>
    public async Task<T> Run<T>(Func<T> action, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.frameworkDestroyed.Token);

        linkedCts.Token.ThrowIfCancellationRequested();

        if (this.IsInFrameworkUpdateThread)
        {
            return action();
        }

        return await this.FrameworkThreadTaskFactory.StartNew(action, linkedCts.Token);
    }

    /// <inheritdoc/>
    public async Task Run(Func<Task> action, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.frameworkDestroyed.Token);

        linkedCts.Token.ThrowIfCancellationRequested();

        if (this.IsInFrameworkUpdateThread)
        {
            await action();
        }
        else
        {
            await this.FrameworkThreadTaskFactory.StartNew(action, linkedCts.Token).Unwrap();
        }
    }

    /// <inheritdoc/>
    public async Task<T> Run<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.frameworkDestroyed.Token);

        linkedCts.Token.ThrowIfCancellationRequested();

        if (this.IsInFrameworkUpdateThread)
        {
            return await action();
        }

        return await this.FrameworkThreadTaskFactory.StartNew(action, linkedCts.Token).Unwrap();
    }

    /// <inheritdoc/>
    public Task<T> RunOnFrameworkThread<T>(Func<T> func) =>
        this.IsInFrameworkUpdateThread || this.frameworkDestroyed.IsCancellationRequested ? Task.FromResult(func()) : this.RunOnTick(func);

    /// <inheritdoc/>
    public Task RunOnFrameworkThread(Action action)
    {
        if (this.IsInFrameworkUpdateThread || this.frameworkDestroyed.IsCancellationRequested)
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
    [Obsolete("Pending Removal")]
    public Task<T> RunOnFrameworkThread<T>(Func<Task<T>> func) =>
        this.IsInFrameworkUpdateThread || this.frameworkDestroyed.IsCancellationRequested ? func() : this.RunOnTick(func);

    /// <inheritdoc/>
    [Obsolete("Pending Removal")]
    public Task RunOnFrameworkThread(Func<Task> func) =>
        this.IsInFrameworkUpdateThread || this.frameworkDestroyed.IsCancellationRequested ? func() : this.RunOnTick(func);

    /// <inheritdoc/>
    public async Task<T> RunOnTick<T>(Func<T> func, TimeSpan delay = default, int delayTicks = 0, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.frameworkDestroyed.Token);

        if (delay != TimeSpan.Zero || delayTicks is not 0)
        {
            await Task.WhenAll(Task.Delay(delay, linkedCts.Token), this.DelayTicks(delayTicks, linkedCts.Token)).ConfigureAwait(false);
        }

        return await Task.Factory.StartNew(
                   func,
                   linkedCts.Token,
                   TaskCreationOptions.HideScheduler | TaskCreationOptions.RunContinuationsAsynchronously,
                   this.frameworkThreadTaskScheduler).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RunOnTick(Action action, TimeSpan delay = default, int delayTicks = 0, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.frameworkDestroyed.Token);

        if (delay != TimeSpan.Zero || delayTicks is not 0)
        {
            await Task.WhenAll(Task.Delay(delay, linkedCts.Token), this.DelayTicks(delayTicks, linkedCts.Token)).ConfigureAwait(false);
        }

        await Task.Factory.StartNew(
            action,
            linkedCts.Token,
            TaskCreationOptions.HideScheduler | TaskCreationOptions.RunContinuationsAsynchronously,
            this.frameworkThreadTaskScheduler).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<T> RunOnTick<T>(Func<Task<T>> func, TimeSpan delay = default, int delayTicks = 0, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.frameworkDestroyed.Token);

        if (delay != TimeSpan.Zero || delayTicks is not 0)
        {
            await Task.WhenAll(Task.Delay(delay, linkedCts.Token), this.DelayTicks(delayTicks, linkedCts.Token)).ConfigureAwait(false);
        }

        return await Task.Factory.StartNew(
                   func,
                   linkedCts.Token,
                   TaskCreationOptions.HideScheduler | TaskCreationOptions.RunContinuationsAsynchronously,
                   this.frameworkThreadTaskScheduler).Unwrap().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RunOnTick(Func<Task> func, TimeSpan delay = default, int delayTicks = 0, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.frameworkDestroyed.Token);

        if (delay != TimeSpan.Zero || delayTicks is not 0)
        {
            await Task.WhenAll(Task.Delay(delay, linkedCts.Token), this.DelayTicks(delayTicks, linkedCts.Token)).ConfigureAwait(false);
        }

        await Task.Factory.StartNew(
            func,
            linkedCts.Token,
            TaskCreationOptions.HideScheduler | TaskCreationOptions.RunContinuationsAsynchronously,
            this.frameworkThreadTaskScheduler).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public IDebouncer CreateDebouncer(TimeSpan delay, Action action)
    {
        return new Debouncer(this, delay, action);
    }

    /// <summary>
    /// Dispose of managed and unmanaged resources.
    /// </summary>
    void IInternalDisposableService.DisposeService()
    {
        foreach (var k in this.tickDelayedTaskCompletionSources.Keys)
        {
            k.SetCanceled(this.frameworkDestroy.Token);
        }

        this.tickDelayedTaskCompletionSources.Clear();

        this.frameworkDestroyed.Cancel();
        this.updateHook.Dispose();

        this.updateStopwatch.Reset();
        StatsStopwatch.Reset();
    }

    /// <summary>
    /// Adds a update time to the stat's history.
    /// </summary>
    /// <param name="key">Delegate Name.</param>
    /// <param name="ms">Runtime.</param>
    internal static void AddToStats(string key, double ms)
    {
        if (!StatsHistory.TryGetValue(key, out var value))
        {
            value = [];
            StatsHistory.Add(key, value);
        }

        value.Add(ms);

        if (value.Count > 1000)
        {
            value.RemoveRange(0, value.Count - 1000);
        }
    }

    /// <summary>
    /// Cancels CancellationTokenSources, sets GameLifecycle to shutting down and unloads Dalamud services.
    /// </summary>
    internal void UnloadDalamud()
    {
        if (this.frameworkDestroy.IsCancellationRequested)
        {
            return;
        }

        this.frameworkDestroy.Cancel();
        this.DispatchUpdateEvents = false;

        // All the same, for now...
        this.lifecycle.SetShuttingDown();
        this.lifecycle.SetUnloading();

        Service<Dalamud>.Get().Unload();
    }

    /// <summary>
    /// Profiles each sub-delegate in the eventDelegate and logs to StatsHistory.
    /// </summary>
    /// <param name="eventDelegate">The Delegate to Profile.</param>
    /// <param name="frameworkInstance">The Framework Instance to pass to delegate.</param>
    /// <param name="errorHandler">A function that is called with the exception, if one arrises.</param>
    internal void ProfileAndInvoke(IFramework.OnUpdateDelegate? eventDelegate, IFramework frameworkInstance, Action<Exception, string>? errorHandler = null)
    {
        // Individually invoke OnUpdate handlers and time them.
        foreach (var d in Delegate.EnumerateInvocationList(eventDelegate))
        {
            var isScopedService = d.Method.DeclaringType == typeof(FrameworkPluginScoped); // ignore FrameworkPluginScoped.OnUpdateForward itself
            var key = $"{d.Target}::{d.Method.Name}";
            var startTime = Stopwatch.GetTimestamp();

            try
            {
                d(frameworkInstance);
            }
            catch (Exception ex)
            {
                if (errorHandler is not null)
                {
                    errorHandler.InvokeSafely(ex, key);
                }
                else if (!isScopedService)
                {
                    Log.Error(ex, "Exception while dispatching Framework::Update event.");
                }
            }

            var elapsedMilliseconds = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;

            if (!isScopedService && StatsEnabled)
            {
                this.NonUpdatedSubDelegates.Remove(key);
                AddToStats(key, elapsedMilliseconds);
            }

            if (!isScopedService && elapsedMilliseconds > this.configuration.FrameworkUpdateHitch)
            {
                var now = DateTime.UtcNow;
                var cooldownTimeSpan = TimeSpan.FromSeconds(30);

                var hasCooldown = this.HitchLogHistory.TryGetValue(key, out DateTime lastLogTimestamp);
                if (!hasCooldown || (hasCooldown && now - lastLogTimestamp > cooldownTimeSpan))
                {
                    this.HitchLogHistory[key] = now;
                    Serilog.Log.Warning("[HITCH] Long {Name} detected, {Total}ms > {Max}ms", key, elapsedMilliseconds, this.configuration.FrameworkUpdateHitch);
                }

                // Clean up old entries in HitchLogHistory
                var threshold = now - cooldownTimeSpan;
                foreach (var rmKey in this.HitchLogHistory.Where(kvp => kvp.Value < threshold).Select(kvp => kvp.Key).ToArray())
                {
                    this.HitchLogHistory.Remove(rmKey);
                }
            }
        }
    }

    private unsafe bool HandleFrameworkUpdate(CSFramework* thisPtr)
    {
        try
        {
            this.RunFrameworkTick();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception in Framework.HandleFrameworkUpdate.");
        }

        return this.updateHook.OriginalDisposeSafe(thisPtr);
    }

    private void RunFrameworkTick()
    {
        this.frameworkThreadTaskScheduler.BoundThread ??= Thread.CurrentThread;

        ThreadSafety.MarkMainThread();

        this.ProfileAndInvoke(this.BeforeUpdate, this);

        try
        {
            this.configuration.Update();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception in DalamudConfiguration.Update.");
        }

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

        // Only call Update as long as we're in the actual Framework loop
        if (this.DispatchUpdateEvents)
        {
            // Stat Tracking for Framework Updates
            if (StatsEnabled)
            {
                this.NonUpdatedSubDelegates = StatsHistory.Keys.ToList();
            }

            this.ProfileAndInvoke(this.Update, this);

            // Cleanup handlers that are no longer being called
            if (StatsEnabled)
            {
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
        }
    }
}

/// <summary>
/// Plugin-scoped version of a Framework service.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IFramework>]
#pragma warning restore SA1015
internal class FrameworkPluginScoped : IInternalDisposableService, IFramework
{
    private readonly LocalPlugin plugin;
    private readonly PluginErrorHandler pluginErrorHandler;

    [ServiceManager.ServiceDependency]
    private readonly Framework frameworkService = Service<Framework>.Get();

    private readonly CancellationTokenSource pluginUnloadCancellationToken;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameworkPluginScoped"/> class.
    /// </summary>
    /// <param name="plugin">The plugin.</param>
    /// <param name="pluginErrorHandler">Error handler instance.</param>
    internal FrameworkPluginScoped(LocalPlugin plugin, PluginErrorHandler pluginErrorHandler)
    {
        this.pluginUnloadCancellationToken = new CancellationTokenSource();

        this.plugin = plugin;
        this.pluginErrorHandler = pluginErrorHandler;

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
        this.pluginUnloadCancellationToken.Cancel();

        this.frameworkService.Update -= this.OnUpdateForward;

        this.Update = null;
    }

    /// <inheritdoc/>
    public TaskFactory GetTaskFactory() => this.frameworkService.GetTaskFactory();

    /// <inheritdoc/>
    public async Task DelayTicks(long numTicks, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.pluginUnloadCancellationToken.Token);

        await this.frameworkService.DelayTicks(numTicks, linkedCts.Token);
    }

    /// <inheritdoc/>
    public async Task Run(Action action, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.pluginUnloadCancellationToken.Token);

        await this.frameworkService.Run(action, linkedCts.Token);
    }

    /// <inheritdoc/>
    public async Task<T> Run<T>(Func<T> action, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.pluginUnloadCancellationToken.Token);

        return await this.frameworkService.Run(action, linkedCts.Token);
    }

    /// <inheritdoc/>
    public async Task Run(Func<Task> action, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.pluginUnloadCancellationToken.Token);

        await this.frameworkService.Run(action, linkedCts.Token);
    }

    /// <inheritdoc/>
    public async Task<T> Run<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.pluginUnloadCancellationToken.Token);

        return await this.frameworkService.Run(action, linkedCts.Token);
    }

    /// <inheritdoc/>
    public Task<T> RunOnFrameworkThread<T>(Func<T> func)
        => this.frameworkService.RunOnFrameworkThread(func);

    /// <inheritdoc/>
    public Task RunOnFrameworkThread(Action action)
        => this.frameworkService.RunOnFrameworkThread(action);

    /// <inheritdoc/>
    [Obsolete("Pending Removal")]
    public Task<T> RunOnFrameworkThread<T>(Func<Task<T>> func)
        => this.frameworkService.RunOnFrameworkThread(func);

    /// <inheritdoc/>
    [Obsolete("Pending Removal")]
    public Task RunOnFrameworkThread(Func<Task> func)
        => this.frameworkService.RunOnFrameworkThread(func);

    /// <inheritdoc/>
    public async Task<T> RunOnTick<T>(Func<T> func, TimeSpan delay = default, int delayTicks = 0, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.pluginUnloadCancellationToken.Token);

        return await this.frameworkService.RunOnTick(func, delay, delayTicks, linkedCts.Token);
    }

    /// <inheritdoc/>
    public async Task RunOnTick(Action action, TimeSpan delay = default, int delayTicks = 0, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.pluginUnloadCancellationToken.Token);

        await this.frameworkService.RunOnTick(action, delay, delayTicks, linkedCts.Token);
    }

    /// <inheritdoc/>
    public async Task<T> RunOnTick<T>(Func<Task<T>> func, TimeSpan delay = default, int delayTicks = 0, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.pluginUnloadCancellationToken.Token);

        return await this.frameworkService.RunOnTick(func, delay, delayTicks, linkedCts.Token);
    }

    /// <inheritdoc/>
    public async Task RunOnTick(Func<Task> func, TimeSpan delay = default, int delayTicks = 0, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.pluginUnloadCancellationToken.Token);

        await this.frameworkService.RunOnTick(func, delay, delayTicks, linkedCts.Token);
    }

    /// <inheritdoc/>
    public IDebouncer CreateDebouncer(TimeSpan delay, Action action)
        => this.frameworkService.CreateDebouncer(delay, action);

    private void OnUpdateForward(IFramework framework)
    {
        if (this.pluginUnloadCancellationToken.IsCancellationRequested)
        {
            return;
        }

        this.frameworkService.ProfileAndInvoke(this.Update, this, (ex, handlerName) =>
        {
            Serilog.Log.Error(ex, "[{PluginInternalName}] Exception in event handler {{EventHandlerName}}", this.plugin.InternalName, handlerName);
            this.pluginErrorHandler.NotifyError();
        });
    }
}
