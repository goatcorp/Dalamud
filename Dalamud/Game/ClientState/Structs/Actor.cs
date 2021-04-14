using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Actors;

namespace Dalamud.Game.ClientState.Structs
{
    public class ActorOffsets
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
        public const int UIStatusEffects = 0x19F8;
    }

    /// <summary>
    /// Native memory representation of a FFXIV actor.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 2)]
    public struct Actor
    {
        [FieldOffset(ActorOffsets.Name)] [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)] public string Name;
        [FieldOffset(ActorOffsets.ActorId)] public int ActorId;
        [FieldOffset(ActorOffsets.DataId)] public int DataId;
        [FieldOffset(ActorOffsets.OwnerId)] public int OwnerId;
        [FieldOffset(ActorOffsets.ObjectKind)] public ObjectKind ObjectKind;
        [FieldOffset(ActorOffsets.SubKind)] public byte SubKind;
        [FieldOffset(ActorOffsets.IsFriendly)] public bool IsFriendly;
        [FieldOffset(ActorOffsets.YalmDistanceFromPlayerX)] public byte YalmDistanceFromPlayerX; // Demo says one of these is x distance
        [FieldOffset(ActorOffsets.PlayerTargetStatus)] public byte PlayerTargetStatus; // This is some kind of enum
        [FieldOffset(ActorOffsets.YalmDistanceFromPlayerY)] public byte YalmDistanceFromPlayerY; // and the other is z distance
        [FieldOffset(ActorOffsets.Position)] public Position3 Position;
        [FieldOffset(ActorOffsets.Rotation)] public float Rotation; // Rotation around the vertical axis (yaw), from -pi to pi radians
        [FieldOffset(ActorOffsets.CurrentHp)] public int CurrentHp;
        [FieldOffset(ActorOffsets.MaxHp)] public int MaxHp;
        [FieldOffset(ActorOffsets.CurrentMp)] public int CurrentMp;
        [FieldOffset(ActorOffsets.MaxMp)] public short MaxMp;
        [FieldOffset(ActorOffsets.CurrentGp)] public short CurrentGp;
        [FieldOffset(ActorOffsets.MaxGp)] public short MaxGp;
        [FieldOffset(ActorOffsets.CurrentCp)] public short CurrentCp;
        [FieldOffset(ActorOffsets.MaxCp)] public short MaxCp;
        [FieldOffset(ActorOffsets.ClassJob)] public byte ClassJob;
        [FieldOffset(ActorOffsets.Level)] public byte Level;
        [FieldOffset(ActorOffsets.PlayerCharacterTargetActorId)] public int PlayerCharacterTargetActorId;
        [FieldOffset(ActorOffsets.Customize)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)] public byte[] Customize;

        // Normally pack=2 should work, but ByTVal or Injection breaks this.
        // [FieldOffset(ActorOffsets.CompanyTag)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)] public string CompanyTag;
        [FieldOffset(ActorOffsets.BattleNpcTargetActorId)] public int BattleNpcTargetActorId;
        [FieldOffset(ActorOffsets.NameId)] public int NameId;
        [FieldOffset(ActorOffsets.CurrentWorld)] public ushort CurrentWorld;
        [FieldOffset(ActorOffsets.HomeWorld)] public ushort HomeWorld;
        [FieldOffset(ActorOffsets.IsCasting)] public bool IsCasting;
        [FieldOffset(ActorOffsets.IsCasting2)] public bool IsCasting2;
        [FieldOffset(ActorOffsets.CurrentCastSpellActionId)] public uint CurrentCastSpellActionId;
        [FieldOffset(ActorOffsets.CurrentCastTargetActorId)] public uint CurrentCastTargetActorId;
        [FieldOffset(ActorOffsets.CurrentCastTime)] public float CurrentCastTime;
        [FieldOffset(ActorOffsets.TotalCastTime)] public float TotalCastTime;
        [FieldOffset(ActorOffsets.UIStatusEffects)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)] public StatusEffect[] UIStatusEffects;
    }
}
