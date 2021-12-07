using System;

using Dalamud.Game.ClientState.Statuses;

namespace Dalamud.Game.ClientState.Objects.Types
{
    /// <summary>
    /// This class represents the battle characters.
    /// </summary>
    public unsafe class BattleChara : Character
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BattleChara"/> class.
        /// This represents a battle character.
        /// </summary>
        /// <param name="address">The address of this character in memory.</param>
        internal BattleChara(IntPtr address)
            : base(address)
        {
        }

        /// <summary>
        /// Gets the current status effects.
        /// </summary>
        public StatusList StatusList => new(&this.Struct->StatusManager);

        /// <summary>
        /// Gets a value indicating whether the chara is currently casting.
        /// </summary>
        public bool IsCasting => this.Struct->SpellCastInfo.IsCasting > 0;

        /// <summary>
        /// Gets a value indicating whether the cast is interruptible.
        /// </summary>
        public bool IsCastInterruptible => this.Struct->SpellCastInfo.Interruptible > 0;

        /// <summary>
        /// Gets the spell action type of the spell being cast by the actor.
        /// </summary>
        public byte CastActionType => (byte)this.Struct->SpellCastInfo.ActionType;

        /// <summary>
        /// Gets the spell action ID of the spell being cast by the actor.
        /// </summary>
        public uint CastActionId => this.Struct->SpellCastInfo.ActionID;

        /// <summary>
        /// Gets the object ID of the target currently being cast at by the chara.
        /// </summary>
        public uint CastTargetObjectId => this.Struct->SpellCastInfo.CastTargetID;

        /// <summary>
        /// Gets the current casting time of the spell being cast by the chara.
        /// </summary>
        public float CurrentCastTime => this.Struct->SpellCastInfo.CurrentCastTime;

        /// <summary>
        /// Gets the total casting time of the spell being cast by the chara.
        /// </summary>
        public float TotalCastTime => this.Struct->SpellCastInfo.TotalCastTime;

        /// <summary>
        /// Gets the underlying structure.
        /// </summary>
        protected internal new FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)this.Address;
    }
}
