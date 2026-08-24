using System.Collections.Generic;

using Dalamud.Game.ClientState.Buddy;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This collection represents the buddies present in your squadron or trust party.
/// It does not include the local player.
/// </summary>
public interface IBuddyList : IDalamudService, IReadOnlyCollection<IBuddyMember>
{
    /// <summary>
    /// Gets the amount of battle buddies the local player has.
    /// </summary>
    int Length { get; }

    /// <summary>
    /// Gets the active companion buddy.
    /// </summary>
    IBuddyMember? CompanionBuddy { get; }

    /// <summary>
    /// Gets the active pet buddy.
    /// </summary>
    IBuddyMember? PetBuddy { get; }

    /// <summary>
    /// Gets a battle buddy at the specified spawn index.
    /// </summary>
    /// <param name="index">Spawn index.</param>
    /// <returns>A <see cref="BuddyMember"/> at the specified spawn index.</returns>
    IBuddyMember? this[int index] { get; }

    /// <summary>
    /// Gets the address of the companion buddy.
    /// </summary>
    /// <returns>The memory address of the companion buddy.</returns>
    nint GetCompanionBuddyMemberAddress();

    /// <summary>
    /// Gets the address of the pet buddy.
    /// </summary>
    /// <returns>The memory address of the pet buddy.</returns>
    nint GetPetBuddyMemberAddress();

    /// <summary>
    /// Gets the address of the battle buddy at the specified index of the buddy list.
    /// </summary>
    /// <param name="index">The index of the battle buddy.</param>
    /// <returns>The memory address of the battle buddy.</returns>
    nint GetBattleBuddyMemberAddress(int index);

    /// <summary>
    /// Create a reference to a buddy.
    /// </summary>
    /// <param name="address">The address of the buddy in memory.</param>
    /// <returns><see cref="BuddyMember"/> object containing the requested data.</returns>
    IBuddyMember? CreateBuddyMemberReference(nint address);
}
