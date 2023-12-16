#pragma warning disable SA1402 // File may only contain a single type

namespace Dalamud.Plugin.Ipc.Internal;

/// <inheritdoc cref="CallGatePubSubBase"/>
internal class CallGatePubSub<TRet> : CallGatePubSubBase, ICallGateProvider<TRet>, ICallGateSubscriber<TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase(string)"/>
    public CallGatePubSub(string name)
        : base(name)
    {
    }

    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action action)
        => base.RegisterAction(action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<TRet> func)
        => base.RegisterFunc(func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage()
        => base.SendMessage();

    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action action)
        => base.Subscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action action)
        => base.Unsubscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction()
        => base.InvokeAction();

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc{TRet}"/>
    public TRet InvokeFunc()
        => this.InvokeFunc<TRet>();
}

/// <inheritdoc cref="CallGatePubSubBase"/>
internal class CallGatePubSub<T1, TRet> : CallGatePubSubBase, ICallGateProvider<T1, TRet>, ICallGateSubscriber<T1, TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase(string)"/>
    public CallGatePubSub(string name)
        : base(name)
    {
    }

    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action<T1> action)
        => base.RegisterAction(action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<T1, TRet> func)
        => base.RegisterFunc(func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage(T1 arg1)
        => base.SendMessage(arg1);

    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action<T1> action)
        => base.Subscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action<T1> action)
        => base.Unsubscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction(T1 arg1)
        => base.InvokeAction(arg1);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc{TRet}"/>
    public TRet InvokeFunc(T1 arg1)
        => this.InvokeFunc<TRet>(arg1);
}

/// <inheritdoc cref="CallGatePubSubBase"/>
internal class CallGatePubSub<T1, T2, TRet> : CallGatePubSubBase, ICallGateProvider<T1, T2, TRet>, ICallGateSubscriber<T1, T2, TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase(string)"/>
    public CallGatePubSub(string name)
        : base(name)
    {
    }

    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action<T1, T2> action)
        => base.RegisterAction(action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<T1, T2, TRet> func)
        => base.RegisterFunc(func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage(T1 arg1, T2 arg2)
        => base.SendMessage(arg1, arg2);

    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action<T1, T2> action)
        => base.Subscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action<T1, T2> action)
        => base.Unsubscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction(T1 arg1, T2 arg2)
        => base.InvokeAction(arg1, arg2);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc{TRet}"/>
    public TRet InvokeFunc(T1 arg1, T2 arg2)
        => this.InvokeFunc<TRet>(arg1, arg2);
}

/// <inheritdoc cref="CallGatePubSubBase"/>
internal class CallGatePubSub<T1, T2, T3, TRet> : CallGatePubSubBase, ICallGateProvider<T1, T2, T3, TRet>, ICallGateSubscriber<T1, T2, T3, TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase(string)"/>
    public CallGatePubSub(string name)
        : base(name)
    {
    }

    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action<T1, T2, T3> action)
        => base.RegisterAction(action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<T1, T2, T3, TRet> func)
        => base.RegisterFunc(func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage(T1 arg1, T2 arg2, T3 arg3)
        => base.SendMessage(arg1, arg2, arg3);

    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action<T1, T2, T3> action)
        => base.Subscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action<T1, T2, T3> action)
        => base.Unsubscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction(T1 arg1, T2 arg2, T3 arg3)
        => base.InvokeAction(arg1, arg2, arg3);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc{TRet}"/>
    public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3)
        => this.InvokeFunc<TRet>(arg1, arg2, arg3);
}

/// <inheritdoc cref="CallGatePubSubBase"/>
internal class CallGatePubSub<T1, T2, T3, T4, TRet> : CallGatePubSubBase, ICallGateProvider<T1, T2, T3, T4, TRet>, ICallGateSubscriber<T1, T2, T3, T4, TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase(string)"/>
    public CallGatePubSub(string name)
        : base(name)
    {
    }

    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action<T1, T2, T3, T4> action)
        => base.RegisterAction(action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<T1, T2, T3, T4, TRet> func)
        => base.RegisterFunc(func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        => base.SendMessage(arg1, arg2, arg3, arg4);

    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action<T1, T2, T3, T4> action)
        => base.Subscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action<T1, T2, T3, T4> action)
        => base.Unsubscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        => base.InvokeAction(arg1, arg2, arg3, arg4);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc{TRet}"/>
    public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        => this.InvokeFunc<TRet>(arg1, arg2, arg3, arg4);
}

