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
        [FieldOffset(0x30)] [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)] public string Name;
        [FieldOffset(116)] public int ActorId;
        [FieldOffset(128)] public int DataId;
        [FieldOffset(132)] public int OwnerId;
        [FieldOffset(140)] public ObjectKind ObjectKind;
        [FieldOffset(141)] public byte SubKind;
        [FieldOffset(160)] public Position3 Position;
        [FieldOffset(6308)] public byte CurrentWorld;
        [FieldOffset(6310)] public byte HomeWorld;
        [FieldOffset(6328)] public int CurrentHp;
        [FieldOffset(6332)] public int MaxHp;
        [FieldOffset(6336)] public int CurrentMp;
        [FieldOffset(6340)] public int MaxMp;
        [FieldOffset(6388)] public byte ClassJob;
        [FieldOffset(6390)] public byte Level;
    }
}
