using Dalamud.Plugin.Ipc.Internal;

#pragma warning disable SA1402 // File may only contain a single type

namespace Dalamud.Plugin.Ipc;

/// <inheritdoc cref="CallGatePubSubBase"/>
public interface ICallGateSubscriber<TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction();

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc"/>
    public TRet InvokeFunc();
}

/// <inheritdoc cref="CallGatePubSubBase"/>
public interface ICallGateSubscriber<T1, TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action<T1> action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action<T1> action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction(T1 arg1);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc"/>
    public TRet InvokeFunc(T1 arg1);
}

/// <inheritdoc cref="CallGatePubSubBase"/>
public interface ICallGateSubscriber<T1, T2, TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action<T1, T2> action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action<T1, T2> action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction(T1 arg1, T2 arg2);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc"/>
    public TRet InvokeFunc(T1 arg1, T2 arg2);
}

/// <inheritdoc cref="CallGatePubSubBase"/>
public interface ICallGateSubscriber<T1, T2, T3, TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action<T1, T2, T3> action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action<T1, T2, T3> action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction(T1 arg1, T2 arg2, T3 arg3);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc"/>
    public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3);
}

/// <inheritdoc cref="CallGatePubSubBase"/>
public interface ICallGateSubscriber<T1, T2, T3, T4, TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action<T1, T2, T3, T4> action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action<T1, T2, T3, T4> action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc"/>
    public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
}

/// <inheritdoc cref="CallGatePubSubBase"/>
public interface ICallGateSubscriber<T1, T2, T3, T4, T5, TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action<T1, T2, T3, T4, T5> action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action<T1, T2, T3, T4, T5> action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc"/>
    public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
}

/// <inheritdoc cref="CallGatePubSubBase"/>
public interface ICallGateSubscriber<T1, T2, T3, T4, T5, T6, TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action<T1, T2, T3, T4, T5, T6> action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action<T1, T2, T3, T4, T5, T6> action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc"/>
    public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
}

/// <inheritdoc cref="CallGatePubSubBase"/>
public interface ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action<T1, T2, T3, T4, T5, T6, T7> action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action<T1, T2, T3, T4, T5, T6, T7> action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc"/>
    public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
}

/// <inheritdoc cref="CallGatePubSubBase"/>
public interface ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action<T1, T2, T3, T4, T5, T6, T7, T8> action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action<T1, T2, T3, T4, T5, T6, T7, T8> action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc"/>
    public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
}

#pragma warning restore SA1402 // File may only contain a single type
