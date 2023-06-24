using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Serilog;

namespace Dalamud.Game.ClientState.Buddy;

/// <summary>
/// This collection represents the buddies present in your squadron or trust party.
/// It does not include the local player.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IBuddyList>]
#pragma warning restore SA1015
public sealed partial class BuddyList : IServiceType, IBuddyList
{
    private const uint InvalidObjectID = 0xE0000000;

    [ServiceManager.ServiceDependency]
    private readonly ClientState clientState = Service<ClientState>.Get();

    private readonly ClientStateAddressResolver address;

    [ServiceManager.ServiceConstructor]
    private BuddyList()
    {
        this.address = this.clientState.AddressResolver;

        Log.Verbose($"Buddy list address 0x{this.address.BuddyList.ToInt64():X}");
    }

    /// <inheritdoc/>
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
    [Obsolete("Use CompanionBuddy != null", false)]
    public bool CompanionBuddyPresent => this.CompanionBuddy != null;

    /// <summary>
    /// Gets a value indicating whether the local player's pet is present.
    /// </summary>
    [Obsolete("Use PetBuddy != null", false)]
    public bool PetBuddyPresent => this.PetBuddy != null;

    /// <inheritdoc/>
    public BuddyMember? CompanionBuddy
    {
        get
        {
            var addr = this.GetCompanionBuddyMemberAddress();
            return this.CreateBuddyMemberReference(addr);
        }
    }

    /// <inheritdoc/>
    public BuddyMember? PetBuddy
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

    private static int BuddyMemberSize { get; } = Marshal.SizeOf<FFXIVClientStructs.FFXIV.Client.Game.UI.Buddy.BuddyMember>();

    private unsafe FFXIVClientStructs.FFXIV.Client.Game.UI.Buddy* BuddyListStruct => (FFXIVClientStructs.FFXIV.Client.Game.UI.Buddy*)this.BuddyListAddress;

    /// <inheritdoc/>
    public BuddyMember? this[int index]
    {
        get
        {
            var address = this.GetBattleBuddyMemberAddress(index);
            return this.CreateBuddyMemberReference(address);
        }
    }

    /// <inheritdoc/>
    public unsafe IntPtr GetCompanionBuddyMemberAddress()
    {
        return (IntPtr)(&this.BuddyListStruct->Companion);
    }

    /// <inheritdoc/>
    public unsafe IntPtr GetPetBuddyMemberAddress()
    {
        return (IntPtr)(&this.BuddyListStruct->Pet);
    }

    /// <inheritdoc/>
    public unsafe IntPtr GetBattleBuddyMemberAddress(int index)
    {
        if (index < 0 || index >= 3)
            return IntPtr.Zero;

        return (IntPtr)(this.BuddyListStruct->BattleBuddies + (index * BuddyMemberSize));
    }

    /// <inheritdoc/>
    public BuddyMember? CreateBuddyMemberReference(IntPtr address)
    {
        if (this.clientState.LocalContentId == 0)
            return null;

        if (address == IntPtr.Zero)
            return null;

        var buddy = new BuddyMember(address);
        if (buddy.ObjectId == InvalidObjectID)
            return null;

        return buddy;
    }
}

/// <summary>
/// This collection represents the buddies present in your squadron or trust party.
/// </summary>
public sealed partial class BuddyList
{
    /// <inheritdoc/>
    int IReadOnlyCollection<BuddyMember>.Count => this.Length;

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
}
