using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

using Dalamud.Interface.Internal;
using Dalamud.Interface.SpannedStrings.Internal;

using ImGuiNET;

namespace Dalamud.Interface.SpannedStrings.Rendering.Internal;

/// <summary>A custom text renderer implementation.</summary>
internal sealed unsafe partial class SpannableRenderer
{
    /// <summary>The path of texture files associated with gfdata.gfd.</summary>
    public static readonly string[] GfdTexturePaths =
    {
        "common/font/fonticon_xinput.tex",
        "common/font/fonticon_ps3.tex",
        "common/font/fonticon_ps4.tex",
        "common/font/fonticon_ps5.tex",
        "common/font/fonticon_lys.tex",
    };
        
    private static readonly BitArray WordBreakNormalBreakChars;
    private static readonly delegate* unmanaged<uint, nint, void> ImGuiSetActiveId;
    private static readonly delegate* unmanaged<uint, void> ImGuiSetHoveredId;

    private readonly byte[] gfdFile;
    private readonly IDalamudTextureWrap[] gfdTextures;

    /// <summary>Initializes static members of the <see cref="SpannableRenderer"/> class.</summary>
    static SpannableRenderer()
    {
        _ = ImGui.GetCurrentContext();

        var cimgui = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
                            .First(x => x.ModuleName == "cimgui.dll")
                            .BaseAddress;
        ImGuiSetActiveId = (delegate* unmanaged<uint, IntPtr, void>)(cimgui + CImGuiSetActiveIdOffset);
        ImGuiSetHoveredId = (delegate* unmanaged<uint, void>)(cimgui + CImGuiSetHoverIdOffset);

        // Initialize which characters will make a valid word break point.

        WordBreakNormalBreakChars = new(char.MaxValue + 1);

        // https://en.wikipedia.org/wiki/Whitespace_character
        foreach (var c in
                 "\t\n\v\f\r\x20\u0085\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2008\u2009\u200A\u2028\u2029\u205F\u3000\u180E\u200B\u200C\u200D")
            WordBreakNormalBreakChars[c] = true;

        foreach (var range in new[]
                 {
                     UnicodeRanges.HangulJamo,
                     UnicodeRanges.HangulSyllables,
                     UnicodeRanges.HangulCompatibilityJamo,
                     UnicodeRanges.HangulJamoExtendedA,
                     UnicodeRanges.HangulJamoExtendedB,
                     UnicodeRanges.CjkCompatibility,
                     UnicodeRanges.CjkCompatibilityForms,
                     UnicodeRanges.CjkCompatibilityIdeographs,
                     UnicodeRanges.CjkRadicalsSupplement,
                     UnicodeRanges.CjkSymbolsandPunctuation,
                     UnicodeRanges.CjkStrokes,
                     UnicodeRanges.CjkUnifiedIdeographs,
                     UnicodeRanges.CjkUnifiedIdeographsExtensionA,
                     UnicodeRanges.Hiragana,
                     UnicodeRanges.Katakana,
                     UnicodeRanges.KatakanaPhoneticExtensions,
                 })
        {
            for (var i = 0; i < range.Length; i++)
                WordBreakNormalBreakChars[range.FirstCodePoint + i] = true;
        }
    }

    /// <summary>Gets the textures for graphic font icons.</summary>
    private ReadOnlySpan<IDalamudTextureWrap> GfdTextures => this.gfdTextures;

    /// <summary>Gets the GFD file view.</summary>
    private GfdFileView GfdFileView => new(new(Unsafe.AsPointer(ref this.gfdFile[0]), this.gfdFile.Length));
}
