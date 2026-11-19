using Dalamud.Plugin.Ipc.Internal;

#pragma warning disable SA1402 // File may only contain a single type

namespace Dalamud.Plugin.Ipc;

/// <summary>
/// A bound IPC function.
/// </summary>
/// <typeparam name="TRet">The return type.</typeparam>
/// <param name="gate">The underlying subscriber.</param>
public sealed class IpcFunc<TRet>(ICallGateSubscriber<TRet> gate) : IIpcBoundCallable
{
    private bool disposed;

    /// <summary>
    /// Gets a value indicating whether a provider function is registered and this binding has not been disposed.
    /// </summary>
    public bool HasFunction => !this.disposed && gate.HasFunction;

    /// <summary>
    /// Invokes the IPC function.
    /// </summary>
    /// <returns>The return value.</returns>
    public TRet Invoke()
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        return gate.InvokeFunc();
    }

    /// <summary>
    /// Invokes the IPC function when <see cref="HasFunction"/> is <c>true</c>.
    /// </summary>
    /// <param name="result">The return value when the function was invoked.</param>
    /// <returns>Whether the function was invoked.</returns>
    public bool TryInvoke(out TRet result)
    {
        if (!this.HasFunction)
        {
            result = default!;
            return false;
        }

        result = gate.InvokeFunc();
        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}

/// <inheritdoc cref="IpcFunc{TRet}"/>
public sealed class IpcFunc<T1, TRet>(ICallGateSubscriber<T1, TRet> gate) : IIpcBoundCallable
{
    private bool disposed;

    /// <inheritdoc cref="IpcFunc{TRet}.HasFunction"/>
    public bool HasFunction => !this.disposed && gate.HasFunction;

    /// <inheritdoc cref="IpcFunc{TRet}.Invoke"/>
    public TRet Invoke(T1 arg1)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        return gate.InvokeFunc(arg1);
    }

    /// <inheritdoc cref="IpcFunc{TRet}.TryInvoke"/>
    public bool TryInvoke(T1 arg1, out TRet result)
    {
        if (!this.HasFunction)
        {
            result = default!;
            return false;
        }

        result = gate.InvokeFunc(arg1);
        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}

/// <inheritdoc cref="IpcFunc{TRet}"/>
public sealed class IpcFunc<T1, T2, TRet>(ICallGateSubscriber<T1, T2, TRet> gate) : IIpcBoundCallable
{
    private bool disposed;

    /// <inheritdoc cref="IpcFunc{TRet}.HasFunction"/>
    public bool HasFunction => !this.disposed && gate.HasFunction;

    /// <inheritdoc cref="IpcFunc{TRet}.Invoke"/>
    public TRet Invoke(T1 arg1, T2 arg2)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        return gate.InvokeFunc(arg1, arg2);
    }

    /// <inheritdoc cref="IpcFunc{TRet}.TryInvoke"/>
    public bool TryInvoke(T1 arg1, T2 arg2, out TRet result)
    {
        if (!this.HasFunction)
        {
            result = default!;
            return false;
        }

        result = gate.InvokeFunc(arg1, arg2);
        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}

/// <inheritdoc cref="IpcFunc{TRet}"/>
public sealed class IpcFunc<T1, T2, T3, TRet>(ICallGateSubscriber<T1, T2, T3, TRet> gate) : IIpcBoundCallable
{
    private bool disposed;

    /// <inheritdoc cref="IpcFunc{TRet}.HasFunction"/>
    public bool HasFunction => !this.disposed && gate.HasFunction;

    /// <inheritdoc cref="IpcFunc{TRet}.Invoke"/>
    public TRet Invoke(T1 arg1, T2 arg2, T3 arg3)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        return gate.InvokeFunc(arg1, arg2, arg3);
    }

    /// <inheritdoc cref="IpcFunc{TRet}.TryInvoke"/>
    public bool TryInvoke(T1 arg1, T2 arg2, T3 arg3, out TRet result)
    {
        if (!this.HasFunction)
        {
            result = default!;
            return false;
        }

        result = gate.InvokeFunc(arg1, arg2, arg3);
        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}

/// <inheritdoc cref="IpcFunc{TRet}"/>
public sealed class IpcFunc<T1, T2, T3, T4, TRet>(ICallGateSubscriber<T1, T2, T3, T4, TRet> gate) : IIpcBoundCallable
{
    private bool disposed;

