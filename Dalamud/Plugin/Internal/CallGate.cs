using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalamud.Plugin.Internal
{
    /// <summary>
    /// This class facilitates inter-plugin communication.
    /// </summary>
    internal class CallGate
    {
        private Dictionary<string, CallGatePubSubBase> gates = new();

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
        private CallGatePubSubBase GetIpcPubSub(string name, params Type[] types)
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
                callGate = (CallGatePubSubBase)Activator.CreateInstance(type);
                callGate.Name = name;

                this.gates[name] = callGate;
            }

            var requested = callGate.GetType().GenericTypeArguments;
            if (!Enumerable.SequenceEqual(requested, types))
                throw new IpcTypeMismatchError(name, requested, types);

            return callGate;
        }
    }
}
