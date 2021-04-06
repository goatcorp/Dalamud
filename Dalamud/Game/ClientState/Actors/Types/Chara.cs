using System;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors.Resolvers;
using Dalamud.Game.ClientState.Structs;

namespace Dalamud.Game.ClientState.Actors.Types {
    /// <summary>
    ///     This class represents the base for non-static entities.
    /// </summary>
    public unsafe class Chara : Actor {
        /// <summary>
        ///     Set up a new Chara with the provided memory representation.
        /// </summary>
        /// <param name="address">The address of this actor in memory.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        protected Chara(IntPtr address, Dalamud dalamud) : base(address, dalamud) { }

        /// <summary>
        ///     The level of this Chara.
        /// </summary>
        public byte Level => *(byte*)(this.Address + ActorOffsets.Level);

        /// <summary>
        ///     The ClassJob of this Chara.
        /// </summary>
        public ClassJob ClassJob => new ClassJob(*(byte*)(this.Address + ActorOffsets.ClassJob), this.dalamud);

        /// <summary>
        ///     The current HP of this Chara.
        /// </summary>
        public uint CurrentHp => *(uint*)(this.Address + ActorOffsets.CurrentHp);

        /// <summary>
        ///     The maximum HP of this Chara.
        /// </summary>
        public uint MaxHp => *(uint*)(this.Address + ActorOffsets.MaxHp);

        /// <summary>
        ///     The current MP of this Chara.
        /// </summary>
        public uint CurrentMp => *(uint*)(this.Address + ActorOffsets.CurrentMp);

        /// <summary>
        ///     The maximum MP of this Chara.
        /// </summary>
        public uint MaxMp => *(uint*)(this.Address + ActorOffsets.MaxMp);

        /// <summary>
        ///     The current GP of this Chara.
        /// </summary>
        public uint CurrentGp => *(uint*)(this.Address + ActorOffsets.CurrentGp);

        /// <summary>
        ///     The maximum GP of this Chara.
        /// </summary>
        public uint MaxGp => *(uint*)(this.Address + ActorOffsets.MaxGp);

        /// <summary>
        ///     The current CP of this Chara.
        /// </summary>
        public uint CurrentCp => *(uint*)(this.Address + ActorOffsets.CurrentCp);

        /// <summary>
        ///     The maximum CP of this Chara.
        /// </summary>
        public uint MaxCp => *(uint*)(this.Address + ActorOffsets.MaxCp);

        /// <summary>
        /// Byte array describing the visual appearance of this Chara. Indexed by <see cref="CustomizeIndex"/>.
        /// </summary>
        public byte[] Customize
        {
            get
            {
                var data = new byte[26];
                Marshal.Copy(this.Address + ActorOffsets.Customize, data, 0, 26);
                return data;
            }
        }
    }
}
