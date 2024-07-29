using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Interface.ImGuiSeStringRenderer.Internal.TextProcessing;

/// <summary><a href="https://unicode.org/reports/tr14/#Table1">Unicode line break class</a>.</summary>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Unicode Data")]
[SuppressMessage(
    "StyleCop.CSharp.DocumentationRules",
    "SA1300:Element should begin with an uppercase letter",
    Justification = "Unicode Data")]
internal enum UnicodeLineBreakClass : byte
{
    /// <summary>Start of text.</summary>
    sot,

    /// <summary>End of text.</summary>
    eot,

    /// <summary>Mandatory Break; NL, PARAGRAPH SEPARATOR; Cause a line break (after).</summary>
    BK,

    /// <summary>Carriage Return; CR; Cause a line break (after), except between CR and LF.</summary>
    CR,

    /// <summary>Line Feed; LF; Cause a line break (after).</summary>
    LF,

    /// <summary>Combining Mark; Combining marks, control codes; Prohibit a line break between the character and the preceding character.</summary>
    CM,

    /// <summary>Next Line; NEL; Cause a line break (after).</summary>
    NL,

    /// <summary>Surrogate; Surrogates; Do not occur in well-formed text.</summary>
    SG,

    /// <summary>Word Joiner; WJ; Prohibit line breaks before and after.</summary>
    WJ,

    /// <summary>Zero Width Space; ZWSP; Provide a break opportunity.</summary>
    ZW,

    /// <summary>Non-breaking (“Glue”); CGJ, NBSP, ZWNBSP; Prohibit line breaks before and after.</summary>
    GL,

    /// <summary>Space; SPACE; Enable indirect line breaks.</summary>
    SP,

    /// <summary>Zero Width Joiner; Zero Width Joiner; Prohibit line breaks within joiner sequences.</summary>
    ZWJ,

    /// <summary>Break Opportunity Before and After; Em dash; Provide a line break opportunity before and after the character.</summary>
    B2,

    /// <summary>Break After; Spaces, hyphens; Generally provide a line break opportunity after the character.</summary>
    BA,

    /// <summary>Break Before; Punctuation used in dictionaries; Generally provide a line break opportunity before the character.</summary>
    BB,

    /// <summary>Hyphen; HYPHEN-MINUS; Provide a line break opportunity after the character, except in numeric context.</summary>
    HY,

    /// <summary>Contingent Break Opportunity; Inline objects; Provide a line break opportunity contingent on additional information.</summary>
    CB,

    /// <summary>Close Punctuation; “}”, “❳”, “⟫” etc.; Prohibit line breaks before.</summary>
    CL,

    /// <summary>Close Parenthesis; “)”, “]”; Prohibit line breaks before.</summary>
    CP,

    /// <summary>Exclamation/Interrogation; “!”, “?”, etc.; Prohibit line breaks before.</summary>
    EX,

    /// <summary>Inseparable; Leaders; Allow only indirect line breaks between pairs.</summary>
    IN,

    /// <summary>Nonstarter; “‼”, “‽”, “⁇”, “⁉”, etc.; Allow only indirect line breaks before.</summary>
    NS,

    /// <summary>Open Punctuation; “(“, “[“, “{“, etc.; Prohibit line breaks after.</summary>
    OP,

    /// <summary>Quotation; Quotation marks; Act like they are both opening and closing.</summary>
    QU,

    /// <summary>Infix Numeric Separator; . ,; Prevent breaks after any and before numeric.</summary>
    IS,

    /// <summary>Numeric; Digits; Form numeric expressions for line breaking purposes.</summary>
    NU,

    /// <summary>Postfix Numeric; %, ¢; Do not break following a numeric expression.</summary>
    PO,

    /// <summary>Prefix Numeric; $, £, ¥, etc.; Do not break in front of a numeric expression.</summary>
    PR,

    /// <summary>Symbols Allowing Break After; /; Prevent a break before, and allow a break after.</summary>
    SY,

    /// <summary>Ambiguous (Alphabetic or Ideographic); Characters with Ambiguous East Asian Width; Act like AL when the resolved EAW is N; otherwise, act as ID.</summary>
    AI,

    /// <summary>Aksara; Consonants; Form orthographic syllables in Brahmic scripts.</summary>
    AK,

    /// <summary>Alphabetic; Alphabets and regular symbols; Are alphabetic characters or symbols that are used with alphabetic characters.</summary>
    AL,

    /// <summary>Aksara Pre-Base; Pre-base repha; Form orthographic syllables in Brahmic scripts.</summary>
    AP,

    /// <summary>Aksara Start; Independent vowels; Form orthographic syllables in Brahmic scripts.</summary>
    AS,

    /// <summary>Conditional Japanese Starter; Small kana; Treat as NS or ID for strict or normal breaking.</summary>
    CJ,

    /// <summary>Emoji Base; All emoji allowing modifiers; Do not break from following Emoji Modifier.</summary>
    EB,

    /// <summary>Emoji Modifier; Skin tone modifiers; Do not break from preceding Emoji Base.</summary>
    EM,

    /// <summary>Hangul LV Syllable; Hangul; Form Korean syllable blocks.</summary>
    H2,

    /// <summary>Hangul LVT Syllable; Hangul; Form Korean syllable blocks.</summary>
    H3,

    /// <summary>Hebrew Letter; Hebrew; Do not break around a following hyphen; otherwise act as Alphabetic.</summary>
    HL,

    /// <summary>Ideographic; Ideographs; Break before or after, except in some numeric context.</summary>
    ID,

    /// <summary>Hangul L Jamo; Conjoining jamo; Form Korean syllable blocks.</summary>
    JL,

    /// <summary>Hangul V Jamo; Conjoining jamo; Form Korean syllable blocks.</summary>
    JV,

    /// <summary>Hangul T Jamo; Conjoining jamo; Form Korean syllable blocks.</summary>
    JT,

    /// <summary>Regional Indicator; REGIONAL INDICATOR SYMBOL LETTER A .. Z; Keep pairs together. For pairs, break before and after other classes.</summary>
    RI,

    /// <summary>Complex Context Dependent (South East Asian); South East Asian: Thai, Lao, Khmer; Provide a line break opportunity contingent on additional, language-specific context analysis.</summary>
    SA,

    /// <summary>Virama Final; Viramas for final consonants; Form orthographic syllables in Brahmic scripts.</summary>
    VF,

    /// <summary>Virama; Conjoining viramas; Form orthographic syllables in Brahmic scripts.</summary>
    VI,

    /// <summary>Unknown; Most unassigned, private-use; Have as yet unknown line breaking behavior or unassigned code positions.</summary>
    XX,
}
