namespace Dalamud.Game.ClientState.Objects.Enums;

/// <summary>
/// An Enum describing possible BattleNpc kinds.
/// </summary>
public enum BattleNpcSubKind : byte
{
    /// <summary>
    /// Invalid BattleNpc.
    /// </summary>
    None = 0,

    /// <summary>
    /// Weak Spots / Battle NPC parts
    /// Eg: Titan's Heart (Naval), Tioman's left and right wing (Sohm Al), Golem Soulstone (The Sunken Temple of Qarn).
    /// </summary>
    BattleNpcPart = 1,

    /// <summary>
    /// BattleNpc representing a Pet.
    /// </summary>
    Pet = 2,

    /// <summary>
    /// BattleNpc representing a Chocobo.
    /// </summary>
    Chocobo = 3,

    /// <summary>
    /// BattleNpc representing a standard enemy. This includes allies (overworld guards and allies in single-player duties).
    /// </summary>
    Enemy = 5,

    /// <summary>
    /// BattleNpc representing an NPC party member (from Duty Support, Trust, or Grand Company Command Mission).
    /// </summary>
    NpcPartyMember = 9,
}
