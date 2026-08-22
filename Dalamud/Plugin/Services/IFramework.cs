using System.Threading;
using System.Threading.Tasks;

using Dalamud.Utility;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class represents the Framework of the native game client and grants access to various subsystems.
/// </summary>
public interface IFramework : IDalamudService
{
    /// <summary>
    /// A delegate type used with the <see cref="Update"/> event.
    /// </summary>
    /// <param name="framework">The Framework instance.</param>
    public delegate void OnUpdateDelegate(IFramework framework);

    /// <summary>
    /// Event that gets fired every time the game framework updates.
    /// </summary>
    event OnUpdateDelegate Update;

    /// <summary>
    /// Gets the last time that the Framework Update event was triggered.
    /// </summary>
    DateTime LastUpdate { get; }

    /// <summary>
    /// Gets the last time in UTC that the Framework Update event was triggered.
    /// </summary>
    DateTime LastUpdateUTC { get; }

    /// <summary>
    /// Gets the delta between the last Framework Update and the currently executing one.
    /// </summary>
    TimeSpan UpdateDelta { get; }

    /// <summary>
    /// Gets a value indicating whether currently executing code is running in the game's framework update thread.
    /// </summary>
    bool IsInFrameworkUpdateThread { get; }

    /// <summary>
    /// Gets a value indicating whether game Framework is unloading.
    /// </summary>
    bool IsFrameworkUnloading { get; }

    /// <summary>Gets a <see cref="TaskFactory"/> that runs tasks during Framework Update event.</summary>
    /// <returns>The task factory.</returns>
    TaskFactory GetTaskFactory();

