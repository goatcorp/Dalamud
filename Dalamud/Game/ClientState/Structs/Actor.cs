using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Actors;
#pragma warning disable 1591

namespace Dalamud.Game.ClientState.Structs
{
    public class ActorOffsets
    {
        public const int Name = 0x30;
        public const int ActorId = 0x74;
        public const int DataId = 0x80;
        public const int OwnerId = 0x84;
        public const int ObjectKind = 0x8C;
        public const int SubKind = 0x8D;
        public const int IsFriendly = 0x8E;
        public const int YalmDistanceFromPlayerX = 0x90;
        public const int PlayerTargetStatus = 0x91;
        public const int YalmDistanceFromPlayerY = 0x92;
        public const int Position = 0xA0;
        public const int Rotation = 0xB0;
        public const int Customize = 0x17B8;
        public const int PlayerCharacterTargetActorId = 0x1F0;
        public const int BattleNpcTargetActorId = 0x17F8;
        public const int CompanyTag = 0x17D0;
        public const int NameId = 0x1868;
        public const int CurrentWorld = 0x1884;
        public const int HomeWorld = 0x1886;
        public const int CurrentHp = 0x1898;
        public const int MaxHp = 0x189C;
        public const int CurrentMp = 0x18A0;
        public const int MaxMp = 0x18AA;
        public const int ClassJob = 0x18DA;
        public const int Level = 0x18DC;
        public const int UIStatusEffects = 0x1958;
    }

    /// <summary>
    /// Native memory representation of a FFXIV actor.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct Actor {
        [FieldOffset(ActorOffsets.Name)] [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public string Name;

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

        // This field can't be correctly aligned, so we have to cut it manually.
        [FieldOffset(ActorOffsets.CompanyTag)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] CompanyTag;

        [FieldOffset(ActorOffsets.NameId)] public int NameId;
        [FieldOffset(ActorOffsets.CurrentWorld)] public ushort CurrentWorld;
        [FieldOffset(ActorOffsets.HomeWorld)] public ushort HomeWorld;
        [FieldOffset(ActorOffsets.CurrentHp)] public int CurrentHp;
        [FieldOffset(ActorOffsets.MaxHp)] public int MaxHp;
        [FieldOffset(ActorOffsets.CurrentMp)] public int CurrentMp;
        // This value is weird.  It seems to change semi-randomly between 0 and 10k, definitely
        // in response to mp-using events, but it doesn't often have a value and the changing seems
        // somewhat arbitrary.
        [FieldOffset(ActorOffsets.MaxMp)] public int MaxMp;
        [FieldOffset(ActorOffsets.ClassJob)] public byte ClassJob;
        [FieldOffset(ActorOffsets.Level)] public byte Level;
        [FieldOffset(ActorOffsets.UIStatusEffects)][MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)] public StatusEffect[] UIStatusEffects; 
        
    }
}
