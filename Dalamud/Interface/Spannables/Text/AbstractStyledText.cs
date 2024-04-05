using System.Collections;
using System.Collections.Generic;
using System.Text.Unicode;

using Dalamud.Interface.Spannables.Text.Internal;

namespace Dalamud.Interface.Spannables.Text;

/// <summary>Base class for <see cref="StyledText"/> and <see cref="StyledTextBuilder"/>.</summary>
public abstract class AbstractStyledText : ISpannableTemplate
{
    /// <summary>The display character in place of a soft hyphen character.</summary>
    public const char SoftHyphenReplacementChar = '-';

    /// <summary>Characters that should be considered as word break points.</summary>
    internal static readonly BitArray WordBreakNormalBreakChars;

    static AbstractStyledText()
    {
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

    /// <summary>Describes states of links.</summary>
    public enum LinkState
    {
        /// <summary>The link is in normal state.</summary>
        Clear,

        /// <summary>The link is hovered.</summary>
        Hovered,

        /// <summary>The link is active.</summary>
        Active,

        /// <summary>The link has been clicked.</summary>
        ActiveNotHovered,
    }

    /// <inheritdoc cref="ISpannableTemplate.CreateSpannable"/>
    public StyledTextSpannable CreateSpannable() => new(this);

    /// <inheritdoc/>
    Spannable ISpannableTemplate.CreateSpannable() => this.CreateSpannable();

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var s in this.GetChildrenTemplates())
            s?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets the children templates.</summary>
    /// <returns>The children templates.</returns>
    public abstract IReadOnlyList<ISpannableTemplate?> GetChildrenTemplates();

    /// <summary>Gets the data required for rendering.</summary>
    /// <returns>The data.</returns>
    internal abstract TsDataMemory AsMemory();
}
