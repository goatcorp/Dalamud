using System;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Actors.Types;
using JetBrains.Annotations;

namespace Dalamud.Game.ClientState.Actors
{
    /// <summary>
    /// Get and set various kinds of targets for the player.
    /// </summary>
    public sealed class Targets
    {
        private readonly Dalamud dalamud;
        private readonly ClientStateAddressResolver address;

        /// <summary>
        /// Initializes a new instance of the <see cref="Targets"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance.</param>
        /// <param name="addressResolver">The ClientStateAddressResolver instance.</param>
        internal Targets(Dalamud dalamud, ClientStateAddressResolver addressResolver)
        {
            this.dalamud = dalamud;
            this.address = addressResolver;
        }

        /// <summary>
        /// Gets the current target.
        /// </summary>
        [CanBeNull]
        public Actor CurrentTarget => this.GetActorByOffset(TargetOffsets.CurrentTarget);

        /// <summary>
        /// Gets the mouseover target.
        /// </summary>
        [CanBeNull]
        public Actor MouseOverTarget => this.GetActorByOffset(TargetOffsets.MouseOverTarget);

        /// <summary>
        /// Gets the focus target.
        /// </summary>
        [CanBeNull]
        public Actor FocusTarget => this.GetActorByOffset(TargetOffsets.FocusTarget);

        /// <summary>
        /// Gets the previous target.
        /// </summary>
        [CanBeNull]
        public Actor PreviousTarget => this.GetActorByOffset(TargetOffsets.PreviousTarget);

        /// <summary>
        /// Gets the soft target.
        /// </summary>
        [CanBeNull]
        public Actor SoftTarget => this.GetActorByOffset(TargetOffsets.SoftTarget);

        /// <summary>
        /// Sets the current target.
        /// </summary>
        /// <param name="actor">Actor to target.</param>
        public void SetCurrentTarget(Actor actor) => this.SetTarget(actor?.Address ?? IntPtr.Zero, TargetOffsets.CurrentTarget);

        /// <summary>
        /// Sets the current target.
        /// </summary>
        /// <param name="actorAddress">Actor (address) to target.</param>
        public void SetCurrentTarget(IntPtr actorAddress) => this.SetTarget(actorAddress, TargetOffsets.CurrentTarget);

        /// <summary>
        /// Sets the focus target.
        /// </summary>
        /// <param name="actor">Actor to focus.</param>
        public void SetFocusTarget(Actor actor) => this.SetTarget(actor?.Address ?? IntPtr.Zero, TargetOffsets.FocusTarget);

        /// <summary>
        /// Sets the focus target.
        /// </summary>
        /// <param name="actorAddress">Actor (address) to focus.</param>
        public void SetFocusTarget(IntPtr actorAddress) => this.SetTarget(actorAddress, TargetOffsets.FocusTarget);

        /// <summary>
        /// Clears the current target.
        /// </summary>
        public void ClearCurrentTarget() => this.SetCurrentTarget(IntPtr.Zero);

        /// <summary>
        /// Clears the focus target.
        /// </summary>
        public void ClearFocusTarget() => this.SetFocusTarget(IntPtr.Zero);

        private void SetTarget(IntPtr actorAddress, int offset)
        {
            if (this.address.TargetManager == IntPtr.Zero)
                return;

            Marshal.WriteIntPtr(this.address.TargetManager, offset, actorAddress);
        }

        [CanBeNull]
        private Actor GetActorByOffset(int offset)
        {
            if (this.address.TargetManager == IntPtr.Zero)
                return null;

            var actorAddress = Marshal.ReadIntPtr(this.address.TargetManager + offset);
            if (actorAddress == IntPtr.Zero)
                return null;

            return this.dalamud.ClientState.Actors.CreateActorReference(actorAddress);
        }
    }

    /// <summary>
    /// Memory offsets for the <see cref="Targets"/> type.
    /// </summary>
    public static class TargetOffsets
    {
        public const int CurrentTarget = 0x80;
        public const int SoftTarget = 0x88;
        public const int MouseOverTarget = 0xD0;
        public const int FocusTarget = 0xF8;
        public const int PreviousTarget = 0x110;
    }
}
