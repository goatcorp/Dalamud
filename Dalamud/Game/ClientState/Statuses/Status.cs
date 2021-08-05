using System;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Resolvers;
using JetBrains.Annotations;

namespace Dalamud.Game.ClientState.Statuses
{
    /// <summary>
    /// This class represents a status effect an actor is afflicted by.
    /// </summary>
    public unsafe class Status
    {
        private Dalamud dalamud;

        /// <summary>
        /// Initializes a new instance of the <see cref="Status"/> class.
        /// </summary>
        /// <param name="address">Status address.</param>
        /// <param name="dalamud">Dalamud instance.</param>
        internal Status(IntPtr address, Dalamud dalamud)
        {
            this.dalamud = dalamud;
            this.Address = address;
        }

        /// <summary>
        /// Gets the address of the status in memory.
        /// </summary>
        public IntPtr Address { get; }

        /// <summary>
        /// Gets the status ID of this status.
        /// </summary>
        public uint StatusID => this.Struct->StatusID;

        /// <summary>
        /// Gets the GameData associated with this status.
        /// </summary>
        public Lumina.Excel.GeneratedSheets.Status GameData => new ExcelResolver<Lumina.Excel.GeneratedSheets.Status>(this.Struct->StatusID, this.dalamud).GameData;

        /// <summary>
        /// Gets the parameter value of the status.
        /// </summary>
        public byte Param => this.Struct->Param;

        /// <summary>
        /// Gets the stack count of this status.
        /// </summary>
        public byte StackCount => this.Struct->StackCount;

        /// <summary>
        /// Gets the time remaining of this status.
        /// </summary>
        public float RemainingTime => this.Struct->RemainingTime;

        /// <summary>
        /// Gets the source ID of this status.
        /// </summary>
        public uint SourceID => this.Struct->SourceID;

        /// <summary>
        /// Gets the source actor associated with this status.
        /// </summary>
        /// <remarks>
        /// This iterates the actor table, it should be used with care.
        /// </remarks>
        [CanBeNull]
        public GameObject SourceActor => this.dalamud.ClientState.Objects.SearchByID(this.SourceID);

        private FFXIVClientStructs.FFXIV.Client.Game.Status* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Status*)this.Address;
    }
}
