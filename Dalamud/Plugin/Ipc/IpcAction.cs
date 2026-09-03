using Dalamud.Plugin.Ipc.Internal;

#pragma warning disable SA1402 // File may only contain a single type

namespace Dalamud.Plugin.Ipc;

/// <summary>
/// A bound IPC action.
/// </summary>
public sealed class IpcAction : IIpcBoundCallable
{
    private readonly ICallGateSubscriber<object>? gate;
    private readonly Action? sender;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="IpcAction"/> class as a subscriber.
    /// </summary>
    /// <param name="gate">The underlying subscriber.</param>
    public IpcAction(ICallGateSubscriber<object> gate) => this.gate = gate;

    private IpcAction(Action sender) => this.sender = sender;

    /// <summary>
    /// Gets a value indicating whether a provider action is registered (or this event sender is not disposed).
    /// </summary>
    public bool HasAction => !this.disposed && (this.sender != null || this.gate!.HasAction);

    /// <summary>
    /// Creates an event-sender action wrapping <see cref="ICallGateProvider{TRet}.SendMessage"/>.
    /// </summary>
    /// <param name="send">The send delegate.</param>
    /// <returns>The action.</returns>
    public static IpcAction CreateSender(Action send) => new(send);

    /// <summary>
    /// Invokes the IPC action.
    /// </summary>
    public void Invoke()
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        if (this.sender != null)
            this.sender();
        else
            this.gate!.InvokeAction();
    }

    /// <summary>
    /// Invokes the IPC action when <see cref="HasAction"/> is <c>true</c>.
    /// </summary>
    /// <returns>Whether the action was invoked.</returns>
    public bool TryInvoke()
    {
        if (!this.HasAction)
            return false;

        if (this.sender != null)
            this.sender();
        else
            this.gate!.InvokeAction();

        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}

/// <inheritdoc cref="IpcAction"/>
public sealed class IpcAction<T1> : IIpcBoundCallable
{
    private readonly ICallGateSubscriber<T1, object>? gate;
    private readonly Action<T1>? sender;
    private bool disposed;

    /// <inheritdoc cref="IpcAction(ICallGateSubscriber{object})"/>
    public IpcAction(ICallGateSubscriber<T1, object> gate) => this.gate = gate;

    private IpcAction(Action<T1> sender) => this.sender = sender;

    /// <inheritdoc cref="IpcAction.HasAction"/>
    public bool HasAction => !this.disposed && (this.sender != null || this.gate!.HasAction);

    /// <inheritdoc cref="IpcAction.CreateSender"/>
    public static IpcAction<T1> CreateSender(Action<T1> send) => new(send);

    /// <inheritdoc cref="IpcAction.Invoke"/>
    public void Invoke(T1 arg1)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        if (this.sender != null)
            this.sender(arg1);
        else
            this.gate!.InvokeAction(arg1);
    }

    /// <inheritdoc cref="IpcAction.TryInvoke"/>
    public bool TryInvoke(T1 arg1)
    {
        if (!this.HasAction)
            return false;

        if (this.sender != null)
            this.sender(arg1);
        else
            this.gate!.InvokeAction(arg1);

        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}

/// <inheritdoc cref="IpcAction"/>
public sealed class IpcAction<T1, T2> : IIpcBoundCallable
{
    private readonly ICallGateSubscriber<T1, T2, object>? gate;
    private readonly Action<T1, T2>? sender;
    private bool disposed;

    /// <inheritdoc cref="IpcAction(ICallGateSubscriber{object})"/>
    public IpcAction(ICallGateSubscriber<T1, T2, object> gate) => this.gate = gate;

    private IpcAction(Action<T1, T2> sender) => this.sender = sender;

    /// <inheritdoc cref="IpcAction.HasAction"/>
    public bool HasAction => !this.disposed && (this.sender != null || this.gate!.HasAction);

    /// <inheritdoc cref="IpcAction.CreateSender"/>
    public static IpcAction<T1, T2> CreateSender(Action<T1, T2> send) => new(send);

    /// <inheritdoc cref="IpcAction.Invoke"/>
    public void Invoke(T1 arg1, T2 arg2)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        if (this.sender != null)
            this.sender(arg1, arg2);
        else
            this.gate!.InvokeAction(arg1, arg2);
    }

    /// <inheritdoc cref="IpcAction.TryInvoke"/>
    public bool TryInvoke(T1 arg1, T2 arg2)
    {
        if (!this.HasAction)
            return false;

        if (this.sender != null)
            this.sender(arg1, arg2);
        else
            this.gate!.InvokeAction(arg1, arg2);

        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}

