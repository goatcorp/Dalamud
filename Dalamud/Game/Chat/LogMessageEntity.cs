using System.Diagnostics.CodeAnalysis;

using Dalamud.Data;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI.Misc;

using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Chat;

/// <summary>
/// Interface representing an entity related to a log message.
/// </summary>
public interface ILogMessageEntity : IEquatable<ILogMessageEntity>
{
    /// <summary>
    /// Gets the name of this entity.
    /// </summary>
    ReadOnlySeString Name { get; }

    /// <summary>
    /// Gets the ID of the homeworld of this entity, if it is a player.
    /// </summary>
    ushort HomeWorldId { get; }

    /// <summary>
    /// Gets the homeworld of this entity, if it is a player.
    /// </summary>
    RowRef<World> HomeWorld { get; }

    /// <summary>
    /// Gets the ObjStr ID of this entity, if not a player. See <seealso cref="ISeStringEvaluator.EvaluateObjStr"/>.
    /// </summary>
    uint ObjStrId { get; }

    /// <summary>
    /// Gets a boolean indicating if this entity is a player.
    /// </summary>
    bool IsPlayer { get; }
}


/// <summary>
/// This struct represents an entity related to a log message.
/// </summary>
/// <param name="ptr">A pointer to the log message item.</param>
/// <param name="source">If <see langword="true"/> represents the source entity of the log message, otherwise represents the target entity</param>
internal unsafe readonly struct LogMessageEntity(LogMessageQueueItem* ptr, bool source) : ILogMessageEntity
{
    public Span<byte> NameSpan => source ? ptr->SourceName : ptr->TargetName;

    public ReadOnlySeString Name => new ReadOnlySeString(this.NameSpan);

    public ushort HomeWorldId => source ? ptr->SourceHomeWorld : ptr->TargetHomeWorld;

    public RowRef<World> HomeWorld => LuminaUtils.CreateRef<World>(this.HomeWorldId);

    public uint ObjStrId => source ? ptr->SourceObjStrId : ptr->TargetObjStrId;

    public byte Kind => source ? (byte)ptr->SourceKind : (byte)ptr->TargetKind;

    public byte Sex => source ? ptr->SourceSex : ptr->TargetSex;

    public bool IsPlayer => source ? ptr->SourceIsPlayer : ptr->TargetIsPlayer;

    public bool IsSourceEntity => source;

    public static bool operator ==(LogMessageEntity x, LogMessageEntity y) => x.Equals(y);

    public static bool operator !=(LogMessageEntity x, LogMessageEntity y) => !(x == y);

    public bool Equals(LogMessageEntity other)
    {
        return this.Name == other.Name && this.HomeWorldId == other.HomeWorldId && this.ObjStrId == other.ObjStrId && this.Kind == other.Kind && this.Sex == other.Sex && this.IsPlayer == other.IsPlayer;
    }

    /// <inheritdoc/>
    public bool Equals(ILogMessageEntity other)
    {
        return other is LogMessageEntity entity && this.Equals(entity);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is LogMessageEntity entity && this.Equals(entity);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.Name, this.HomeWorldId, this.ObjStrId, this.Sex, this.IsPlayer);
    }
}
