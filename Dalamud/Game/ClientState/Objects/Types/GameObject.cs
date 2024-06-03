using System.Numerics;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;

namespace Dalamud.Game.ClientState.Objects.Types;

/// <summary>
/// This class represents a GameObject in FFXIV.
/// </summary>
public unsafe partial class GameObject : IEquatable<GameObject>
{
    /// <summary>
    /// IDs of non-networked GameObjects.
    /// </summary>
    public const uint InvalidGameObjectId = 0xE0000000;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameObject"/> class.
    /// </summary>
    /// <param name="address">The address of this game object in memory.</param>
    internal GameObject(IntPtr address)
    {
        this.Address = address;
    }

    /// <summary>
    /// Gets the address of the game object in memory.
    /// </summary>
    public IntPtr Address { get; internal set; }

    /// <summary>
    /// Gets the Dalamud instance.
    /// </summary>
    private protected Dalamud Dalamud { get; }

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
    public static bool IsValid(GameObject? actor)
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
    bool IEquatable<GameObject>.Equals(GameObject other) => this.ObjectId == other?.ObjectId;

    /// <inheritdoc/>
    public override bool Equals(object obj) => ((IEquatable<GameObject>)this).Equals(obj as GameObject);

    /// <inheritdoc/>
    public override int GetHashCode() => this.ObjectId.GetHashCode();
}

/// <summary>
/// This class represents a basic actor (GameObject) in FFXIV.
/// </summary>
public unsafe partial class GameObject
{
    /// <summary>
    /// Gets the name of this <see cref="GameObject" />.
    /// </summary>
    public SeString Name => MemoryHelper.ReadSeString((IntPtr)this.Struct->Name, 64);

    /// <summary>
    /// Gets the object ID of this <see cref="GameObject" />.
    /// </summary>
    public uint ObjectId => this.Struct->ObjectID;

    /// <summary>
    /// Gets the data ID for linking to other respective game data.
    /// </summary>
    public uint DataId => this.Struct->DataID;

    /// <summary>
    /// Gets the ID of this GameObject's owner.
    /// </summary>
    public uint OwnerId => this.Struct->OwnerID;

    /// <summary>
    /// Gets the index of this object in the object table.
    /// </summary>
    public ushort ObjectIndex => this.Struct->ObjectIndex;

    /// <summary>
    /// Gets the entity kind of this <see cref="GameObject" />.
    /// See <see cref="ObjectKind">the ObjectKind enum</see> for possible values.
    /// </summary>
    public ObjectKind ObjectKind => (ObjectKind)this.Struct->ObjectKind;

    /// <summary>
    /// Gets the sub kind of this Actor.
    /// </summary>
    public byte SubKind => this.Struct->SubKind;

    /// <summary>
    /// Gets the X distance from the local player in yalms.
    /// </summary>
    public byte YalmDistanceX => this.Struct->YalmDistanceFromPlayerX;

    /// <summary>
    /// Gets the Y distance from the local player in yalms.
    /// </summary>
    public byte YalmDistanceZ => this.Struct->YalmDistanceFromPlayerZ;

    /// <summary>
    /// Gets a value indicating whether the object is dead or alive.
    /// </summary>
    public bool IsDead => this.Struct->IsDead();

    /// <summary>
    /// Gets a value indicating whether the object is targetable.
    /// </summary>
    public bool IsTargetable => this.Struct->GetIsTargetable();

    /// <summary>
    /// Gets the position of this <see cref="GameObject" />.
    /// </summary>
    public Vector3 Position => new(this.Struct->Position.X, this.Struct->Position.Y, this.Struct->Position.Z);

    /// <summary>
    /// Gets the rotation of this <see cref="GameObject" />.
    /// This ranges from -pi to pi radians.
    /// </summary>
    public float Rotation => this.Struct->Rotation;

    /// <summary>
    /// Gets the hitbox radius of this <see cref="GameObject" />.
    /// </summary>
    public float HitboxRadius => this.Struct->HitboxRadius;

    /// <summary>
    /// Gets the current target of the game object.
    /// </summary>
    public virtual ulong TargetObjectId => 0;

    /// <summary>
    /// Gets the target object of the game object.
    /// </summary>
    /// <remarks>
    /// This iterates the actor table, it should be used with care.
    /// </remarks>
    // TODO: Fix for non-networked GameObjects
    public virtual GameObject? TargetObject => Service<ObjectTable>.Get().SearchById(this.TargetObjectId);

    /// <summary>
    /// Gets the underlying structure.
    /// </summary>
    protected internal FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)this.Address;

    /// <inheritdoc/>
    public override string ToString() => $"{this.ObjectId:X}({this.Name.TextValue} - {this.ObjectKind}) at {this.Address:X}";
}
