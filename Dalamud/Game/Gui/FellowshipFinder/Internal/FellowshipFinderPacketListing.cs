using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;

namespace Dalamud.Game.Gui.FellowshipFinder.Internal
{
    /// <summary>
    /// The structure of an individual listing within a packet.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Sequential struct marshaling.")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Document the field usage.")]
    [StructLayout(LayoutKind.Sequential, Size = 0x178, Pack = 1)]
    public unsafe struct FellowshipFinderPacketListing
    {
        // [FieldOffset(0x00)]
        internal uint Id;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private byte[] unk1;

        // [FieldOffset(0x08)]
        internal uint MasterContentIdLower;

        private ushort unk2;
        private ushort unk3;

        // [FieldOffset(0x10)]
        internal uint RecruiterContentIdLower;

        private ushort unk4;
        private ushort unk5;

        // [FieldOffset(0x18)]
        internal uint Deadline;

        // [FieldOffset(0x1C)]
        internal uint LangsEnabled;

        // [FieldOffset(0x20)]
        internal uint LangPrimary;

        // [FieldOffset(0x24)]
        internal ushort MasterWorld;

        // [FieldOffset(0x26)]
        internal ushort RecruiterWorld;

        // [FieldOffset(0x28)]
        internal ushort Members;

        // [FieldOffset(0x2A)]
        internal ushort Target;

        // [FieldOffset(0x2C)]
        internal ushort Activity1;

        private byte unk6;
        private byte unk7;
        private ushort unk8;

        // [FieldOffset(0x32)]
        internal ushort Activity2;

        // [FieldOffset(0x34)]
        internal ushort Activity3;

        // [FieldOffset(0x36)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
        internal byte[] RawName;

        private byte unk9;

        // [FieldOffset(0x73)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        internal byte[] RawMasterName;

        // [FieldOffset(0x93)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        internal byte[] RawRecruiterName;

        // [FieldOffset(0xB3)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 192)]
        internal byte[] RawComment;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        private byte[] unk10;

        internal SeString Name
        {
            get
            {
                fixed (byte* ptr = this.RawName)
                {
                    return MemoryHelper.ReadSeString((IntPtr)ptr, 60);
                }
            }
        }

        internal SeString MasterName
        {
            get
            {
                fixed (byte* ptr = this.RawMasterName)
                {
                    return MemoryHelper.ReadSeString((IntPtr)ptr, 32);
                }
            }
        }

        internal SeString RecruiterName
        {
            get
            {
                fixed (byte* ptr = this.RawRecruiterName)
                {
                    return MemoryHelper.ReadSeString((IntPtr)ptr, 32);
                }
            }
        }

        internal SeString Comment
        {
            get
            {
                fixed (byte* ptr = this.RawComment)
                {
                    return MemoryHelper.ReadSeString((IntPtr)ptr, 192);
                }
            }
        }
    }
}
