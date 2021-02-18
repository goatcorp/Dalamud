using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors;
#pragma warning disable 1591

namespace Dalamud.Game.ClientState.Structs
{
    public class ActorOffsets
    {
        // ??? Offsets based on https://github.com/FFXIVAPP/sharlayan-resources/blob/master/structures/5.3/x64.json

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
        // public const int ??? = 148;                       // 0x0094 TargetFlags
        // public const int ??? = 148;                       // 0x0094 GatheringInvisible
        public const int Position = 160;                     // 0x00A0 (X,Z,Y)
        public const int Rotation = 176;                     // 0x00B0 Heading
        // public const int ??? = 190;                       // 0x00BE EventObjectType
        // public const int ??? = 192;                       // 0x00C0 HitBoxRadius
        // public const int ??? = 228;                       // 0x00E4 Fate
        // public const int ??? = 396;                       // 0x018C IsGM
        // public const int ??? = 464;                       // 0x01D0 TargetType
        // public const int ??? = 480;                       // 0x01E0 EntityCount
        // public const int ??? = 488;                       // 0x01E8 GatheringStatus
        public const int PlayerCharacterTargetActorId = 560; // 0x01F0 TargetID
        // public const int ??? = 5297;                      // 0x14B1 Status
        public const int Customize = 6264;                   // 0x17B8
        public const int CompanyTag = 6290;                  // 0x17D0
        public const int BattleNpcTargetActorId = 6328;      // 0x17F8 ClaimedByID
        public const int NameId = 6432;                      // 0x1868 ModelID
        public const int CurrentWorld = 6460;                // 0x1884
        public const int HomeWorld = 6462;                   // 0x1886
        public const int CurrentHp = 452;                   // 0x1898 HPCurrent
        public const int MaxHp = 456;                       // 0x189C HPMax
        public const int CurrentMp = 460;                   // 0x18A0 MPCurrent
        public const int MaxMp = 464;                       // 0x18A4 MPMax
        public const int CurrentGp = 468;                   // 0x18AA GPCurrent
        public const int MaxGp = 472;                       // 0x18AC GPMax
        public const int CurrentCp = 476;                   // 0x18AE CPCurrent
        public const int MaxCp = 480;                       // 0x18B0 CPMax
        // public const int ??? = 6326;                      // 0x18B6 Title
        // public const int ??? = 6354;                      // 0x18D2 Icon
        // public const int ??? = 6356;                      // 0x18D4 ActionStatus
        public const int ClassJob = 482;                    // 0x18DA Job
        public const int Level = 483;                       // 0x18DC Level
        // public const int ??? = 6367;                      // 0x18DF GrandCompany
        // public const int ??? = 6367;                      // 0x18DF GrandCompanyRank
        // public const int ??? = 6371;                      // 0x18E3 DifficultyRank
        // public const int ??? = 6385;                      // 0x18F1 AgroFlags
        // public const int ??? = 6406;                      // 0x1906 CombatFlags
        public const int UIStatusEffects = 6616;             // 0x1958 DefaultStatusEffectOffset
        // public const int ??? = 6880;                      // 0x1AE0 IsCasting1
        // public const int ??? = 6882;                      // 0x1AE2 IsCasting2
        // public const int ??? = 6884;                      // 0x1AE4 CastingID
        // public const int ??? = 6896;                      // 0x1AF0 CastingTargetID
        // public const int ??? = 6932;                      // 0x1B14 CastingProgress
        // public const int ??? = 6936;                      // 0x1B18 CastingTime
    }
    /// <summary>
    /// Native memory representation of a FFXIV actor.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct Actor
    {
        [FieldOffset(ActorOffsets.Name)] [MarshalAs(UnmanagedType.LPUTF8Str, SizeConst = 30)] public string Name;

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

        [FieldOffset(ActorOffsets.Customize)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)] public byte[] Customize;

        [FieldOffset(ActorOffsets.PlayerCharacterTargetActorId)] public int PlayerCharacterTargetActorId;
        [FieldOffset(ActorOffsets.BattleNpcTargetActorId)] public int BattleNpcTargetActorId;

        [FieldOffset(ActorOffsets.NameId)] public int NameId;
        [FieldOffset(ActorOffsets.CurrentWorld)] public ushort CurrentWorld;
        [FieldOffset(ActorOffsets.HomeWorld)] public ushort HomeWorld;
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
        [FieldOffset(ActorOffsets.UIStatusEffects)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)] public StatusEffect[] UIStatusEffects;
    }
}
