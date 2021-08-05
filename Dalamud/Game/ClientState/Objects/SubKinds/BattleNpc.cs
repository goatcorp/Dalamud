using System;

using Dalamud.Game.ClientState.Objects.Enums;

namespace Dalamud.Game.ClientState.Objects.Types
{
    /// <summary>
    /// This class represents a battle NPC.
    /// </summary>
    public unsafe class BattleNpc : BattleChara
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BattleNpc"/> class.
        /// Set up a new BattleNpc with the provided memory representation.
        /// </summary>
        /// <param name="address">The address of this actor in memory.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        internal BattleNpc(IntPtr address, Dalamud dalamud)
            : base(address, dalamud)
        {
        }

        /// <summary>
        /// Gets the BattleNpc <see cref="BattleNpcSubKind" /> of this BattleNpc.
        /// </summary>
        public BattleNpcSubKind BattleNpcKind => (BattleNpcSubKind)this.Struct->Character.GameObject.SubKind;
    }
}
