using System.Diagnostics.CodeAnalysis;

using Dalamud.Data;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;

using Lumina.Excel;

using CSBuddyMember = FFXIVClientStructs.FFXIV.Client.Game.UI.Buddy.BuddyMember;

namespace Dalamud.Game.ClientState.Buddy;

/// <summary>
/// Interface representing represents a buddy such as the chocobo companion, summoned pets, squadron groups and trust parties.
/// </summary>
public interface IBuddyMember : IEquatable<IBuddyMember>
{
    /// <summary>
    /// Gets the address of the buddy in memory.
    /// </summary>
    nint Address { get; }

    /// <summary>
    /// Gets the object ID of this buddy.
    /// </summary>
    [Obsolete("Renamed to EntityId")]
    uint ObjectId { get; }

    /// <summary>
    /// Gets the entity ID of this buddy.
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
    /// Gets the current health of this buddy.
    /// </summary>
    uint CurrentHP { get; }

    /// <summary>
    /// Gets the maximum health of this buddy.
    /// </summary>
    uint MaxHP { get; }

    /// <summary>
    /// Gets the data ID of this buddy.
    /// </summary>
    uint DataID { get; }

    /// <summary>
    /// Gets the Mount data related to this buddy. It should only be used with companion buddies.
    /// </summary>
    RowRef<Lumina.Excel.Sheets.Mount> MountData { get; }

    /// <summary>
    /// Gets the Pet data related to this buddy. It should only be used with pet buddies.
    /// </summary>
    RowRef<Lumina.Excel.Sheets.Pet> PetData { get; }

    /// <summary>
    /// Gets the Trust data related to this buddy. It should only be used with battle buddies.
    /// </summary>
    RowRef<Lumina.Excel.Sheets.DawnGrowMember> TrustData { get; }
}

/// <summary>
/// This struct represents a buddy such as the chocobo companion, summoned pets, squadron groups and trust parties.
/// </summary>
/// <param name="ptr">A pointer to the BuddyMember.</param>
internal readonly unsafe struct BuddyMember(CSBuddyMember* ptr) : IBuddyMember
{
    [ServiceManager.ServiceDependency]
    private readonly ObjectTable objectTable = Service<ObjectTable>.Get();

    /// <inheritdoc />
    public nint Address => (nint)ptr;

    /// <inheritdoc />
    public uint ObjectId => this.EntityId;

    /// <inheritdoc />
    public uint EntityId => ptr->EntityId;

    /// <inheritdoc />
    public IGameObject? GameObject => this.objectTable.SearchById(this.EntityId);

    /// <inheritdoc />
    public uint CurrentHP => ptr->CurrentHealth;

    /// <inheritdoc />
    public uint MaxHP => ptr->MaxHealth;

    /// <inheritdoc />
    public uint DataID => ptr->DataId;

    /// <inheritdoc />
    public RowRef<Lumina.Excel.Sheets.Mount> MountData => LuminaUtils.CreateRef<Lumina.Excel.Sheets.Mount>(this.DataID);

    /// <inheritdoc />
    public RowRef<Lumina.Excel.Sheets.Pet> PetData => LuminaUtils.CreateRef<Lumina.Excel.Sheets.Pet>(this.DataID);

    /// <inheritdoc />
    public RowRef<Lumina.Excel.Sheets.DawnGrowMember> TrustData => LuminaUtils.CreateRef<Lumina.Excel.Sheets.DawnGrowMember>(this.DataID);

    public static bool operator ==(BuddyMember x, BuddyMember y) => x.Equals(y);

    public static bool operator !=(BuddyMember x, BuddyMember y) => !(x == y);

    /// <inheritdoc/>
    public bool Equals(IBuddyMember? other)
    {
        return this.EntityId == other.EntityId;
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is BuddyMember fate && this.Equals(fate);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return this.EntityId.GetHashCode();
    }
}
