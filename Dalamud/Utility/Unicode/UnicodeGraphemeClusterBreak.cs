using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Utility.Unicode;

/// <summary><a href="https://www.unicode.org/reports/tr29/#Grapheme_Cluster_Break_Property_Values">Unicode grapheme
/// cluster break property</a>.</summary>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Unicode Data")]
public enum UnicodeGraphemeClusterBreak : byte
{
    /// <summary>All code points not explicitly listed for Grapheme_Cluster_Break have the value Other (XX).</summary>
    Other,

    /// <summary>U+000D CARRIAGE RETURN (CR).</summary>
    CR,

    /// <summary>U+000A LINE FEED (LF).</summary>
    LF,

    /// <summary>General_Category = Line_Separator, or General_Category = Paragraph_Separator, or General_Category =
    /// Control, or General_Category = Unassigned and Default_Ignorable_Code_Point, or General_Category = Format and not
    /// U+000D CARRIAGE RETURN and not U+000A LINE FEED and not U+200C ZERO WIDTH NON-JOINER (ZWNJ) and not U+200D ZERO
    /// WIDTH JOINER (ZWJ) and not Prepended_Concatenation_Mark = Yes.</summary>
    Control,

    /// <summary>Grapheme_Extend = Yes, or Emoji_Modifier=Yes. This includes: General_Category = Nonspacing_Mark,
    /// General_Category = Enclosing_Mark, U+200C ZERO WIDTH NON-JOINER, plus a few General_Category = Spacing_Mark needed
    /// for canonical equivalence.</summary>
    Extend,

    /// <summary>U+200D ZERO WIDTH JOINER.</summary>
    ZWJ,

    /// <summary>Regional_Indicator = Yes. This consists of the range: U+1F1E6 REGIONAL INDICATOR SYMBOL LETTER A
    /// ..U+1F1FF REGIONAL INDICATOR SYMBOL LETTER Z.</summary>
    Regional_Indicator,

    /// <summary>Indic_Syllabic_Category = Consonant_Preceding_Repha, or Indic_Syllabic_Category = Consonant_Prefixed, or
    /// Prepended_Concatenation_Mark = Yes.</summary>
    Prepend,

    /// <summary>Grapheme_Cluster_Break ≠ Extend, and General_Category = Spacing_Mark, or any of the following (which
    /// have General_Category = Other_Letter): U+0E33 THAI CHARACTER SARA AM, U+0EB3 LAO VOWEL SIGN AM.</summary>
    /// <remarks>Exceptions: The following (which have General_Category = Spacing_Mark and would otherwise be included)
    /// are specifically excluded: U+102B MYANMAR VOWEL SIGN TALL AA, U+102C MYANMAR VOWEL SIGN AA, U+1038 MYANMAR SIGN
    /// VISARGA, U+1062 MYANMAR VOWEL SIGN SGAW KAREN EU ..U+1064 MYANMAR TONE MARK SGAW KAREN KE PHO, U+1067 MYANMAR
    /// VOWEL SIGN WESTERN PWO KAREN EU ..U+106D MYANMAR SIGN WESTERN PWO KAREN TONE-5, U+1083 MYANMAR VOWEL SIGN SHAN
    /// AA, U+1087 MYANMAR SIGN SHAN TONE-2 ..U+108C MYANMAR SIGN SHAN COUNCIL TONE-3, U+108F MYANMAR SIGN RUMAI PALAUNG
    /// TONE-5, U+109A MYANMAR SIGN KHAMTI TONE-1 ..U+109C MYANMAR VOWEL SIGN AITON A, U+1A61 TAI THAM VOWEL SIGN A,
    /// U+1A63 TAI THAM VOWEL SIGN AA, U+1A64 TAI THAM VOWEL SIGN TALL AA, U+AA7B MYANMAR SIGN PAO KAREN TONE, U+AA7D
    /// MYANMAR SIGN TAI LAING TONE-5, U+11720 AHOM VOWEL SIGN A, U+11721 AHOM VOWEL SIGN AA.</remarks>
    SpacingMark,

    /// <summary>Hangul_Syllable_Type=L, such as: U+1100 HANGUL CHOSEONG KIYEOK, U+115F HANGUL CHOSEONG FILLER, U+A960
    /// HANGUL CHOSEONG TIKEUT-MIEUM, U+A97C HANGUL CHOSEONG SSANGYEORINHIEUH.</summary>
    L,

    /// <summary>Hangul_Syllable_Type=V, such as: U+1160 HANGUL JUNGSEONG FILLER, U+11A2 HANGUL JUNGSEONG SSANGARAEA,
    /// U+D7B0 HANGUL JUNGSEONG O-YEO, U+D7C6 HANGUL JUNGSEONG ARAEA-E, and: U+16D63 KIRAT RAI VOWEL SIGN AA, U+16D67
    /// KIRAT RAI VOWEL SIGN E ..U+16D6A KIRAT RAI VOWEL SIGN AU.</summary>
    V,

    /// <summary>Hangul_Syllable_Type=T, such as: U+11A8 HANGUL JONGSEONG KIYEOK, U+11F9 HANGUL JONGSEONG YEORINHIEUH,
    /// U+D7CB HANGUL JONGSEONG NIEUN-RIEUL, U+D7FB HANGUL JONGSEONG PHIEUPH-THIEUTH.</summary>
    T,

    /// <summary>Hangul_Syllable_Type=LV, that is: U+AC00 HANGUL SYLLABLE GA, U+AC1C HANGUL SYLLABLE GAE, U+AC38 HANGUL
    /// SYLLABLE GYA, ...</summary>
    LV,

    /// <summary>Hangul_Syllable_Type=LVT, that is: U+AC01 HANGUL SYLLABLE GAG, U+AC02 HANGUL SYLLABLE GAGG, U+AC03
    /// HANGUL SYLLABLE GAGS, U+AC04 HANGUL SYLLABLE GAN, ...</summary>
    LVT,
}
