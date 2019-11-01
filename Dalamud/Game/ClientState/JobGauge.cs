using System.Runtime.InteropServices;
using Serilog;

namespace Dalamud.Game.ClientState {
    public class JobGauges {
        private ClientStateAddressResolver Address { get; }

        public JobGauges(ClientStateAddressResolver addressResolver) {
            Address = addressResolver;

            Log.Verbose("JobGaugeData address {JobGaugeData}", Address.ActorTable);
        }

        // Should only be called with the gauge types in 
        // ClientState.Structs.JobGauge
        public T Get<T>() {
            return Marshal.PtrToStructure<T>(Address.JobGaugeData);
        }
    }
}