/// <inheritdoc cref="IpcAction"/>
public sealed class IpcAction<T1, T2, T3> : IIpcBoundCallable
{
    private readonly ICallGateSubscriber<T1, T2, T3, object>? gate;
    private readonly Action<T1, T2, T3>? sender;
    private bool disposed;

    /// <inheritdoc cref="IpcAction(ICallGateSubscriber{object})"/>
    public IpcAction(ICallGateSubscriber<T1, T2, T3, object> gate) => this.gate = gate;

    private IpcAction(Action<T1, T2, T3> sender) => this.sender = sender;

    /// <inheritdoc cref="IpcAction.HasAction"/>
    public bool HasAction => !this.disposed && (this.sender != null || this.gate!.HasAction);

    /// <inheritdoc cref="IpcAction.CreateSender"/>
    public static IpcAction<T1, T2, T3> CreateSender(Action<T1, T2, T3> send) => new(send);

    /// <inheritdoc cref="IpcAction.Invoke"/>
    public void Invoke(T1 arg1, T2 arg2, T3 arg3)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        if (this.sender != null)
            this.sender(arg1, arg2, arg3);
        else
            this.gate!.InvokeAction(arg1, arg2, arg3);
    }

    /// <inheritdoc cref="IpcAction.TryInvoke"/>
    public bool TryInvoke(T1 arg1, T2 arg2, T3 arg3)
    {
        if (!this.HasAction)
            return false;

        if (this.sender != null)
            this.sender(arg1, arg2, arg3);
        else
            this.gate!.InvokeAction(arg1, arg2, arg3);

        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}

/// <inheritdoc cref="IpcAction"/>
public sealed class IpcAction<T1, T2, T3, T4> : IIpcBoundCallable
{
    private readonly ICallGateSubscriber<T1, T2, T3, T4, object>? gate;
    private readonly Action<T1, T2, T3, T4>? sender;
    private bool disposed;

    /// <inheritdoc cref="IpcAction(ICallGateSubscriber{object})"/>
    public IpcAction(ICallGateSubscriber<T1, T2, T3, T4, object> gate) => this.gate = gate;

    private IpcAction(Action<T1, T2, T3, T4> sender) => this.sender = sender;

    /// <inheritdoc cref="IpcAction.HasAction"/>
    public bool HasAction => !this.disposed && (this.sender != null || this.gate!.HasAction);

    /// <inheritdoc cref="IpcAction.CreateSender"/>
    public static IpcAction<T1, T2, T3, T4> CreateSender(Action<T1, T2, T3, T4> send) => new(send);

    /// <inheritdoc cref="IpcAction.Invoke"/>
    public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        if (this.sender != null)
            this.sender(arg1, arg2, arg3, arg4);
        else
            this.gate!.InvokeAction(arg1, arg2, arg3, arg4);
    }

    /// <inheritdoc cref="IpcAction.TryInvoke"/>
    public bool TryInvoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (!this.HasAction)
            return false;

        if (this.sender != null)
            this.sender(arg1, arg2, arg3, arg4);
        else
            this.gate!.InvokeAction(arg1, arg2, arg3, arg4);

        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}

/// <inheritdoc cref="IpcAction"/>
public sealed class IpcAction<T1, T2, T3, T4, T5> : IIpcBoundCallable
{
    private readonly ICallGateSubscriber<T1, T2, T3, T4, T5, object>? gate;
    private readonly Action<T1, T2, T3, T4, T5>? sender;
    private bool disposed;

    /// <inheritdoc cref="IpcAction(ICallGateSubscriber{object})"/>
    public IpcAction(ICallGateSubscriber<T1, T2, T3, T4, T5, object> gate) => this.gate = gate;

    private IpcAction(Action<T1, T2, T3, T4, T5> sender) => this.sender = sender;

    /// <inheritdoc cref="IpcAction.HasAction"/>
    public bool HasAction => !this.disposed && (this.sender != null || this.gate!.HasAction);

    /// <inheritdoc cref="IpcAction.CreateSender"/>
    public static IpcAction<T1, T2, T3, T4, T5> CreateSender(Action<T1, T2, T3, T4, T5> send) => new(send);

    /// <inheritdoc cref="IpcAction.Invoke"/>
    public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        if (this.sender != null)
            this.sender(arg1, arg2, arg3, arg4, arg5);
        else
            this.gate!.InvokeAction(arg1, arg2, arg3, arg4, arg5);
    }

    /// <inheritdoc cref="IpcAction.TryInvoke"/>
    public bool TryInvoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (!this.HasAction)
            return false;

        if (this.sender != null)
            this.sender(arg1, arg2, arg3, arg4, arg5);
        else
            this.gate!.InvokeAction(arg1, arg2, arg3, arg4, arg5);

        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}

