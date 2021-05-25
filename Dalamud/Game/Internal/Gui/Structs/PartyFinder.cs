using System.Linq;
using System.Runtime.InteropServices;

namespace Dalamud.Game.Internal.Gui.Structs
{
    /// <summary>
    /// PartyFinder related network structs and static constants.
    /// </summary>
    internal static class PartyFinder
    {
        /// <summary>
        /// The structure of the PartyFinder packet.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct Packet
        {
            internal readonly int BatchNumber;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            private readonly byte[] padding1;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            internal readonly Listing[] Listings;
        }

        /// <summary>
        /// The structure of an individual listing within a packet.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct Listing
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            private readonly byte[] header1;

            internal readonly uint Id;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            private readonly byte[] header2;

            internal readonly uint ContentIdLower;
            private readonly ushort unknownShort1;
            private readonly ushort unknownShort2;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            private readonly byte[] header3;

            internal readonly byte Category;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            private readonly byte[] header4;

            internal readonly ushort Duty;
            internal readonly byte DutyType;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
            private readonly byte[] header5;

            internal readonly ushort World;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            private readonly byte[] header6;

            internal readonly byte Objective;
            internal readonly byte BeginnersWelcome;
            internal readonly byte Conditions;
            internal readonly byte DutyFinderSettings;
            internal readonly byte LootRules;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            private readonly byte[] header7; // all zero in every pf I've examined

            private readonly uint lastPatchHotfixTimestamp; // last time the servers were restarted?
            internal readonly ushort SecondsRemaining;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            private readonly byte[] header8; // 00 00 01 00 00 00 in every pf I've examined

            internal readonly ushort MinimumItemLevel;
            internal readonly ushort HomeWorld;
            internal readonly ushort CurrentWorld;

            private readonly byte header9;

            internal readonly byte NumSlots;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            private readonly byte[] header10;

            internal readonly byte SearchArea;

            private readonly byte header11;

            internal readonly byte NumParties;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            private readonly byte[] header12; // 00 00 00 always. maybe numParties is a u32?

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            internal readonly uint[] Slots;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            internal readonly byte[] JobsPresent;

            // Note that ByValTStr will not work here because the strings are UTF-8 and there's only a CharSet for UTF-16 in C#.
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            internal readonly byte[] Name;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 192)]
            internal readonly byte[] Description;

            internal bool IsNull()
            {
                // a valid party finder must have at least one slot set
                return this.Slots.All(slot => slot == 0);
            }
        }

        /// <summary>
        /// PartyFinder packet constants.
        /// </summary>
        public static class PacketInfo
        {
            /// <summary>
            /// The size of the PartyFinder packet.
            /// </summary>
            public static readonly int PacketSize = Marshal.SizeOf<Packet>();
        }
    }
}
