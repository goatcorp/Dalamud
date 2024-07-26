using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

using Lumina.Data;

namespace Dalamud.Interface.Internal.ImGuiSeStringRenderer;

/// <summary>Reference member view of a .gfd file data.</summary>
internal sealed unsafe class GfdFile : FileResource
{
    /// <summary>Gets or sets the file header.</summary>
    public GfdHeader Header { get; set; }

    /// <summary>Gets or sets the entries.</summary>
    public GfdEntry[] Entries { get; set; } = [];

    /// <inheritdoc/>
    public override void LoadFile()
    {
        if (this.DataSpan.Length < sizeof(GfdHeader))
            throw new InvalidDataException($"Not enough space for a {nameof(GfdHeader)}");
        if (this.DataSpan.Length < sizeof(GfdHeader) + (this.Header.Count * sizeof(GfdEntry)))
            throw new InvalidDataException($"Not enough space for all the {nameof(GfdEntry)}");

        this.Header = MemoryMarshal.AsRef<GfdHeader>(this.DataSpan);
        this.Entries = MemoryMarshal.Cast<byte, GfdEntry>(this.DataSpan[sizeof(GfdHeader)..]).ToArray();
    }

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
        if (iconId <= this.Entries.Length && entries[(int)(iconId - 1)].Id == iconId)
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

        /// <summary>Gets or sets the size of this entry.</summary>
        public Vector2 Size
        {
            get => new(this.Width, this.Height);
            set => (this.Width, this.Height) = (checked((ushort)value.X), checked((ushort)value.Y));
        }

        /// <summary>Gets the UV0 of this entry.</summary>
        public Vector2 Uv0 => new(this.Left / 512f, this.Top / 1024f);

        /// <summary>Gets the UV1 of this entry.</summary>
        public Vector2 Uv1 => new((this.Left + this.Width) / 512f, (this.Top + this.Height) / 1024f);

        /// <summary>Gets the UV0 of the HQ version of this entry.</summary>
        public Vector2 HqUv0 => new(this.Left / 256f, (this.Top + 170.5f) / 512f);

        /// <summary>Gets the UV1 of the HQ version of this entry.</summary>
        public Vector2 HqUv1 => new((this.Left + this.Width) / 256f, (this.Top + this.Height + 170.5f) / 512f);
    }
}
