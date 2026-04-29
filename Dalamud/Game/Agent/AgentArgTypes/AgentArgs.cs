using Dalamud.Game.NativeWrapper;

namespace Dalamud.Game.Agent.AgentArgTypes;

/// <summary>
/// Base class for AgentLifecycle AgentArgTypes.
/// </summary>
public unsafe class AgentArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentArgs"/> class.
    /// </summary>
    internal AgentArgs()
    {
    }

    /// <summary>
    /// Gets the pointer to the Agents AgentInterface*.
    /// </summary>
    public AgentInterfacePtr Agent { get; internal set; }

    /// <summary>
    /// Gets the agent id.
    /// </summary>
    public AgentId AgentId { get; internal set; }

    /// <summary>
    /// Gets the type of these args.
    /// </summary>
    public virtual AgentArgsType Type => AgentArgsType.Generic;

    /// <summary>
    /// Gets or sets a value indicating whether original is being requested to be skipped.
    /// </summary>
    internal bool PreventOriginalRequested { get; set; }

    /// <summary>
    /// Gets the typed pointer to the Agents AgentInterface*.
    /// </summary>
    /// <typeparam name="T">AgentInterface.</typeparam>
    /// <returns>Typed pointer to contained Agents AgentInterface.</returns>
    public T* GetAgentPointer<T>() where T : unmanaged
        => (T*)this.Agent.Address;

    /// <summary>
    /// Request that the call to original is skipped.
    /// Only valid to be called from a Pre event listener not a Post event listener.
    /// </summary>
    public void PreventOriginal() => this.PreventOriginalRequested = true;
}
