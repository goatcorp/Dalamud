using System;

using Dalamud.Game.Internal;

namespace Dalamud.Game.ClientState
{
    /// <summary>
    /// Client state memory address resolver.
    /// </summary>
    public sealed class ClientStateAddressResolver : BaseAddressResolver
    {
        // Static offsets

        /// <summary>
        /// Gets the address of the actor table.
        /// </summary>
        public IntPtr ActorTable { get; private set; }

        /// <summary>
        /// Gets the address of the fate table pointer.
        /// </summary>
        /// <remarks>
        /// This is a static address to a pointer, not the address of the table itself.
        /// </remarks>
        public IntPtr FateTablePtr { get; private set; }

        /// <summary>
        /// Gets the address of the Group Manager.
        /// </summary>
        public IntPtr GroupManager { get; private set; }

        /// <summary>
        /// Gets the address of the local content id.
        /// </summary>
        public IntPtr LocalContentId { get; private set; }

        /// <summary>
        /// Gets the address of job gauge data.
        /// </summary>
        public IntPtr JobGaugeData { get; private set; }

        /// <summary>
        /// Gets the address of the keyboard state.
        /// </summary>
        public IntPtr KeyboardState { get; private set; }

        /// <summary>
        /// Gets the address of the target manager.
        /// </summary>
        public IntPtr TargetManager { get; private set; }

        /// <summary>
        /// Gets the address of the condition flag array.
        /// </summary>
        public IntPtr ConditionFlags { get; private set; }

        // Functions

        /// <summary>
        /// Gets the address of the method which sets the territory type.
        /// </summary>
        public IntPtr SetupTerritoryType { get; private set; }

        /// <summary>
        /// Gets the address of the method which polls the gamepads for data.
        /// Called every frame, even when `Enable Gamepad` is off in the settings.
        /// </summary>
        public IntPtr GamepadPoll { get; private set; }

        /// <summary>
        /// Scan for and setup any configured address pointers.
        /// </summary>
        /// <param name="sig">The signature scanner to facilitate setup.</param>
        protected override void Setup64Bit(SigScanner sig)
        {
            // We don't need those anymore, but maybe someone else will - let's leave them here for good measure
            // ViewportActorTable = sig.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? 85 ED", 0) + 0x148;
            // SomeActorTableAccess = sig.ScanText("E8 ?? ?? ?? ?? 48 8D 55 A0 48 8D 8E ?? ?? ?? ??");

            this.ActorTable = sig.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 44 0F B6 83");

            this.FateTablePtr = sig.GetStaticAddressFromSig("48 8B 15 ?? ?? ?? ?? 48 8B F9 44 0F B7 41 ??");

            this.GroupManager = sig.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 80 B8 ?? ?? ?? ?? ?? 76 50");

            this.LocalContentId = sig.GetStaticAddressFromSig("48 0F 44 05 ?? ?? ?? ?? 48 39 07");
            this.JobGaugeData = sig.GetStaticAddressFromSig("E8 ?? ?? ?? ?? FF C6 48 8D 5B 0C", 0xB9) + 0x10;

            this.SetupTerritoryType = sig.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F9 66 89 91 ?? ?? ?? ??");

            // This resolves to a fixed offset only, without the base address added in,
            // so GetStaticAddressFromSig() can't be used. lea rcx, ds:1DB9F74h[rax*4]
            this.KeyboardState = sig.ScanText("48 8D 0C 85 ?? ?? ?? ?? 8B 04 31 85 C2 0F 85") + 0x4;

            this.ConditionFlags = sig.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? BA ?? ?? ?? ?? E8 ?? ?? ?? ?? B0 01 48 83 C4 30");

            this.TargetManager = sig.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? FF 50 ?? 48 85 DB", 3);

            this.GamepadPoll = sig.ScanText("40 ?? 57 41 ?? 48 81 EC ?? ?? ?? ?? 44 0F ?? ?? ?? ?? ?? ?? ?? 48 8B");
        }
    }
}
