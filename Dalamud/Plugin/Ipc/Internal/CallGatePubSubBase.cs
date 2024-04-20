using Dalamud.Plugin.Ipc.Exceptions;

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
    protected CallGatePubSubBase(string name)
    {
        this.Channel = Service<CallGate>.Get().GetOrCreateChannel(name);
    }

    /// <summary>
    /// Gets the underlying channel implementation.
    /// </summary>
    protected CallGateChannel Channel { get; init; }

    /// <summary>
    /// Removes a registered Action from inter-plugin communication.
    /// </summary>
    public void UnregisterAction()
        => this.Channel.Action = null;

    /// <summary>
    /// Removes a registered Func from inter-plugin communication.
    /// </summary>
    public void UnregisterFunc()
        => this.Channel.Func = null;

    /// <summary>
    /// Registers an Action for inter-plugin communication.
    /// </summary>
    /// <param name="action">Action to register.</param>
    private protected void RegisterAction(Delegate action)
        => this.Channel.Action = action;

    /// <summary>
    /// Registers a Func for inter-plugin communication.
    /// </summary>
    /// <param name="func">Func to register.</param>
    private protected void RegisterFunc(Delegate func)
        => this.Channel.Func = func;

    /// <summary>
    /// Subscribe an expression to this registration.
    /// </summary>
    /// <param name="action">Action to subscribe.</param>
    private protected void Subscribe(Delegate action)
        => this.Channel.Subscribe(action);

    /// <summary>
    /// Unsubscribe an expression from this registration.
    /// </summary>
    /// <param name="action">Action to unsubscribe.</param>
    private protected void Unsubscribe(Delegate action)
        => this.Channel.Unsubscribe(action);

    /// <summary>
    /// Invoke an action registered for inter-plugin communication.
    /// </summary>
    /// <param name="args">Action arguments.</param>
    /// <exception cref="IpcNotReadyError">This is thrown when the IPC publisher has not registered an action for calling yet.</exception>
    private protected void InvokeAction(params object?[]? args)
        => this.Channel.InvokeAction(args);

    /// <summary>
    /// Invoke a function registered for inter-plugin communication.
    /// </summary>
    /// <param name="args">Parameter args.</param>
    /// <returns>The return value.</returns>
    /// <typeparam name="TRet">The return type.</typeparam>
    /// <exception cref="IpcNotReadyError">This is thrown when the IPC publisher has not registered a func for calling yet.</exception>
    private protected TRet InvokeFunc<TRet>(params object?[]? args)
        => this.Channel.InvokeFunc<TRet>(args);

    /// <summary>
    /// Invoke all actions that have subscribed to this IPC.
    /// </summary>
    /// <param name="args">Delegate arguments.</param>
    private protected void SendMessage(params object?[]? args)
        => this.Channel.SendMessage(args);
}
