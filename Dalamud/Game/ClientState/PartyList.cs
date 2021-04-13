using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Actors.Types;

namespace Dalamud.Game.ClientState
{
    /// <summary>
    /// A PartyList.
    /// </summary>
    public class PartyList : IReadOnlyCollection<PartyMember>
    {
        private readonly Dalamud dalamud;
        private readonly ClientStateAddressResolver address;
        private readonly GetPartyMemberCountDelegate getCrossPartyMemberCount;
        private readonly GetCompanionMemberCountDelegate getCompanionMemberCount;
        private readonly GetCrossMemberByGrpIndexDelegate getCrossMemberByGrpIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartyList"/> class.
        /// </summary>
        /// <param name="dalamud">A Dalamud.</param>
        /// <param name="addressResolver">A ClientStateAddressResolver.</param>
        public PartyList(Dalamud dalamud, ClientStateAddressResolver addressResolver)
        {
            this.address = addressResolver;
            this.dalamud = dalamud;
            this.getCrossPartyMemberCount = Marshal.GetDelegateForFunctionPointer<GetPartyMemberCountDelegate>(addressResolver.GetCrossRealmMemberCount);
            this.getCrossMemberByGrpIndex = Marshal.GetDelegateForFunctionPointer<GetCrossMemberByGrpIndexDelegate>(addressResolver.GetCrossMemberByGrpIndex);
            this.getCompanionMemberCount = Marshal.GetDelegateForFunctionPointer<GetCompanionMemberCountDelegate>(addressResolver.GetCompanionMemberCount);
        }

        private delegate byte GetPartyMemberCountDelegate();

        private delegate IntPtr GetCrossMemberByGrpIndexDelegate(int index, int group);

        private delegate byte GetCompanionMemberCountDelegate(IntPtr manager);

        /// <inheritdoc/>
        public int Count
        {
            get
            {
                var count = this.getCrossPartyMemberCount();
                if (count > 0)
                    return count;
                count = this.GetRegularMemberCount();
                if (count > 1)
                    return count;
                count = this.GetCompanionMemberCount();
                return count > 0 ? count + 1 : 0;
            }
        }

        /// <summary>
        /// Gets the PartyMember at the specified index or null.
        /// </summary>
        /// <param name="index">The index.</param>
        public PartyMember this[int index]
        {
            get
            {
                if (index < 0 || index >= this.Count)
                    return null;

                if (this.getCrossPartyMemberCount() > 0)
                {
                    var member = this.getCrossMemberByGrpIndex(index, -1);
                    if (member == IntPtr.Zero)
                        return null;
                    return PartyMember.CrossRealmMember(this.dalamud.ClientState.Actors, member);
                }

                if (this.GetRegularMemberCount() > 1)
                {
                    var member = this.address.GroupManager + (0x230 * index);
                    return PartyMember.RegularMember(this.dalamud.ClientState.Actors, member);
                }

                if (this.GetCompanionMemberCount() > 0)
                {
                    if (index >= 3) // return a dummy player member if it's not one of the npcs
                        return PartyMember.LocalPlayerMember(this.dalamud);
                    var member = Marshal.ReadIntPtr(this.address.CompanionManagerPtr) + (0x198 * index);
                    return PartyMember.CompanionMember(this.dalamud.ClientState.Actors, member);
                }

                return null;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <inheritdoc/>
        public IEnumerator<PartyMember> GetEnumerator()
        {
            for (var i = 0; i < this.Count; i++)
            {
                var member = this[i];
                if (member != null)
                    yield return member;
            }
        }

        private byte GetRegularMemberCount()
        {
            return Marshal.ReadByte(this.address.GroupManager, 0x3D5C);
        }

        private byte GetCompanionMemberCount()
        {
            var manager = Marshal.ReadIntPtr(this.address.CompanionManagerPtr);
            if (manager == IntPtr.Zero)
                return 0;
            return this.getCompanionMemberCount(this.address.CompanionManagerPtr);
        }
    }
}
