using System.Reactive.Disposables;
using System.Threading;

using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Ipc.Exceptions;
using Dalamud.Utility;

using Serilog;

namespace Dalamud.Plugin.Ipc.Internal;

/// <summary>
/// This class facilitates inter-plugin communication.
/// </summary>
internal abstract class CallGatePubSubBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CallGatePubSubBase"/> class.
    /// </summary>
    /// <param name="name">The name of the IPC registration.</param>
    /// <param name="owningPlugin">The plugin that owns this IPC pubsub.</param>
    protected CallGatePubSubBase(string name, LocalPlugin? owningPlugin)
    {
        this.Channel = Service<CallGate>.Get().GetOrCreateChannel(name);
        this.OwningPlugin = owningPlugin;
    }

    /// <summary>
    /// Gets a value indicating whether this IPC call gate has an associated Action. Only exposed to
    /// <see cref="ICallGateSubscriber"/>s.
    /// </summary>
    public bool HasAction => this.Channel.Action != null;

    /// <summary>
    /// Gets a value indicating whether this IPC call gate has an associated Function. Only exposed to
    /// <see cref="ICallGateSubscriber"/>s.
    /// </summary>
    public bool HasFunction => this.Channel.Func != null;

    /// <summary>
    /// Gets the count of subscribers listening for messages through this call gate. Only exposed to
    /// <see cref="ICallGateProvider"/>s, and can be used to determine if messages should be sent through the gate.
    /// </summary>
    public int SubscriptionCount => this.Channel.Subscriptions.Count;

    /// <summary>
    /// Gets the underlying channel implementation.
    /// </summary>
    protected CallGateChannel Channel { get; init; }

    /// <summary>
    /// Gets the plugin that owns this pubsub instance.
    /// </summary>
    protected LocalPlugin? OwningPlugin { get; init; }

    /// <summary>
    /// Removes the associated Action from this call gate, effectively disabling RPC calls.
    /// </summary>
    /// <seealso cref="RegisterAction"/>
    public void UnregisterAction()
        => this.Channel.Action = null;

    /// <summary>
    /// Removes the associated Function from this call gate.
    /// </summary>
    /// <seealso cref="RegisterFunc"/>
    public void UnregisterFunc()
        => this.Channel.Func = null;

    /// <summary>
    /// Gets the current context for this IPC call. This will only be present when called from within an IPC action
    /// or function handler, and will be null otherwise.
    /// </summary>
    /// <returns>Returns a potential IPC context.</returns>
    public IpcContext? GetContext()
    {
        return this.Channel.GetInvocationContext();
    }

    /// <summary>
    /// Registers a <see cref="Delegate"/> for use by other plugins via RPC. This Delegate must satisfy the constraints
    /// of an <see cref="Action"/> type as defined by the interface, meaning they may not return a value and must have
    /// the proper number of parameters.
    /// </summary>
    /// <param name="action">Action to register.</param>
    /// <seealso cref="UnregisterAction"/>
    /// <seealso cref="InvokeAction"/>
    private protected void RegisterAction(Delegate action)
        => this.Channel.Action = action;

    /// <summary>
    /// Registers a <see cref="Delegate"/> for use by other plugins via RPC. This Delegate must satisfy the constraints
    /// of a <see cref="Func{TResult}"/> type as defined by the interface, meaning its return type and parameters must
    /// match accordingly.
    /// </summary>
    /// <param name="func">Func to register.</param>
    /// <seealso cref="UnregisterFunc"/>
    /// <seealso cref="InvokeFunc{TRet}"/>
    private protected void RegisterFunc(Delegate func)
        => this.Channel.Func = func;

    /// <summary>
    /// Registers a <see cref="Delegate"/> (of type <see cref="Action{T}"/>) that will be called when the providing
    /// plugin calls <see cref="ICallGateProvider{T1}.SendMessage"/>. This method can be used to receive notifications
    /// of events or data updates from a specific plugin.
    /// </summary>
    /// <param name="action">Action to subscribe.</param>
    /// <seealso cref="Unsubscribe"/>
    private protected void Subscribe(Delegate action)
        => this.Channel.Subscribe(action);

    /// <summary>
    /// Removes a subscription created through <see cref="Subscribe"/>. Note that the <see cref="Delegate"/> to be
    /// unsubscribed must be the same instance as the one passed in.
    /// </summary>
    /// <param name="action">Action to unsubscribe.</param>
    /// <seealso cref="Subscribe"/>
    private protected void Unsubscribe(Delegate action)
        => this.Channel.Unsubscribe(action);

    /// <summary>
    /// Executes the Action registered for this IPC call gate via <see cref="RegisterAction"/>. This method is intended
    /// to be called by plugins wishing to access another plugin via RPC. The parameters passed to this method will be
    /// passed to the owning plugin, with appropriate serialization for complex data types. Primitive data types will
    /// be passed as-is. The target Action will be called on the <em>same thread</em> as the caller.
    /// </summary>
    /// <param name="args">Action arguments.</param>
    /// <exception cref="IpcNotReadyError">This is thrown when the IPC publisher has not registered an action for calling yet.</exception>
    /// <seealso cref="RegisterAction"/>
    /// <seealso cref="UnregisterAction"/>
    private protected void InvokeAction(params object?[]? args)
    {
        using (this.BuildContext())
        {
            this.Channel.InvokeAction(args);
        }
    }

    /// <summary>
    /// Executes the Function registered for this IPC call gate via <see cref="RegisterFunc"/>. This method is intended
    /// to be called by plugins wishing to access another plugin via RPC. The parameters passed to this method will be
    /// passed to the owning plugin, with appropriate serialization for complex data types. Primitive data types will
    /// be passed as-is. The target Action will be called on the <em>same thread</em> as the caller.
    /// </summary>
    /// <param name="args">Parameter args.</param>
    /// <returns>The return value.</returns>
    /// <typeparam name="TRet">The return type.</typeparam>
    /// <exception cref="IpcNotReadyError">This is thrown when the IPC publisher has not registered a func for calling yet.</exception>
    /// <seealso cref="RegisterFunc"/>
    /// <seealso cref="UnregisterFunc"/>
    private protected TRet InvokeFunc<TRet>(params object?[]? args)
    {
        using (this.BuildContext())
        {
            return this.Channel.InvokeFunc<TRet>(args);
        }
    }

    /// <summary>
    /// Send the given arguments to all subscribers (through <see cref="Subscribe"/>) of this IPC call gate. This method
    /// is intended to be used by the provider plugin to notify all subscribers of an event or data update. The
    /// parameters passed to this method will be passed to all subscribers, with appropriate serialization for complex
    /// data types. Primitive data types will be passed as-is. The subscription actions will be called sequentially in
    /// order of registration on the <em>same thread</em> as the caller.
    /// </summary>
    /// <param name="args">Delegate arguments.</param>
    private protected void SendMessage(params object?[]? args)
        => this.Channel.SendMessage(args);

    private IDisposable BuildContext()
    {
        this.Channel.SetInvocationContext(new IpcContext
        {
            SourcePlugin = this.OwningPlugin != null ? new ExposedPlugin(this.OwningPlugin) : null,
        });

        return Disposable.Create(() => { this.Channel.ClearInvocationContext(); });
    }
}
