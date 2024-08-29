using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;

namespace Dalamud.Game.ClientState.Objects.Types;

/// <summary>
/// Interface representing a game object.
/// </summary>
public interface IGameObject : IEquatable<IGameObject>
{
    /// <summary>
    /// Gets the name of this <see cref="GameObject" />.
    /// </summary>
    public SeString Name { get; }

    /// <summary>
    /// Gets the GameObjectID for this GameObject. The Game Object ID is a globally unique identifier that points to
    /// this specific object. This ID is used to reference specific objects on the local client (e.g. for targeting).
    ///
    /// Not to be confused with <see cref="EntityId"/>.
    /// </summary>
    public ulong GameObjectId { get; }

    /// <summary>
    /// Gets the Entity ID for this GameObject. Entity IDs are assigned to networked GameObjects.
    ///
    /// A value of <c>0xE000_0000</c> indicates that this entity is not networked and has specific interactivity rules.
    /// </summary>
    public uint EntityId { get; }

    /// <summary>
    /// Gets the data ID for linking to other respective game data.
    /// </summary>
    public uint DataId { get; }

    /// <summary>
    /// Gets the ID of this GameObject's owner.
    /// </summary>
    public uint OwnerId { get; }

    /// <summary>
    /// Gets the index of this object in the object table.
    /// </summary>
    public ushort ObjectIndex { get; }

    /// <summary>
    /// Gets the entity kind of this <see cref="GameObject" />.
    /// See <see cref="ObjectKind">the ObjectKind enum</see> for possible values.
    /// </summary>
    public ObjectKind ObjectKind { get; }

    /// <summary>
    /// Gets the sub kind of this Actor.
    /// </summary>
    public byte SubKind { get; }

    /// <summary>
    /// Gets the X distance from the local player in yalms.
    /// </summary>
    public byte YalmDistanceX { get; }

    /// <summary>
    /// Gets the Y distance from the local player in yalms.
    /// </summary>
    public byte YalmDistanceZ { get; }

    /// <summary>
    /// Gets a value indicating whether the object is dead or alive.
    /// </summary>
    public bool IsDead { get; }

    /// <summary>
    /// Gets a value indicating whether the object is targetable.
    /// </summary>
    public bool IsTargetable { get; }

    /// <summary>
    /// Gets the position of this <see cref="GameObject" />.
    /// This is in yalms (x+ = east, x- = west, y+ = up, y- down, z+ = south, z- = north) (the numbers under the map have Y and Z are swapped and are not in yalms).
    /// </summary>
    public Vector3 Position { get; }

    /// <summary>
    /// Gets the rotation of this <see cref="GameObject" />.
    /// This ranges from -pi to pi radians (counterclockwise from south; for Atan2 trigonometry, use Position.Z as the x axis and Position.X as the y axis).
    /// </summary>
    public float Rotation { get; }

    /// <summary>
    /// Gets the hitbox radius of this <see cref="GameObject" />.
    /// </summary>
    public float HitboxRadius { get; }

    /// <summary>
    /// Gets the current target of the game object.
    /// </summary>
    public ulong TargetObjectId { get; }

    /// <summary>
    /// Gets the target object of the game object.
    /// </summary>
    /// <remarks>
    /// This iterates the actor table, it should be used with care.
    /// </remarks>
    // TODO: Fix for non-networked GameObjects
    public IGameObject? TargetObject { get; }

    /// <summary>
    /// Gets the address of the game object in memory.
    /// </summary>
    public IntPtr Address { get; }

    /// <summary>
    /// Gets a value indicating whether this actor is still valid in memory.
    /// </summary>
    /// <returns>True or false.</returns>
    public bool IsValid();
}

