using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Game.Player;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

using CSGroupManager = FFXIVClientStructs.FFXIV.Client.Game.Group.GroupManager;
using CSPartyMember = FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember;

namespace Dalamud.Game.ClientState.Party;

/// <summary>
/// This collection represents the actors present in your party or alliance.
/// </summary>
[PluginInterface]
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IPartyList>]
#pragma warning restore SA1015
internal sealed unsafe partial class PartyList : IServiceType, IPartyList
{
    private const int GroupLength = 8;
    private const int AllianceLength = 20;

    [ServiceManager.ServiceDependency]
    private readonly PlayerState playerState = Service<PlayerState>.Get();

    [ServiceManager.ServiceConstructor]
    private PartyList()
    {
    }

    /// <inheritdoc/>
    public int Length => this.GroupManagerStruct->MainGroup.MemberCount;

    /// <inheritdoc/>
    public uint PartyLeaderIndex => this.GroupManagerStruct->MainGroup.PartyLeaderIndex;

    /// <inheritdoc/>
    public bool IsAlliance => this.GroupManagerStruct->MainGroup.AllianceFlags > 0;

    /// <inheritdoc/>
    public unsafe nint GroupManagerAddress => (nint)CSGroupManager.Instance();

    /// <inheritdoc/>
    public nint GroupListAddress => (nint)Unsafe.AsPointer(ref this.GroupManagerStruct->MainGroup.PartyMembers[0]);

    /// <inheritdoc/>
    public nint AllianceListAddress => (nint)Unsafe.AsPointer(ref this.GroupManagerStruct->MainGroup.AllianceMembers[0]);

    /// <inheritdoc/>
    public long PartyId => this.GroupManagerStruct->MainGroup.PartyId;

    private static int PartyMemberSize { get; } = Marshal.SizeOf<CSPartyMember>();

    private CSGroupManager* GroupManagerStruct => (CSGroupManager*)this.GroupManagerAddress;

    /// <inheritdoc/>
    public IPartyMember? this[int index]
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

    /// <inheritdoc/>
    public nint GetPartyMemberAddress(int index)
    {
        if (index < 0 || index >= GroupLength)
            return 0;

        return this.GroupListAddress + (index * PartyMemberSize);
    }

    /// <inheritdoc/>
    public IPartyMember? CreatePartyMemberReference(nint address)
    {
        if (this.playerState.ContentId == 0)
            return null;

        if (address == 0)
            return null;

        return new PartyMember((CSPartyMember*)address);
    }

    /// <inheritdoc/>
    public nint GetAllianceMemberAddress(int index)
    {
        if (index < 0 || index >= AllianceLength)
            return 0;

        return this.AllianceListAddress + (index * PartyMemberSize);
    }

    /// <inheritdoc/>
    public IPartyMember? CreateAllianceMemberReference(nint address)
    {
        if (this.playerState.ContentId == 0)
            return null;

        if (address == 0)
            return null;

        return new PartyMember((CSPartyMember*)address);
    }
}

/// <summary>
/// This collection represents the party members present in your party or alliance.
/// </summary>
internal sealed partial class PartyList
{
    /// <inheritdoc/>
    int IReadOnlyCollection<IPartyMember>.Count => this.Length;

    /// <inheritdoc/>
    public IEnumerator<IPartyMember> GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private struct Enumerator(PartyList partyList) : IEnumerator<IPartyMember>
    {
        private int index = -1;

        public IPartyMember Current { get; private set; }

        object IEnumerator.Current => this.Current;

        public bool MoveNext()
        {
            while (++this.index < partyList.Length)
            {
                var partyMember = partyList[this.index];
                if (partyMember != null)
                {
                    this.Current = partyMember;
                    return true;
                }
            }

            this.Current = default;
            return false;
        }

        public void Reset()
        {
            this.index = -1;
        }

        public void Dispose()
        {
        }
    }
}
