#nullable enable
#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.CorePlugin.MyFonts.ImFontWrappers;

internal abstract unsafe class ImFontWrapper : IDisposable
{
    protected ImFontWrapper(FontChainAtlas atlas, BitArray? loadAttemptedGlyphs)
    {
        this.Atlas = atlas;
        this.FontNative = ImGuiNative.ImFont_ImFont();
        this.IndexedHotData = new(&this.FontNative->IndexedHotData, null);
        this.FrequentKerningPairs = new(&this.FontNative->FrequentKerningPairs, null);
        this.IndexLookup = new(&this.FontNative->IndexLookup, null);
        this.Glyphs = new(&this.FontNative->Glyphs, null);
        this.KerningPairs = new(&this.FontNative->KerningPairs, null);
        this.LoadAttemptedGlyphs = loadAttemptedGlyphs ?? new(0x10000, false);
    }

    ~ImFontWrapper() => this.Dispose(false);

    public bool IsDisposed { get; private set; }

    public FontChainAtlas Atlas { get; }

    public ref ImFont Font => ref *this.FontNative;

    public ImFontPtr FontPtr => new(this.FontNative);

    public ImVectorWrapper<float> FrequentKerningPairs { get; }

    public ImVectorWrapper<ImGuiHelpers.ImFontGlyphReal> Glyphs { get; }

    public ImVectorWrapper<ImGuiHelpers.ImFontGlyphHotDataReal> IndexedHotData { get; }

    public ImVectorWrapper<ushort> IndexLookup { get; }

    public ImVectorWrapper<ImFontKerningPair> KerningPairs { get; }

    public BitArray LoadAttemptedGlyphs { get; }

    protected ImFont* FontNative { get; }

    public abstract void LoadGlyphs(IEnumerable<char> chars);

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
            var n = this.FindGlyphNoFallback(c);
            if (n != null)
                return (char)n->Codepoint;
        }

        return lastc;
    }

    public char FirstAvailableChar(params char[] chars) => this.FirstAvailableChar(chars.AsEnumerable());

    public ImGuiHelpers.ImFontGlyphReal* FindGlyph(int c)
        => c is >= 0 and <= ushort.MaxValue
               ? (ImGuiHelpers.ImFontGlyphReal*)ImGuiNative.ImFont_FindGlyph(this.FontNative, (ushort)c)
               : null;

    public ImGuiHelpers.ImFontGlyphReal* FindGlyphNoFallback(int c)
        => c is >= 0 and <= ushort.MaxValue
               ? (ImGuiHelpers.ImFontGlyphReal*)ImGuiNative.ImFont_FindGlyph(this.FontNative, (ushort)c)
               : null;

    public void GrowIndex(ushort maxCodepoint)
    {
        var oldLength = this.IndexLookup.Length;
        if (oldLength >= maxCodepoint + 1)
            return;

        this.IndexedHotData.Resize(maxCodepoint + 1);
        this.IndexLookup.Resize(maxCodepoint + 1, ushort.MaxValue);
    }

    protected void Dispose(bool disposing)
    {
        if (this.IsDisposed)
            return;
        this.IsDisposed = true;
        ImGuiNative.ImFont_destroy(this.FontNative);
    }

    /// <summary>
    /// Repairs IndexedHotData, so that &quot;InputTextCalcTextSizeW&quot; does not trip.
    /// </summary>
    /// <remarks>
    /// Need to fix our custom ImGui, so that imgui_widgets.cpp:3656 stops thinking
    /// Codepoint &lt; FallbackHotData.size always means it's not fallback char.
    /// </remarks>
    protected void RepairHotData()
    {
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
