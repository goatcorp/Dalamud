using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;

namespace Dalamud.Game.Gui.PartyFinder.Internal;

/// <summary>
/// The structure of an individual listing within a packet.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Sequential struct marshaling.")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Document the field usage.")]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct PartyFinderPacketListing
{
    private fixed byte padding1[4];
    internal uint Id;

    private fixed byte padding2[4];
    internal uint PaddingId;

    private fixed byte padding3[4];
    internal ulong ContentId;

    private fixed byte padding4[4];
    internal ushort Category;

    private fixed byte padding5[2];
    internal ushort Duty;
    internal byte DutyType;

    private fixed byte padding6[11];
    internal ushort World;

    private fixed byte padding7[8];
    internal byte Objective;
    internal byte BeginnersWelcome;
    internal byte Conditions;
    internal byte DutyFinderSettings;
    internal byte LootRules;

    private fixed byte padding8[3];
    internal uint LastPatchHotfixTimestamp; // last time the servers were restarted?
    internal ushort SecondsRemaining;

    private fixed byte padding9[6];
    internal ushort MinimumItemLevel;
    internal ushort HomeWorld;
    internal ushort CurrentWorld;

    private byte padding10;
    internal byte NumSlots;
    internal byte NumSlotsFilled;

    private byte padding11;
    internal byte SearchArea;

    private byte padding12;
    internal byte NumParties;

    private fixed byte padding13[7];
    private fixed ulong slots[8];
    private fixed byte jobsPresent[8];
    private fixed byte name[32];
    private fixed byte description[192];

    private fixed byte padding14[4];
    
    #region Helper

    internal ulong[] Slots
    {
        get
        {
            fixed (ulong* ptr = this.slots)
            {
                return new ReadOnlySpan<ulong>(ptr, 8).ToArray();
            }
        }
    }

    internal byte[] JobsPresent
    {
        get
        {
            fixed (byte* ptr = this.jobsPresent)
            {
                return new ReadOnlySpan<byte>(ptr, 8).ToArray();
            }
        }
    }

    internal byte[] Name
    {
        get
        {
            fixed (byte* ptr = this.name)
            {
                return new ReadOnlySpan<byte>(ptr, 32).ToArray();
            }
        }
    }

    internal byte[] Description
    {
        get
        {
            fixed (byte* ptr = this.description)
            {
                return new ReadOnlySpan<byte>(ptr, 192).ToArray();
            }
        }
    }

    #endregion

    internal bool IsNull()
    {
        // a valid party finder must have at least one slot set
        return this.Slots.All(slot => slot == 0);
    }
}
