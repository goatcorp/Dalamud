using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal.Windows.Data.Widgets;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class represents the Framework of the native game client and grants access to various subsystems.
/// </summary>
/// <remarks>
/// <para><b>Choosing between <c>RunOnFrameworkThread</c> and <c>Run</c></b></para>
/// <ul>
/// <li>If you do need to do use <c>await</c> and have your task keep executing on the main thread after waiting is
/// done, use <c>Run</c>.</li>
/// <li>If you need to call <see cref="Task.Wait()"/> or <see cref="Task{TResult}.Result"/>, use
/// <c>RunOnFrameworkThread</c>. It also skips the task scheduler if invoked already from the framework thread.</li>
/// </ul>
/// <para>The game is likely to completely lock up if you call above synchronous function and getter, because starting
/// a new task by default runs on <see cref="TaskScheduler.Current"/>, which would make the task run on the framework
/// thread if invoked via <c>Run</c>. This includes <c>Task.Factory.StartNew</c> and
/// <c>Task.ContinueWith</c>. Use <c>Task.Run</c> if you need to start a new task from the callback specified to
/// <c>Run</c>, as it will force your task to be run in the default thread pool.</para>
/// <para>See <see cref="TaskSchedulerWidget"/> to see the difference in behaviors, and how would a misuse of these
/// functions result in a deadlock.</para>
/// </remarks>
public interface IFramework
{
    /// <summary>
    /// A delegate type used with the <see cref="Update"/> event.
    /// </summary>
    /// <param name="framework">The Framework instance.</param>
    public delegate void OnUpdateDelegate(IFramework framework);
    
    /// <summary>
    /// Event that gets fired every time the game framework updates.
    /// </summary>
    public event OnUpdateDelegate Update;
    
    /// <summary>
    /// Gets the last time that the Framework Update event was triggered.
    /// </summary>
    public DateTime LastUpdate { get; }

    /// <summary>
    /// Gets the last time in UTC that the Framework Update event was triggered.
    /// </summary>
    public DateTime LastUpdateUTC { get; }

    /// <summary>
    /// Gets the delta between the last Framework Update and the currently executing one.
    /// </summary>
    public TimeSpan UpdateDelta { get; }

    /// <summary>
    /// Gets a value indicating whether currently executing code is running in the game's framework update thread.
    /// </summary>
    public bool IsInFrameworkUpdateThread { get; }

    /// <summary>
    /// Gets a value indicating whether game Framework is unloading.
    /// </summary>
    public bool IsFrameworkUnloading { get; }

    /// <summary>Gets a <see cref="TaskFactory"/> that runs tasks during Framework Update event.</summary>
    /// <returns>The task factory.</returns>
    public TaskFactory GetTaskFactory();

