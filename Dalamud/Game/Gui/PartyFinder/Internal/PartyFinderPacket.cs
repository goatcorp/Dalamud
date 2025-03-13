using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dalamud.Game.Gui.PartyFinder.Internal;

/// <summary>
/// The structure of the PartyFinder packet.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Sequential struct marshaling.")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Sequential struct marshaling.")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Document the field usage.")]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct PartyFinderPacket
{
    /// <summary>
    /// Gets the size of this packet.
    /// </summary>
    internal static int PacketSize { get; } = Marshal.SizeOf<PartyFinderPacket>();

    internal readonly int BatchNumber;

    private fixed byte paddingOuter[0x8];

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    internal readonly PartyFinderPacketListing[] Listings;
}
