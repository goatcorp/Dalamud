using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Actors;

namespace Dalamud.Game.ClientState.Structs
{
    /// <summary>
    /// Native memory representation of a FFXIV actor.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct Actor {
        [FieldOffset(0x30)] [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public string Name;

        [FieldOffset(116)] public int ActorId;
        [FieldOffset(128)] public int DataId;
        [FieldOffset(132)] public int OwnerId;
        [FieldOffset(140)] public ObjectKind ObjectKind;
        [FieldOffset(141)] public byte SubKind;
        [FieldOffset(142)] public bool IsFriendly;
        [FieldOffset(144)] public byte YalmDistanceFromPlayerX; // Demo says one of these is x distance
        [FieldOffset(145)] public byte PlayerTargetStatus; // This is some kind of enum
        [FieldOffset(146)] public byte YalmDistanceFromPlayerY; // and the other is z distance
        [FieldOffset(160)] public Position3 Position;
        [FieldOffset(176)] public float Rotation; // Rotation around the vertical axis (yaw), from -pi to pi radians     

        [FieldOffset(0x17B8)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)] public byte[] Customize;

        [FieldOffset(0x1F0)] public int PlayerTargetActorId;
        [FieldOffset(0x17F8)] public int TargetActorId;

        // This field can't be correctly aligned, so we have to cut it manually.
        [FieldOffset(0x17d0)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] CompanyTag;

        [FieldOffset(0x1868)] public int NameId;
        [FieldOffset(0x1884)] public byte CurrentWorld;
        [FieldOffset(0x1886)] public byte HomeWorld;
        [FieldOffset(0x1898)] public int CurrentHp;
        [FieldOffset(0x189C)] public int MaxHp;
        [FieldOffset(0x18A0)] public int CurrentMp;
        // This value is weird.  It seems to change semi-randomly between 0 and 10k, definitely
        // in response to mp-using events, but it doesn't often have a value and the changing seems
        // somewhat arbitrary.
        [FieldOffset(0x18AA)] public int MaxMp;
        [FieldOffset(6358)] public byte ClassJob;
        [FieldOffset(6360)] public byte Level;
        [FieldOffset(0x1958)][MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)] public StatusEffect[] UIStatusEffects; 
        
    }
}
