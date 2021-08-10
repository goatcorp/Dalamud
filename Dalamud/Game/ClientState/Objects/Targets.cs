using System;

using Dalamud.Game.ClientState.Objects.Types;
using JetBrains.Annotations;

namespace Dalamud.Game.ClientState.Objects
{
    /// <summary>
    /// Get and set various kinds of targets for the player.
    /// </summary>
    public sealed unsafe class Targets
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
        /// Gets the address of the target manager.
        /// </summary>
        public IntPtr Address => this.address.TargetManager;

        /// <summary>
        /// Gets or sets the current target.
        /// </summary>
        [CanBeNull]
        public GameObject Target
        {
            get => this.dalamud.ClientState.Objects.CreateObjectReference((IntPtr)Struct->Target);
            set => this.SetTarget(value);
        }

        /// <summary>
        /// Gets or sets the mouseover target.
        /// </summary>
        [CanBeNull]
        public GameObject MouseOverTarget
        {
            get => this.dalamud.ClientState.Objects.CreateObjectReference((IntPtr)Struct->MouseOverTarget);
            set => this.SetMouseOverTarget(value);
        }

        /// <summary>
        /// Gets or sets the focus target.
        /// </summary>
        [CanBeNull]
        public GameObject FocusTarget
        {
            get => this.dalamud.ClientState.Objects.CreateObjectReference((IntPtr)Struct->FocusTarget);
            set => this.SetFocusTarget(value);
        }

        /// <summary>
        /// Gets or sets the previous target.
        /// </summary>
        [CanBeNull]
        public GameObject PreviousTarget
        {
            get => this.dalamud.ClientState.Objects.CreateObjectReference((IntPtr)Struct->PreviousTarget);
            set => this.SetPreviousTarget(value);
        }

        /// <summary>
        /// Gets or sets the soft target.
        /// </summary>
        [CanBeNull]
        public GameObject SoftTarget
        {
            get => this.dalamud.ClientState.Objects.CreateObjectReference((IntPtr)Struct->SoftTarget);
            set => this.SetSoftTarget(value);
        }

        private FFXIVClientStructs.FFXIV.Client.Game.Control.TargetSystem* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Control.TargetSystem*)this.Address;

        /// <summary>
        /// Sets the current target.
        /// </summary>
        /// <param name="actor">Actor to target.</param>
        public void SetTarget(GameObject actor) => this.SetTarget(actor?.Address ?? IntPtr.Zero);

        /// <summary>
        /// Sets the mouseover target.
        /// </summary>
        /// <param name="actor">Actor to target.</param>
        public void SetMouseOverTarget(GameObject actor) => this.SetTarget(actor?.Address ?? IntPtr.Zero);

        /// <summary>
        /// Sets the focus target.
        /// </summary>
        /// <param name="actor">Actor to target.</param>
        public void SetFocusTarget(GameObject actor) => this.SetTarget(actor?.Address ?? IntPtr.Zero);

        /// <summary>
        /// Sets the previous target.
        /// </summary>
        /// <param name="actor">Actor to target.</param>
        public void SetPreviousTarget(GameObject actor) => this.SetTarget(actor?.Address ?? IntPtr.Zero);

        /// <summary>
        /// Sets the soft target.
        /// </summary>
        /// <param name="actor">Actor to target.</param>
        public void SetSoftTarget(GameObject actor) => this.SetTarget(actor?.Address ?? IntPtr.Zero);

        /// <summary>
        /// Sets the current target.
        /// </summary>
        /// <param name="actorAddress">Actor (address) to target.</param>
        public void SetTarget(IntPtr actorAddress) => Struct->Target = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actorAddress;

        /// <summary>
        /// Sets the mouseover target.
        /// </summary>
        /// <param name="actorAddress">Actor (address) to target.</param>
        public void SetMouseOverTarget(IntPtr actorAddress) => Struct->MouseOverTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actorAddress;

        /// <summary>
        /// Sets the focus target.
        /// </summary>
        /// <param name="actorAddress">Actor (address) to target.</param>
        public void SetFocusTarget(IntPtr actorAddress) => Struct->FocusTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actorAddress;

        /// <summary>
        /// Sets the previous target.
        /// </summary>
        /// <param name="actorAddress">Actor (address) to target.</param>
        public void SetPreviousTarget(IntPtr actorAddress) => Struct->PreviousTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actorAddress;

        /// <summary>
        /// Sets the soft target.
        /// </summary>
        /// <param name="actorAddress">Actor (address) to target.</param>
        public void SetSoftTarget(IntPtr actorAddress) => Struct->SoftTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actorAddress;

        /// <summary>
        /// Clears the current target.
        /// </summary>
        public void ClearTarget() => this.SetTarget(IntPtr.Zero);

        /// <summary>
        /// Clears the mouseover target.
        /// </summary>
        public void ClearMouseOverTarget() => this.SetMouseOverTarget(IntPtr.Zero);

        /// <summary>
        /// Clears the focus target.
        /// </summary>
        public void ClearFocusTarget() => this.SetFocusTarget(IntPtr.Zero);

        /// <summary>
        /// Clears the previous target.
        /// </summary>
        public void ClearPreviousTarget() => this.SetPreviousTarget(IntPtr.Zero);

        /// <summary>
        /// Clears the soft target.
        /// </summary>
        public void ClearSoftTarget() => this.SetSoftTarget(IntPtr.Zero);
    }
}
