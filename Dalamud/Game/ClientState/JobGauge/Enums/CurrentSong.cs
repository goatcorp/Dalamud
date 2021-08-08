namespace Dalamud.Game.ClientState.JobGauge.Enums
{
    /// <summary>
    /// BRD Current Song types.
    /// </summary>
    public enum CurrentSong : byte
    {
        /// <summary>
        /// No song is active type.
        /// </summary>
        NONE = 0,

        /// <summary>
        /// Mage's Ballad type.
        /// </summary>
        MAGE = 5,

        /// <summary>
        /// Army's Paeon type.
        /// </summary>
        ARMY = 0xA,

        /// <summary>
        /// The Wanderer's Minuet type.
        /// </summary>
        WANDERER = 0xF,
    }
}
