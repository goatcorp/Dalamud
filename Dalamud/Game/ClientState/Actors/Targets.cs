using System;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Actors.Types;

namespace Dalamud.Game.ClientState.Actors
{
    public static class TargetOffsets
    {
        public const int CurrentTarget = 0x80;
        public const int MouseOverTarget = 0xD0;
        public const int FocusTarget = 0xF8;
        public const int PreviousTarget = 0x110;
        public const int SoftTarget = 0x88;
    }

    public sealed class Targets
    {
        private ClientStateAddressResolver Address { get; }

        private Dalamud dalamud;

        public Actor CurrentTarget => this.GetActorByOffset(TargetOffsets.CurrentTarget);

        public Actor MouseOverTarget => this.GetActorByOffset(TargetOffsets.MouseOverTarget);

        public Actor FocusTarget => this.GetActorByOffset(TargetOffsets.FocusTarget);

        public Actor PreviousTarget => this.GetActorByOffset(TargetOffsets.PreviousTarget);

        public Actor SoftTarget => this.GetActorByOffset(TargetOffsets.SoftTarget);

        internal Targets(Dalamud dalamud, ClientStateAddressResolver addressResolver)
        {
            this.dalamud = dalamud;
            this.Address = addressResolver;
        }

        public void SetCurrentTarget(Actor actor) => this.SetTarget(actor?.Address ?? IntPtr.Zero, TargetOffsets.CurrentTarget);

        public void SetCurrentTarget(IntPtr actorAddress) => this.SetTarget(actorAddress, TargetOffsets.CurrentTarget);

        public void SetFocusTarget(Actor actor) => this.SetTarget(actor?.Address ?? IntPtr.Zero, TargetOffsets.FocusTarget);

        public void SetFocusTarget(IntPtr actorAddress) => this.SetTarget(actorAddress, TargetOffsets.FocusTarget);

        public void ClearCurrentTarget() => this.SetCurrentTarget(IntPtr.Zero);

        public void ClearFocusTarget() => this.SetFocusTarget(IntPtr.Zero);

        private void SetTarget(IntPtr actorAddress, int offset)
        {
            if (this.Address.TargetManager == IntPtr.Zero)
                return;

            Marshal.WriteIntPtr(this.Address.TargetManager, offset, actorAddress);
        }

        private Actor GetActorByOffset(int offset)
        {
            if (this.Address.TargetManager == IntPtr.Zero)
                return null;

            var actorAddress = Marshal.ReadIntPtr(this.Address.TargetManager + offset);
            if (actorAddress == IntPtr.Zero)
                return null;

            return this.dalamud.ClientState.Actors.ReadActorFromMemory(actorAddress);
        }
    }
}
