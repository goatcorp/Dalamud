using System.Threading;

using Dalamud.Plugin.Services;

namespace Dalamud.Utility;

/// <summary>
/// Provides a thread-safe mechanism to debounce actions, ensuring that a rapid succession 
/// of calls only triggers the action after a specified delay has elapsed since the last call.
/// </summary>
public class Debouncer : IDisposable
{
    private readonly IFramework framework;
    private readonly TimeSpan delay;
    private readonly Action action;
    private readonly Lock debouncerLock = new();
    private CancellationTokenSource cts = new();
    private DateTime targetTime = DateTime.MinValue;
    private bool isPending;
    private bool isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Debouncer"/> class.
    /// </summary>
    /// <param name="framework">Dalamuds <see cref="IFramework"/> service.</param>
    /// <param name="delay">The delay to wait after the last request before executing the action.</param>
    /// <param name="action">The delegate to execute when the debounce period elapses.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public Debouncer(IFramework framework, TimeSpan delay, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        this.framework = framework;
        this.delay = delay;
        this.action = action;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        using var scope = this.debouncerLock.EnterScope();

        if (this.isDisposed)
            return;

        this.isDisposed = true;
        this.cts.Cancel();
        this.cts.Dispose();
    }

    /// <summary>
    /// Requests the execution of the action.
    /// </summary>
    public void Debounce()
    {
        using var scope = this.debouncerLock.EnterScope();

        if (this.isDisposed)
            return;

        this.targetTime = DateTime.UtcNow + this.delay;

        if (this.isPending)
            return;

        this.cts.Cancel();
        this.cts.Dispose();
        this.cts = new();

        this.isPending = true;
        this.framework.RunOnTick(this.OnTick, this.delay, cancellationToken: this.cts.Token);
    }

    private void OnTick()
    {
        using (this.debouncerLock.EnterScope())
        {
            if (this.isDisposed)
                return;

            var now = DateTime.UtcNow;
            if (now < this.targetTime)
            {
                this.isPending = true;
                this.framework.RunOnTick(this.OnTick, this.targetTime - now, cancellationToken: this.cts.Token);
                return;
            }

            this.isPending = false;
        }

        this.action();
    }
}
