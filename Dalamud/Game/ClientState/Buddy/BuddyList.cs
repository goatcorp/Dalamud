using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace Dalamud.Game.ClientState.Buddy;

/// <summary>
/// This collection represents the buddies present in your squadron or trust party.
/// It does not include the local player.
/// </summary>
[PluginInterface]
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IBuddyList>]
#pragma warning restore SA1015
internal sealed partial class BuddyList : IServiceType, IBuddyList
{
    private const uint InvalidObjectID = 0xE0000000;

    [ServiceManager.ServiceDependency]
    private readonly ClientState clientState = Service<ClientState>.Get();

    [ServiceManager.ServiceConstructor]
    private BuddyList()
    {
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

    /// <inheritdoc/>
    public IBuddyMember? CompanionBuddy
    {
        get
        {
            var addr = this.GetCompanionBuddyMemberAddress();
            return this.CreateBuddyMemberReference(addr);
        }
    }

    /// <inheritdoc/>
    public IBuddyMember? PetBuddy
    {
        get
        {
            var addr = this.GetPetBuddyMemberAddress();
            return this.CreateBuddyMemberReference(addr);
        }
    }

    private unsafe FFXIVClientStructs.FFXIV.Client.Game.UI.Buddy* BuddyListStruct => &UIState.Instance()->Buddy;

    /// <inheritdoc/>
    public IBuddyMember? this[int index]
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
        return (IntPtr)this.BuddyListStruct->CompanionInfo.Companion;
    }

    /// <inheritdoc/>
    public unsafe IntPtr GetPetBuddyMemberAddress()
    {
        return (IntPtr)this.BuddyListStruct->PetInfo.Pet;
    }

    /// <inheritdoc/>
    public unsafe IntPtr GetBattleBuddyMemberAddress(int index)
    {
        if (index < 0 || index >= 3)
            return IntPtr.Zero;

        return (IntPtr)Unsafe.AsPointer(ref this.BuddyListStruct->BattleBuddies[index]);
    }

    /// <inheritdoc/>
    public IBuddyMember? CreateBuddyMemberReference(IntPtr address)
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
internal sealed partial class BuddyList
{
    /// <inheritdoc/>
    int IReadOnlyCollection<IBuddyMember>.Count => this.Length;

    /// <inheritdoc/>
    public IEnumerator<IBuddyMember> GetEnumerator()
    {
        for (var i = 0; i < this.Length; i++)
        {
            yield return this[i];
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
