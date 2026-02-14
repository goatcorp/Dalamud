using Dalamud.Data;
using Dalamud.Game.ClientState.Customize;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Dalamud.Game.ClientState.Objects.Types;

/// <summary>
/// Interface representing a character.
/// </summary>
public interface ICharacter : IGameObject
{
    /// <summary>
    /// Gets the current HP of this character.
    /// </summary>
    public uint CurrentHp { get; }

    /// <summary>
    /// Gets the maximum HP of this character.
    /// </summary>
    public uint MaxHp { get; }

    /// <summary>
    /// Gets the current MP of this character.
    /// </summary>
    public uint CurrentMp { get; }

    /// <summary>
    /// Gets the maximum MP of this character.
    /// </summary>
    public uint MaxMp { get; }

    /// <summary>
    /// Gets the current GP of this character.
    /// </summary>
    public uint CurrentGp { get; }

    /// <summary>
    /// Gets the maximum GP of this character.
    /// </summary>
    public uint MaxGp { get; }

    /// <summary>
    /// Gets the current CP of this character.
    /// </summary>
    public uint CurrentCp { get; }

    /// <summary>
    /// Gets the maximum CP of this character.
    /// </summary>
    public uint MaxCp { get; }

    /// <summary>
    /// Gets the shield percentage of this character.
    /// </summary>
    public byte ShieldPercentage { get; }

    /// <summary>
    /// Gets the ClassJob of this character.
    /// </summary>
    public RowRef<ClassJob> ClassJob { get; }

    /// <summary>
    /// Gets the level of this character.
    /// </summary>
    public byte Level { get; }

    /// <summary>
    /// Gets a byte array describing the visual appearance of this character.
    /// Indexed by <see cref="CustomizeIndex"/>.
    /// </summary>
    public byte[] Customize { get; }

    /// <summary>
    /// Gets the underlying CustomizeData struct for this character.
    /// </summary>
    public ICustomizeData CustomizeData { get; }

    /// <summary>
    /// Gets the Free Company tag of this character.
    /// </summary>
    public SeString CompanyTag { get; }

    /// <summary>
    /// Gets the name ID of the character.
    /// </summary>
    public uint NameId { get; }

    /// <summary>
    /// Gets the current online status of the character.
    /// </summary>
    public RowRef<OnlineStatus> OnlineStatus { get; }

    /// <summary>
    /// Gets the status flags.
    /// </summary>
    public StatusFlags StatusFlags { get; }

    /// <summary>
    /// Gets the current mount for this character. Will be <c>null</c> if the character doesn't have a mount.
    /// </summary>
    public RowRef<Mount>? CurrentMount { get; }

    /// <summary>
    /// Gets the current minion summoned for this character. Will be <c>null</c> if the character doesn't have a minion.
    /// This method *will* return information about a spawned (but invisible) minion, e.g. if the character is riding a
    /// mount.
    /// </summary>
    public RowRef<Companion>? CurrentMinion { get; }
}

/// <summary>
/// This class represents the base for non-static entities.
/// </summary>
internal unsafe class Character : GameObject, ICharacter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Character"/> class.
    /// This represents a non-static entity.
    /// </summary>
    /// <param name="address">The address of this character in memory.</param>
    internal Character(nint address)
        : base(address)
    {
    }

    /// <inheritdoc/>
    public uint CurrentHp => this.Struct->CharacterData.Health;

    /// <inheritdoc/>
    public uint MaxHp => this.Struct->CharacterData.MaxHealth;

    /// <inheritdoc/>
    public uint CurrentMp => this.Struct->CharacterData.Mana;

    /// <inheritdoc/>
    public uint MaxMp => this.Struct->CharacterData.MaxMana;

    /// <inheritdoc/>
    public uint CurrentGp => this.Struct->CharacterData.GatheringPoints;

    /// <inheritdoc/>
    public uint MaxGp => this.Struct->CharacterData.MaxGatheringPoints;

    /// <inheritdoc/>
    public uint CurrentCp => this.Struct->CharacterData.CraftingPoints;

    /// <inheritdoc/>
    public uint MaxCp => this.Struct->CharacterData.MaxCraftingPoints;

    /// <inheritdoc/>
    public byte ShieldPercentage => this.Struct->CharacterData.ShieldValue;

    /// <inheritdoc/>
    public RowRef<ClassJob> ClassJob => LuminaUtils.CreateRef<ClassJob>(this.Struct->CharacterData.ClassJob);

    /// <inheritdoc/>
    public byte Level => this.Struct->CharacterData.Level;

    /// <inheritdoc/>
    [Api15ToDo("Do not allocate on each call, use the CS Span and let consumers do allocation if necessary")]
    public byte[] Customize => this.Struct->DrawData.CustomizeData.Data.ToArray();

    /// <inheritdoc/>
    public ICustomizeData CustomizeData => new CustomizeData((nint)(&this.Struct->DrawData.CustomizeData));

    /// <inheritdoc/>
    public SeString CompanyTag => SeString.Parse(this.Struct->FreeCompanyTag);

    /// <summary>
    /// Gets the target object ID of the character.
    /// </summary>
    public override ulong TargetObjectId => this.Struct->TargetId;

    /// <inheritdoc/>
    public uint NameId => this.Struct->NameId;

    /// <inheritdoc/>
    public RowRef<OnlineStatus> OnlineStatus => LuminaUtils.CreateRef<OnlineStatus>(this.Struct->CharacterData.OnlineStatus);

    /// <summary>
    /// Gets the status flags.
    /// </summary>
    public StatusFlags StatusFlags =>
        (this.Struct->IsHostile ? StatusFlags.Hostile : StatusFlags.None) |
        (this.Struct->InCombat ? StatusFlags.InCombat : StatusFlags.None) |
        (this.Struct->IsWeaponDrawn ? StatusFlags.WeaponOut : StatusFlags.None) |
        (this.Struct->IsOffhandDrawn ? StatusFlags.OffhandOut : StatusFlags.None) |
        (this.Struct->IsPartyMember ? StatusFlags.PartyMember : StatusFlags.None) |
        (this.Struct->IsAllianceMember ? StatusFlags.AllianceMember : StatusFlags.None) |
        (this.Struct->IsFriend ? StatusFlags.Friend : StatusFlags.None) |
        (this.Struct->IsCasting ? StatusFlags.IsCasting : StatusFlags.None);

    /// <inheritdoc />
    public RowRef<Mount>? CurrentMount
    {
        get
        {
            if (this.Struct->IsNotMounted()) return null; // just for safety.

            var mountId = this.Struct->Mount.MountId;
            return mountId == 0 ? null : LuminaUtils.CreateRef<Mount>(mountId);
        }
    }

    /// <inheritdoc />
    public RowRef<Companion>? CurrentMinion
    {
        get
        {
            if (this.Struct->CompanionObject != null)
                return LuminaUtils.CreateRef<Companion>(this.Struct->CompanionObject->BaseId);

            // this is only present if a minion is summoned but hidden (e.g. the player's on a mount).
            var hiddenCompanionId = this.Struct->CompanionData.CompanionId;
            return hiddenCompanionId == 0 ? null : LuminaUtils.CreateRef<Companion>(hiddenCompanionId);
        }
    }

    /// <summary>
    /// Gets the underlying structure.
    /// </summary>
    protected internal new FFXIVClientStructs.FFXIV.Client.Game.Character.Character* Struct =>
        (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)this.Address;
}
