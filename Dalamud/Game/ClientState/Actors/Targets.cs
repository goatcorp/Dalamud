using System;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors.Types;

namespace Dalamud.Game.ClientState.Actors {
    public static class TargetOffsets {
        public const int CurrentTarget = 0x80;
        public const int MouseOverTarget = 0xD0;
        public const int FocusTarget = 0xF8;
        public const int PreviousTarget = 0x110;
        public const int SoftTarget = 0x88;
    }

    public sealed class Targets {
        private ClientStateAddressResolver Address { get; }
        private Dalamud dalamud;

        public Actor CurrentTarget => GetActorByOffset(TargetOffsets.CurrentTarget);
        public Actor MouseOverTarget => GetActorByOffset(TargetOffsets.MouseOverTarget);
        public Actor FocusTarget => GetActorByOffset(TargetOffsets.FocusTarget);
        public Actor PreviousTarget => GetActorByOffset(TargetOffsets.PreviousTarget);
        public Actor SoftTarget => GetActorByOffset(TargetOffsets.SoftTarget);

        internal Targets(Dalamud dalamud, ClientStateAddressResolver addressResolver) {
            this.dalamud = dalamud;
            Address = addressResolver;
        }

        public void SetCurrentTarget(Actor actor) => SetTarget(actor?.Address ?? IntPtr.Zero, TargetOffsets.CurrentTarget);
        public void SetCurrentTarget(IntPtr actorAddress) => SetTarget(actorAddress, TargetOffsets.CurrentTarget);

        public void SetFocusTarget(Actor actor) => SetTarget(actor?.Address ?? IntPtr.Zero, TargetOffsets.FocusTarget);
        public void SetFocusTarget(IntPtr actorAddress) => SetTarget(actorAddress, TargetOffsets.FocusTarget);

        public void ClearCurrentTarget() => SetCurrentTarget(IntPtr.Zero);
        public void ClearFocusTarget() => SetFocusTarget(IntPtr.Zero);

        private void SetTarget(IntPtr actorAddress, int offset) {
            if (Address.TargetManager == IntPtr.Zero) return;
            Marshal.WriteIntPtr(Address.TargetManager, offset, actorAddress);
        }
        
        private Actor GetActorByOffset(int offset) {
            if (Address.TargetManager == IntPtr.Zero) return null;
            var actorAddress = Marshal.ReadIntPtr(Address.TargetManager + offset);
            if (actorAddress == IntPtr.Zero) return null;
            return this.dalamud.ClientState.Actors.ReadActorFromMemory(actorAddress);
        }
    }
}
