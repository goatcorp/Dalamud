using System.Diagnostics.CodeAnalysis;

using Dalamud.Data;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;

using Lumina.Excel;

using CSStatus = FFXIVClientStructs.FFXIV.Client.Game.Status;

namespace Dalamud.Game.ClientState.Statuses;

/// <summary>
/// Interface representing a status.
/// </summary>
public interface IStatus : IEquatable<IStatus>
{
    /// <summary>
    /// Gets the address of the status in memory.
    /// </summary>
    nint Address { get; }

    /// <summary>
    /// Gets the status ID of this status.
    /// </summary>
    uint StatusId { get; }

    /// <summary>
    /// Gets the GameData associated with this status.
    /// </summary>
    RowRef<Lumina.Excel.Sheets.Status> GameData { get; }

    /// <summary>
    /// Gets the parameter value of the status.
    /// </summary>
    ushort Param { get; }

    /// <summary>
    /// Gets the time remaining of this status.
    /// </summary>
    float RemainingTime { get; }

    /// <summary>
    /// Gets the source ID of this status.
    /// </summary>
    uint SourceId { get; }

    /// <summary>
    /// Gets the source actor associated with this status.
    /// </summary>
    /// <remarks>
    /// This iterates the actor table, it should be used with care.
    /// </remarks>
    IGameObject? SourceObject { get; }
}

/// <summary>
/// This struct represents a status effect an actor is afflicted by.
/// </summary>
/// <param name="ptr">A pointer to the Status.</param>
internal unsafe readonly struct Status(CSStatus* ptr) : IStatus
{
    /// <inheritdoc/>
    public nint Address => (nint)ptr;

    /// <inheritdoc/>
    public uint StatusId => ptr->StatusId;

    /// <inheritdoc/>
    public RowRef<Lumina.Excel.Sheets.Status> GameData => LuminaUtils.CreateRef<Lumina.Excel.Sheets.Status>(ptr->StatusId);

    /// <inheritdoc/>
    public ushort Param => ptr->Param;

    /// <inheritdoc/>
    public float RemainingTime => ptr->RemainingTime;

    /// <inheritdoc/>
    public uint SourceId => ptr->SourceObject.ObjectId;

    /// <inheritdoc/>
    public IGameObject? SourceObject => Service<ObjectTable>.Get().SearchById(this.SourceId);

    public static bool operator ==(Status x, Status y) => x.Equals(y);

    public static bool operator !=(Status x, Status y) => !(x == y);

    /// <inheritdoc/>
    public bool Equals(IStatus? other)
    {
        return this.StatusId == other.StatusId && this.SourceId == other.SourceId && this.Param == other.Param && this.RemainingTime == other.RemainingTime;
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Status fate && this.Equals(fate);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.StatusId, this.SourceId, this.Param, this.RemainingTime);
    }
}
