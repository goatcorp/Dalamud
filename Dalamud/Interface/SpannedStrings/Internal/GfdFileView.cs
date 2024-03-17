using System.IO;
using System.Runtime.InteropServices;

namespace Dalamud.Interface.SpannedStrings.Internal;

/// <summary>Reference member view of a .gfd file data.</summary>
internal readonly unsafe ref struct GfdFileView
{
    private readonly ReadOnlySpan<byte> span;
    private readonly bool directLookup;

    /// <summary>Initializes a new instance of the <see cref="GfdFileView"/> struct.</summary>
    /// <param name="span">The data.</param>
    public GfdFileView(ReadOnlySpan<byte> span)
    {
        this.span = span;
        if (span.Length < sizeof(GfdHeader))
            throw new InvalidDataException($"Not enough space for a {nameof(GfdHeader)}");
        if (span.Length < sizeof(GfdHeader) + (this.Header.Count * sizeof(GfdEntry)))
            throw new InvalidDataException($"Not enough space for all the {nameof(GfdEntry)}");

        var entries = this.Entries;
        this.directLookup = true;
        for (var i = 0; i < entries.Length && this.directLookup; i++)
            this.directLookup &= i + 1 == entries[i].Id;
    }

    /// <summary>Gets the header.</summary>
    public ref readonly GfdHeader Header => ref MemoryMarshal.AsRef<GfdHeader>(this.span);

    /// <summary>Gets the entries.</summary>
    public ReadOnlySpan<GfdEntry> Entries => MemoryMarshal.Cast<byte, GfdEntry>(this.span[sizeof(GfdHeader)..]);

    /// <summary>Attempts to get an entry.</summary>
    /// <param name="iconId">The icon ID.</param>
    /// <param name="entry">The entry.</param>
    /// <param name="followRedirect">Whether to follow redirects.</param>
    /// <returns><c>true</c> if found.</returns>
    public bool TryGetEntry(uint iconId, out GfdEntry entry, bool followRedirect = true)
    {
        if (iconId == 0)
        {
            entry = default;
            return false;
        }

        var entries = this.Entries;
        if (this.directLookup)
        {
            if (iconId <= entries.Length)
            {
                entry = entries[(int)(iconId - 1)];
                return !entry.IsEmpty;
            }

            entry = default;
            return false;
        }

        var lo = 0;
        var hi = entries.Length;
        while (lo <= hi)
        {
            var i = lo + ((hi - lo) >> 1);
            if (entries[i].Id == iconId)
            {
                if (followRedirect && entries[i].Redirect != 0)
                {
                    iconId = entries[i].Redirect;
                    lo = 0;
                    hi = entries.Length;
                    continue;
                }

                entry = entries[i];
                return !entry.IsEmpty;
            }

            if (entries[i].Id < iconId)
                lo = i + 1;
            else
                hi = i - 1;
        }

        entry = default;
        return false;
    }

    /// <summary>Header of a .gfd file.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GfdHeader
    {
        /// <summary>Signature: "gftd0100".</summary>
        public fixed byte Signature[8];

        /// <summary>Number of entries.</summary>
        public int Count;

        /// <summary>Unused/unknown.</summary>
        public fixed byte Padding[4];
    }

    /// <summary>An entry of a .gfd file.</summary>
    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct GfdEntry
    {
        /// <summary>ID of the entry.</summary>
        public ushort Id;

        /// <summary>The left offset of the entry.</summary>
        public ushort Left;

        /// <summary>The top offset of the entry.</summary>
        public ushort Top;

        /// <summary>The width of the entry.</summary>
        public ushort Width;

        /// <summary>The height of the entry.</summary>
        public ushort Height;

        /// <summary>Unknown/unused.</summary>
        public ushort Unk0A;

        /// <summary>The redirected entry, maybe.</summary>
        public ushort Redirect;

        /// <summary>Unknown/unused.</summary>
        public ushort Unk0E;

        /// <summary>Gets a value indicating whether this entry is effectively empty.</summary>
        public bool IsEmpty => this.Width == 0 || this.Height == 0;
    }
}