/// <inheritdoc cref="IpcAction"/>
public sealed class IpcAction<T1, T2, T3, T4, T5, T6> : IIpcBoundCallable
{
    private readonly ICallGateSubscriber<T1, T2, T3, T4, T5, T6, object>? gate;
    private readonly Action<T1, T2, T3, T4, T5, T6>? sender;
    private bool disposed;

    /// <inheritdoc cref="IpcAction(ICallGateSubscriber{object})"/>
    public IpcAction(ICallGateSubscriber<T1, T2, T3, T4, T5, T6, object> gate) => this.gate = gate;

    private IpcAction(Action<T1, T2, T3, T4, T5, T6> sender) => this.sender = sender;

    /// <inheritdoc cref="IpcAction.HasAction"/>
    public bool HasAction => !this.disposed && (this.sender != null || this.gate!.HasAction);

    /// <inheritdoc cref="IpcAction.CreateSender"/>
    public static IpcAction<T1, T2, T3, T4, T5, T6> CreateSender(Action<T1, T2, T3, T4, T5, T6> send) => new(send);

    /// <inheritdoc cref="IpcAction.Invoke"/>
    public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        if (this.sender != null)
            this.sender(arg1, arg2, arg3, arg4, arg5, arg6);
        else
            this.gate!.InvokeAction(arg1, arg2, arg3, arg4, arg5, arg6);
    }

    /// <inheritdoc cref="IpcAction.TryInvoke"/>
    public bool TryInvoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (!this.HasAction)
            return false;

        if (this.sender != null)
            this.sender(arg1, arg2, arg3, arg4, arg5, arg6);
        else
            this.gate!.InvokeAction(arg1, arg2, arg3, arg4, arg5, arg6);

        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}

/// <inheritdoc cref="IpcAction"/>
public sealed class IpcAction<T1, T2, T3, T4, T5, T6, T7> : IIpcBoundCallable
{
    private readonly ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, object>? gate;
    private readonly Action<T1, T2, T3, T4, T5, T6, T7>? sender;
    private bool disposed;

    /// <inheritdoc cref="IpcAction(ICallGateSubscriber{object})"/>
    public IpcAction(ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, object> gate) => this.gate = gate;

    private IpcAction(Action<T1, T2, T3, T4, T5, T6, T7> sender) => this.sender = sender;

    /// <inheritdoc cref="IpcAction.HasAction"/>
    public bool HasAction => !this.disposed && (this.sender != null || this.gate!.HasAction);

    /// <inheritdoc cref="IpcAction.CreateSender"/>
    public static IpcAction<T1, T2, T3, T4, T5, T6, T7> CreateSender(Action<T1, T2, T3, T4, T5, T6, T7> send) => new(send);

    /// <inheritdoc cref="IpcAction.Invoke"/>
    public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        if (this.sender != null)
            this.sender(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        else
            this.gate!.InvokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }

    /// <inheritdoc cref="IpcAction.TryInvoke"/>
    public bool TryInvoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if (!this.HasAction)
            return false;

        if (this.sender != null)
            this.sender(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        else
            this.gate!.InvokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7);

        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}

/// <inheritdoc cref="IpcAction"/>
public sealed class IpcAction<T1, T2, T3, T4, T5, T6, T7, T8> : IIpcBoundCallable
{
    private readonly ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, object>? gate;
    private readonly Action<T1, T2, T3, T4, T5, T6, T7, T8>? sender;
    private bool disposed;

    /// <inheritdoc cref="IpcAction(ICallGateSubscriber{object})"/>
    public IpcAction(ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, object> gate) => this.gate = gate;

    private IpcAction(Action<T1, T2, T3, T4, T5, T6, T7, T8> sender) => this.sender = sender;

    /// <inheritdoc cref="IpcAction.HasAction"/>
    public bool HasAction => !this.disposed && (this.sender != null || this.gate!.HasAction);

    /// <inheritdoc cref="IpcAction.CreateSender"/>
    public static IpcAction<T1, T2, T3, T4, T5, T6, T7, T8> CreateSender(Action<T1, T2, T3, T4, T5, T6, T7, T8> send) => new(send);

    /// <inheritdoc cref="IpcAction.Invoke"/>
    public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        if (this.sender != null)
            this.sender(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        else
            this.gate!.InvokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }

    /// <inheritdoc cref="IpcAction.TryInvoke"/>
    public bool TryInvoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if (!this.HasAction)
            return false;

        if (this.sender != null)
            this.sender(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        else
            this.gate!.InvokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}
