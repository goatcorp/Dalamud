using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Serilog;

#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1402 // File may only contain a single type

namespace Dalamud.Plugin
{
    /// <summary>
    /// This class facilitates inter-plugin communication.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    public class CallGate
    {
        private Dictionary<string, CallGateBase> gates = new();

        #region GetIpcPubSub

        /// <inheritdoc cref="GetIpcPubSub"/>
        internal CallGatePubSub<TRet> GetIpcPubSub<TRet>(string name)
            => (CallGatePubSub<TRet>)this.GetIpcPubSub(name, typeof(TRet));

        /// <inheritdoc cref="GetIpcPubSub"/>
        internal CallGatePubSub<T1, TRet> GetIpcPubSub<T1, TRet>(string name)
            => (CallGatePubSub<T1, TRet>)this.GetIpcPubSub(name, typeof(T1), typeof(TRet));

        /// <inheritdoc cref="GetIpcPubSub"/>
        internal CallGatePubSub<T1, T2, TRet> GetIpcPubSub<T1, T2, TRet>(string name)
            => (CallGatePubSub<T1, T2, TRet>)this.GetIpcPubSub(name, typeof(T1), typeof(T2), typeof(TRet));

        /// <inheritdoc cref="GetIpcPubSub"/>
        internal CallGatePubSub<T1, T2, T3, TRet> GetIpcPubSub<T1, T2, T3, TRet>(string name)
            => (CallGatePubSub<T1, T2, T3, TRet>)this.GetIpcPubSub(name, typeof(T1), typeof(T2), typeof(T3), typeof(TRet));

        /// <inheritdoc cref="GetIpcPubSub"/>
        internal CallGatePubSub<T1, T2, T3, T4, TRet> GetIpcPubSub<T1, T2, T3, T4, TRet>(string name)
            => (CallGatePubSub<T1, T2, T3, T4, TRet>)this.GetIpcPubSub(name, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(TRet));

        /// <inheritdoc cref="GetIpcPubSub"/>
        internal CallGatePubSub<T1, T2, T3, T4, T5, TRet> GetIpcPubSub<T1, T2, T3, T4, T5, TRet>(string name)
            => (CallGatePubSub<T1, T2, T3, T4, T5, TRet>)this.GetIpcPubSub(name, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(TRet));

        /// <inheritdoc cref="GetIpcPubSub"/>
        internal CallGatePubSub<T1, T2, T3, T4, T5, T6, TRet> GetIpcPubSub<T1, T2, T3, T4, T5, T6, TRet>(string name)
            => (CallGatePubSub<T1, T2, T3, T4, T5, T6, TRet>)this.GetIpcPubSub(name, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(TRet));

        /// <inheritdoc cref="GetIpcPubSub"/>
        internal CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, TRet> GetIpcPubSub<T1, T2, T3, T4, T5, T6, T7, TRet>(string name)
            => (CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, TRet>)this.GetIpcPubSub(name, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(TRet));

        /// <inheritdoc cref="GetIpcPubSub"/>
        internal CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, T8, TRet> GetIpcPubSub<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(string name)
            => (CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, T8, TRet>)this.GetIpcPubSub(name, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(TRet));

        #endregion

        /// <summary>
        /// Gets or sets an IPC pub/sub callgate.
        /// </summary>
        /// <param name="name">Name of the IPC registration.</param>
        /// <param name="types">The callgate parameter types.</param>
        /// <returns>The IPC pub/sub callgate.</returns>
        private CallGateBase GetIpcPubSub(string name, params Type[] types)
        {
            if (!this.gates.TryGetValue(name, out var callGate))
            {
                var generic = types.Length switch
                {
                    1 => typeof(CallGatePubSub<>),
                    2 => typeof(CallGatePubSub<,>),
                    3 => typeof(CallGatePubSub<,,>),
                    4 => typeof(CallGatePubSub<,,,>),
                    5 => typeof(CallGatePubSub<,,,,>),
                    6 => typeof(CallGatePubSub<,,,,,>),
                    7 => typeof(CallGatePubSub<,,,,,,>),
                    8 => typeof(CallGatePubSub<,,,,,,,>),
                    9 => typeof(CallGatePubSub<,,,,,,,,>),
                    _ => throw new Exception("Misconfigured number of type args"),
                };

                var type = generic.MakeGenericType(types);
                callGate = (CallGateBase)Activator.CreateInstance(type);
                callGate.Name = name;

                this.gates[name] = callGate;
            }

            var requested = callGate.GetType().GenericTypeArguments;
            if (!Enumerable.SequenceEqual(requested, types))
                throw new IpcTypeMismatchError(name, requested, types);

            return callGate;
        }
    }