/// <inheritdoc cref="CallGatePubSubBase"/>
internal class CallGatePubSub<T1, T2, T3, T4, T5, TRet> : CallGatePubSubBase, ICallGateProvider<T1, T2, T3, T4, T5, TRet>, ICallGateSubscriber<T1, T2, T3, T4, T5, TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase(string)"/>
    public CallGatePubSub(string name)
        : base(name)
    {
    }

    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action<T1, T2, T3, T4, T5> action)
        => base.RegisterAction(action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<T1, T2, T3, T4, T5, TRet> func)
        => base.RegisterFunc(func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        => base.SendMessage(arg1, arg2, arg3, arg4, arg5);

    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action<T1, T2, T3, T4, T5> action)
        => base.Subscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action<T1, T2, T3, T4, T5> action)
        => base.Unsubscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        => base.InvokeAction(arg1, arg2, arg3, arg4, arg5);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc{TRet}"/>
    public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        => this.InvokeFunc<TRet>(arg1, arg2, arg3, arg4, arg5);
}

/// <inheritdoc cref="CallGatePubSubBase"/>
internal class CallGatePubSub<T1, T2, T3, T4, T5, T6, TRet> : CallGatePubSubBase, ICallGateProvider<T1, T2, T3, T4, T5, T6, TRet>, ICallGateSubscriber<T1, T2, T3, T4, T5, T6, TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase(string)"/>
    public CallGatePubSub(string name)
        : base(name)
    {
    }

    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action<T1, T2, T3, T4, T5, T6> action)
        => base.RegisterAction(action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<T1, T2, T3, T4, T5, T6, TRet> func)
        => base.RegisterFunc(func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        => base.SendMessage(arg1, arg2, arg3, arg4, arg5, arg6);

    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action<T1, T2, T3, T4, T5, T6> action)
        => base.Subscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action<T1, T2, T3, T4, T5, T6> action)
        => base.Unsubscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        => base.InvokeAction(arg1, arg2, arg3, arg4, arg5, arg6);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc{TRet}"/>
    public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        => this.InvokeFunc<TRet>(arg1, arg2, arg3, arg4, arg5, arg6);
}

/// <inheritdoc cref="CallGatePubSubBase"/>
internal class CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, TRet> : CallGatePubSubBase, ICallGateProvider<T1, T2, T3, T4, T5, T6, T7, TRet>, ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase(string)"/>
    public CallGatePubSub(string name)
        : base(name)
    {
    }

    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action<T1, T2, T3, T4, T5, T6, T7> action)
        => base.RegisterAction(action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<T1, T2, T3, T4, T5, T6, T7, TRet> func)
        => base.RegisterFunc(func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        => base.SendMessage(arg1, arg2, arg3, arg4, arg5, arg6, arg7);

    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action<T1, T2, T3, T4, T5, T6, T7> action)
        => base.Subscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action<T1, T2, T3, T4, T5, T6, T7> action)
        => base.Unsubscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        => base.InvokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc{TRet}"/>
    public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        => this.InvokeFunc<TRet>(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
}

/// <inheritdoc cref="CallGatePubSubBase"/>
internal class CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, T8, TRet> : CallGatePubSubBase, ICallGateProvider<T1, T2, T3, T4, T5, T6, T7, T8, TRet>, ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, TRet>
{
    /// <inheritdoc cref="CallGatePubSubBase(string)"/>
    public CallGatePubSub(string name)
        : base(name)
    {
    }

    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action<T1, T2, T3, T4, T5, T6, T7, T8> action)
        => base.RegisterAction(action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<T1, T2, T3, T4, T5, T6, T7, T8, TRet> func)
        => base.RegisterFunc(func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        => base.SendMessage(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

    /// <inheritdoc cref="CallGatePubSubBase.Subscribe"/>
    public void Subscribe(Action<T1, T2, T3, T4, T5, T6, T7, T8> action)
        => base.Subscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.Unsubscribe"/>
    public void Unsubscribe(Action<T1, T2, T3, T4, T5, T6, T7, T8> action)
        => base.Unsubscribe(action);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeAction"/>
    public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        => base.InvokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

    /// <inheritdoc cref="CallGatePubSubBase.InvokeFunc{TRet}"/>
    public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        => this.InvokeFunc<TRet>(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
}

#pragma warning restore SA1402 // File may only contain a single type
