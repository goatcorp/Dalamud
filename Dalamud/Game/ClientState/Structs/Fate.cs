using System;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Fates;
#pragma warning disable 1591

namespace Dalamud.Game.ClientState.Structs
{
    public class FateOffsets
    {
        public const int FateId = 0x18;
        public const int StartTimeEpoch = 0x20;
        public const int Duration = 0x28;
        public const int Name = 0xC0;
        public const int State = 0x3AC;
        public const int Progress = 0x3B8;
        public const int Level = 0x3F9;
        public const int Position = 0x450;
        public const int Territory = 0x74E;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Fate
    {
        [FieldOffset(FateOffsets.FateId)] public ushort FateId;
        [FieldOffset(FateOffsets.StartTimeEpoch)] public int StartTimeEpoch;
        [FieldOffset(FateOffsets.Duration)] public short Duration;
        [FieldOffset(FateOffsets.Name)] public IntPtr Name;
        [FieldOffset(FateOffsets.State)] public FateState State;
        [FieldOffset(FateOffsets.Progress)] public byte Progress;
        [FieldOffset(FateOffsets.Level)] public byte Level;
        [FieldOffset(FateOffsets.Position)] public Position3 Position;
        [FieldOffset(FateOffsets.Territory)] public ushort TerritoryId;
    }
}
