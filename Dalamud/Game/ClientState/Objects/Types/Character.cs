using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Resolvers;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Game.ClientState.Objects.Types;

/// <summary>
/// This class represents the base for non-static entities.
/// </summary>
public unsafe class Character : GameObject
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

    /// <summary>
    /// Gets the current HP of this Chara.
    /// </summary>
    public uint CurrentHp => this.Struct->CharacterData.Health;

    /// <summary>
    /// Gets the maximum HP of this Chara.
    /// </summary>
    public uint MaxHp => this.Struct->CharacterData.MaxHealth;

    /// <summary>
    /// Gets the current MP of this Chara.
    /// </summary>
    public uint CurrentMp => this.Struct->CharacterData.Mana;

    /// <summary>
    /// Gets the maximum MP of this Chara.
    /// </summary>
    public uint MaxMp => this.Struct->CharacterData.MaxMana;

    /// <summary>
    /// Gets the current GP of this Chara.
    /// </summary>
    public uint CurrentGp => this.Struct->CharacterData.GatheringPoints;

    /// <summary>
    /// Gets the maximum GP of this Chara.
    /// </summary>
    public uint MaxGp => this.Struct->CharacterData.MaxGatheringPoints;

    /// <summary>
    /// Gets the current CP of this Chara.
    /// </summary>
    public uint CurrentCp => this.Struct->CharacterData.CraftingPoints;

    /// <summary>
    /// Gets the maximum CP of this Chara.
    /// </summary>
    public uint MaxCp => this.Struct->CharacterData.MaxCraftingPoints;

    /// <summary>
    /// Gets the shield percentage of this Chara.
    /// </summary>
    public byte ShieldPercentage => this.Struct->CharacterData.ShieldValue;

    /// <summary>
    /// Gets the ClassJob of this Chara.
    /// </summary>
    public ExcelResolver<ClassJob> ClassJob => new(this.Struct->CharacterData.ClassJob);

    /// <summary>
    /// Gets the level of this Chara.
    /// </summary>
    public byte Level => this.Struct->CharacterData.Level;

    /// <summary>
    /// Gets a byte array describing the visual appearance of this Chara.
    /// Indexed by <see cref="CustomizeIndex"/>.
    /// </summary>
    public byte[] Customize => MemoryHelper.Read<byte>((IntPtr)this.Struct->DrawData.CustomizeData.Data, 28);

    /// <summary>
    /// Gets the Free Company tag of this chara.
    /// </summary>
    public SeString CompanyTag => MemoryHelper.ReadSeString((IntPtr)this.Struct->FreeCompanyTag, 6);

    /// <summary>
    /// Gets the target object ID of the character.
    /// </summary>
    public override ulong TargetObjectId => this.Struct->TargetId;

    /// <summary>
    /// Gets the name ID of the character.
    /// </summary>
    public uint NameId => this.Struct->NameID;

    /// <summary>
    /// Gets the current online status of the character.
    /// </summary>
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

    /// <summary>
    /// Gets the underlying structure.
    /// </summary>
    protected internal new FFXIVClientStructs.FFXIV.Client.Game.Character.Character* Struct =>
        (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)this.Address;
}
