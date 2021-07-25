using System.Runtime.InteropServices;

using Serilog;

namespace Dalamud.Game.ClientState
{
    /// <summary>
    /// This class converts in-memory Job gauge data to structs.
    /// </summary>
    public class JobGauges
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobGauges"/> class.
        /// </summary>
        /// <param name="addressResolver">Address resolver with the JobGauge memory location(s).</param>
        public JobGauges(ClientStateAddressResolver addressResolver)
        {
            this.Address = addressResolver;

            Log.Verbose($"JobGaugeData address 0x{this.Address.JobGaugeData.ToInt64():X}");
        }

        private ClientStateAddressResolver Address { get; }

        /// <summary>
        /// Get the JobGauge for a given job.
        /// </summary>
        /// <typeparam name="T">A JobGauge struct from ClientState.Structs.JobGauge.</typeparam>
        /// <returns>A JobGauge.</returns>
        public T Get<T>()
        {
            return Marshal.PtrToStructure<T>(this.Address.JobGaugeData);
        }
    }
}
