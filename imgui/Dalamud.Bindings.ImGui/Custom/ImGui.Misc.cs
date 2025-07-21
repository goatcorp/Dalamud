using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    public static ImFontPtr AddFontFromFileTTF(
        ImFontAtlasPtr self, AutoUtf8Buffer filename, float sizePixels, ImFontConfigPtr fontCfg = default,
        ushort* glyphRanges = null)
    {
        fixed (byte* filenamePtr = filename.NullTerminatedSpan)
        {
            var r = ImGuiNative.AddFontFromFileTTF(self, filenamePtr, sizePixels, fontCfg, glyphRanges);
            filename.Dispose();
            return r;
        }
    }

    public static ImFontPtr AddFontFromMemoryCompressedBase85TTF(
        ImFontAtlasPtr self, AutoUtf8Buffer compressedFontDatabase85, float sizePixels,
        ImFontConfigPtr fontCfg = default, ushort* glyphRanges = null)
    {
        fixed (byte* compressedFontDatabase85Ptr = compressedFontDatabase85.NullTerminatedSpan)
        {
            var r = ImGuiNative.AddFontFromMemoryCompressedBase85TTF(
                self,
                compressedFontDatabase85Ptr,
                sizePixels,
                fontCfg,
                glyphRanges);
            compressedFontDatabase85.Dispose();
            return r;
        }
    }

    public static ImFontPtr AddFontFromMemoryCompressedTTF(
        ImFontAtlasPtr self, ReadOnlySpan<byte> compressedFontData, float sizePixels, ImFontConfigPtr fontCfg = default,
        ushort* glyphRanges = null)
    {
        fixed (byte* compressedFontPtr = compressedFontData)
            return ImGuiNative.AddFontFromMemoryCompressedTTF(
                self,
                compressedFontPtr,
                compressedFontData.Length,
                sizePixels,
                fontCfg,
                glyphRanges);
    }

    public static ImFontPtr AddFontFromMemoryTTF(
        ImFontAtlasPtr self, ReadOnlySpan<byte> fontData, float sizePixels, ImFontConfigPtr fontCfg = default,
        ushort* glyphRanges = null)
    {
        fixed (byte* fontDataPtr = fontData)
            return ImGuiNative.AddFontFromMemoryTTF(
                self,
                fontDataPtr,
                fontData.Length,
                sizePixels,
                fontCfg,
                glyphRanges);
    }

// DISCARDED: PlotHistogram
// DISCARDED: PlotLines
}
