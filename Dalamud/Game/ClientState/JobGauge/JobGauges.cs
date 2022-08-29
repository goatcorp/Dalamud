using System;
using System.Collections.Generic;
using System.Reflection;

using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Serilog;

namespace Dalamud.Game.ClientState.JobGauge
{
    /// <summary>
    /// This class converts in-memory Job gauge data to structs.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    [ServiceManager.BlockingEarlyLoadedService]
    public class JobGauges : IServiceType
    {
        private Dictionary<Type, JobGaugeBase> cache = new();

        [ServiceManager.ServiceConstructor]
        private JobGauges(ClientState clientState)
        {
            this.Address = clientState.AddressResolver.JobGaugeData;

            Log.Verbose($"JobGaugeData address 0x{this.Address.ToInt64():X}");
        }

        /// <summary>
        /// Gets the address of the JobGauge data.
        /// </summary>
        public IntPtr Address { get; }

        /// <summary>
        /// Get the JobGauge for a given job.
        /// </summary>
        /// <typeparam name="T">A JobGauge struct from ClientState.Structs.JobGauge.</typeparam>
        /// <returns>A JobGauge.</returns>
        public T Get<T>() where T : JobGaugeBase
        {
            // This is cached to mitigate the effects of using activator for instantiation.
            // Since the gauge itself reads from live memory, there isn't much downside to doing this.
            if (!this.cache.TryGetValue(typeof(T), out var gauge))
            {
                gauge = this.cache[typeof(T)] = (T)Activator.CreateInstance(typeof(T), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { this.Address }, null);
            }

            return (T)gauge;
        }
    }
}
