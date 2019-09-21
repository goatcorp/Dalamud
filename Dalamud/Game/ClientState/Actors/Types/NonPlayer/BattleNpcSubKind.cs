namespace Dalamud.Game.ClientState.Actors.Types.NonPlayer {
    /// <summary>
    ///     Enum describing possible BattleNpc kinds.
    /// </summary>
    public enum BattleNpcSubKind : byte {
        /// <summary>
        ///     Invalid BattleNpc.
        /// </summary>
        None = 0,

        /// <summary>
        ///     BattleNpc representing a Pet.
        /// </summary>
        Pet = 2,

        /// <summary>
        ///     BattleNpc representing a standard enemy.
        /// </summary>
        Enemy = 5
    }
}
