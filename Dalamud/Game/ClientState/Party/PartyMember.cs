using System.Numerics;

using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Resolvers;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;

namespace Dalamud.Game.ClientState.Party;

/// <summary>
/// This class represents a party member in the group manager.
/// </summary>
public unsafe class PartyMember
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
    public long ContentId => this.Struct->ContentID;

    /// <summary>
    /// Gets the actor ID of this party member.
    /// </summary>
    public uint ObjectId => this.Struct->ObjectID;

    /// <summary>
    /// Gets the actor associated with this buddy.
    /// </summary>
    /// <remarks>
    /// This iterates the actor table, it should be used with care.
    /// </remarks>
    public GameObject? GameObject => Service<ObjectTable>.Get().SearchById(this.ObjectId);

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
    public ExcelResolver<Lumina.Excel.GeneratedSheets.TerritoryType> Territory => new(this.Struct->TerritoryType);

    /// <summary>
    /// Gets the World this party member resides in.
    /// </summary>
    public ExcelResolver<Lumina.Excel.GeneratedSheets.World> World => new(this.Struct->HomeWorld);

    /// <summary>
    /// Gets the displayname of this party member.
    /// </summary>
    public SeString Name => MemoryHelper.ReadSeString((IntPtr)Struct->Name, 0x40);

    /// <summary>
    /// Gets the sex of this party member.
    /// </summary>
    public byte Sex => this.Struct->Sex;

    /// <summary>
    /// Gets the classjob of this party member.
    /// </summary>
    public ExcelResolver<Lumina.Excel.GeneratedSheets.ClassJob> ClassJob => new(this.Struct->ClassJob);

    /// <summary>
    /// Gets the level of this party member.
    /// </summary>
    public byte Level => this.Struct->Level;

    private FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember*)this.Address;
}
