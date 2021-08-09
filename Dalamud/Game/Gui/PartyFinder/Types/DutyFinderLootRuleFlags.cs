using System;

namespace Dalamud.Game.Gui.PartyFinder.Types
{
    /// <summary>
    /// Loot rule flags for the <see cref="PartyFinder"/> class.
    /// </summary>
    [Flags]
    public enum DutyFinderLootRuleFlags : uint
    {
        /// <summary>
        /// No loot rules.
        /// </summary>
        None = 0,

        /// <summary>
        /// The greed only rule.
        /// </summary>
        GreedOnly = 1,

        /// <summary>
        /// The lootmaster rule.
        /// </summary>
        Lootmaster = 2,
    }
}
