using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Serilog;

namespace Dalamud.Game.ClientState.Party
{
    /// <summary>
    /// This collection represents the actors present in your party or alliance.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    [ServiceManager.BlockingEarlyLoadedService]
    public sealed unsafe partial class PartyList : IServiceType
    {
        private const int GroupLength = 8;
        private const int AllianceLength = 20;

        [ServiceManager.ServiceDependency]
        private readonly ClientState clientState = Service<ClientState>.Get();

        private readonly ClientStateAddressResolver address;

        [ServiceManager.ServiceConstructor]
        private PartyList()
        {
            this.address = this.clientState.AddressResolver;

            Log.Verbose($"Group manager address 0x{this.address.GroupManager.ToInt64():X}");
        }

        /// <summary>
        /// Gets the amount of party members the local player has.
        /// </summary>
        public int Length => this.GroupManagerStruct->MemberCount;

        /// <summary>
        /// Gets the index of the party leader.
        /// </summary>
        public uint PartyLeaderIndex => this.GroupManagerStruct->PartyLeaderIndex;

        /// <summary>
        /// Gets a value indicating whether this group is an alliance.
        /// </summary>
        public bool IsAlliance => this.GroupManagerStruct->IsAlliance;

        /// <summary>
        /// Gets the address of the Group Manager.
        /// </summary>
        public IntPtr GroupManagerAddress => this.address.GroupManager;

        /// <summary>
        /// Gets the address of the party list within the group manager.
        /// </summary>
        public IntPtr GroupListAddress => (IntPtr)GroupManagerStruct->PartyMembers;

        /// <summary>
        /// Gets the address of the alliance member list within the group manager.
        /// </summary>
        public IntPtr AllianceListAddress => (IntPtr)this.GroupManagerStruct->AllianceMembers;

        /// <summary>
        /// Gets the ID of the party.
        /// </summary>
        public long PartyId => this.GroupManagerStruct->PartyId;

        private static int PartyMemberSize { get; } = Marshal.SizeOf<FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember>();

        private FFXIVClientStructs.FFXIV.Client.Game.Group.GroupManager* GroupManagerStruct => (FFXIVClientStructs.FFXIV.Client.Game.Group.GroupManager*)this.GroupManagerAddress;

        /// <summary>
        /// Get a party member at the specified spawn index.
        /// </summary>
        /// <param name="index">Spawn index.</param>
        /// <returns>A <see cref="PartyMember"/> at the specified spawn index.</returns>
        public PartyMember? this[int index]
        {
            get
            {
                // Normally using Length results in a recursion crash, however we know the party size via ptr.
                if (index < 0 || index >= this.Length)
                    return null;

                if (this.Length > GroupLength)
                {
                    var addr = this.GetAllianceMemberAddress(index);
                    return this.CreateAllianceMemberReference(addr);
                }
                else
                {
                    var addr = this.GetPartyMemberAddress(index);
                    return this.CreatePartyMemberReference(addr);
                }
            }
        }

        /// <summary>
        /// Gets the address of the party member at the specified index of the party list.
        /// </summary>
        /// <param name="index">The index of the party member.</param>
        /// <returns>The memory address of the party member.</returns>
        public IntPtr GetPartyMemberAddress(int index)
        {
            if (index < 0 || index >= GroupLength)
                return IntPtr.Zero;

            return this.GroupListAddress + (index * PartyMemberSize);
        }

        /// <summary>
        /// Create a reference to an FFXIV party member.
        /// </summary>
        /// <param name="address">The address of the party member in memory.</param>
        /// <returns>The party member object containing the requested data.</returns>
        public PartyMember? CreatePartyMemberReference(IntPtr address)
        {
            if (this.clientState.LocalContentId == 0)
                return null;

            if (address == IntPtr.Zero)
                return null;

            return new PartyMember(address);
        }

        /// <summary>
        /// Gets the address of the alliance member at the specified index of the alliance list.
        /// </summary>
        /// <param name="index">The index of the alliance member.</param>
        /// <returns>The memory address of the alliance member.</returns>
        public IntPtr GetAllianceMemberAddress(int index)
        {
            if (index < 0 || index >= AllianceLength)
                return IntPtr.Zero;

            return this.AllianceListAddress + (index * PartyMemberSize);
        }

        /// <summary>
        /// Create a reference to an FFXIV alliance member.
        /// </summary>
        /// <param name="address">The address of the alliance member in memory.</param>
        /// <returns>The party member object containing the requested data.</returns>
        public PartyMember? CreateAllianceMemberReference(IntPtr address)
        {
            if (this.clientState.LocalContentId == 0)
                return null;

            if (address == IntPtr.Zero)
                return null;

            return new PartyMember(address);
        }
    }

    /// <summary>
    /// This collection represents the party members present in your party or alliance.
    /// </summary>
    public sealed partial class PartyList : IReadOnlyCollection<PartyMember>
    {
        /// <inheritdoc/>
        int IReadOnlyCollection<PartyMember>.Count => this.Length;

        /// <inheritdoc/>
        public IEnumerator<PartyMember> GetEnumerator()
        {
            // Normally using Length results in a recursion crash, however we know the party size via ptr.
            for (var i = 0; i < this.Length; i++)
            {
                var member = this[i];

                if (member == null)
                    break;

                yield return member;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