/// <summary>
/// This class represents a GameObject in FFXIV.
/// </summary>
internal partial class GameObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameObject"/> class.
    /// </summary>
    /// <param name="address">The address of this game object in memory.</param>
    internal GameObject(IntPtr address)
    {
        this.Address = address;
    }

    /// <summary>
    /// Gets or sets the address of the game object in memory.
    /// </summary>
    public IntPtr Address { get; internal set; }

    /// <summary>
    /// This allows you to <c>if (obj) {...}</c> to check for validity.
    /// </summary>
    /// <param name="gameObject">The actor to check.</param>
    /// <returns>True or false.</returns>
    public static implicit operator bool(GameObject? gameObject) => IsValid(gameObject);

    public static bool operator ==(GameObject? gameObject1, GameObject? gameObject2)
    {
        // Using == results in a stack overflow.
        if (gameObject1 is null || gameObject2 is null)
            return Equals(gameObject1, gameObject2);

        return gameObject1.Equals(gameObject2);
    }

    public static bool operator !=(GameObject? actor1, GameObject? actor2) => !(actor1 == actor2);

    /// <summary>
    /// Gets a value indicating whether this actor is still valid in memory.
    /// </summary>
    /// <param name="actor">The actor to check.</param>
    /// <returns>True or false.</returns>
    public static bool IsValid(IGameObject? actor)
    {
        var clientState = Service<ClientState>.GetNullable();

        if (actor is null || clientState == null)
            return false;

        if (clientState.LocalContentId == 0)
            return false;

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether this actor is still valid in memory.
    /// </summary>
    /// <returns>True or false.</returns>
    public bool IsValid() => IsValid(this);

    /// <inheritdoc/>
    bool IEquatable<IGameObject>.Equals(IGameObject other) => this.GameObjectId == other?.GameObjectId;

    /// <inheritdoc/>
    public override bool Equals(object obj) => ((IEquatable<IGameObject>)this).Equals(obj as IGameObject);

    /// <inheritdoc/>
    public override int GetHashCode() => this.GameObjectId.GetHashCode();
}

/// <summary>
/// This class represents a basic actor (GameObject) in FFXIV.
/// </summary>
internal unsafe partial class GameObject : IGameObject
{
    /// <inheritdoc/>
    public SeString Name => MemoryHelper.ReadSeString((nint)Unsafe.AsPointer(ref this.Struct->Name[0]), 64);

    /// <inheritdoc/>
    public ulong GameObjectId => this.Struct->GetGameObjectId();

    /// <inheritdoc/>
    public uint EntityId => this.Struct->EntityId;

    /// <inheritdoc/>
    public uint DataId => this.Struct->BaseId;

    /// <inheritdoc/>
    public uint OwnerId => this.Struct->OwnerId;

    /// <inheritdoc/>
    public ushort ObjectIndex => this.Struct->ObjectIndex;

    /// <inheritdoc/>
    public ObjectKind ObjectKind => (ObjectKind)this.Struct->ObjectKind;

    /// <inheritdoc/>
    public byte SubKind => this.Struct->SubKind;

    /// <inheritdoc/>
    public byte YalmDistanceX => this.Struct->YalmDistanceFromPlayerX;

    /// <inheritdoc/>
    public byte YalmDistanceZ => this.Struct->YalmDistanceFromPlayerZ;

    /// <inheritdoc/>
    public bool IsDead => this.Struct->IsDead();

    /// <inheritdoc/>
    public bool IsTargetable => this.Struct->GetIsTargetable();

    /// <inheritdoc/>
    public Vector3 Position => new(this.Struct->Position.X, this.Struct->Position.Y, this.Struct->Position.Z);

    /// <inheritdoc/>
    public float Rotation => this.Struct->Rotation;

    /// <inheritdoc/>
    public float HitboxRadius => this.Struct->HitboxRadius;

    /// <inheritdoc/>
    public virtual ulong TargetObjectId => 0;

    /// <inheritdoc/>
    // TODO: Fix for non-networked GameObjects
    public virtual IGameObject? TargetObject => Service<ObjectTable>.Get().SearchById(this.TargetObjectId);

    /// <summary>
    /// Gets the underlying structure.
    /// </summary>
    protected internal FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)this.Address;

    /// <inheritdoc/>
    public override string ToString() => $"{this.GameObjectId:X}({this.Name.TextValue} - {this.ObjectKind}) at {this.Address:X}";
}
