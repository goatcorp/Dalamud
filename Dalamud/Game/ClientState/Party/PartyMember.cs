using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Data;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;

using Lumina.Excel;

using CSPartyMember = FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember;

namespace Dalamud.Game.ClientState.Party;

/// <summary>
/// Interface representing a party member.
/// </summary>
public interface IPartyMember : IEquatable<IPartyMember>
{
    /// <summary>
    /// Gets the address of this party member in memory.
    /// </summary>
    nint Address { get; }

    /// <summary>
    /// Gets a list of buffs or debuffs applied to this party member.
    /// </summary>
    StatusList Statuses { get; }

    /// <summary>
    /// Gets the position of the party member.
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// Gets the content ID of the party member.
    /// </summary>
    long ContentId { get; }

    /// <summary>
    /// Gets the actor ID of this party member.
    /// </summary>
    [Obsolete("Renamed to EntityId")]
    uint ObjectId { get; }

    /// <summary>
    /// Gets the entity ID of this party member.
    /// </summary>
    uint EntityId { get; }

    /// <summary>
    /// Gets the actor associated with this buddy.
    /// </summary>
    /// <remarks>
    /// This iterates the actor table, it should be used with care.
    /// </remarks>
    IGameObject? GameObject { get; }

    /// <summary>
    /// Gets the current HP of this party member.
    /// </summary>
    uint CurrentHP { get; }

    /// <summary>
    /// Gets the maximum HP of this party member.
    /// </summary>
    uint MaxHP { get; }

    /// <summary>
    /// Gets the current MP of this party member.
    /// </summary>
    ushort CurrentMP { get; }

    /// <summary>
    /// Gets the maximum MP of this party member.
    /// </summary>
    ushort MaxMP { get; }

    /// <summary>
    /// Gets the territory this party member is located in.
    /// </summary>
    RowRef<Lumina.Excel.Sheets.TerritoryType> Territory { get; }

    /// <summary>
    /// Gets the World this party member resides in.
    /// </summary>
    RowRef<Lumina.Excel.Sheets.World> World { get; }

    /// <summary>
    /// Gets the displayname of this party member.
    /// </summary>
    SeString Name { get; }

    /// <summary>
    /// Gets the sex of this party member.
    /// </summary>
    byte Sex { get; }

    /// <summary>
    /// Gets the classjob of this party member.
    /// </summary>
    RowRef<Lumina.Excel.Sheets.ClassJob> ClassJob { get; }

    /// <summary>
    /// Gets the level of this party member.
    /// </summary>
    byte Level { get; }
}

/// <summary>
/// This struct represents a party member in the group manager.
/// </summary>
/// <param name="ptr">A pointer to the PartyMember.</param>
internal unsafe readonly struct PartyMember(CSPartyMember* ptr) : IPartyMember
{
    /// <inheritdoc/>
    public nint Address => (nint)ptr;

    /// <inheritdoc/>
    public StatusList Statuses => new(&ptr->StatusManager);

    /// <inheritdoc/>
    public Vector3 Position => ptr->Position;

    /// <inheritdoc/>
    [Api15ToDo("Change type to ulong.")]
    public long ContentId => (long)ptr->ContentId;

    /// <inheritdoc/>
    public uint ObjectId => ptr->EntityId;

    /// <inheritdoc/>
    public uint EntityId => ptr->EntityId;

    /// <inheritdoc/>
    public IGameObject? GameObject => Service<ObjectTable>.Get().SearchById(this.EntityId);

    /// <inheritdoc/>
    public uint CurrentHP => ptr->CurrentHP;

    /// <inheritdoc/>
    public uint MaxHP => ptr->MaxHP;

    /// <inheritdoc/>
    public ushort CurrentMP => ptr->CurrentMP;

    /// <inheritdoc/>
    public ushort MaxMP => ptr->MaxMP;

    /// <inheritdoc/>
    public RowRef<Lumina.Excel.Sheets.TerritoryType> Territory => LuminaUtils.CreateRef<Lumina.Excel.Sheets.TerritoryType>(ptr->TerritoryType);

    /// <inheritdoc/>
    public RowRef<Lumina.Excel.Sheets.World> World => LuminaUtils.CreateRef<Lumina.Excel.Sheets.World>(ptr->HomeWorld);

    /// <inheritdoc/>
    public SeString Name => SeString.Parse(ptr->Name);

    /// <inheritdoc/>
    public byte Sex => ptr->Sex;

    /// <inheritdoc/>
    public RowRef<Lumina.Excel.Sheets.ClassJob> ClassJob => LuminaUtils.CreateRef<Lumina.Excel.Sheets.ClassJob>(ptr->ClassJob);

    /// <inheritdoc/>
    public byte Level => ptr->Level;

    public static bool operator ==(PartyMember x, PartyMember y) => x.Equals(y);

    public static bool operator !=(PartyMember x, PartyMember y) => !(x == y);

    /// <inheritdoc/>
    public bool Equals(IPartyMember? other)
    {
        return this.EntityId == other.EntityId;
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is PartyMember fate && this.Equals(fate);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return this.EntityId.GetHashCode();
    }
}
