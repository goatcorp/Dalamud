using System;

using Dalamud.Game.ClientState.Actors.Resolvers;
using Dalamud.Game.ClientState.Structs;
using Dalamud.Memory;

namespace Dalamud.Game.ClientState.Actors.Types
{
    /// <summary>
    /// This class represents the base for non-static entities.
    /// </summary>
    public unsafe class Chara : Actor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Chara"/> class.
        /// This represents a non-static entity.
        /// </summary>
        /// <param name="address">The address of this actor in memory.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        internal Chara(IntPtr address, Dalamud dalamud)
            : base(address, dalamud)
        {
        }

        /// <summary>
        /// Gets the current HP of this Chara.
        /// </summary>
        public uint CurrentHp => *(uint*)(this.Address + ActorOffsets.CurrentHp);

        /// <summary>
        /// Gets the maximum HP of this Chara.
        /// </summary>
        public uint MaxHp => *(uint*)(this.Address + ActorOffsets.MaxHp);

        /// <summary>
        /// Gets the current MP of this Chara.
        /// </summary>
        public uint CurrentMp => *(uint*)(this.Address + ActorOffsets.CurrentMp);

        /// <summary>
        /// Gets the maximum MP of this Chara.
        /// </summary>
        public uint MaxMp => *(uint*)(this.Address + ActorOffsets.MaxMp);

        /// <summary>
        /// Gets the current GP of this Chara.
        /// </summary>
        public uint CurrentGp => *(uint*)(this.Address + ActorOffsets.CurrentGp);

        /// <summary>
        /// Gets the maximum GP of this Chara.
        /// </summary>
        public uint MaxGp => *(uint*)(this.Address + ActorOffsets.MaxGp);

        /// <summary>
        /// Gets the current CP of this Chara.
        /// </summary>
        public uint CurrentCp => *(uint*)(this.Address + ActorOffsets.CurrentCp);

        /// <summary>
        /// Gets the maximum CP of this Chara.
        /// </summary>
        public uint MaxCp => *(uint*)(this.Address + ActorOffsets.MaxCp);

        /// <summary>
        /// Gets the ClassJob of this Chara.
        /// </summary>
        public ClassJob ClassJob => new(*(byte*)(this.Address + ActorOffsets.ClassJob), this.Dalamud);

        /// <summary>
        /// Gets the level of this Chara.
        /// </summary>
        public byte Level => *(byte*)(this.Address + ActorOffsets.Level);

        /// <summary>
        /// Gets a byte array describing the visual appearance of this Chara.
        /// Indexed by <see cref="CustomizeIndex"/>.
        /// </summary>
        public byte[] Customize => MemoryHelper.Read<byte>(this.Address + ActorOffsets.Customize, 28);

        /// <summary>
        /// Gets the status flags.
        /// </summary>
        public StatusFlags StatusFlags => *(StatusFlags*)(this.Address + ActorOffsets.StatusFlags);

        /// <summary>
        /// Gets the current status effects.
        /// </summary>
        /// <remarks>
        /// This copies every time it is invoked, so make sure to only grab it once.
        /// </remarks>
        public StatusEffect[] StatusEffects => MemoryHelper.Read<StatusEffect>(this.Address + ActorOffsets.UIStatusEffects, 20, true);

        /// <summary>
        /// Gets a value indicating whether the actor is currently casting.
        /// </summary>
        public bool IsCasting => *(int*)(this.Address + ActorOffsets.IsCasting) > 0;

        /// <summary>
        /// Gets a value indicating whether the actor is currently casting (again?).
        /// </summary>
        public bool IsCasting2 => *(int*)(this.Address + ActorOffsets.IsCasting2) > 0;

        /// <summary>
        /// Gets the spell action ID currently being cast by the actor.
        /// </summary>
        public uint CurrentCastSpellActionId => *(uint*)(this.Address + ActorOffsets.CurrentCastSpellActionId);

        /// <summary>
        /// Gets the actor ID of the target currently being cast at by the actor.
        /// </summary>
        public uint CurrentCastTargetActorId => *(uint*)(this.Address + ActorOffsets.CurrentCastTargetActorId);

        /// <summary>
        /// Gets the current casting time of the spell being cast by the actor.
        /// </summary>
        public float CurrentCastTime => *(float*)(this.Address + ActorOffsets.CurrentCastTime);

        /// <summary>
        /// Gets the total casting time of the spell being cast by the actor.
        /// </summary>
        public float TotalCastTime => *(float*)(this.Address + ActorOffsets.TotalCastTime);
    }
}
