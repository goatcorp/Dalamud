using System.Collections.Generic;

namespace Dalamud.Plugin.Ipc.Internal;

/// <summary>
/// This class facilitates inter-plugin communication.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class CallGate : IServiceType
{
    private readonly Dictionary<string, CallGateChannel> gates = new();

    [ServiceManager.ServiceConstructor]
    private CallGate()
    {
    }

    /// <summary>
    /// Gets the provider associated with the specified name.
    /// </summary>
    /// <param name="name">Name of the IPC registration.</param>
    /// <returns>A CallGate registered under the given name.</returns>
    public CallGateChannel GetOrCreateChannel(string name)
    {
        lock (this.gates)
        {
            if (!this.gates.TryGetValue(name, out var gate))
                gate = this.gates[name] = new(name);
            return gate;
        }
    }
}
