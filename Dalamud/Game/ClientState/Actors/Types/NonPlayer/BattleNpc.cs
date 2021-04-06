using System;

namespace Dalamud.Game.ClientState.Actors.Types.NonPlayer
{
    /// <summary>
    ///     This class represents a battle NPC.
    /// </summary>
    public unsafe class BattleNpc : Npc
    {
        /// <summary>
        /// Set up a new BattleNpc with the provided memory representation.
        /// </summary>
        /// <param name="address">The address of this actor in memory.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        public BattleNpc(IntPtr address, Dalamud dalamud) : base(address, dalamud)
        {
        }

        /// <summary>
        /// The BattleNpc <see cref="BattleNpcSubKind" /> of this BattleNpc.
        /// </summary>
        public BattleNpcSubKind BattleNpcKind => *(BattleNpcSubKind*)(Address + Structs.ActorOffsets.SubKind);

        /// <summary>
        /// The ID of this BattleNpc's owner.
        /// </summary>
        public int OwnerId => *(int*)(Address + Structs.ActorOffsets.OwnerId);

        /// <summary>
        /// Target of the Battle NPC.
        /// </summary>
        public override int TargetActorID => *(int*)(Address + Structs.ActorOffsets.BattleNpcTargetActorId);

    }
}
