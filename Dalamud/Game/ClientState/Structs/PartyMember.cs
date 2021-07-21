using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Actors.Types;

namespace Dalamud.Game.ClientState.Structs
{
    /// <summary>
    /// This represents a native PartyMember class in memory.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Pending rework for Actor/Fate style struct.")]
    [StructLayout(LayoutKind.Explicit)]
    public struct PartyMember
    {
        [FieldOffset(0x0)]
        public IntPtr NamePtr;

        [FieldOffset(0x8)]
        public long Unknown;

        [FieldOffset(0x10)]
        public int ActorId;

        [FieldOffset(0x14)]
        public ObjectKind ObjectKind;
    }
}
