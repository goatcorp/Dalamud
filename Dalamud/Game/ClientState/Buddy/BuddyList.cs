using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Serilog;

namespace Dalamud.Game.ClientState.Buddy;

/// <summary>
/// This collection represents the buddies present in your squadron or trust party.
/// It does not include the local player.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
public sealed partial class BuddyList : IServiceType
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
    public BuddyMember? CompanionBuddy
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

    /// <summary>
    /// Gets a battle buddy at the specified spawn index.
    /// </summary>
    /// <param name="index">Spawn index.</param>
    /// <returns>A <see cref="BuddyMember"/> at the specified spawn index.</returns>
    public BuddyMember? this[int index]
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

        return (IntPtr)(this.BuddyListStruct->BattleBuddies + (index * BuddyMemberSize));
    }

    /// <summary>
    /// Create a reference to a buddy.
    /// </summary>
    /// <param name="address">The address of the buddy in memory.</param>
    /// <returns><see cref="BuddyMember"/> object containing the requested data.</returns>
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
public sealed partial class BuddyList : IReadOnlyCollection<BuddyMember>
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
