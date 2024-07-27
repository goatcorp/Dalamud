namespace Dalamud.Game.Gui.NamePlate;

/// <summary>
/// An enum describing what kind of game object this nameplate represents.
/// </summary>
public enum NamePlateKind : byte
{
    /// <summary>
    /// A player character.
    /// </summary>
    PlayerCharacter = 0,

    /// <summary>
    /// An event NPC or companion.
    /// </summary>
    EventNpcCompanion = 1,

    /// <summary>
    /// A retainer.
    /// </summary>
    Retainer = 2,

    /// <summary>
    /// An enemy battle NPC.
    /// </summary>
    BattleNpcEnemy = 3,

    /// <summary>
    /// A friendly battle NPC.
    /// </summary>
    BattleNpcFriendly = 4,

    /// <summary>
    /// An event object.
    /// </summary>
    EventObject = 5,

    /// <summary>
    /// Treasure.
    /// </summary>
    Treasure = 6,

    /// <summary>
    /// A gathering point.
    /// </summary>
    GatheringPoint = 7,

    /// <summary>
    /// A battle NPC with subkind 6.
    /// </summary>
    BattleNpcSubkind6 = 8,

    /// <summary>
    /// Something else.
    /// </summary>
    Other = 9,
}
