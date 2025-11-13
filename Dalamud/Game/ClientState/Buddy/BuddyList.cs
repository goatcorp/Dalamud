using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.UI;

using CSBuddy = FFXIVClientStructs.FFXIV.Client.Game.UI.Buddy;
using CSBuddyMember = FFXIVClientStructs.FFXIV.Client.Game.UI.Buddy.BuddyMember;

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
    private const uint InvalidEntityId = 0xE0000000;

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

    private unsafe CSBuddy* BuddyListStruct => &UIState.Instance()->Buddy;

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
    public unsafe nint GetCompanionBuddyMemberAddress()
    {
        return (nint)this.BuddyListStruct->CompanionInfo.Companion;
    }

    /// <inheritdoc/>
    public unsafe nint GetPetBuddyMemberAddress()
    {
        return (nint)this.BuddyListStruct->PetInfo.Pet;
    }

    /// <inheritdoc/>
    public unsafe nint GetBattleBuddyMemberAddress(int index)
    {
        if (index < 0 || index >= 3)
            return 0;

        return (nint)Unsafe.AsPointer(ref this.BuddyListStruct->BattleBuddies[index]);
    }

    /// <inheritdoc/>
    public unsafe IBuddyMember? CreateBuddyMemberReference(nint address)
    {
        if (address == 0)
            return null;

        if (this.clientState.LocalContentId == 0)
            return null;

        var buddy = new BuddyMember((CSBuddyMember*)address);
        if (buddy.EntityId == InvalidEntityId)
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
        return new Enumerator(this);
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private struct Enumerator(BuddyList buddyList) : IEnumerator<IBuddyMember>
    {
        private int index = 0;

        public IBuddyMember Current { get; private set; }

        object IEnumerator.Current => this.Current;

        public bool MoveNext()
        {
            if (this.index == buddyList.Length) return false;
            this.Current = buddyList[this.index];
            this.index++;
            return true;
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
