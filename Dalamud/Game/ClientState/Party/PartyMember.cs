using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Resolvers;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using JetBrains.Annotations;

namespace Dalamud.Game.ClientState.Party
{
    /// <summary>
    /// This class represents a party member in the group manager.
    /// </summary>
    public unsafe class PartyMember
    {
        private readonly int statusSize = Marshal.SizeOf<FFXIVClientStructs.FFXIV.Client.Game.Status>();

        private Dalamud dalamud;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartyMember"/> class.
        /// </summary>
        /// <param name="address">Address of the party member.</param>
        /// <param name="dalamud">Dalamud itself.</param>
        internal PartyMember(IntPtr address, Dalamud dalamud)
        {
            this.Address = address;
            this.dalamud = dalamud;
        }

        /// <summary>
        /// Gets the address of this party member in memory.
        /// </summary>
        public IntPtr Address { get; }

        /// <summary>
        /// Gets a list of buffs or debuffs applied to this party member.
        /// </summary>
        public StatusList StatusList => new((IntPtr)(&this.Struct->StatusManager), this.dalamud);

        /// <summary>
        /// Gets the position of the party member.
        /// </summary>
        public Position3 Position => new(Struct->X, this.Struct->Y, this.Struct->Z);

        /// <summary>
        /// Gets the content ID of the party member.
        /// </summary>
        public long ContentID => this.Struct->ContentID;

        /// <summary>
        /// Gets the actor ID of this party member.
        /// </summary>
        public uint ActorID => this.Struct->ObjectID;

        /// <summary>
        /// Gets the actor associated with this buddy.
        /// </summary>
        /// <remarks>
        /// This iterates the actor table, it should be used with care.
        /// </remarks>
        [CanBeNull]
        public Actor Actor => this.dalamud.ClientState.Actors.SearchByID(this.ActorID);

        /// <summary>
        /// Gets the current HP of this party member.
        /// </summary>
        public uint CurrentHP => this.Struct->CurrentHP;

        /// <summary>
        /// Gets the maximum HP of this party member.
        /// </summary>
        public uint MaxHP => this.Struct->MaxHP;

        /// <summary>
        /// Gets the current MP of this party member.
        /// </summary>
        public ushort CurrentMP => this.Struct->CurrentMP;

        /// <summary>
        /// Gets the maximum MP of this party member.
        /// </summary>
        public ushort MaxMP => this.Struct->MaxMP;

        /// <summary>
        /// Gets the territory this party member is located in.
        /// </summary>
        public TerritoryTypeResolver Territory => new(Struct->TerritoryType, this.dalamud);

        /// <summary>
        /// Gets the displayname of this party member.
        /// </summary>
        public SeString Name => MemoryHelper.ReadSeString((IntPtr)Struct->Name, 0x40);

        /// <summary>
        /// Gets the sex of this party member.
        /// </summary>
        public byte Sex => this.Struct->Sex;

        /// <summary>
        /// Gets the classjob of this party member.
        /// </summary>
        public ClassJobResolver ClassJob => new(Struct->ClassJob, this.dalamud);

        private FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember*)this.Address;
    }
}