    #region ICallGatePub

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGatePub<TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<TRet> func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage();
    }

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGatePub<T1, TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<T1, TRet> func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage(T1 arg1);
    }

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGatePub<T1, T2, TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<T1, T2, TRet> func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage(T1 arg1, T2 arg2);
    }

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGatePub<T1, T2, T3, TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<T1, T2, T3, TRet> func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage(T1 arg1, T2 arg2, T3 arg3);
    }

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGatePub<T1, T2, T3, T4, TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<T1, T2, T3, T4, TRet> func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    }

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGatePub<T1, T2, T3, T4, T5, TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<T1, T2, T3, T4, T5, TRet> func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    }

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGatePub<T1, T2, T3, T4, T5, T6, TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<T1, T2, T3, T4, T5, T6, TRet> func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    }

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGatePub<T1, T2, T3, T4, T5, T6, T7, TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<T1, T2, T3, T4, T5, T6, T7, TRet> func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    }

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGatePub<T1, T2, T3, T4, T5, T6, T7, T8, TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<T1, T2, T3, T4, T5, T6, T7, T8, TRet> func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
    }

    #endregion

    #region ICallGateSub

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGateSub<TRet>
    {
        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction();

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc();
    }

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGateSub<T1, TRet>
    {
        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction(T1 arg1);

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc(T1 arg1);
    }

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGateSub<T1, T2, TRet>
    {
        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction(T1 arg1, T2 arg2);

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc(T1 arg1, T2 arg2);
    }

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGateSub<T1, T2, T3, TRet>
    {
        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction(T1 arg1, T2 arg2, T3 arg3);

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3);
    }

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGateSub<T1, T2, T3, T4, TRet>
    {
        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4);

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    }

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGateSub<T1, T2, T3, T4, T5, TRet>
    {
        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    }

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGateSub<T1, T2, T3, T4, T5, T6, TRet>
    {
        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    }

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGateSub<T1, T2, T3, T4, T5, T6, T7, TRet>
    {
        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    }

    /// <inheritdoc cref="CallGateBase"/>
    public interface ICallGateSub<T1, T2, T3, T4, T5, T6, T7, T8, TRet>
    {
        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action<T1> action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
    }

    #endregion

    #region CallGatePubSub

    /// <inheritdoc cref="CallGateBase"/>
    internal class CallGatePubSub<TRet> : CallGateBase, ICallGatePub<TRet>, ICallGateSub<TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action action)
            => base.RegisterAction(action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<TRet> func)
            => base.RegisterFunc(func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage()
            => base.SendMessage();

        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action action)
            => base.Subscribe(action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action action)
            => base.Unsubscribe(action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction()
            => base.InvokeAction();

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc()
            => (TRet)base.InvokeFunc();
    }

    /// <inheritdoc cref="CallGateBase"/>
    internal class CallGatePubSub<T1, TRet> : CallGateBase, ICallGatePub<T1, TRet>, ICallGateSub<T1, TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action<T1> action)
            => base.RegisterAction(action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<T1, TRet> func)
            => base.RegisterFunc(func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage(T1 arg1)
            => base.SendMessage(arg1);

        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action<T1> action)
            => base.Subscribe(action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action<T1> action)
            => base.Unsubscribe(action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction(T1 arg1)
            => base.InvokeAction(arg1);

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc(T1 arg1)
            => (TRet)base.InvokeFunc(arg1);
    }

    /// <inheritdoc cref="CallGateBase"/>
    internal class CallGatePubSub<T1, T2, TRet> : CallGateBase, ICallGatePub<T1, T2, TRet>, ICallGateSub<T1, T2, TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action<T1> action)
            => base.RegisterAction(action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<T1, T2, TRet> func)
            => base.RegisterFunc(func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage(T1 arg1, T2 arg2)
            => base.SendMessage(arg1, arg2);

        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action<T1> action)
            => base.Subscribe(action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action<T1> action)
            => base.Unsubscribe(action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction(T1 arg1, T2 arg2)
            => base.InvokeAction(arg1, arg2);

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc(T1 arg1, T2 arg2)
            => (TRet)base.InvokeFunc(arg1, arg2);
    }

    /// <inheritdoc cref="CallGateBase"/>
    internal class CallGatePubSub<T1, T2, T3, TRet> : CallGateBase, ICallGatePub<T1, T2, T3, TRet>, ICallGateSub<T1, T2, T3, TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action<T1> action)
            => base.RegisterAction(action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<T1, T2, T3, TRet> func)
            => base.RegisterFunc(func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage(T1 arg1, T2 arg2, T3 arg3)
            => base.SendMessage(arg1, arg2, arg3);

        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action<T1> action)
            => base.Subscribe(action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action<T1> action)
            => base.Unsubscribe(action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction(T1 arg1, T2 arg2, T3 arg3)
            => base.InvokeAction(arg1, arg2, arg3);

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3)
            => (TRet)base.InvokeFunc(arg1, arg2, arg3);
    }

    /// <inheritdoc cref="CallGateBase"/>
    internal class CallGatePubSub<T1, T2, T3, T4, TRet> : CallGateBase, ICallGatePub<T1, T2, T3, T4, TRet>, ICallGateSub<T1, T2, T3, T4, TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action<T1> action)
            => base.RegisterAction(action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<T1, T2, T3, T4, TRet> func)
            => base.RegisterFunc(func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => base.SendMessage(arg1, arg2, arg3, arg4);

        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action<T1> action)
            => base.Subscribe(action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action<T1> action)
            => base.Unsubscribe(action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => base.InvokeAction(arg1, arg2, arg3, arg4);

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => (TRet)base.InvokeFunc(arg1, arg2, arg3, arg4);
    }

    /// <inheritdoc cref="CallGateBase"/>
    internal class CallGatePubSub<T1, T2, T3, T4, T5, TRet> : CallGateBase, ICallGatePub<T1, T2, T3, T4, T5, TRet>, ICallGateSub<T1, T2, T3, T4, T5, TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action<T1> action)
            => base.RegisterAction(action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<T1, T2, T3, T4, T5, TRet> func)
            => base.RegisterFunc(func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => base.SendMessage(arg1, arg2, arg3, arg4, arg5);

        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action<T1> action)
            => base.Subscribe(action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action<T1> action)
            => base.Unsubscribe(action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => base.InvokeAction(arg1, arg2, arg3, arg4, arg5);

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => (TRet)base.InvokeFunc(arg1, arg2, arg3, arg4, arg5);
    }

    /// <inheritdoc cref="CallGateBase"/>
    internal class CallGatePubSub<T1, T2, T3, T4, T5, T6, TRet> : CallGateBase, ICallGatePub<T1, T2, T3, T4, T5, T6, TRet>, ICallGateSub<T1, T2, T3, T4, T5, T6, TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action<T1> action)
            => base.RegisterAction(action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<T1, T2, T3, T4, T5, T6, TRet> func)
            => base.RegisterFunc(func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => base.SendMessage(arg1, arg2, arg3, arg4, arg5, arg6);

        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action<T1> action)
            => base.Subscribe(action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action<T1> action)
            => base.Unsubscribe(action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => base.InvokeAction(arg1, arg2, arg3, arg4, arg5, arg6);

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => (TRet)base.InvokeFunc(arg1, arg2, arg3, arg4, arg5, arg6);
    }

    /// <inheritdoc cref="CallGateBase"/>
    internal class CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, TRet> : CallGateBase, ICallGatePub<T1, T2, T3, T4, T5, T6, T7, TRet>, ICallGateSub<T1, T2, T3, T4, T5, T6, T7, TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action<T1> action)
            => base.RegisterAction(action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<T1, T2, T3, T4, T5, T6, T7, TRet> func)
            => base.RegisterFunc(func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
            => base.SendMessage(arg1, arg2, arg3, arg4, arg5, arg6, arg7);

        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action<T1> action)
            => base.Subscribe(action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action<T1> action)
            => base.Unsubscribe(action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
            => base.InvokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7);

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
            => (TRet)base.InvokeFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }

    /// <inheritdoc cref="CallGateBase"/>
    internal class CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, T8, TRet> : CallGateBase, ICallGatePub<T1, T2, T3, T4, T5, T6, T7, T8, TRet>, ICallGateSub<T1, T2, T3, T4, T5, T6, T7, T8, TRet>
    {
        /// <inheritdoc cref="CallGateBase.RegisterAction"/>
        public void RegisterAction(Action<T1> action)
            => base.RegisterAction(action);

        /// <inheritdoc cref="CallGateBase.RegisterFunc"/>
        public void RegisterFunc(Func<T1, T2, T3, T4, T5, T6, T7, T8, TRet> func)
            => base.RegisterFunc(func);

        /// <inheritdoc cref="CallGateBase.SendMessage"/>
        public void SendMessage(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
            => base.SendMessage(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

        /// <inheritdoc cref="CallGateBase.Subscribe"/>
        public void Subscribe(Action<T1> action)
            => base.Subscribe(action);

        /// <inheritdoc cref="CallGateBase.Unsubscribe"/>
        public void Unsubscribe(Action<T1> action)
            => base.Unsubscribe(action);

        /// <inheritdoc cref="CallGateBase.InvokeAction"/>
        public void InvokeAction(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
            => base.InvokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

        /// <inheritdoc cref="CallGateBase.InvokeFunc"/>
        public TRet InvokeFunc(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
            => (TRet)base.InvokeFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }

    #endregion

    /// <summary>
    /// This class facilitates inter-plugin communication.
    /// </summary>
    internal class CallGateBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallGateBase"/> class.
        /// </summary>
        internal CallGateBase()
        {
        }

        /// <summary>
        /// Gets or sets the name of the IPC registration.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        protected Delegate? Action { get; set; }

        /// <summary>
        /// Gets or sets the Func.
        /// </summary>
        protected Delegate? Func { get; set; }

        /// <summary>
        /// Gets the list of subscribed delegates.
        /// </summary>
        protected List<Delegate> Subs { get; } = new();

        /// <summary>
        /// Removes a registered Action from inter-plugin communication.
        /// </summary>
        public void UnregisterAction()
            => this.Action = null;

        /// <summary>
        /// Removes a registered Func from inter-plugin communication.
        /// </summary>
        public void UnregisterFunc()
            => this.Func = null;

        /// <summary>
        /// Removes a registered Action from inter-plugin communication.
        /// </summary>
        /// <param name="action">Action to register.</param>
        private protected void RegisterAction(Delegate action)
            => this.Action = action;

        /// <summary>
        /// Removes a registered Func from inter-plugin communication.
        /// </summary>
        /// <param name="func">Func to register.</param>
        private protected void RegisterFunc(Delegate func)
            => this.Func = func;

        /// <summary>
        /// Invoke all actions that have subscribed to this IPC.
        /// </summary>
        /// <param name="args">Delegate arguments.</param>
        private protected void SendMessage(params object?[]? args)
        {
            foreach (var sub in this.Subs)
            {
                try
                {
                    sub.DynamicInvoke(args);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error invoking a subscription of {this.Name}");
                }
            }
        }

        /// <summary>
        /// Subscribe an expression to this registration.
        /// </summary>
        /// <param name="action">Action to subscribe.</param>
        private protected void Subscribe(Delegate action)
            => this.Subs.Add(action);

        /// <summary>
        /// Unsubscribe an expression from this registration.
        /// </summary>
        /// <param name="action">Action to unsubscribe.</param>
        private protected void Unsubscribe(Delegate action)
            => this.Subs.Remove(action);

        /// <summary>
        /// Invoke an action registered for inter-plugin communication.
        /// </summary>
        /// <param name="args">Action arguments.</param>
        /// <exception cref="IpcNotReadyError">This is thrown when the IPC publisher has not registered an action for calling yet.</exception>
        private protected void InvokeAction(params object?[]? args)
            => (this.Action ?? throw new IpcNotReadyError(this.Name)).DynamicInvoke(args);

        /// <summary>
        /// Invoke a function registered for inter-plugin communication.
        /// </summary>
        /// <param name="args">Parameter args.</param>
        /// <returns>The return value.</returns>
        /// <exception cref="IpcNotReadyError">This is thrown when the IPC publisher has not registered a func for calling yet.</exception>
        private protected object InvokeFunc(params object?[]? args)
            => (this.Func ?? throw new IpcNotReadyError(this.Name)).DynamicInvoke(args);
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1201 // Elements should appear in the correct order
