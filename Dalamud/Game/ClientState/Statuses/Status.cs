using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Resolvers;

namespace Dalamud.Game.ClientState.Statuses;

/// <summary>
/// This class represents a status effect an actor is afflicted by.
/// </summary>
public unsafe class Status
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Status"/> class.
    /// </summary>
    /// <param name="address">Status address.</param>
    internal Status(IntPtr address)
    {
        this.Address = address;
    }

    /// <summary>
    /// Gets the address of the status in memory.
    /// </summary>
    public IntPtr Address { get; }

    /// <summary>
    /// Gets the status ID of this status.
    /// </summary>
    public uint StatusId => this.Struct->StatusID;

    /// <summary>
    /// Gets the GameData associated with this status.
    /// </summary>
    public Lumina.Excel.GeneratedSheets.Status GameData => new ExcelResolver<Lumina.Excel.GeneratedSheets.Status>(this.Struct->StatusID).GameData;

    /// <summary>
    /// Gets the parameter value of the status.
    /// </summary>
    public ushort Param => this.Struct->Param;

    /// <summary>
    /// Gets the stack count of this status.
    /// </summary>
    public byte StackCount => this.Struct->StackCount;

    /// <summary>
    /// Gets the time remaining of this status.
    /// </summary>
    public float RemainingTime => this.Struct->RemainingTime;

    /// <summary>
    /// Gets the source ID of this status.
    /// </summary>
    public uint SourceId => this.Struct->SourceID;

    /// <summary>
    /// Gets the source actor associated with this status.
    /// </summary>
    /// <remarks>
    /// This iterates the actor table, it should be used with care.
    /// </remarks>
    public GameObject? SourceObject => Service<ObjectTable>.Get().SearchById(this.SourceId);

    private FFXIVClientStructs.FFXIV.Client.Game.Status* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Status*)this.Address;
}
