using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Data;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;

using Lumina.Excel;

namespace Dalamud.Game.ClientState.Party;

/// <summary>
/// Interface representing a party member.
/// </summary>
public interface IPartyMember
{
    /// <summary>
    /// Gets the address of this party member in memory.
    /// </summary>
    IntPtr Address { get; }

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
/// This class represents a party member in the group manager.
/// </summary>
internal unsafe class PartyMember : IPartyMember
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PartyMember"/> class.
    /// </summary>
    /// <param name="address">Address of the party member.</param>
    internal PartyMember(IntPtr address)
    {
        this.Address = address;
    }

    /// <inheritdoc/>
    public IntPtr Address { get; }

    /// <inheritdoc/>
    public StatusList Statuses => new(&this.Struct->StatusManager);

    /// <inheritdoc/>
    public Vector3 Position => this.Struct->Position;

    /// <inheritdoc/>
    public long ContentId => (long)this.Struct->ContentId;

    /// <inheritdoc/>
    public uint ObjectId => this.Struct->EntityId;

    /// <inheritdoc/>
    public uint EntityId => this.Struct->EntityId;

    /// <inheritdoc/>
    public IGameObject? GameObject => Service<ObjectTable>.Get().SearchById(this.EntityId);

    /// <inheritdoc/>
    public uint CurrentHP => this.Struct->CurrentHP;

    /// <inheritdoc/>
    public uint MaxHP => this.Struct->MaxHP;

    /// <inheritdoc/>
    public ushort CurrentMP => this.Struct->CurrentMP;

    /// <inheritdoc/>
    public ushort MaxMP => this.Struct->MaxMP;

    /// <inheritdoc/>
    public RowRef<Lumina.Excel.Sheets.TerritoryType> Territory => LuminaUtils.CreateRef<Lumina.Excel.Sheets.TerritoryType>(this.Struct->TerritoryType);

    /// <inheritdoc/>
    public RowRef<Lumina.Excel.Sheets.World> World => LuminaUtils.CreateRef<Lumina.Excel.Sheets.World>(this.Struct->HomeWorld);

    /// <inheritdoc/>
    public SeString Name => SeString.Parse(this.Struct->Name);

    /// <inheritdoc/>
    public byte Sex => this.Struct->Sex;

    /// <inheritdoc/>
    public RowRef<Lumina.Excel.Sheets.ClassJob> ClassJob => LuminaUtils.CreateRef<Lumina.Excel.Sheets.ClassJob>(this.Struct->ClassJob);

    /// <inheritdoc/>
    public byte Level => this.Struct->Level;

    private FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember*)this.Address;
}
