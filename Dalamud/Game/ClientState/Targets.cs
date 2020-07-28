using System;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors.Types;

namespace Dalamud.Game.ClientState {
    public sealed class Targets {
        private ClientStateAddressResolver Address { get; }
        private Dalamud dalamud;

        public Actor CurrentTarget => GetActorByOffset(0x80);
        public Actor MouseOverTarget => GetActorByOffset(0xD0);
        public Actor FocusTarget => GetActorByOffset(0xF8);
        public Actor PreviousTarget => GetActorByOffset(0x110);
        
        internal Targets(Dalamud dalamud, ClientStateAddressResolver addressResolver) {
            this.dalamud = dalamud;
            Address = addressResolver;
        }

        private Actor GetActorByOffset(int offset) {
            if (Address.TargetManager == IntPtr.Zero) return null;
            var actorAddress = Marshal.ReadIntPtr(Address.TargetManager + offset);
            if (actorAddress == IntPtr.Zero) return null;
            var data = Marshal.PtrToStructure<Structs.Actor>(actorAddress);
            return new Actor(actorAddress, data, this.dalamud);
        }
    }
}
