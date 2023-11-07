#nullable enable
#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Dalamud.Interface.EasyFonts;
using Dalamud.Interface.Internal;

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
        this.Font.FallbackGlyph = (ImFontGlyph*)(this.Glyphs.Data + this.Font.FallbackChar);
        this.Font.FallbackHotData = (ImFontGlyphHotData*)(this.IndexedHotData.Data + this.Font.FallbackChar);
    }

    public FontChain Chain { get; set; }

    public IReadOnlyList<ImFontWrapper> Subfonts { get; set; }

    /// <inheritdoc/>
    public override void LoadGlyphs(IEnumerable<char> chars)
    {
        if (chars is ICollection<char> coll)
        {
            this.GrowIndex(coll.Max());
        }
        else
        {
            foreach (var c in chars)
            {
                this.GrowIndex(c);
            }
        }

        throw new NotImplementedException();
    }
}
