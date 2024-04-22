using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// Deals with TrueType.
/// </summary>
internal static partial class TrueTypeUtils
{
    /// <summary>
    /// Checks whether the given <paramref name="fontConfig"/> will fail in <see cref="ImFontAtlasPtr.Build"/>,
    /// and throws an appropriate exception if it is the case.
    /// </summary>
    /// <param name="fontConfig">The font config.</param>
    public static unsafe void CheckImGuiCompatibleOrThrow(in ImFontConfig fontConfig)
    {
        var ranges = fontConfig.GlyphRanges;
        var sfnt = AsSfntFile(fontConfig);
        var cmap = new Cmap(sfnt);
        if (cmap.UnicodeTable is not { } unicodeTable)
            throw new NotSupportedException("The font does not have a compatible Unicode character mapping table.");
        if (unicodeTable.All(x => !ImGuiHelpers.IsCodepointInSuppliedGlyphRangesUnsafe(x.Key, ranges)))
            throw new NotSupportedException("The font does not have any glyph that falls under the requested range.");
    }

    /// <summary>
    /// Enumerates through horizontal pair adjustments of a kern and gpos tables.
    /// </summary>
    /// <param name="fontConfig">The font config.</param>
    /// <returns>The enumerable of pair adjustments. Distance values need to be multiplied by font size in pixels.</returns>
    public static IEnumerable<(char Left, char Right, float Distance)> ExtractHorizontalPairAdjustments(
        ImFontConfig fontConfig)
    {
        float multiplier;
        Dictionary<ushort, char[]> glyphToCodepoints;
        Gpos gpos = default;
        Kern kern = default;

        try
        {
            var sfnt = AsSfntFile(fontConfig);
            var head = new Head(sfnt);
            multiplier = 3f / 4 / head.UnitsPerEm;

            if (new Cmap(sfnt).UnicodeTable is not { } table)
                yield break;

            if (sfnt.ContainsKey(Kern.DirectoryTableTag))
                kern = new(sfnt);
            else if (sfnt.ContainsKey(Gpos.DirectoryTableTag))
                gpos = new(sfnt);
            else
                yield break;

            glyphToCodepoints = table
                                .GroupBy(x => x.Value, x => x.Key)
                                .OrderBy(x => x.Key)
                                .ToDictionary(
                                    x => x.Key,
                                    x => x.Where(y => y <= ushort.MaxValue)
                                          .Select(y => (char)y)
                                          .ToArray());
        }
        catch
        {
            // don't care; give up
            yield break;
        }

        if (kern.Memory.Count != 0)
        {
            foreach (var pair in kern.EnumerateHorizontalPairs())
            {
                if (!glyphToCodepoints.TryGetValue(pair.Left, out var leftChars))
                    continue;
                if (!glyphToCodepoints.TryGetValue(pair.Right, out var rightChars))
                    continue;

                foreach (var l in leftChars)
                {
                    foreach (var r in rightChars)
                        yield return (l, r, pair.Value * multiplier);
                }
            }
        }
        else if (gpos.Memory.Count != 0)
        {
            foreach (var pair in gpos.ExtractAdvanceX())
            {
                if (!glyphToCodepoints.TryGetValue(pair.Left, out var leftChars))
                    continue;
                if (!glyphToCodepoints.TryGetValue(pair.Right, out var rightChars))
                    continue;

                foreach (var l in leftChars)
                {
                    foreach (var r in rightChars)
                        yield return (l, r, pair.Value * multiplier);
                }
            }
        }
    }

    private static unsafe SfntFile AsSfntFile(in ImFontConfig fontConfig)
    {
        var memory = new PointerSpan<byte>((byte*)fontConfig.FontData, fontConfig.FontDataSize);
        if (memory.Length < 4)
            throw new NotSupportedException("File is too short to even have a magic.");

        var magic = memory.ReadU32Big(0);
        if (BitConverter.IsLittleEndian)
            magic = BinaryPrimitives.ReverseEndianness(magic);

        if (magic == SfntFile.FileTagTrueType1.NativeValue)
            return new(memory);
        if (magic == SfntFile.FileTagType1.NativeValue)
            return new(memory);
        if (magic == SfntFile.FileTagOpenTypeWithCff.NativeValue)
            return new(memory);
        if (magic == SfntFile.FileTagOpenType1_0.NativeValue)
            return new(memory);
        if (magic == SfntFile.FileTagTrueTypeApple.NativeValue)
            return new(memory);
        if (magic == TtcFile.FileTag.NativeValue)
            return new TtcFile(memory)[fontConfig.FontNo];

        throw new NotSupportedException($"The given file with the magic 0x{magic:X08} is not supported.");
    }
}
