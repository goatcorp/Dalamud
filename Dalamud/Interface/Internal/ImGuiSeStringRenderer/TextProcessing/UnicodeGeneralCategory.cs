using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Interface.Internal.ImGuiSeStringRenderer.TextProcessing;

/// <summary><a href="https://www.unicode.org/reports/tr44/#General_Category_Values">Unicode general category.</a>.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Unicode Data")]
internal enum UnicodeGeneralCategory : byte
{
    /// <summary>Uppercase_Letter; an uppercase letter.</summary>
    Lu,

    /// <summary>Lowercase_Letter; a lowercase letter.</summary>
    Ll,

    /// <summary>Titlecase_Letter; a digraph encoded as a single character, with first part uppercase.</summary>
    Lt,

    /// <summary>Modifier_Letter; a modifier letter.</summary>
    Lm,

    /// <summary>Other_Letter; other letters, including syllables and ideographs.</summary>
    Lo,

    /// <summary>Nonspacing_Mark; a nonspacing combining mark (zero advance width).</summary>
    Mn,

    /// <summary>Spacing_Mark; a spacing combining mark (positive advance width).</summary>
    Mc,

    /// <summary>Enclosing_Mark; an enclosing combining mark.</summary>
    Me,

    /// <summary>Decimal_Number; a decimal digit.</summary>
    Nd,

    /// <summary>Letter_Number; a letterlike numeric character.</summary>
    Nl,

    /// <summary>Other_Number; a numeric character of other type.</summary>
    No,

    /// <summary>Connector_Punctuation; a connecting punctuation mark, like a tie.</summary>
    Pc,

    /// <summary>Dash_Punctuation; a dash or hyphen punctuation mark.</summary>
    Pd,

    /// <summary>Open_Punctuation; an opening punctuation mark (of a pair).</summary>
    Ps,

    /// <summary>Close_Punctuation; a closing punctuation mark (of a pair).</summary>
    Pe,

    /// <summary>Initial_Punctuation; an initial quotation mark.</summary>
    Pi,

    /// <summary>Final_Punctuation; a final quotation mark.</summary>
    Pf,

    /// <summary>Other_Punctuation; a punctuation mark of other type.</summary>
    Po,

    /// <summary>Math_Symbol; a symbol of mathematical use.</summary>
    Sm,

    /// <summary>Currency_Symbol; a currency sign.</summary>
    Sc,

    /// <summary>Modifier_Symbol; a non-letterlike modifier symbol.</summary>
    Sk,

    /// <summary>Other_Symbol; a symbol of other type.</summary>
    So,

    /// <summary>Space_Separator; a space character (of various non-zero widths).</summary>
    Zs,

    /// <summary>Line_Separator; U+2028 LINE SEPARATOR only.</summary>
    Zl,

    /// <summary>Paragraph_Separator; U+2029 PARAGRAPH SEPARATOR only.</summary>
    Zp,

    /// <summary>Control; a C0 or C1 control code.</summary>
    Cc,

    /// <summary>Format; a format control character.</summary>
    Cf,

    /// <summary>Surrogate; a surrogate code point.</summary>
    Cs,

    /// <summary>Private_Use; a private-use character.</summary>
    Co,

    /// <summary>Unassigned; a reserved unassigned code point or a noncharacter.</summary>
    Cn,
}
