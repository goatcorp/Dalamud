#nullable enable
#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Unicode;

using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.CorePlugin.MyFonts.OnDemandFonts;

internal abstract unsafe class OnDemandFont : IDisposable
{
    protected const int FrequentKerningPairsMaxCodepoint = 128;

    protected OnDemandFont(OnDemandAtlas atlas, BitArray? loadAttemptedGlyphs)
    {
        this.Atlas = atlas;
        this.FontNative = ImGuiNative.ImFont_ImFont();
        this.IndexedHotData = new(&this.FontNative->IndexedHotData, null);
        this.FrequentKerningPairs = new(&this.FontNative->FrequentKerningPairs, null);
        this.IndexLookup = new(&this.FontNative->IndexLookup, null);
        this.Glyphs = new(&this.FontNative->Glyphs, null);
        this.KerningPairs = new(&this.FontNative->KerningPairs, null);
        this.LoadAttemptedGlyphs = loadAttemptedGlyphs ?? new(0x10000, false);

        this.FrequentKerningPairs.Resize(FrequentKerningPairsMaxCodepoint * FrequentKerningPairsMaxCodepoint);
    }

    ~OnDemandFont() => this.Dispose(false);

    public bool IsDisposed { get; private set; }

    public OnDemandAtlas Atlas { get; }

    public ref ImFont Font => ref *this.FontNative;

    public ImFontPtr FontPtr => new(this.FontNative);

    public ImVectorWrapper<float> FrequentKerningPairs { get; }

    public ImVectorWrapper<ImGuiHelpers.ImFontGlyphReal> Glyphs { get; }

    public ImVectorWrapper<ImGuiHelpers.ImFontGlyphHotDataReal> IndexedHotData { get; }

    public ImVectorWrapper<ushort> IndexLookup { get; }

    public ImVectorWrapper<ImFontKerningPair> KerningPairs { get; }

    public BitArray LoadAttemptedGlyphs { get; }

    protected ImFont* FontNative { get; }

    public abstract bool IsCharAvailable(char c);

    public abstract void LoadGlyphs(IEnumerable<char> chars);

    public abstract void LoadGlyphs(IEnumerable<UnicodeRange> ranges);

    public void LoadGlyphs(params char[] chars) => this.LoadGlyphs((IEnumerable<char>)chars);

    public void LoadGlyphs(params UnicodeRange[] ranges) => this.LoadGlyphs((IEnumerable<UnicodeRange>)ranges);

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public char FirstAvailableChar(IEnumerable<char> chars)
    {
        var lastc = '\0';
        foreach (var c in chars)
        {
            lastc = c;
            if (this.IsCharAvailable(c))
                return c;
        }

        return lastc;
    }

    public char FirstAvailableChar(params char[] chars) => this.FirstAvailableChar(chars.AsEnumerable());

    public ImGuiHelpers.ImFontGlyphReal* FindLoadedGlyph(int c)
        => c is >= 0 and <= ushort.MaxValue
               ? (ImGuiHelpers.ImFontGlyphReal*)ImGuiNative.ImFont_FindGlyph(this.FontNative, (ushort)c)
               : null;

    public ImGuiHelpers.ImFontGlyphReal* FindLoadedGlyphNoFallback(int c)
        => c is >= 0 and <= ushort.MaxValue
               ? (ImGuiHelpers.ImFontGlyphReal*)ImGuiNative.ImFont_FindGlyphNoFallback(this.FontNative, (ushort)c)
               : null;

    public void EnsureIndex(int maxCodepoint)
    {
        maxCodepoint = Math.Max(ushort.MaxValue, maxCodepoint);
        var oldLength = this.IndexLookup.Length;
        if (oldLength >= maxCodepoint + 1)
            return;

        this.IndexedHotData.Resize(maxCodepoint + 1);
        this.IndexLookup.Resize(maxCodepoint + 1, ushort.MaxValue);
    }

    internal void SanityCheck()
    {
        _ = Marshal.ReadIntPtr((nint)this.FontNative->ContainerAtlas);
        _ = Marshal.ReadIntPtr((nint)this.FontNative->FallbackGlyph);
        var texIndex = ((ImGuiHelpers.ImFontGlyphReal*)this.FontNative->FallbackGlyph)->TextureIndex;
        var textures = this.FontNative->ContainerAtlas->Textures.Wrap<ImFontAtlasTexture>();
        var texId = textures[texIndex].TexID;
        if (texId != 0)
            _ = Marshal.ReadIntPtr(texId);
    }

    protected void AllocateGlyphSpaces(int startIndex, int count)
    {
        foreach (ref var glyph in this.Glyphs.AsSpan.Slice(startIndex, count))
            this.Atlas.AllocateGlyphSpace(ref glyph);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.IsDisposed)
            return;
        this.IsDisposed = true;
        ImGuiNative.ImFont_destroy(this.FontNative);
    }

    /// <summary>
    /// Updates references stored in ImFont.
    /// </summary>
    /// <remarks>
    /// Need to fix our custom ImGui, so that imgui_widgets.cpp:3656 stops thinking
    /// Codepoint &lt; FallbackHotData.size always means it's not fallback char.
    /// </remarks>
    protected void UpdateReferencesToVectorItems()
    {
        this.Font.FallbackGlyph = (ImFontGlyph*)this.FindLoadedGlyphNoFallback(this.Font.FallbackChar);
        this.Font.FallbackHotData =
            this.Font.FallbackChar == ushort.MaxValue
                ? null
                : (ImFontGlyphHotData*)(this.IndexedHotData.Data + this.Font.FallbackChar);

        var fallbackHotData = this.IndexedHotData[this.Font.FallbackChar];
        foreach (var codepoint in Enumerable.Range(0, this.IndexedHotData.Length))
        {
            if (this.IndexLookup[codepoint] == ushort.MaxValue)
                this.IndexedHotData[codepoint] = fallbackHotData;
        }
    }

    protected void Mark4KPageUsed(in ImGuiHelpers.ImFontGlyphReal glyph)
    {
        // Mark 4K page as used
        var pageIndex = unchecked((ushort)(glyph.Codepoint / 4096));
        this.Font.Used4kPagesMap[pageIndex >> 3] |= unchecked((byte)(1 << (pageIndex & 7)));
    }
}
