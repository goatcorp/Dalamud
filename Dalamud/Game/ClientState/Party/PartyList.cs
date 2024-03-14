using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

using Serilog;

namespace Dalamud.Game.ClientState.Party;

/// <summary>
/// This collection represents the actors present in your party or alliance.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IPartyList>]
#pragma warning restore SA1015
internal sealed unsafe partial class PartyList : IServiceType, IPartyList
{
    private const int GroupLength = 8;
    private const int AllianceLength = 20;

    [ServiceManager.ServiceDependency]
    private readonly ClientState clientState = Service<ClientState>.Get();

    private readonly ClientStateAddressResolver address;

    private PartyMember[] cachedMember;

    [ServiceManager.ServiceConstructor]
    private PartyList()
    {
        this.address = this.clientState.AddressResolver;

        this.cachedMember = new PartyMember[AllianceLength];
        for (var i = 0; i < this.cachedMember.Length; i++)
        {
            this.cachedMember[i] = new PartyMember(nint.Zero);
        }

        Log.Verbose($"Group manager address 0x{this.address.GroupManager.ToInt64():X}");
    }

    /// <inheritdoc/>
    public int Length => this.GroupManagerStruct->MemberCount;

    /// <inheritdoc/>
    public uint PartyLeaderIndex => this.GroupManagerStruct->PartyLeaderIndex;

    /// <inheritdoc/>
    public bool IsAlliance => this.GroupManagerStruct->AllianceFlags > 0;

    /// <inheritdoc/>
    public IntPtr GroupManagerAddress => this.address.GroupManager;

    /// <inheritdoc/>
    public IntPtr GroupListAddress => (IntPtr)GroupManagerStruct->PartyMembers;

    /// <inheritdoc/>
    public IntPtr AllianceListAddress => (IntPtr)this.GroupManagerStruct->AllianceMembers;

    /// <inheritdoc/>
    public long PartyId => this.GroupManagerStruct->PartyId;

    private static int PartyMemberSize { get; } = Marshal.SizeOf<FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember>();

    private FFXIVClientStructs.FFXIV.Client.Game.Group.GroupManager* GroupManagerStruct => (FFXIVClientStructs.FFXIV.Client.Game.Group.GroupManager*)this.GroupManagerAddress;

    /// <inheritdoc/>
    public PartyMember? this[int index]
    {
        get
        {
            // Normally using Length results in a recursion crash, however we know the party size via ptr.
            if (index < 0 || index >= this.Length)
                return null;

            var address = this.Length > GroupLength
                ? this.GetAllianceMemberAddress(index)
                : this.GetPartyMemberAddress(index);

            if (address == nint.Zero)
                return null;

            var partyMember = this.cachedMember[index];
            partyMember.Address = address;
            return partyMember;
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
    public PartyMember? CreatePartyMemberReference(IntPtr address)
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
internal sealed partial class PartyList
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
