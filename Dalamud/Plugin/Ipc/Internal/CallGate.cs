using System.Collections.Generic;
using System.Collections.Immutable;

namespace Dalamud.Plugin.Ipc.Internal;

/// <summary>
/// This class facilitates inter-plugin communication.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class CallGate : IServiceType
{
    private readonly Dictionary<string, CallGateChannel> gates = new();

    private ImmutableDictionary<string, CallGateChannel>? gatesCopy;

    [ServiceManager.ServiceConstructor]
    private CallGate()
    {
    }

    /// <summary>
    /// Gets the thread-safe view of the registered gates.
    /// </summary>
    public IReadOnlyDictionary<string, CallGateChannel> Gates
    {
        get
        {
            var copy = this.gatesCopy;
            if (copy is not null)
                return copy;
            lock (this.gates)
                return this.gatesCopy ??= this.gates.ToImmutableDictionary(x => x.Key, x => x.Value);
        }
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
            {
                gate = this.gates[name] = new(name);
                this.gatesCopy = null;
            }

            return gate;
        }
    }

    /// <summary>
    /// Remove empty gates from <see cref="Gates"/>.
    /// </summary>
    public void PurgeEmptyGates()
    {
        lock (this.gates)
        {
            var changed = false;
            foreach (var (k, v) in this.Gates)
            {
                if (v.IsEmpty)
                    changed |= this.gates.Remove(k);
            }

            if (changed)
                this.gatesCopy = null;
        }
    }
}
