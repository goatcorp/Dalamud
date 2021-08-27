using System;
using System.Collections.Generic;

using Serilog;

namespace Dalamud.Plugin.Internal
{
    /// <summary>
    /// This class facilitates inter-plugin communication.
    /// </summary>
    internal abstract class CallGatePubSubBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallGatePubSubBase"/> class.
        /// </summary>
        internal CallGatePubSubBase()
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
        /// Registers an Action for inter-plugin communication.
        /// </summary>
        /// <param name="action">Action to register.</param>
        private protected void RegisterAction(Delegate action)
            => this.Action = action;

        /// <summary>
        /// Registers a Func for inter-plugin communication.
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
