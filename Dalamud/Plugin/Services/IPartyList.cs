using System.Collections.Generic;

using Dalamud.Game.ClientState.Party;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This collection represents the actors present in your party or alliance.
/// </summary>
public interface IPartyList : IReadOnlyCollection<PartyMember>
{
    /// <summary>
    /// Gets the amount of party members the local player has.
    /// </summary>
    public int Length { get; }
    
    /// <summary>
    /// Gets the index of the party leader.
    /// </summary>
    public uint PartyLeaderIndex { get; }
    
    /// <summary>
    /// Gets a value indicating whether this group is an alliance.
    /// </summary>
    public bool IsAlliance { get; }
    
    /// <summary>
    /// Gets the address of the Group Manager.
    /// </summary>
    public nint GroupManagerAddress { get; }
    
    /// <summary>
    /// Gets the address of the party list within the group manager.
    /// </summary>
    public nint GroupListAddress { get; }
    
    /// <summary>
    /// Gets the address of the alliance member list within the group manager.
    /// </summary>
    public nint AllianceListAddress { get; }
    
    /// <summary>
    /// Gets the ID of the party.
    /// </summary>
    public long PartyId { get; }
    
    /// <summary>
    /// Get a party member at the specified spawn index.
    /// </summary>
    /// <param name="index">Spawn index.</param>
    /// <returns>A <see cref="PartyMember"/> at the specified spawn index.</returns>
    public PartyMember? this[int index] { get; }

    /// <summary>
    /// Gets the address of the party member at the specified index of the party list.
    /// </summary>
    /// <param name="index">The index of the party member.</param>
    /// <returns>The memory address of the party member.</returns>
    public nint GetPartyMemberAddress(int index);

    /// <summary>
    /// Create a reference to an FFXIV party member.
    /// </summary>
    /// <param name="address">The address of the party member in memory.</param>
    /// <returns>The party member object containing the requested data.</returns>
    public PartyMember? CreatePartyMemberReference(nint address);
    
    /// <summary>
    /// Gets the address of the alliance member at the specified index of the alliance list.
    /// </summary>
    /// <param name="index">The index of the alliance member.</param>
    /// <returns>The memory address of the alliance member.</returns>
    public nint GetAllianceMemberAddress(int index);

    /// <summary>
    /// Create a reference to an FFXIV alliance member.
    /// </summary>
    /// <param name="address">The address of the alliance member in memory.</param>
    /// <returns>The party member object containing the requested data.</returns>
    public PartyMember? CreateAllianceMemberReference(nint address);
}
