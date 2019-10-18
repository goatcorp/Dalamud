using Dalamud.Game.ClientState.Structs.JobGauge;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState {
    public class JobGauges {
        private ClientStateAddressResolver Address { get; }

        public JobGauges(ClientStateAddressResolver addressResolver) {
            Address = addressResolver;
        }

        // Should only be called with the gauge types in 
        // ClientState.Structs.JobGauge
        public T Get<T>() {
            return Marshal.PtrToStructure<T>(Address.ActorTable);
        }
    }
}
