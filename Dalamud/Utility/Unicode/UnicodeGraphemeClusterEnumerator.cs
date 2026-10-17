using System.Collections;
using System.Collections.Generic;

using Lumina.Text;

using static Dalamud.Utility.Unicode.UnicodeGraphemeClusterBreak;

namespace Dalamud.Utility.Unicode;

/// <summary>Enumerates the boundaries of extended grapheme clusters according to
/// <a href="https://www.unicode.org/reports/tr29/">UAX #29 (r49, Unicode 18.0)</a>.</summary>
public ref struct UnicodeGraphemeClusterEnumerator
{
    private readonly int dataLength;

    private UtfEnumerator enumerator;

    // Whether a character was read. If not, the text is empty (GB1, GB2).
    private bool started;

    // The last character's Grapheme_Cluster_Break.
    private UnicodeGraphemeClusterBreak previous;

    // The number of regional indicators up to the last character (GB12, GB13).
    private int regionalIndicators;

    // Where the last characters are in \p{Extended_Pictographic} Extend* ZWJ (GB11).
    private PictographicState pictographic;

    // Whether the last characters are \p{InCB=Linker} \p{InCB=Extend}* (GB9c).
    private bool afterLinker;

    /// <summary>Initializes a new instance of the <see cref="UnicodeGraphemeClusterEnumerator"/> struct.</summary>
    /// <param name="data">UTF-N byte sequence.</param>
    /// <param name="enumeratorFlags">Flags to pass to sub-enumerator.</param>
    public UnicodeGraphemeClusterEnumerator(ReadOnlySpan<byte> data, UtfEnumeratorFlags enumeratorFlags = UtfEnumeratorFlags.Default)
    {
        this.enumerator = UtfEnumerator.From(data, enumeratorFlags);
        this.dataLength = data.Length;
    }

    private enum PictographicState : byte
    {
        /// <summary>Not after <c>\p{Extended_Pictographic} Extend*</c>.</summary>
        None,

        /// <summary>After <c>\p{Extended_Pictographic} Extend*</c>.</summary>
        Pictographic,

        /// <summary>After <c>\p{Extended_Pictographic} Extend* ZWJ</c>.</summary>
        AfterZwj,
    }

    /// <summary>Gets the byte offset of the current boundary in the original byte span.</summary>
    public int Current { get; private set; }

    /// <summary>Gets a value indicating whether the end of the underlying span has been reached.</summary>
    public bool Finished { get; private set; }

    /// <inheritdoc cref="IEnumerator.MoveNext"/>
    public bool MoveNext()
    {
        if (this.Finished)
            return false;

        while (this.enumerator.MoveNext())
        {
            var c = this.enumerator.Current.EffectiveInt;
            if (c == char.MaxValue)
                continue;

            var boundary = this.started && this.IsBoundaryBefore(c);
            this.Advance(c);
            if (boundary)
            {
                this.Current = this.enumerator.Current.ByteOffset;
                return true;
            }
        }

        // GB1, GB2 Break at the start and end of text, unless the text is empty.
        this.Finished = true;
        if (!this.started)
            return false;
        this.Current = this.dataLength;
        return true;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public readonly UnicodeGraphemeClusterEnumerator GetEnumerator() => this;

    private static bool IsExtendedPictographic(int c) =>
        (UnicodeData.EmojiProperty[c] & UnicodeEmojiProperty.Extended_Pictographic) != 0;

    /// <summary>Gets whether a cluster boundary comes before a character, after those read.</summary>
    private readonly bool IsBoundaryBefore(int c)
    {
        var prev = this.previous;
        var curr = UnicodeData.GraphemeClusterBreak[c];

        // GB3 Do not break between a CR and LF.
        if (prev is CR && curr is LF)
            return false;

        // GB4, GB5 Otherwise, break before and after controls.
        if (prev is Control or CR or LF || curr is Control or CR or LF)
            return true;

        // GB6, GB7, GB8 Do not break Hangul syllable or other conjoining sequences.
        if (prev is L && curr is L or V or LV or LVT)
            return false;
        if (prev is LV or V && curr is V or T)
            return false;
        if (prev is LVT or T && curr is T)
            return false;

        // GB9 Do not break before extending characters or ZWJ.
        if (curr is Extend or ZWJ)
            return false;

        // GB9a, GB9b Do not break before SpacingMarks, or after Prepend characters.
        if (curr is SpacingMark || prev is Prepend)
            return false;

        // GB9c Do not break within certain combinations with Indic_Conjunct_Break (InCB)=Linker.
        if (this.afterLinker && UnicodeData.IndicConjunctBreak[c] is UnicodeIndicConjunctBreak.Consonant)
            return false;

        // GB11 Do not break within emoji modifier sequences or emoji zwj sequences.
        if (this.pictographic is PictographicState.AfterZwj && IsExtendedPictographic(c))
            return false;

        // GB12, GB13 Do not break within emoji flag sequences. That is, do not break between regional indicator (RI)
        // symbols if there is an odd number of RI characters before the break point.
        if (prev is Regional_Indicator && curr is Regional_Indicator && this.regionalIndicators % 2 == 1)
            return false;

        // GB999 Otherwise, break everywhere.
        return true;
    }

    /// <summary>Takes a character as read.</summary>
    private void Advance(int c)
    {
        var curr = UnicodeData.GraphemeClusterBreak[c];
        this.regionalIndicators = curr is Regional_Indicator ? this.regionalIndicators + 1 : 0;
        this.pictographic = this.pictographic switch
        {
            _ when IsExtendedPictographic(c) => PictographicState.Pictographic,
            PictographicState.Pictographic when curr is Extend => PictographicState.Pictographic,
            PictographicState.Pictographic when curr is ZWJ => PictographicState.AfterZwj,
            _ => PictographicState.None,
        };

        this.afterLinker = UnicodeData.IndicConjunctBreak[c] switch
        {
            UnicodeIndicConjunctBreak.Linker => true,
            UnicodeIndicConjunctBreak.Extend => this.afterLinker,
            _ => false,
        };
        this.previous = curr;
        this.started = true;
    }
}
