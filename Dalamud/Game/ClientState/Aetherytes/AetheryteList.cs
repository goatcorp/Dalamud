using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Serilog;

namespace Dalamud.Game.ClientState.Aetherytes
{
    /// <summary>
    /// This collection represents the list of available Aetherytes in the Teleport window.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    [ServiceManager.BlockingEarlyLoadedService]
    public sealed partial class AetheryteList : IServiceType
    {
        [ServiceManager.ServiceDependency]
        private readonly ClientState clientState = Service<ClientState>.Get();
        private readonly ClientStateAddressResolver address;
        private readonly UpdateAetheryteListDelegate updateAetheryteListFunc;

        [ServiceManager.ServiceConstructor]
        private AetheryteList()
        {
            this.address = this.clientState.AddressResolver;
            this.updateAetheryteListFunc = Marshal.GetDelegateForFunctionPointer<UpdateAetheryteListDelegate>(this.address.UpdateAetheryteList);

            Log.Verbose($"Teleport address 0x{this.address.Telepo.ToInt64():X}");
        }

        private delegate void UpdateAetheryteListDelegate(IntPtr telepo, byte arg1);

        /// <summary>
        /// Gets the amount of Aetherytes the local player has unlocked.
        /// </summary>
        public unsafe int Length
        {
            get
            {
                if (this.clientState.LocalPlayer == null)
                    return 0;

                this.Update();

                if (TelepoStruct->TeleportList.First == TelepoStruct->TeleportList.Last)
                    return 0;

                return (int)TelepoStruct->TeleportList.Size();
            }
        }

        private unsafe FFXIVClientStructs.FFXIV.Client.Game.UI.Telepo* TelepoStruct => (FFXIVClientStructs.FFXIV.Client.Game.UI.Telepo*)this.address.Telepo;

        /// <summary>
        /// Gets a Aetheryte Entry at the specified index.
        /// </summary>
        /// <param name="index">Index.</param>
        /// <returns>A <see cref="AetheryteEntry"/> at the specified index.</returns>
        public unsafe AetheryteEntry? this[int index]
        {
            get
            {
                if (index < 0 || index >= this.Length)
                {
                    return null;
                }

                if (this.clientState.LocalPlayer == null)
                    return null;

                return new AetheryteEntry(TelepoStruct->TeleportList.Get((ulong)index));
            }
        }

        private void Update()
        {
            // this is very very important as otherwise it crashes
            if (this.clientState.LocalPlayer == null)
                return;

            this.updateAetheryteListFunc(this.address.Telepo, 0);
        }
    }

    /// <summary>
    /// This collection represents the list of available Aetherytes in the Teleport window.
    /// </summary>
    public sealed partial class AetheryteList : IReadOnlyCollection<AetheryteEntry>
    {
        /// <inheritdoc/>
        public int Count => this.Length;

        /// <inheritdoc/>
        public IEnumerator<AetheryteEntry> GetEnumerator()
        {
            for (var i = 0; i < this.Length; i++)
            {
                yield return this[i];
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
