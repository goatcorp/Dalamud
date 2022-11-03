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
    /// BattleNpc representing a Pet.
    /// </summary>
    Pet = 2,

    /// <summary>
    /// BattleNpc representing a Chocobo.
    /// </summary>
    Chocobo = 3,

    /// <summary>
    /// BattleNpc representing a standard enemy.
    /// </summary>
    Enemy = 5,
}
