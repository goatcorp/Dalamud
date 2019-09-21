namespace Dalamud.Game.ClientState.Actors {
    /// <summary>
    ///     Enum describing possible entity kinds.
    /// </summary>
    public enum ObjectKind : byte {
        /// <summary>
        ///     Invalid actor.
        /// </summary>
        None = 0x00,

        /// <summary>
        ///     Objects representing player characters.
        /// </summary>
        Player = 0x01,

        /// <summary>
        ///     Objects representing battle NPCs.
        /// </summary>
        BattleNpc = 0x02,

        /// <summary>
        ///     Objects representing event NPCs.
        /// </summary>
        EventNpc = 0x03,

        /// <summary>
        ///     Objects representing treasures.
        /// </summary>
        Treasure = 0x04,

        /// <summary>
        ///     Objects representing aetherytes.
        /// </summary>
        Aetheryte = 0x05,

        /// <summary>
        ///     Objects representing gathering points.
        /// </summary>
        GatheringPoint = 0x06,

        /// <summary>
        ///     Objects representing event objects.
        /// </summary>
        EventObj = 0x07,

        /// <summary>
        ///     Objects representing mounts.
        /// </summary>
        MountType = 0x08,

        /// <summary>
        ///     Objects representing minions.
        /// </summary>
        Companion = 0x09, // Minion

        /// <summary>
        ///     Objects representing retainers.
        /// </summary>
        Retainer = 0x0A,
        Area = 0x0B,

        /// <summary>
        ///     Objects representing housing objects.
        /// </summary>
        Housing = 0x0C,
        Cutscene = 0x0D,
        CardStand = 0x0E
    }
}
