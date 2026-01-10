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
    /// Gets a value indicating whether this entity is a player.
    /// </summary>
    bool IsPlayer { get; }
}

/// <summary>
/// This struct represents an entity related to a log message.
/// </summary>
/// <param name="ptr">A pointer to the log message item.</param>
/// <param name="source">If <see langword="true"/> represents the source entity of the log message, otherwise represents the target entity.</param>
internal unsafe readonly struct LogMessageEntity(LogMessageQueueItem* ptr, bool source) : ILogMessageEntity
{
    /// <inheritdoc/>
    public ReadOnlySeString Name => new(this.NameSpan[..this.NameSpan.IndexOf((byte)0)]);

    /// <inheritdoc/>
    public ushort HomeWorldId => source ? ptr->SourceHomeWorld : ptr->TargetHomeWorld;

    /// <inheritdoc/>
    public RowRef<World> HomeWorld => LuminaUtils.CreateRef<World>(this.HomeWorldId);

    /// <inheritdoc/>
    public uint ObjStrId => source ? ptr->SourceObjStrId : ptr->TargetObjStrId;

    /// <inheritdoc/>
    public bool IsPlayer => source ? ptr->SourceIsPlayer : ptr->TargetIsPlayer;

    /// <summary>
    /// Gets the Span containing the raw name of this entity.
    /// </summary>
    internal Span<byte> NameSpan => source ? ptr->SourceName : ptr->TargetName;

    /// <summary>
    /// Gets the kind of the entity.
    /// </summary>
    internal byte Kind => source ? (byte)ptr->SourceKind : (byte)ptr->TargetKind;

    /// <summary>
    /// Gets the Sex of this entity.
    /// </summary>
    internal byte Sex => source ? ptr->SourceSex : ptr->TargetSex;

    /// <summary>
    /// Gets a value indicating whether this entity is the source entity of a log message.
    /// </summary>
    internal bool IsSourceEntity => source;

    public static bool operator ==(LogMessageEntity x, LogMessageEntity y) => x.Equals(y);

    public static bool operator !=(LogMessageEntity x, LogMessageEntity y) => !(x == y);

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

    private bool Equals(LogMessageEntity other)
    {
        return this.Name == other.Name && this.HomeWorldId == other.HomeWorldId && this.ObjStrId == other.ObjStrId && this.Kind == other.Kind && this.Sex == other.Sex && this.IsPlayer == other.IsPlayer;
    }
}
