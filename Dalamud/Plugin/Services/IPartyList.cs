using System.Collections.Generic;

using Dalamud.Game.ClientState.Party;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This collection represents the actors present in your party or alliance.
/// </summary>
public interface IPartyList : IDalamudService, IReadOnlyCollection<IPartyMember>
{
    /// <summary>
    /// Gets the amount of party members the local player has.
    /// </summary>
    int Length { get; }

    /// <summary>
    /// Gets the index of the party leader.
    /// </summary>
    uint PartyLeaderIndex { get; }

    /// <summary>
    /// Gets a value indicating whether this group is an alliance.
    /// </summary>
    bool IsAlliance { get; }

    /// <summary>
    /// Gets the address of the Group Manager.
    /// </summary>
    nint GroupManagerAddress { get; }

    /// <summary>
    /// Gets the address of the party list within the group manager.
    /// </summary>
    nint GroupListAddress { get; }

    /// <summary>
    /// Gets the address of the alliance member list within the group manager.
    /// </summary>
    nint AllianceListAddress { get; }

    /// <summary>
    /// Gets the ID of the party.
    /// </summary>
    long PartyId { get; }

    /// <summary>
    /// Get a party member at the specified spawn index.
    /// </summary>
    /// <param name="index">Spawn index.</param>
    /// <returns>A <see cref="PartyMember"/> at the specified spawn index.</returns>
    IPartyMember? this[int index] { get; }

    /// <summary>
    /// Gets the address of the party member at the specified index of the party list.
    /// </summary>
    /// <param name="index">The index of the party member.</param>
    /// <returns>The memory address of the party member.</returns>
    nint GetPartyMemberAddress(int index);

    /// <summary>
    /// Create a reference to an FFXIV party member.
    /// </summary>
    /// <param name="address">The address of the party member in memory.</param>
    /// <returns>The party member object containing the requested data.</returns>
    IPartyMember? CreatePartyMemberReference(nint address);

    /// <summary>
    /// Gets the address of the alliance member at the specified index of the alliance list.
    /// </summary>
    /// <param name="index">The index of the alliance member.</param>
    /// <returns>The memory address of the alliance member.</returns>
    nint GetAllianceMemberAddress(int index);

    /// <summary>
    /// Create a reference to an FFXIV alliance member.
    /// </summary>
    /// <param name="address">The address of the alliance member in memory.</param>
    /// <returns>The party member object containing the requested data.</returns>
    IPartyMember? CreateAllianceMemberReference(nint address);
}
