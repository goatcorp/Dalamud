namespace Dalamud.Game.ClientState.Objects.Enums;

/// <summary>
/// Enum describing possible status flags.
/// </summary>
[Flags]
public enum StatusFlags : byte
{
    /// <summary>
    /// No status flags set.
    /// </summary>
    None = 0,

    /// <summary>
    /// Hostile character.
    /// </summary>
    Hostile = 1,

    /// <summary>
    /// Character in combat.
    /// </summary>
    InCombat = 2,

    /// <summary>
    /// Character weapon is out.
    /// </summary>
    WeaponOut = 4,

    /// <summary>
    /// Character offhand is out.
    /// </summary>
    OffhandOut = 8,

    /// <summary>
    /// Character is a party member.
    /// </summary>
    PartyMember = 16,

    /// <summary>
    /// Character is a alliance member.
    /// </summary>
    AllianceMember = 32,

    /// <summary>
    /// Character is in friend list.
    /// </summary>
    Friend = 64,

    /// <summary>
    /// Character is casting.
    /// </summary>
    IsCasting = 128,
}
