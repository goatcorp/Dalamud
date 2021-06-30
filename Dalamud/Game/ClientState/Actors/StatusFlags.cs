using System;

namespace Dalamud.Game.ClientState.Actors
{
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
        /// Hostile actor.
        /// </summary>
        Hostile = 1,

        /// <summary>
        /// Actor in combat.
        /// </summary>
        InCombat = 2,

        /// <summary>
        /// Actor weapon is out.
        /// </summary>
        WeaponOut = 4,

        /// <summary>
        /// Actor offhand is out.
        /// </summary>
        OffhandOut = 8,

        /// <summary>
        /// Actor is a party member.
        /// </summary>
        PartyMember = 16,

        /// <summary>
        /// Actor is a alliance member.
        /// </summary>
        AllianceMember = 32,

        /// <summary>
        /// Actor is in friend list.
        /// </summary>
        Friend = 64,

        /// <summary>
        /// Actor is casting.
        /// </summary>
        IsCasting = 128,
    }
}
