using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Resolvers;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;

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
    uint ObjectId { get; }

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
    ExcelResolver<Lumina.Excel.Sheets.TerritoryType> Territory { get; }

    /// <summary>
    /// Gets the World this party member resides in.
    /// </summary>
    ExcelResolver<Lumina.Excel.Sheets.World> World { get; }

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
    ExcelResolver<Lumina.Excel.Sheets.ClassJob> ClassJob { get; }

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

    /// <summary>
    /// Gets the address of this party member in memory.
    /// </summary>
    public IntPtr Address { get; }

    /// <summary>
    /// Gets a list of buffs or debuffs applied to this party member.
    /// </summary>
    public StatusList Statuses => new(&this.Struct->StatusManager);

    /// <summary>
    /// Gets the position of the party member.
    /// </summary>
    public Vector3 Position => new(this.Struct->X, this.Struct->Y, this.Struct->Z);

    /// <summary>
    /// Gets the content ID of the party member.
    /// </summary>
    public long ContentId => (long)this.Struct->ContentId;

    /// <summary>
    /// Gets the actor ID of this party member.
    /// </summary>
    public uint ObjectId => this.Struct->EntityId;

    /// <summary>
    /// Gets the actor associated with this buddy.
    /// </summary>
    /// <remarks>
    /// This iterates the actor table, it should be used with care.
    /// </remarks>
    public IGameObject? GameObject => Service<ObjectTable>.Get().SearchById(this.ObjectId);

    /// <summary>
    /// Gets the current HP of this party member.
    /// </summary>
    public uint CurrentHP => this.Struct->CurrentHP;

    /// <summary>
    /// Gets the maximum HP of this party member.
    /// </summary>
    public uint MaxHP => this.Struct->MaxHP;

    /// <summary>
    /// Gets the current MP of this party member.
    /// </summary>
    public ushort CurrentMP => this.Struct->CurrentMP;

    /// <summary>
    /// Gets the maximum MP of this party member.
    /// </summary>
    public ushort MaxMP => this.Struct->MaxMP;

    /// <summary>
    /// Gets the territory this party member is located in.
    /// </summary>
    public ExcelResolver<Lumina.Excel.Sheets.TerritoryType> Territory => new(this.Struct->TerritoryType);

    /// <summary>
    /// Gets the World this party member resides in.
    /// </summary>
    public ExcelResolver<Lumina.Excel.Sheets.World> World => new(this.Struct->HomeWorld);

    /// <summary>
    /// Gets the displayname of this party member.
    /// </summary>
    public SeString Name => MemoryHelper.ReadSeString((nint)Unsafe.AsPointer(ref Struct->Name[0]), 0x40);

    /// <summary>
    /// Gets the sex of this party member.
    /// </summary>
    public byte Sex => this.Struct->Sex;

    /// <summary>
    /// Gets the classjob of this party member.
    /// </summary>
    public ExcelResolver<Lumina.Excel.Sheets.ClassJob> ClassJob => new(this.Struct->ClassJob);

    /// <summary>
    /// Gets the level of this party member.
    /// </summary>
    public byte Level => this.Struct->Level;

    private FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember*)this.Address;
}
