using System;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Actors;

namespace Dalamud.Game.ClientState.Structs
{
    /// <summary>
    /// This represents a native PartyMember class in memory.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct PartyMember
    {
        [FieldOffset(0x0)]
        public IntPtr namePtr;

        [FieldOffset(0x8)]
        public long unknown;

        [FieldOffset(0x10)]
        public int actorId;

        [FieldOffset(0x14)]
        public ObjectKind objectKind;
    }
}