    /// <summary>
    /// Returns a task that completes after the given number of ticks. 
    /// </summary>
    /// <param name="numTicks">Number of ticks to delay.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A new <see cref="Task"/> that gets resolved after specified number of ticks happen.</returns>
    /// <remarks>The continuation will run on the framework thread by default.</remarks>
    public Task DelayTicks(long numTicks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <param name="action">Function to call.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    /// <remarks>
    /// <para>Starting new tasks and waiting on them <b>synchronously</b> from this callback will completely lock up
    /// the game. Use <c>await</c> if you need to wait on something from an <c>async</c> callback.</para>
    /// <para>See the remarks on <see cref="IFramework"/> if you need to choose which one to use, between
    /// <c>Run</c> and <c>RunOnFrameworkThread</c>. Note that <c>RunOnTick</c> is a fancy
    /// version of <c>RunOnFrameworkThread</c>.</para>
    /// </remarks>
    public Task Run(Action action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="action">Function to call.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    /// <remarks>
    /// <para>Starting new tasks and waiting on them <b>synchronously</b> from this callback will completely lock up
    /// the game. Use <c>await</c> if you need to wait on something from an <c>async</c> callback.</para>
    /// <para>See the remarks on <see cref="IFramework"/> if you need to choose which one to use, between
    /// <c>Run</c> and <c>RunOnFrameworkThread</c>. Note that <c>RunOnTick</c> is a fancy
    /// version of <c>RunOnFrameworkThread</c>.</para>
    /// </remarks>
    public Task<T> Run<T>(Func<T> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <param name="action">Function to call.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    /// <remarks>
    /// <para>Starting new tasks and waiting on them <b>synchronously</b> from this callback will completely lock up
    /// the game. Use <c>await</c> if you need to wait on something from an <c>async</c> callback.</para>
    /// <para>See the remarks on <see cref="IFramework"/> if you need to choose which one to use, between
    /// <c>Run</c> and <c>RunOnFrameworkThread</c>. Note that <c>RunOnTick</c> is a fancy
    /// version of <c>RunOnFrameworkThread</c>.</para>
    /// </remarks>
    public Task Run(Func<Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="action">Function to call.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    /// <remarks>
    /// <para>Starting new tasks and waiting on them <b>synchronously</b> from this callback will completely lock up
    /// the game. Use <c>await</c> if you need to wait on something from an <c>async</c> callback.</para>
    /// <para>See the remarks on <see cref="IFramework"/> if you need to choose which one to use, between
    /// <c>Run</c> and <c>RunOnFrameworkThread</c>. Note that <c>RunOnTick</c> is a fancy
    /// version of <c>RunOnFrameworkThread</c>.</para>
    /// </remarks>
    public Task<T> Run<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);

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
    public Task<T> RunOnFrameworkThread<T>(Func<T> func);

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
    public Task RunOnFrameworkThread(Action action);

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
    [Obsolete($"Use {nameof(RunOnTick)} instead.")]
    public Task<T> RunOnFrameworkThread<T>(Func<Task<T>> func);

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
    [Obsolete($"Use {nameof(RunOnTick)} instead.")]
    public Task RunOnFrameworkThread(Func<Task> func);

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
    /// <para><c>await</c>, <c>Task.Factory.StartNew</c> or alike will continue off the framework thread.</para>
    /// <para>Awaiting on the returned <see cref="Task"/> from <c>RunOnFrameworkThread</c>,
    /// <c>Run</c>, or <c>RunOnTick</c> right away inside the callback specified to this
    /// function has a chance of locking up the game. Do not do <c>await framework.RunOnFrameworkThread(...);</c>
    /// directly or indirectly from the delegate passed to this function.</para>
    /// <para>See the remarks on <see cref="IFramework"/> if you need to choose which one to use, between
    /// <c>Run</c> and <c>RunOnFrameworkThread</c>. Note that <c>RunOnTick</c> is a fancy
    /// version of <c>RunOnFrameworkThread</c>.</para>
    /// </remarks>
    public Task<T> RunOnTick<T>(Func<T> func, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function in upcoming Framework.Tick call.
    /// </summary>
    /// <param name="action">Function to call.</param>
    /// <param name="delay">Wait for given timespan before calling this function.</param>
    /// <param name="delayTicks">Count given number of Framework.Tick calls before calling this function. This takes precedence over delay parameter.</param>
    /// <param name="cancellationToken">Cancellation token which will prevent the execution of this function if wait conditions are not met.</param>
    /// <returns>Task representing the pending function.</returns>
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
    public Task RunOnTick(Action action, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default);

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
    /// <para><c>await</c>, <c>Task.Factory.StartNew</c> or alike will continue off the framework thread.</para>
    /// <para>Awaiting on the returned <see cref="Task"/> from <c>RunOnFrameworkThread</c>,
    /// <c>Run</c>, or <c>RunOnTick</c> right away inside the callback specified to this
    /// function has a chance of locking up the game. Do not do <c>await framework.RunOnFrameworkThread(...);</c>
    /// directly or indirectly from the delegate passed to this function.</para>
    /// <para>See the remarks on <see cref="IFramework"/> if you need to choose which one to use, between
    /// <c>Run</c> and <c>RunOnFrameworkThread</c>. Note that <c>RunOnTick</c> is a fancy
    /// version of <c>RunOnFrameworkThread</c>.</para>
    /// </remarks>
    public Task<T> RunOnTick<T>(Func<Task<T>> func, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Run given function in upcoming Framework.Tick call.
    /// </summary>
    /// <param name="func">Function to call.</param>
    /// <param name="delay">Wait for given timespan before calling this function.</param>
    /// <param name="delayTicks">Count given number of Framework.Tick calls before calling this function. This takes precedence over delay parameter.</param>
    /// <param name="cancellationToken">Cancellation token which will prevent the execution of this function if wait conditions are not met.</param>
    /// <returns>Task representing the pending function.</returns>
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
    public Task RunOnTick(Func<Task> func, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default);
}
