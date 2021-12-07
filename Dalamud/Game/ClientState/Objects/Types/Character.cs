using System;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Resolvers;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;

namespace Dalamud.Game.ClientState.Objects.Types
{
    /// <summary>
    /// This class represents the base for non-static entities.
    /// </summary>
    public unsafe class Character : GameObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Character"/> class.
        /// This represents a non-static entity.
        /// </summary>
        /// <param name="address">The address of this character in memory.</param>
        internal Character(IntPtr address)
            : base(address)
        {
        }

        /// <summary>
        /// Gets the current HP of this Chara.
        /// </summary>
        public uint CurrentHp => this.Struct->Health;

        /// <summary>
        /// Gets the maximum HP of this Chara.
        /// </summary>
        public uint MaxHp => this.Struct->MaxHealth;

        /// <summary>
        /// Gets the current MP of this Chara.
        /// </summary>
        public uint CurrentMp => this.Struct->Mana;

        /// <summary>
        /// Gets the maximum MP of this Chara.
        /// </summary>
        public uint MaxMp => this.Struct->MaxMana;

        /// <summary>
        /// Gets the current GP of this Chara.
        /// </summary>
        public uint CurrentGp => this.Struct->GatheringPoints;

        /// <summary>
        /// Gets the maximum GP of this Chara.
        /// </summary>
        public uint MaxGp => this.Struct->MaxGatheringPoints;

        /// <summary>
        /// Gets the current CP of this Chara.
        /// </summary>
        public uint CurrentCp => this.Struct->CraftingPoints;

        /// <summary>
        /// Gets the maximum CP of this Chara.
        /// </summary>
        public uint MaxCp => this.Struct->MaxCraftingPoints;

        /// <summary>
        /// Gets the ClassJob of this Chara.
        /// </summary>
        public ExcelResolver<Lumina.Excel.GeneratedSheets.ClassJob> ClassJob => new(this.Struct->ClassJob);

        /// <summary>
        /// Gets the level of this Chara.
        /// </summary>
        public byte Level => this.Struct->Level;

        /// <summary>
        /// Gets a byte array describing the visual appearance of this Chara.
        /// Indexed by <see cref="CustomizeIndex"/>.
        /// </summary>
        public byte[] Customize => MemoryHelper.Read<byte>((IntPtr)this.Struct->CustomizeData, 28);

        /// <summary>
        /// Gets the Free Company tag of this chara.
        /// </summary>
        public SeString CompanyTag => MemoryHelper.ReadSeString((IntPtr)this.Struct->FreeCompanyTag, 6);

        /// <summary>
        /// Gets the target object ID of the character.
        /// </summary>
        public override uint TargetObjectId => this.Struct->TargetObjectID;

        /// <summary>
        /// Gets the name ID of the character.
        /// </summary>
        public uint NameId => this.Struct->NameID;

        /// <summary>
        /// Gets the status flags.
        /// </summary>
        public StatusFlags StatusFlags => (StatusFlags)this.Struct->StatusFlags;

        /// <summary>
        /// Gets the underlying structure.
        /// </summary>
        protected internal new FFXIVClientStructs.FFXIV.Client.Game.Character.Character* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)this.Address;
    }
}