    /// <inheritdoc cref="IpcFunc{TRet}.HasFunction"/>
    public bool HasFunction => !this.disposed && gate.HasFunction;

    /// <inheritdoc cref="IpcFunc{TRet}.Invoke"/>
    public TRet Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        return gate.InvokeFunc(arg1, arg2, arg3, arg4);
    }

    /// <inheritdoc cref="IpcFunc{TRet}.TryInvoke"/>
    public bool TryInvoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, out TRet result)
    {
        if (!this.HasFunction)
        {
            result = default!;
            return false;
        }

        result = gate.InvokeFunc(arg1, arg2, arg3, arg4);
        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}

/// <inheritdoc cref="IpcFunc{TRet}"/>
public sealed class IpcFunc<T1, T2, T3, T4, T5, TRet>(ICallGateSubscriber<T1, T2, T3, T4, T5, TRet> gate) : IIpcBoundCallable
{
    private bool disposed;

    /// <inheritdoc cref="IpcFunc{TRet}.HasFunction"/>
    public bool HasFunction => !this.disposed && gate.HasFunction;

    /// <inheritdoc cref="IpcFunc{TRet}.Invoke"/>
    public TRet Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        return gate.InvokeFunc(arg1, arg2, arg3, arg4, arg5);
    }

    /// <inheritdoc cref="IpcFunc{TRet}.TryInvoke"/>
    public bool TryInvoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, out TRet result)
    {
        if (!this.HasFunction)
        {
            result = default!;
            return false;
        }

        result = gate.InvokeFunc(arg1, arg2, arg3, arg4, arg5);
        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}

/// <inheritdoc cref="IpcFunc{TRet}"/>
public sealed class IpcFunc<T1, T2, T3, T4, T5, T6, TRet>(ICallGateSubscriber<T1, T2, T3, T4, T5, T6, TRet> gate) : IIpcBoundCallable
{
    private bool disposed;

    /// <inheritdoc cref="IpcFunc{TRet}.HasFunction"/>
    public bool HasFunction => !this.disposed && gate.HasFunction;

    /// <inheritdoc cref="IpcFunc{TRet}.Invoke"/>
    public TRet Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        return gate.InvokeFunc(arg1, arg2, arg3, arg4, arg5, arg6);
    }

    /// <inheritdoc cref="IpcFunc{TRet}.TryInvoke"/>
    public bool TryInvoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, out TRet result)
    {
        if (!this.HasFunction)
        {
            result = default!;
            return false;
        }

        result = gate.InvokeFunc(arg1, arg2, arg3, arg4, arg5, arg6);
        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}

/// <inheritdoc cref="IpcFunc{TRet}"/>
public sealed class IpcFunc<T1, T2, T3, T4, T5, T6, T7, TRet>(ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, TRet> gate) : IIpcBoundCallable
{
    private bool disposed;

    /// <inheritdoc cref="IpcFunc{TRet}.HasFunction"/>
    public bool HasFunction => !this.disposed && gate.HasFunction;

    /// <inheritdoc cref="IpcFunc{TRet}.Invoke"/>
    public TRet Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        return gate.InvokeFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }

    /// <inheritdoc cref="IpcFunc{TRet}.TryInvoke"/>
    public bool TryInvoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, out TRet result)
    {
        if (!this.HasFunction)
        {
            result = default!;
            return false;
        }

        result = gate.InvokeFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}

/// <inheritdoc cref="IpcFunc{TRet}"/>
public sealed class IpcFunc<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, TRet> gate) : IIpcBoundCallable
{
    private bool disposed;

    /// <inheritdoc cref="IpcFunc{TRet}.HasFunction"/>
    public bool HasFunction => !this.disposed && gate.HasFunction;

    /// <inheritdoc cref="IpcFunc{TRet}.Invoke"/>
    public TRet Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        return gate.InvokeFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }

    /// <inheritdoc cref="IpcFunc{TRet}.TryInvoke"/>
    public bool TryInvoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, out TRet result)
    {
        if (!this.HasFunction)
        {
            result = default!;
            return false;
        }

        result = gate.InvokeFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        return true;
    }

    /// <inheritdoc/>
    void IIpcBoundCallable.MarkDisposed() => this.disposed = true;
}
