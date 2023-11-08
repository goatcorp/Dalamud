#nullable enable
#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text.Unicode;

using Dalamud.Interface.EasyFonts;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.CorePlugin.MyFonts.ImFontWrappers;

#pragma warning disable CS1591
internal unsafe class ChainedImFontWrapper : ImFontWrapper
{
    public ChainedImFontWrapper(FontChainAtlas atlas, in FontChain chain, IEnumerable<ImFontWrapper> subfonts)
        : base(atlas, null)
    {
        this.Chain = chain;
        this.Subfonts = subfonts.ToImmutableList();

        this.Font.FontSize = MathF.Round(chain.Fonts[0].SizePx * chain.LineHeight);
        this.Font.FallbackChar = this.FirstAvailableChar(
            (char)InterfaceManager.Fallback1Codepoint,
            (char)InterfaceManager.Fallback2Codepoint,
            '?',
            ' ');
        this.Font.EllipsisChar = this.FirstAvailableChar('…', char.MaxValue);
        this.Font.DotChar = this.FirstAvailableChar('.', char.MaxValue);
        this.Font.DirtyLookupTables = 0;
        this.Font.Scale = 1f;
        this.Font.Ascent = this.Subfonts[0].Font.Ascent
                           + MathF.Ceiling((chain.Fonts[0].SizePx * (chain.LineHeight - 1f)) / 2);
        this.Font.Descent = this.Subfonts[0].Font.Descent
                            + MathF.Floor((chain.Fonts[0].SizePx * (chain.LineHeight - 1f)) / 2);
        this.LoadGlyphs(' ', (char)this.Font.FallbackChar, (char)this.Font.EllipsisChar, (char)this.Font.DotChar);
    }

    public FontChain Chain { get; set; }

    public IReadOnlyList<ImFontWrapper> Subfonts { get; set; }

    /// <inheritdoc/>
    public override bool IsCharAvailable(char c) =>
        this.Chain.Fonts.Zip(this.Subfonts).Any(x => x.First.RangeContainsCharacter(c) && x.Second.IsCharAvailable(c));

    /// <inheritdoc/>
    public override void LoadGlyphs(IEnumerable<char> chars)
    {
        if (chars is not ICollection<char> coll)
            coll = chars.ToArray();

        if (!coll.Any())
            return;
        this.EnsureIndex(coll.Max());

        var changed = false;
        foreach (var (entry, font) in this.Chain.Fonts.Zip(this.Subfonts))
        {
            font.LoadGlyphs(coll);
            foreach (var c in coll)
                changed |= this.EnsureCharacter(c, entry, font);
        }

        foreach (var c in coll)
            this.LoadAttemptedGlyphs[c] = true;

        if (changed)
            this.UpdateReferencesToVectorItems();
    }

    /// <inheritdoc/>
    public override void LoadGlyphs(IEnumerable<UnicodeRange> ranges)
    {
        if (ranges is not ICollection<UnicodeRange> coll)
            coll = ranges.ToArray();

        if (!coll.Any())
            return;
        this.EnsureIndex(coll.Max(x => x.FirstCodePoint + (x.Length - 1)));

        var changed = false;
        foreach (var (entry, font) in this.Chain.Fonts.Zip(this.Subfonts))
        {
            font.LoadGlyphs(coll);
            foreach (var c in coll)
            {
                foreach (var cc in Enumerable.Range(c.FirstCodePoint, c.Length))
                    changed |= this.EnsureCharacter(cc, entry, font);
            }
        }

        foreach (var c in coll)
        {
            foreach (var cc in Enumerable.Range(c.FirstCodePoint, c.Length))
                this.LoadAttemptedGlyphs[cc] = true;
        }

        if (changed)
            this.UpdateReferencesToVectorItems();
    }

    private bool EnsureCharacter(int c, in FontChainEntry entry, in ImFontWrapper font)
    {
        if (this.LoadAttemptedGlyphs[c])
            return false;

        if (!entry.RangeContainsCharacter(c))
            return false;

        var sourceGlyph = font.FindLoadedGlyphNoFallback(c);
        if (sourceGlyph is null)
            return false;

        var offsetVector2 = new Vector2(
            MathF.Round(entry.OffsetX),
            MathF.Round(entry.OffsetY + ((entry.SizePx * (this.Chain.LineHeight - 1f)) / 2)));

        var glyph = new ImGuiHelpers.ImFontGlyphReal
        {
            AdvanceX = sourceGlyph->AdvanceX + entry.LetterSpacing,
            Codepoint = c,
            Colored = sourceGlyph->Colored,
            TextureIndex = sourceGlyph->TextureIndex,
            Visible = sourceGlyph->Visible,
            UV = sourceGlyph->UV,
            XY0 = sourceGlyph->XY0 + offsetVector2,
            XY1 = sourceGlyph->XY1 + offsetVector2,
        };

        this.IndexLookup[c] = unchecked((ushort)this.Glyphs.Length);
        this.Glyphs.Add(glyph);
        this.Mark4KPageUsed(glyph);
        this.LoadAttemptedGlyphs[c] = true;

        ref var indexedHotData = ref this.IndexedHotData[glyph.Codepoint];
        indexedHotData.AdvanceX = glyph.AdvanceX;
        indexedHotData.OccupiedWidth = Math.Max(glyph.AdvanceX, glyph.X1);
        return true;
    }
}
