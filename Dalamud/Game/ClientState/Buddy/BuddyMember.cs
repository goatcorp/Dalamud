using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Resolvers;

namespace Dalamud.Game.ClientState.Buddy;

/// <summary>
/// This class represents a buddy such as the chocobo companion, summoned pets, squadron groups and trust parties.
/// </summary>
internal unsafe class BuddyMember : IBuddyMember
{
    [ServiceManager.ServiceDependency]
    private readonly ObjectTable objectTable = Service<ObjectTable>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="BuddyMember"/> class.
    /// </summary>
    /// <param name="address">Buddy address.</param>
    internal BuddyMember(IntPtr address)
    {
        this.Address = address;
    }

    /// <inheritdoc />
    public IntPtr Address { get; }

    /// <inheritdoc />
    public uint ObjectId => this.Struct->EntityId;

    /// <inheritdoc />
    public IGameObject? GameObject => this.objectTable.SearchById(this.ObjectId);

    /// <inheritdoc />
    public uint CurrentHP => this.Struct->CurrentHealth;

    /// <inheritdoc />
    public uint MaxHP => this.Struct->MaxHealth;

    /// <inheritdoc />
    public uint DataID => this.Struct->DataId;

    /// <inheritdoc />
    public ExcelResolver<Lumina.Excel.GeneratedSheets.Mount> MountData => new(this.DataID);

    /// <inheritdoc />
    public ExcelResolver<Lumina.Excel.GeneratedSheets.Pet> PetData => new(this.DataID);

    /// <inheritdoc />
    public ExcelResolver<Lumina.Excel.GeneratedSheets.DawnGrowMember> TrustData => new(this.DataID);

    private FFXIVClientStructs.FFXIV.Client.Game.UI.Buddy.BuddyMember* Struct => (FFXIVClientStructs.FFXIV.Client.Game.UI.Buddy.BuddyMember*)this.Address;
}

/// <summary>
/// Interface representing represents a buddy such as the chocobo companion, summoned pets, squadron groups and trust parties.
/// </summary>
public interface IBuddyMember
{
    /// <summary>
    /// Gets the address of the buddy in memory.
    /// </summary>
    IntPtr Address { get; }

    /// <summary>
    /// Gets the object ID of this buddy.
    /// </summary>
    unsafe uint ObjectId { get; }

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
    unsafe uint CurrentHP { get; }

    /// <summary>
    /// Gets the maximum health of this buddy.
    /// </summary>
    unsafe uint MaxHP { get; }

    /// <summary>
    /// Gets the data ID of this buddy.
    /// </summary>
    unsafe uint DataID { get; }

    /// <summary>
    /// Gets the Mount data related to this buddy. It should only be used with companion buddies.
    /// </summary>
    ExcelResolver<Lumina.Excel.GeneratedSheets.Mount> MountData { get; }

    /// <summary>
    /// Gets the Pet data related to this buddy. It should only be used with pet buddies.
    /// </summary>
    ExcelResolver<Lumina.Excel.GeneratedSheets.Pet> PetData { get; }

    /// <summary>
    /// Gets the Trust data related to this buddy. It should only be used with battle buddies.
    /// </summary>
    ExcelResolver<Lumina.Excel.GeneratedSheets.DawnGrowMember> TrustData { get; }
}
