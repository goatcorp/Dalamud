using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Dalamud.Plugin.Ipc.Exceptions;
using Newtonsoft.Json;

namespace Dalamud.Plugin.Ipc.Internal
{
    /// <summary>
    /// This class implements the channels and serialization needed for the typed gates to interact with each other.
    /// </summary>
    internal class CallGateChannel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallGateChannel"/> class.
        /// </summary>
        /// <param name="name">The name of this IPC registration.</param>
        public CallGateChannel(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets the name of the IPC registration.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Gets a list of delegate subscriptions for when SendMessage is called.
        /// </summary>
        public List<Delegate> Subscriptions { get; } = new();

        /// <summary>
        /// Gets or sets an action for when InvokeAction is called.
        /// </summary>
        public Delegate Action { get; set; }

        /// <summary>
        /// Gets or sets a func for when InvokeFunc is called.
        /// </summary>
        public Delegate Func { get; set; }

        /// <summary>
        /// Invoke all actions that have subscribed to this IPC.
        /// </summary>
        /// <param name="args">Message arguments.</param>
        internal void SendMessage(object?[]? args)
        {
            if (this.Subscriptions.Count == 0)
                return;

            foreach (var subscription in this.Subscriptions)
            {
                var methodInfo = subscription.GetMethodInfo();
                this.CheckAndConvertArgs(args, methodInfo);

                subscription.DynamicInvoke(args);
            }
        }

        /// <summary>
        /// Invoke an action registered for inter-plugin communication.
        /// </summary>
        /// <param name="args">Action arguments.</param>
        /// <exception cref="IpcNotReadyError">This is thrown when the IPC publisher has not registered a func for calling yet.</exception>
        internal void InvokeAction(object?[]? args)
        {
            if (this.Action == null)
                throw new IpcNotReadyError(this.Name);

            var methodInfo = this.Action.GetMethodInfo();
            this.CheckAndConvertArgs(args, methodInfo);

            this.Action.DynamicInvoke(args);
        }

        /// <summary>
        /// Invoke a function registered for inter-plugin communication.
        /// </summary>
        /// <param name="args">Func arguments.</param>
        /// <returns>The return value.</returns>
        /// <typeparam name="TRet">The return type.</typeparam>
        /// <exception cref="IpcNotReadyError">This is thrown when the IPC publisher has not registered a func for calling yet.</exception>
        internal TRet InvokeFunc<TRet>(object?[]? args)
        {
            if (this.Func == null)
                throw new IpcNotReadyError(this.Name);

            var methodInfo = this.Func.GetMethodInfo();
            this.CheckAndConvertArgs(args, methodInfo);

            var result = this.Func.DynamicInvoke(args);

            if (typeof(TRet) != methodInfo.ReturnType)
                result = this.ConvertObject(result, typeof(TRet));

            return (TRet)result;
        }

        private void CheckAndConvertArgs(object?[]? args, MethodInfo methodInfo)
        {
            var paramTypes = methodInfo.GetParameters()
                .Select(pi => pi.ParameterType).ToArray();

            if (args.Length != paramTypes.Length)
                throw new IpcLengthMismatchError(this.Name, args.Length, paramTypes.Length);

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                var paramType = paramTypes[i];
                if (arg.GetType() != paramType)
                    args[i] = this.ConvertObject(arg, paramType);
            }
        }

        private object? ConvertObject(object? obj, Type type)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    TypeNameHandling = TypeNameHandling.Objects,
                };

                var json = JsonConvert.SerializeObject(obj, settings);
                return JsonConvert.DeserializeObject(json, type, settings);
            }
            catch (Exception ex)
            {
                throw new IpcTypeMismatchError(this.Name, obj.GetType(), type, ex);
            }
        }
    }
}
