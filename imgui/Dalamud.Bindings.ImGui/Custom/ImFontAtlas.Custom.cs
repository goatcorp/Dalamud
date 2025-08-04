namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImFontAtlas
{
    public ImFontPtr AddFontFromFileTTF(
        ImU8String filename, float sizePixels, ImFontConfigPtr fontCfg = default, ushort* glyphRanges = null)
    {
        fixed (ImFontAtlas* thisPtr = &this)
            return ImGui.AddFontFromFileTTF(thisPtr, filename, sizePixels, fontCfg, glyphRanges);
    }

    public ImFontPtr AddFontFromMemoryCompressedBase85TTF(
        ImU8String compressedFontDatabase85, float sizePixels, ImFontConfigPtr fontCfg = default,
        ushort* glyphRanges = null)
    {
        fixed (ImFontAtlas* thisPtr = &this)
        {
            return ImGui.AddFontFromMemoryCompressedBase85TTF(
                thisPtr,
                compressedFontDatabase85,
                sizePixels,
                fontCfg,
                glyphRanges);
        }
    }

    public ImFontPtr AddFontFromMemoryCompressedTTF(
        ReadOnlySpan<byte> compressedFontData, float sizePixels, ImFontConfigPtr fontCfg = default,
        ushort* glyphRanges = null)
    {
        fixed (ImFontAtlas* thisPtr = &this)
        {
            return ImGui.AddFontFromMemoryCompressedTTF(
                thisPtr,
                compressedFontData,
                sizePixels,
                fontCfg,
                glyphRanges);
        }
    }

    public ImFontPtr AddFontFromMemoryTTF(
        ReadOnlySpan<byte> fontData, float sizePixels, ImFontConfigPtr fontCfg = default,
        ushort* glyphRanges = null)
    {
        fixed (ImFontAtlas* thisPtr = &this)
        {
            return ImGui.AddFontFromMemoryTTF(
                thisPtr,
                fontData,
                sizePixels,
                fontCfg,
                glyphRanges);
        }
    }
}

public unsafe partial struct ImFontAtlasPtr
{
    public ImFontPtr AddFontFromFileTTF(
        ImU8String filename, float sizePixels, ImFontConfigPtr fontCfg = default, ushort* glyphRanges = null) =>
        ImGui.AddFontFromFileTTF(this, filename, sizePixels, fontCfg, glyphRanges);

    public ImFontPtr AddFontFromMemoryCompressedBase85TTF(
        ImU8String compressedFontDatabase85, float sizePixels, ImFontConfigPtr fontCfg = default,
        ushort* glyphRanges = null) =>
        ImGui.AddFontFromMemoryCompressedBase85TTF(this, compressedFontDatabase85, sizePixels, fontCfg, glyphRanges);

    public ImFontPtr AddFontFromMemoryCompressedTTF(
        ReadOnlySpan<byte> compressedFontData, float sizePixels, ImFontConfigPtr fontCfg = default,
        ushort* glyphRanges = null) =>
        ImGui.AddFontFromMemoryCompressedTTF(this, compressedFontData, sizePixels, fontCfg, glyphRanges);

    public ImFontPtr AddFontFromMemoryTTF(
        ReadOnlySpan<byte> fontData, float sizePixels, ImFontConfigPtr fontCfg = default,
        ushort* glyphRanges = null) =>
        ImGui.AddFontFromMemoryTTF(this, fontData, sizePixels, fontCfg, glyphRanges);
}
