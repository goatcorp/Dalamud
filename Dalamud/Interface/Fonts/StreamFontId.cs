using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;

using Dalamud.Interface.Fonts.Internal;

using Newtonsoft.Json;

namespace Dalamud.Interface.Fonts;

/// <summary>Represents a font read from a stream.</summary>
/// <remarks>Cannot be serialized.</remarks>
[JsonConverter(typeof(NotSerializableJsonConverter))]
public sealed class StreamFontId : SingleFontId
{
    /// <summary>Initializes a new instance of the <see cref="StreamFontId"/> class.</summary>
    /// <param name="stream">A stream with the font file from its current position.</param>
    /// <param name="name">Display name, or <see langword="null"/> to use a key made from the data.</param>
    /// <param name="fontNo">Index of the face in a collection.</param>
    internal StreamFontId(Stream stream, string? name = null, int fontNo = 0)
    {
        this.FontNo = fontNo;
        long length = -1;
        try
        {
            length = Math.Max(stream.Length - stream.Position, 0);
        }
        catch
        {
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            this.Data = ms.ToArray();
        }

        if (length >= 0)
        {
            var data = new byte[length];
            stream.ReadExactly(data);
            this.Data = data;
        }

        this.Key = ComputeKey(this.Data!);
        this.Name = new(name ?? this.Key);
    }

    /// <summary>Initializes a new instance of the <see cref="StreamFontId"/> class.</summary>
    /// <param name="data">The font file, not copied.</param>
    /// <param name="name">Display name, or <see langword="null"/> to use a key made from the data.</param>
    /// <param name="fontNo">Index of the face in a collection.</param>
    internal StreamFontId(byte[] data, string? name = null, int fontNo = 0)
    {
        this.FontNo = fontNo;
        this.Data = data;
        this.Key = ComputeKey(data);
        this.Name = new(name ?? this.Key);
    }

    /// <inheritdoc/>
    public override LocalizedName Name { get; }

    /// <summary>Gets the index of the face in a font collection, or 0.</summary>
    public int FontNo { get; }

    /// <summary>Gets a string sufficient to distinguish a font from another.</summary>
    internal string Key { get; }

    /// <summary>Gets the font file.</summary>
    internal byte[] Data { get; }

    /// <inheritdoc/>
    private protected override string ShortName =>
        this.FontNo == 0 ? this.Name.English : $"{this.Name.English}#{this.FontNo}";

    public static bool operator ==(StreamFontId? left, StreamFontId? right) => Equals(left, right);

    public static bool operator !=(StreamFontId? left, StreamFontId? right) => !Equals(left, right);

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is StreamFontId other && this.Key == other.Key && this.FontNo == other.FontNo;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(this.Key, this.FontNo);

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(StreamFontId)}:{this.Name.English}";

    // Hashes the head and name tables of every face, plus the file length. The head table holds the version and a
    // checksum of the whole file.
    private static string ComputeKey(byte[] data)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        try
        {
            var span = data.AsSpan();
            var faceOffsets = BinaryPrimitives.ReadUInt32BigEndian(span) == 0x74746366 /* ttcf */
                                  ? ReadCollectionOffsets(span)
                                  : [0];
            foreach (var face in faceOffsets)
            {
                var numTables = BinaryPrimitives.ReadUInt16BigEndian(span[(face + 4)..]);
                for (var i = 0; i < numTables; i++)
                {
                    var record = span[(face + 12 + (i * 16))..];
                    var tag = BinaryPrimitives.ReadUInt32BigEndian(record);
                    if (tag is not (0x68656164 /* head */ or 0x6E616D65 /* name */))
                        continue;
                    var offset = (int)BinaryPrimitives.ReadUInt32BigEndian(record[8..]);
                    var length = (int)BinaryPrimitives.ReadUInt32BigEndian(record[12..]);
                    sha.AppendData(span.Slice(offset, length));
                }
            }
        }
        catch (Exception e) when (e is ArgumentOutOfRangeException or IndexOutOfRangeException)
        {
            sha.AppendData(data);
        }

        return $"{data.Length}:{Convert.ToHexString(sha.GetHashAndReset())}";
    }

    private static int[] ReadCollectionOffsets(ReadOnlySpan<byte> span)
    {
        // A broken file must not make a huge array. The data cannot hold more offsets than this.
        var count = (int)Math.Min(
                        BinaryPrimitives.ReadUInt32BigEndian(span[8..]),
                        (uint)Math.Max(0, (span.Length - 12) / 4));
        var offsets = new int[count];
        for (var i = 0; i < count; i++)
            offsets[i] = (int)BinaryPrimitives.ReadUInt32BigEndian(span[(12 + (i * 4))..]);
        return offsets;
    }
}
