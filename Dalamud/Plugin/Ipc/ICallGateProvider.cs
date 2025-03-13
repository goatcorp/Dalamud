using Dalamud.Plugin.Ipc.Internal;
using Dalamud.Utility;

#pragma warning disable SA1402 // File may only contain a single type

namespace Dalamud.Plugin.Ipc;

/// <summary>
/// The backing interface for the provider ("server") half of an IPC channel. This interface is used to expose methods
/// to other plugins via RPC, as well as to allow other plugins to subscribe to notifications from this plugin.
/// </summary>
public interface ICallGateProvider
{
    /// <inheritdoc cref="CallGatePubSubBase.SubscriptionCount"/>
    public int SubscriptionCount { get; }

    /// <inheritdoc cref="CallGatePubSubBase.UnregisterAction"/>
    public void UnregisterAction();

    /// <inheritdoc cref="CallGatePubSubBase.UnregisterFunc"/>
    public void UnregisterFunc();
}

/// <inheritdoc cref="ICallGateProvider"/>
public interface ICallGateProvider<TRet> : ICallGateProvider
{
    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<TRet> func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage();
}

/// <inheritdoc cref="ICallGateProvider"/>
public interface ICallGateProvider<T1, TRet> : ICallGateProvider
{
    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action<T1> action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<T1, TRet> func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage(T1 arg1);
}

/// <inheritdoc cref="ICallGateProvider"/>
public interface ICallGateProvider<T1, T2, TRet> : ICallGateProvider
{
    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action<T1, T2> action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<T1, T2, TRet> func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage(T1 arg1, T2 arg2);
}

/// <inheritdoc cref="ICallGateProvider"/>
public interface ICallGateProvider<T1, T2, T3, TRet> : ICallGateProvider
{
    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action<T1, T2, T3> action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<T1, T2, T3, TRet> func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage(T1 arg1, T2 arg2, T3 arg3);
}

/// <inheritdoc cref="ICallGateProvider"/>
public interface ICallGateProvider<T1, T2, T3, T4, TRet> : ICallGateProvider
{
    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action<T1, T2, T3, T4> action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<T1, T2, T3, T4, TRet> func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
}

/// <inheritdoc cref="ICallGateProvider"/>
public interface ICallGateProvider<T1, T2, T3, T4, T5, TRet> : ICallGateProvider
{
    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action<T1, T2, T3, T4, T5> action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<T1, T2, T3, T4, T5, TRet> func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
}

/// <inheritdoc cref="ICallGateProvider"/>
public interface ICallGateProvider<T1, T2, T3, T4, T5, T6, TRet> : ICallGateProvider
{
    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action<T1, T2, T3, T4, T5, T6> action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<T1, T2, T3, T4, T5, T6, TRet> func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
}

/// <inheritdoc cref="ICallGateProvider"/>
public interface ICallGateProvider<T1, T2, T3, T4, T5, T6, T7, TRet> : ICallGateProvider
{
    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action<T1, T2, T3, T4, T5, T6, T7> action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<T1, T2, T3, T4, T5, T6, T7, TRet> func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
}

/// <inheritdoc cref="ICallGateProvider"/>
public interface ICallGateProvider<T1, T2, T3, T4, T5, T6, T7, T8, TRet> : ICallGateProvider
{
    /// <inheritdoc cref="CallGatePubSubBase.RegisterAction"/>
    public void RegisterAction(Action<T1, T2, T3, T4, T5, T6, T7, T8> action);

    /// <inheritdoc cref="CallGatePubSubBase.RegisterFunc"/>
    public void RegisterFunc(Func<T1, T2, T3, T4, T5, T6, T7, T8, TRet> func);

    /// <inheritdoc cref="CallGatePubSubBase.SendMessage"/>
    public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
}

#pragma warning restore SA1402 // File may only contain a single type
