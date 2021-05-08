using System.Runtime.InteropServices;

using Serilog;

namespace Dalamud.Game.ClientState
{
    public class JobGauges
    {
        private ClientStateAddressResolver Address { get; }

        public JobGauges(ClientStateAddressResolver addressResolver)
        {
            this.Address = addressResolver;

            Log.Verbose("JobGaugeData address {JobGaugeData}", this.Address.JobGaugeData);
        }

        // Should only be called with the gauge types in ClientState.Structs.JobGauge
        public T Get<T>()
        {
            return Marshal.PtrToStructure<T>(this.Address.JobGaugeData);
        }
    }
}
