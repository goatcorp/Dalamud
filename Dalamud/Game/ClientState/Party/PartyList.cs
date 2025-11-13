using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

using CSGroupManager = FFXIVClientStructs.FFXIV.Client.Game.Group.GroupManager;

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
    private readonly ClientState clientState = Service<ClientState>.Get();

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
    public unsafe IntPtr GroupManagerAddress => (nint)CSGroupManager.Instance();

    /// <inheritdoc/>
    public IntPtr GroupListAddress => (IntPtr)Unsafe.AsPointer(ref GroupManagerStruct->MainGroup.PartyMembers[0]);

    /// <inheritdoc/>
    public IntPtr AllianceListAddress => (IntPtr)Unsafe.AsPointer(ref this.GroupManagerStruct->MainGroup.AllianceMembers[0]);

    /// <inheritdoc/>
    public long PartyId => this.GroupManagerStruct->MainGroup.PartyId;

    private static int PartyMemberSize { get; } = Marshal.SizeOf<FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember>();

    private FFXIVClientStructs.FFXIV.Client.Game.Group.GroupManager* GroupManagerStruct => (FFXIVClientStructs.FFXIV.Client.Game.Group.GroupManager*)this.GroupManagerAddress;

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
    public IntPtr GetPartyMemberAddress(int index)
    {
        if (index < 0 || index >= GroupLength)
            return IntPtr.Zero;

        return this.GroupListAddress + (index * PartyMemberSize);
    }

    /// <inheritdoc/>
    public IPartyMember? CreatePartyMemberReference(IntPtr address)
    {
        if (this.clientState.LocalContentId == 0)
            return null;

        if (address == IntPtr.Zero)
            return null;

        return new PartyMember(address);
    }

    /// <inheritdoc/>
    public IntPtr GetAllianceMemberAddress(int index)
    {
        if (index < 0 || index >= AllianceLength)
            return IntPtr.Zero;

        return this.AllianceListAddress + (index * PartyMemberSize);
    }

    /// <inheritdoc/>
    public IPartyMember? CreateAllianceMemberReference(IntPtr address)
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
        private int index = 0;

        public IPartyMember Current { get; private set; }

        object IEnumerator.Current => this.Current;

        public bool MoveNext()
        {
            if (this.index == partyList.Length) return false;

            for (; this.index < partyList.Length; this.index++)
            {
                var partyMember = partyList[this.index];
                if (partyMember != null)
                {
                    this.Current = partyMember;
                    return true;
                }
            }

            return false;
        }

        public void Reset()
        {
            this.index = 0;
        }

        public void Dispose()
        {
        }
    }
}
