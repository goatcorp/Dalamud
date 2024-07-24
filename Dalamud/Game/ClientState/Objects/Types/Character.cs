using System.Runtime.CompilerServices;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Resolvers;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Game.ClientState.Objects.Types;

/// <summary>
/// Interface representing a character.
/// </summary>
public interface ICharacter : IGameObject
{
    /// <summary>
    /// Gets the current HP of this Chara.
    /// </summary>
    public uint CurrentHp { get; }

    /// <summary>
    /// Gets the maximum HP of this Chara.
    /// </summary>
    public uint MaxHp { get; }

    /// <summary>
    /// Gets the current MP of this Chara.
    /// </summary>
    public uint CurrentMp { get; }

    /// <summary>
    /// Gets the maximum MP of this Chara.
    /// </summary>
    public uint MaxMp { get; }

    /// <summary>
    /// Gets the current GP of this Chara.
    /// </summary>
    public uint CurrentGp { get; }

    /// <summary>
    /// Gets the maximum GP of this Chara.
    /// </summary>
    public uint MaxGp { get; }

    /// <summary>
    /// Gets the current CP of this Chara.
    /// </summary>
    public uint CurrentCp { get; }

    /// <summary>
    /// Gets the maximum CP of this Chara.
    /// </summary>
    public uint MaxCp { get; }

    /// <summary>
    /// Gets the shield percentage of this Chara.
    /// </summary>
    public byte ShieldPercentage { get; }

    /// <summary>
    /// Gets the ClassJob of this Chara.
    /// </summary>
    public ExcelResolver<ClassJob> ClassJob { get; }

    /// <summary>
    /// Gets the level of this Chara.
    /// </summary>
    public byte Level { get; }

    /// <summary>
    /// Gets a byte array describing the visual appearance of this Chara.
    /// Indexed by <see cref="CustomizeIndex"/>.
    /// </summary>
    public byte[] Customize { get; }

    /// <summary>
    /// Gets the Free Company tag of this chara.
    /// </summary>
    public SeString CompanyTag { get; }

    /// <summary>
    /// Gets the name ID of the character.
    /// </summary>
    public uint NameId { get; }

    /// <summary>
    /// Gets the current online status of the character.
    /// </summary>
    public ExcelResolver<OnlineStatus> OnlineStatus { get; }

    /// <summary>
    /// Gets the status flags.
    /// </summary>
    public StatusFlags StatusFlags { get; }
    
    /// <summary>
    /// Gets the current mount for this character. Will be <c>null</c> if the character doesn't have a mount.
    /// </summary>
    public ExcelResolver<Mount>? CurrentMount { get; }
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
    internal Character(IntPtr address)
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
    public ExcelResolver<ClassJob> ClassJob => new(this.Struct->CharacterData.ClassJob);

    /// <inheritdoc/>
    public byte Level => this.Struct->CharacterData.Level;

    /// <inheritdoc/>
    public byte[] Customize => this.Struct->DrawData.CustomizeData.Data.ToArray();

    /// <inheritdoc/>
    public SeString CompanyTag => MemoryHelper.ReadSeString((nint)Unsafe.AsPointer(ref this.Struct->FreeCompanyTag[0]), 6);

    /// <summary>
    /// Gets the target object ID of the character.
    /// </summary>
    public override ulong TargetObjectId => this.Struct->TargetId;

    /// <inheritdoc/>
    public uint NameId => this.Struct->NameId;

    /// <inheritdoc/>
    public ExcelResolver<OnlineStatus> OnlineStatus => new(this.Struct->CharacterData.OnlineStatus);

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
    public ExcelResolver<Mount>? CurrentMount
    {
        get
        {
            if (this.Struct->IsNotMounted()) return null; // safety i guess?
            
            var mountId = this.Struct->Mount.MountId;
            return mountId == 0 ? null : new ExcelResolver<Mount>(mountId);
        }
    }

    /// <summary>
    /// Gets the underlying structure.
    /// </summary>
    protected internal new FFXIVClientStructs.FFXIV.Client.Game.Character.Character* Struct =>
        (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)this.Address;
}
