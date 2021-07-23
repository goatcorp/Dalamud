using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using JetBrains.Annotations;
using Serilog;

namespace Dalamud.Game.ClientState.Buddy
{
    /// <summary>
    /// This collection represents the buddies present in your squadron or trust party.
    /// It does not include the local player.
    /// </summary>
    public sealed partial class BuddyList
    {
        private const uint InvalidActorID = 0xE0000000;
        private readonly int buddyMemberSize = Marshal.SizeOf<FFXIVClientStructs.FFXIV.Client.Game.UI.Buddy>();

        private readonly Dalamud dalamud;
        private readonly ClientStateAddressResolver address;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuddyList"/> class.
        /// </summary>
        /// <param name="dalamud">The <see cref="dalamud"/> instance.</param>
        /// <param name="addressResolver">Client state address resolver.</param>
        internal BuddyList(Dalamud dalamud, ClientStateAddressResolver addressResolver)
        {
            this.dalamud = dalamud;
            this.address = addressResolver;

            Log.Verbose($"Buddy list address 0x{this.address.BuddyList.ToInt64():X}");
        }

        /// <summary>
        /// Gets the amount of battle buddies the local player has.
        /// </summary>
        public int Length
        {
            get
            {
                var i = 0;
                for (; i < 3; i++)
                {
                    var addr = this.GetBattleBuddyMemberAddress(i);
                    var member = this.CreateBuddyMemberReference(addr);
                    if (member == null)
                        break;
                }

                return i;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the local player's companion is present.
        /// </summary>
        public bool CompanionBuddyPresent => this.CompanionBuddy != null;

        /// <summary>
        /// Gets a value indicating whether the local player's pet is present.
        /// </summary>
        public bool PetBuddyPresent => this.PetBuddy != null;

        /// <summary>
        /// Gets the active companion buddy.
        /// </summary>
        [CanBeNull]
        public BuddyMember CompanionBuddy
        {
            get
            {
                var addr = this.GetCompanionBuddyMemberAddress();
                return this.CreateBuddyMemberReference(addr);
            }
        }

        /// <summary>
        /// Gets the active pet buddy.
        /// </summary>
        [CanBeNull]
        public BuddyMember PetBuddy
        {
            get
            {
                var addr = this.GetPetBuddyMemberAddress();
                return this.CreateBuddyMemberReference(addr);
            }
        }

        /// <summary>
        /// Gets the address of the buddy list.
        /// </summary>
        internal IntPtr BuddyListAddress => this.address.BuddyList;

        private unsafe FFXIVClientStructs.FFXIV.Client.Game.UI.Buddy* BuddyListStruct => (FFXIVClientStructs.FFXIV.Client.Game.UI.Buddy*)this.BuddyListAddress;

        /// <summary>
        /// Gets a battle buddy at the specified spawn index.
        /// </summary>
        /// <param name="index">Spawn index.</param>
        /// <returns>A <see cref="BuddyMember"/> at the specified spawn index.</returns>
        [CanBeNull]
        public BuddyMember this[int index]
        {
            get
            {
                var address = this.GetBattleBuddyMemberAddress(index);
                return this.CreateBuddyMemberReference(address);
            }
        }

        /// <summary>
        /// Gets the address of the companion buddy.
        /// </summary>
        /// <returns>The memory address of the companion buddy.</returns>
        public unsafe IntPtr GetCompanionBuddyMemberAddress()
        {
            return (IntPtr)(&this.BuddyListStruct->Companion);
        }

        /// <summary>
        /// Gets the address of the pet buddy.
        /// </summary>
        /// <returns>The memory address of the pet buddy.</returns>
        public unsafe IntPtr GetPetBuddyMemberAddress()
        {
            return (IntPtr)(&this.BuddyListStruct->Pet);
        }

        /// <summary>
        /// Gets the address of the battle buddy at the specified index of the buddy list.
        /// </summary>
        /// <param name="index">The index of the battle buddy.</param>
        /// <returns>The memory address of the battle buddy.</returns>
        public unsafe IntPtr GetBattleBuddyMemberAddress(int index)
        {
            if (index < 0 || index >= 3)
                return IntPtr.Zero;

            return (IntPtr)(this.BuddyListStruct->BattleBuddies + (index * this.buddyMemberSize));
        }

        /// <summary>
        /// Create a reference to a buddy.
        /// </summary>
        /// <param name="address">The address of the buddy in memory.</param>
        /// <returns><see cref="BuddyMember"/> object containing the requested data.</returns>
        [CanBeNull]
        public BuddyMember CreateBuddyMemberReference(IntPtr address)
        {
            if (this.dalamud.ClientState.LocalContentId == 0)
                return null;

            if (address == IntPtr.Zero)
                return null;

            var buddy = new BuddyMember(address, this.dalamud);
            if (buddy.ActorID == InvalidActorID)
                return null;

            return buddy;
        }
    }

    /// <summary>
    /// This collection represents the buddies present in your squadron or trust party.
    /// </summary>
    public sealed partial class BuddyList : IReadOnlyCollection<BuddyMember>, ICollection
    {
        /// <inheritdoc/>
        int IReadOnlyCollection<BuddyMember>.Count => this.Length;

        /// <inheritdoc/>
        int ICollection.Count => this.Length;

        /// <inheritdoc/>
        bool ICollection.IsSynchronized => false;

        /// <inheritdoc/>
        object ICollection.SyncRoot => this;

        /// <inheritdoc/>
        public IEnumerator<BuddyMember> GetEnumerator()
        {
            for (var i = 0; i < this.Length; i++)
            {
                yield return this[i];
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <inheritdoc/>
        void ICollection.CopyTo(Array array, int index)
        {
            for (var i = 0; i < this.Length; i++)
            {
                array.SetValue(this[i], index);
                index++;
            }
        }
    }
}
