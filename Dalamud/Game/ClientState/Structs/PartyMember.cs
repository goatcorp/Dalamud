using Dalamud.Game.ClientState.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct PartyMember
    {
        [FieldOffset(0x0)] public IntPtr namePtr;
        [FieldOffset(0x8)] public long unknown;
        [FieldOffset(0x10)] public int actorId;
        [FieldOffset(0x14)] public ObjectKind objectKind;
    }
}