    /// <summary>
    /// Returns a task that completes after the given number of ticks.
    /// </summary>
    /// <param name="numTicks">Number of ticks to delay.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A new <see cref="Task"/> that gets resolved after specified number of ticks happen.</returns>
    /// <remarks>The continuation will run on the framework thread by default.</remarks>
    Task DelayTicks(long numTicks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <param name="action">Function to call.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    /// <remarks>
    /// Use <c>await</c> if you need to wait on something from an <c>async</c> callback.
    /// </remarks>
    Task Run(Action action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="action">Function to call.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    /// <remarks>
    /// Use <c>await</c> if you need to wait on something from an <c>async</c> callback.
    /// </remarks>
    Task<T> Run<T>(Func<T> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <param name="action">Function to call.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    /// <remarks>
    /// Use <c>await</c> if you need to wait on something from an <c>async</c> callback.
    /// </remarks>
    Task Run(Func<Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="action">Function to call.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    /// <remarks>
    /// Use <c>await</c> if you need to wait on something from an <c>async</c> callback.
    /// </remarks>
    Task<T> Run<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="func">Function to call.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    /// <remarks>
    /// Use <c>await</c> if you need to wait on something from an <c>async</c> callback.
    /// </remarks>
    [Obsolete($"Use {nameof(RunOnTick)} or {nameof(Run)} instead.")]
    Task<T> RunOnFrameworkThread<T>(Func<T> func);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <param name="action">Function to call.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    /// <remarks>
    /// <para><c>await</c>, <c>Task.Factory.StartNew</c> or alike will continue off the framework thread.</para>
    /// <para>Awaiting on the returned <see cref="Task"/> from <c>RunOnFrameworkThread</c>,
    /// <c>Run</c>, or <c>RunOnTick</c> right away inside the callback specified to this
    /// function has a chance of locking up the game. Do not do <c>await framework.RunOnFrameworkThread(...);</c>
    /// directly or indirectly from the delegate passed to this function.</para>
    /// <para>See the remarks on <see cref="IFramework"/> if you need to choose which one to use, between
    /// <c>Run</c> and <c>RunOnFrameworkThread</c>. Note that <c>RunOnTick</c> is a fancy
    /// version of <c>RunOnFrameworkThread</c>.</para>
    /// </remarks>
    [Obsolete($"Use {nameof(RunOnTick)} or {nameof(Run)} instead.")]
    Task RunOnFrameworkThread(Action action);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="func">Function to call.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    /// <remarks>
    /// <para><c>await</c>, <c>Task.Factory.StartNew</c> or alike will continue off the framework thread.</para>
    /// <para>Awaiting on the returned <see cref="Task"/> from <c>RunOnFrameworkThread</c>,
    /// <c>Run</c>, or <c>RunOnTick</c> right away inside the callback specified to this
    /// function has a chance of locking up the game. Do not do <c>await framework.RunOnFrameworkThread(...);</c>
    /// directly or indirectly from the delegate passed to this function.</para>
    /// <para>See the remarks on <see cref="IFramework"/> if you need to choose which one to use, between
    /// <c>Run</c> and <c>RunOnFrameworkThread</c>. Note that <c>RunOnTick</c> is a fancy
    /// version of <c>RunOnFrameworkThread</c>.</para>
    /// </remarks>
    [Obsolete($"Use {nameof(RunOnTick)} instead.", true)]
    Task<T> RunOnFrameworkThread<T>(Func<Task<T>> func);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <param name="func">Function to call.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    /// <remarks>
    /// <para><c>await</c>, <c>Task.Factory.StartNew</c> or alike will continue off the framework thread.</para>
    /// <para>Awaiting on the returned <see cref="Task"/> from <c>RunOnFrameworkThread</c>,
    /// <c>Run</c>, or <c>RunOnTick</c> right away inside the callback specified to this
    /// function has a chance of locking up the game. Do not do <c>await framework.RunOnFrameworkThread(...);</c>
    /// directly or indirectly from the delegate passed to this function.</para>
    /// <para>See the remarks on <see cref="IFramework"/> if you need to choose which one to use, between
    /// <c>Run</c> and <c>RunOnFrameworkThread</c>. Note that <c>RunOnTick</c> is a fancy
    /// version of <c>RunOnFrameworkThread</c>.</para>
    /// </remarks>
    [Obsolete($"Use {nameof(RunOnTick)} instead.", true)]
    Task RunOnFrameworkThread(Func<Task> func);

    /// <summary>
    /// Run given function in upcoming Framework.Tick call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="func">Function to call.</param>
    /// <param name="delay">Wait for given timespan before calling this function.</param>
    /// <param name="delayTicks">Count given number of Framework.Tick calls before calling this function. This takes precedence over delay parameter.</param>
    /// <param name="cancellationToken">Cancellation token which will prevent the execution of this function if wait conditions are not met.</param>
    /// <returns>Task representing the pending function.</returns>
    /// <remarks>
    /// If you await this call, after awaiting completes you are guaranteed to no longer be on the games framework thread,
    /// even if nested inside another Run or RunOnTick.
    /// </remarks>
    Task<T> RunOnTick<T>(Func<T> func, TimeSpan delay = default, int delayTicks = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function in upcoming Framework.Tick call.
    /// </summary>
    /// <param name="action">Function to call.</param>
    /// <param name="delay">Wait for given timespan before calling this function.</param>
    /// <param name="delayTicks">Count given number of Framework.Tick calls before calling this function. This takes precedence over delay parameter.</param>
    /// <param name="cancellationToken">Cancellation token which will prevent the execution of this function if wait conditions are not met.</param>
    /// <returns>Task representing the pending function.</returns>
    /// <remarks>
    /// If you await this call, after awaiting completes you are guaranteed to no longer be on the games framework thread,
    /// even if nested inside another Run or RunOnTick.
    /// </remarks>
    Task RunOnTick(Action action, TimeSpan delay = default, int delayTicks = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function in upcoming Framework.Tick call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="func">Function to call.</param>
    /// <param name="delay">Wait for given timespan before calling this function.</param>
    /// <param name="delayTicks">Count given number of Framework.Tick calls before calling this function. This takes precedence over delay parameter.</param>
    /// <param name="cancellationToken">Cancellation token which will prevent the execution of this function if wait conditions are not met.</param>
    /// <returns>Task representing the pending function.</returns>
    /// <remarks>
    /// If you await this call, after awaiting completes you are guaranteed to no longer be on the games framework thread,
    /// even if nested inside another Run or RunOnTick.
    /// </remarks>
    Task<T> RunOnTick<T>(Func<Task<T>> func, TimeSpan delay = default, int delayTicks = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function in upcoming Framework.Tick call.
    /// </summary>
    /// <param name="func">Function to call.</param>
    /// <param name="delay">Wait for given timespan before calling this function.</param>
    /// <param name="delayTicks">Count given number of Framework.Tick calls before calling this function. This takes precedence over delay parameter.</param>
    /// <param name="cancellationToken">Cancellation token which will prevent the execution of this function if wait conditions are not met.</param>
    /// <returns>Task representing the pending function.</returns>
    /// <remarks>
    /// If you await this call, after awaiting completes you are guaranteed to no longer be on the games framework thread,
    /// even if nested inside another Run or RunOnTick.
    /// </remarks>
    Task RunOnTick(Func<Task> func, TimeSpan delay = default, int delayTicks = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new <see cref="IDebouncer"/> instance.
    /// </summary>
    /// <param name="delay">The delay to wait after the last request before executing the action.</param>
    /// <param name="action">The delegate to execute when the debounce period elapses.</param>
    /// <returns>A new, thread-safe <see cref="Debouncer"/> instance. The caller is responsible for disposing of this instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    IDebouncer CreateDebouncer(TimeSpan delay, Action action);
}
