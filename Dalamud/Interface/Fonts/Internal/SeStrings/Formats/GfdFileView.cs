using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Interface.Fonts.Internal.SeStrings.Formats;

/// <summary>View of a game bitmap font icon table file (common/font/gfdata.gfd).</summary>
internal readonly struct GfdFileView
{
    private readonly ReadOnlyMemory<byte> data;
    private readonly int safeCount;

    /// <summary>Initializes a new instance of the <see cref="GfdFileView"/> struct.</summary>
    /// <param name="data">Contents of the file.</param>
    /// <exception cref="InvalidDataException">If the header is invalid or does not fit in the data.</exception>
    public GfdFileView(ReadOnlyMemory<byte> data)
    {
        this.data = data;
        if (data.Length < Unsafe.SizeOf<GfdHeader>())
            throw new InvalidDataException("Truncated file");
        if (!this.Header.ValidateSignature())
            throw new InvalidDataException("Invalid signature");
        this.safeCount = Math.Clamp(
            this.Header.Count,
            0,
            (data.Length - Unsafe.SizeOf<GfdHeader>()) / Unsafe.SizeOf<GfdEntry>());
    }

    /// <summary>Gets the header.</summary>
    public ref readonly GfdHeader Header => ref MemoryMarshal.AsRef<GfdHeader>(this.data.Span);

    /// <summary>Gets the entries, sorted by ID.</summary>
    public ReadOnlySpan<GfdEntry> Entries =>
        MemoryMarshal.Cast<byte, GfdEntry>(
            this.data.Span.Slice(Unsafe.SizeOf<GfdHeader>(), this.safeCount * Unsafe.SizeOf<GfdEntry>()));

    /// <summary>Gets an entry.</summary>
    /// <param name="iconId">The icon ID.</param>
    /// <param name="entry">The entry, after redirects.</param>
    /// <returns>Whether there is a non-empty entry.</returns>
    public bool TryGetEntry(uint iconId, out GfdEntry entry)
    {
        var entries = this.Entries;
        entry = default;

        // The hops are bounded by the number of entries, so that a cycle ends.
        for (var hops = 0; iconId != 0 && hops <= entries.Length; hops++)
        {
            // Entries are usually at their ID minus one.
            var i = iconId <= entries.Length && entries[(int)iconId - 1].Id == iconId
                        ? (int)iconId - 1
                        : entries.BinarySearch(new IdKey(iconId));
            if (i < 0)
                return false;
            if (entries[i].Redirect != 0 && entries[i].Redirect != iconId)
            {
                iconId = entries[i].Redirect;
                continue;
            }

            entry = entries[i];
            return !entry.IsEmpty;
        }

        return false;
    }

    private readonly record struct IdKey(uint Id) : IComparable<GfdEntry>
    {
        public int CompareTo(GfdEntry other) => this.Id.CompareTo(other.Id);
    }
}
