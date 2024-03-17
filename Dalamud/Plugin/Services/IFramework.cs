using System.Threading;
using System.Threading.Tasks;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class represents the Framework of the native game client and grants access to various subsystems.
/// </summary>
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
    /// Gets a <see cref="TaskFactory"/> that runs tasks during Framework Update event.
    /// </summary>
    public TaskFactory FrameworkThreadTaskFactory { get; }

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
    public Task RunOnFrameworkThreadAwaitable(Action action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="action">Function to call.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    public Task<T> RunOnFrameworkThreadAwaitable<T>(Func<T> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <param name="action">Function to call.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    public Task RunOnFrameworkThreadAwaitable(Func<Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="action">Function to call.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    public Task<T> RunOnFrameworkThreadAwaitable<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="func">Function to call.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    /// <remarks><c>await</c>, <see cref="Task.Run(Action)"/> or alike will continue off the framework thread.</remarks>
    public Task<T> RunOnFrameworkThread<T>(Func<T> func);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <param name="action">Function to call.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    /// <remarks><c>await</c>, <see cref="Task.Run(Action)"/> or alike will continue off the framework thread.</remarks>
    public Task RunOnFrameworkThread(Action action);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="func">Function to call.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    /// <remarks><c>await</c>, <see cref="Task.Run(Action)"/> or alike will continue off the framework thread.</remarks>
    [Obsolete($"Use {nameof(RunOnTick)} instead.")]
    public Task<T> RunOnFrameworkThread<T>(Func<Task<T>> func);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <param name="func">Function to call.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    /// <remarks><c>await</c>, <see cref="Task.Run(Action)"/> or alike will continue off the framework thread.</remarks>
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
    /// <remarks><c>await</c>, <see cref="Task.Run(Action)"/> or alike will continue off the framework thread.</remarks>
    public Task<T> RunOnTick<T>(Func<T> func, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function in upcoming Framework.Tick call.
    /// </summary>
    /// <param name="action">Function to call.</param>
    /// <param name="delay">Wait for given timespan before calling this function.</param>
    /// <param name="delayTicks">Count given number of Framework.Tick calls before calling this function. This takes precedence over delay parameter.</param>
    /// <param name="cancellationToken">Cancellation token which will prevent the execution of this function if wait conditions are not met.</param>
    /// <returns>Task representing the pending function.</returns>
    /// <remarks><c>await</c>, <see cref="Task.Run(Action)"/> or alike will continue off the framework thread.</remarks>
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
    /// <remarks><c>await</c>, <see cref="Task.Run(Action)"/> or alike will continue off the framework thread.</remarks>
    public Task<T> RunOnTick<T>(Func<Task<T>> func, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Run given function in upcoming Framework.Tick call.
    /// </summary>
    /// <param name="func">Function to call.</param>
    /// <param name="delay">Wait for given timespan before calling this function.</param>
    /// <param name="delayTicks">Count given number of Framework.Tick calls before calling this function. This takes precedence over delay parameter.</param>
    /// <param name="cancellationToken">Cancellation token which will prevent the execution of this function if wait conditions are not met.</param>
    /// <returns>Task representing the pending function.</returns>
    /// <remarks><c>await</c>, <see cref="Task.Run(Action)"/> or alike will continue off the framework thread.</remarks>
    public Task RunOnTick(Func<Task> func, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default);
}
