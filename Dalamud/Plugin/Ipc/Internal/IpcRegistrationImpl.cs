using System.Collections.Generic;

namespace Dalamud.Plugin.Ipc.Internal;

/// <inheritdoc cref="IpcRegistration"/>
internal class IpcRegistrationImpl : IpcRegistration
{
    private readonly List<Action> disposeActions = [];
    private readonly List<IIpcBoundCallable> callables = [];

    /// <inheritdoc/>
    public bool IsDisposed { get; private set; }

    /// <summary>Registers a dispose action to run when this registration is disposed.</summary>
    /// <param name="action">The action.</param>
    public void AddDisposeAction(Action action) => this.disposeActions.Add(action);

    /// <summary>Tracks a bound callable to mark disposed with this registration.</summary>
    /// <param name="callable">The callable.</param>
    public void AddCallable(IIpcBoundCallable callable) => this.callables.Add(callable);

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.IsDisposed)
            return;

        this.IsDisposed = true;

        foreach (var callable in this.callables)
        {
            try
            {
                callable.MarkDisposed();
            }
            catch
            {
            }
        }

        foreach (var action in this.disposeActions)
        {
            try
            {
                action();
            }
            catch
            {
            }
        }
    }
}

/// <inheritdoc cref="IpcRegistration{T}"/>
/// <typeparam name="T">The IPC type.</typeparam>
/// <summary>
/// Initializes a new instance of the <see cref="IpcRegistrationImpl{T}"/> class.
/// </summary>
/// <param name="instance">The bound instance.</param>
internal sealed class IpcRegistrationImpl<T>(T instance) : IpcRegistrationImpl, IpcRegistration<T>
{
    /// <inheritdoc/>
    public T Instance { get; } = instance;
}
