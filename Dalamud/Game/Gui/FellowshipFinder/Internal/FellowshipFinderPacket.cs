using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dalamud.Game.Gui.FellowshipFinder.Internal
{
    /// <summary>
    /// The structure of the FellowshipFinder packet.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Sequential struct marshaling.")]
    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Sequential struct marshaling.")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Document the field usage.")]
    [StructLayout(LayoutKind.Explicit)]
    public struct FellowshipFinderPacket
    {
        [FieldOffset(8)]
        internal uint Unknown;

        [FieldOffset(12)]
        internal uint ChunkNumber;

        [FieldOffset(16)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        internal FellowshipFinderPacketListing[] Listings;
    }
}
