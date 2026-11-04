using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Utility.Unicode;

/// <summary><a href="https://www.unicode.org/reports/tr44/#Indic_Conjunct_Break">Unicode Indic_Conjunct_Break
/// property</a>. This property defines values used in Grapheme Cluster Break algorithm in UAX #29.</summary>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Unicode Data")]
public enum UnicodeIndicConjunctBreak : byte
{
    /// <summary>All code points not explicitly listed for Indic_Conjunct_Break have the value None.</summary>
    None,

    /// <summary>Define the set of characters with InCB=Linker as those characters in the set conjunctLinkingScripts which
    /// have the Indic_Syllabic_Category values of Virama, Invisible_Stacker, or Consonant_With_Stacker.</summary>
    Linker,

    /// <summary>Define the set of characters with InCB=Consonant as those characters in the set conjunctLinkingScripts
    /// which have the Indic_Syllabic_Category value of Consonant, as well as those characters in the
    /// invisibleStackerScripts which have the Indic_Syllabic_Category value of Vowel_Independent, plus two exceptional
    /// Balinese characters which also take conjunct forms.</summary>
    Consonant,

    /// <summary>Define the set of characters with InCB=Extend by taking the set of characters with
    /// Grapheme_Cluster_Break = Extend and subtracting those characters just determined to have either InCB = Linker or
    /// InCB = Consonant values (plus some other minor adjustments for ZWJ and ZWNJ).</summary>
    Extend,
}
