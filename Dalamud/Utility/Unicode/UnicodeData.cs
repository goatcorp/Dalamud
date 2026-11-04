using JetBrains.Annotations;

namespace Dalamud.Utility.Unicode;

/// <summary>Stores Unicode data.</summary>
public static partial class UnicodeData
{
    /// <summary>Gets the <c>Line_Break</c>.</summary>
    [GeneratedUnicodeData(
        "LineBreak.txt",
        UnicodeLineBreakClass.XX,
        "https://www.unicode.org/Public/UCD/latest/ucd/LineBreak.txt")]
    public static partial UnicodePropertyTable<UnicodeLineBreakClass> LineBreak { get; }

    /// <summary>Gets the <c>East_Asian_Width</c>.</summary>
    [GeneratedUnicodeData(
        "EastAsianWidth.txt",
        UnicodeEastAsianWidthClass.N,
        "https://www.unicode.org/Public/UCD/latest/ucd/EastAsianWidth.txt")]
    public static partial UnicodePropertyTable<UnicodeEastAsianWidthClass> EastAsianWidth { get; }

    /// <summary>Gets the <c>General_Category</c>.</summary>
    [GeneratedUnicodeData(
        "DerivedGeneralCategory.txt",
        UnicodeGeneralCategory.Cn,
        "https://www.unicode.org/Public/UCD/latest/ucd/extracted/DerivedGeneralCategory.txt")]
    public static partial UnicodePropertyTable<UnicodeGeneralCategory> GeneralCategory { get; }

    /// <summary>Gets the emoji properties.</summary>
    [GeneratedUnicodeData(
        "emoji-data.txt",
        UnicodeEmojiProperty.None,
        "https://www.unicode.org/Public/UCD/latest/ucd/emoji/emoji-data.txt")]
    public static partial UnicodePropertyTable<UnicodeEmojiProperty> EmojiProperty { get; }

    /// <summary>Gets the <c>Grapheme_Cluster_Break</c>.</summary>
    [GeneratedUnicodeData(
        "GraphemeBreakProperty.txt",
        UnicodeGraphemeClusterBreak.Other,
        "https://www.unicode.org/Public/UCD/latest/ucd/auxiliary/GraphemeBreakProperty.txt")]
    public static partial UnicodePropertyTable<UnicodeGraphemeClusterBreak> GraphemeClusterBreak { get; }

    /// <summary>Gets the <c>Indic_Conjunct_Break</c>.</summary>
    [GeneratedUnicodeData(
        "IndicConjunctBreak.txt",
        UnicodeIndicConjunctBreak.None,
        "https://www.unicode.org/Public/UCD/latest/ucd/DerivedCoreProperties.txt",
        "Indic_Conjunct_Break")]
    public static partial UnicodePropertyTable<UnicodeIndicConjunctBreak> IndicConjunctBreak { get; }

#pragma warning disable CS9113 // Disable the unread parameter warning
    [AttributeUsage(AttributeTargets.Property)]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private sealed class GeneratedUnicodeDataAttribute(
        string fileName,
        object? defaultValue,
        string sourceUrl,
        string? property = null) : Attribute
#pragma warning restore CS9113 // Restore the warning for the rest of the file
    {
        public string FileName { get; } = fileName;

        public object? DefaultValue { get; } = defaultValue;
    }
}
