using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Actors;

namespace Dalamud.Game.ClientState.Structs
{
    /// <summary>
    /// Native memory representation of an FFXIV actor.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 2)]
    public struct Actor
    {
        /// <summary>
        /// The actor name.
        /// </summary>
        [FieldOffset(ActorOffsets.Name)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
        public byte[] Name;

        /// <summary>
        /// The actor's internal id.
        /// </summary>
        [FieldOffset(ActorOffsets.ActorId)]
        public int ActorId;

        /// <summary>
        /// The actor's data id.
        /// </summary>
        [FieldOffset(ActorOffsets.DataId)]
        public int DataId;

        /// <summary>
        /// The actor's owner id. This is useful for pets, summons, and the like.
        /// </summary>
        [FieldOffset(ActorOffsets.OwnerId)]
        public int OwnerId;

        /// <summary>
        /// The type or kind of actor.
        /// </summary>
        [FieldOffset(ActorOffsets.ObjectKind)]
        public ObjectKind ObjectKind;

        /// <summary>
        /// The sub-type or sub-kind of actor.
        /// </summary>
        [FieldOffset(ActorOffsets.SubKind)]
        public byte SubKind;

        /// <summary>
        /// Whether the actor is friendly.
        /// </summary>
        [FieldOffset(ActorOffsets.IsFriendly)]
        public bool IsFriendly;

        /// <summary>
        /// The horizontal distance in game units from the player.
        /// </summary>
        [FieldOffset(ActorOffsets.YalmDistanceFromPlayerX)]
        public byte YalmDistanceFromPlayerX;

        /// <summary>
        /// The player target status.
        /// </summary>
        /// <remarks>
        /// This is some kind of enum.
        /// </remarks>
        [FieldOffset(ActorOffsets.PlayerTargetStatus)]
        public byte PlayerTargetStatus;

        /// <summary>
        /// The vertical distance in game units from the player.
        /// </summary>
        [FieldOffset(ActorOffsets.YalmDistanceFromPlayerY)]
        public byte YalmDistanceFromPlayerY;

        /// <summary>
        /// The (X,Z,Y) position of the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.Position)]
        public Position3 Position;

        /// <summary>
        /// The rotation of the actor.
        /// </summary>
        /// <remarks>
        /// The rotation is around the vertical axis (yaw), from -pi to pi radians.
        /// </remarks>
        [FieldOffset(ActorOffsets.Rotation)]
        public float Rotation;

        /// <summary>
        /// The hitbox radius of the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.HitboxRadius)]
        public float HitboxRadius;

        /// <summary>
        /// The current HP of the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.CurrentHp)]
        public int CurrentHp;

        /// <summary>
        /// The max HP of the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.MaxHp)]
        public int MaxHp;

        /// <summary>
        /// The current MP of the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.CurrentMp)]
        public int CurrentMp;

        /// <summary>
        /// The max MP of the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.MaxMp)]
        public short MaxMp;

        /// <summary>
        /// The current GP of the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.CurrentGp)]
        public short CurrentGp;

        /// <summary>
        /// The max GP of the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.MaxGp)]
        public short MaxGp;

        /// <summary>
        /// The current CP of the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.CurrentCp)]
        public short CurrentCp;

        /// <summary>
        /// The max CP of the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.MaxCp)]
        public short MaxCp;

        /// <summary>
        /// The class-job of the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.ClassJob)]
        public byte ClassJob;

        /// <summary>
        /// The level of the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.Level)]
        public byte Level;

        /// <summary>
        /// The (player character) actor ID being targeted by the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.PlayerCharacterTargetActorId)]
        public int PlayerCharacterTargetActorId;

        /// <summary>
        /// The customization byte/bitfield of the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.Customize)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
        public byte[] Customize;

        // Normally pack=2 should work, but ByTVal or Injection breaks this.
        // [FieldOffset(ActorOffsets.CompanyTag)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)] public string CompanyTag;

        /// <summary>
        /// The (battle npc) actor ID being targeted by the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.BattleNpcTargetActorId)]
        public int BattleNpcTargetActorId;

        /// <summary>
        /// The name ID of the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.NameId)]
        public int NameId;

        /// <summary>
        /// The current world ID of the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.CurrentWorld)]
        public ushort CurrentWorld;

        /// <summary>
        /// The home world ID of the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.HomeWorld)]
        public ushort HomeWorld;

        /// <summary>
        /// Whether the actor is currently casting.
        /// </summary>
        [FieldOffset(ActorOffsets.IsCasting)]
        public bool IsCasting;

        /// <summary>
        /// Whether the actor is currently casting (dup?).
        /// </summary>
        [FieldOffset(ActorOffsets.IsCasting2)]
        public bool IsCasting2;

        /// <summary>
        /// The spell action ID currently being cast by the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.CurrentCastSpellActionId)]
        public uint CurrentCastSpellActionId;

        /// <summary>
        /// The actor ID of the target currently being cast at by the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.CurrentCastTargetActorId)]
        public uint CurrentCastTargetActorId;

        /// <summary>
        /// The current casting time of the spell being cast by the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.CurrentCastTime)]
        public float CurrentCastTime;

        /// <summary>
        /// The total casting time of the spell being cast by the actor.
        /// </summary>
        [FieldOffset(ActorOffsets.TotalCastTime)]
        public float TotalCastTime;

        /// <summary>
        /// Actor status flags.
        /// </summary>
        [FieldOffset(ActorOffsets.StatusFlags)]
        public StatusFlags StatusFlags;

        /// <summary>
        /// The array of status effects that the actor is currently affected by.
        /// </summary>
        [FieldOffset(ActorOffsets.UIStatusEffects)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
        public StatusEffect[] UIStatusEffects;
    }

    /// <summary>
    /// Memory offsets for the <see cref="Actor"/> type.
    /// </summary>
    public static class ActorOffsets
    {
        // Reference https://github.com/FFXIVAPP/sharlayan-resources/blob/master/structures/5.4/x64.json for more
        public const int Name = 48;                          // 0x0030
        public const int ActorId = 116;                      // 0x0074
        // public const int ??? = 120;                       // 0x0078 NPCID1
        public const int DataId = 128;                       // 0x0080 NPCID2
        public const int OwnerId = 132;                      // 0x0084
        public const int ObjectKind = 140;                   // 0x008C Type
        public const int SubKind = 141;                      // 0x008D
        public const int IsFriendly = 142;                   // 0x008E
        public const int YalmDistanceFromPlayerX = 144;      // 0x0090
        public const int PlayerTargetStatus = 145;           // 0x0091
        public const int YalmDistanceFromPlayerY = 146;      // 0x0092 Distance
        public const int Position = 160;                     // 0x00A0 (X,Z,Y)
        public const int Rotation = 176;                     // 0x00B0 Heading
        public const int HitboxRadius = 192;                 // 0x00C0
        public const int CurrentHp = 452;                    // 0x01C4 HPCurrent
        public const int MaxHp = 456;                        // 0x01C8 HPMax
        public const int CurrentMp = 460;                    // 0x01CC MPCurrent
        public const int MaxMp = 464;                        // 0x01D0 MPMax
        public const int CurrentGp = 468;                    // 0x01D4 GPCurrent
        public const int MaxGp = 470;                        // 0x01D6 GPMax
        public const int CurrentCp = 472;                    // 0x01D8 CPCurrent
        public const int MaxCp = 474;                        // 0x01DA CPMax
        public const int ClassJob = 482;                     // 0x01E2 Job
        public const int Level = 483;                        // 0x01E3 Level
        public const int PlayerCharacterTargetActorId = 560; // 0x01F0 TargetID

        public const int Customize = 0x1898;  // Needs verification
        public const int CompanyTag = 0x18B2;
        public const int BattleNpcTargetActorId = 0x18D8;  // Needs verification
        public const int NameId = 0x1940;  // Needs verification
        public const int CurrentWorld = 0x195C;
        public const int HomeWorld = 0x195E;

        public const int IsCasting = 0x1B80;
        public const int IsCasting2 = 0x1B82;
        public const int CurrentCastSpellActionId = 0x1B84;
        public const int CurrentCastTargetActorId = 0x1B90;
        public const int CurrentCastTime = 0x1BB4;
        public const int TotalCastTime = 0x1BB8;
        public const int StatusFlags = 0x19A0;
        public const int UIStatusEffects = 0x19F8;
    }
}
