using System.Collections.Generic;

namespace Dalamud.Plugin.Ipc.Internal
{
    /// <summary>
    /// This class facilitates inter-plugin communication.
    /// </summary>
    internal class CallGate : IEarlyLoadableServiceObject
    {
        private readonly Dictionary<string, CallGateChannel> gates = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="CallGate"/> class.
        /// </summary>
        /// <param name="tag">Tag.</param>
        internal CallGate(ServiceManager.Tag tag)
        {
        }

        /// <summary>
        /// Gets the provider associated with the specified name.
        /// </summary>
        /// <param name="name">Name of the IPC registration.</param>
        /// <returns>A CallGate registered under the given name.</returns>
        public CallGateChannel GetOrCreateChannel(string name)
        {
            if (!this.gates.TryGetValue(name, out var gate))
                gate = this.gates[name] = new CallGateChannel(name);
            return gate;
        }
    }
}
