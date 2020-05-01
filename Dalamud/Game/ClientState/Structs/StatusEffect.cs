using System;
using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs
{
    /// <summary>
    /// Native memory representation of a FFXIV status effect.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct StatusEffect
    {
        public short EffectId;
        public byte StackCount;
        public byte Param;
        public float Duration;
        public int OwnerId;
    }
}
