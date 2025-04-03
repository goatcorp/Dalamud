using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Lumina.Text;

using static Dalamud.Interface.ImGuiSeStringRenderer.Internal.TextProcessing.UnicodeEastAsianWidthClass;
using static Dalamud.Interface.ImGuiSeStringRenderer.Internal.TextProcessing.UnicodeGeneralCategory;
using static Dalamud.Interface.ImGuiSeStringRenderer.Internal.TextProcessing.UnicodeLineBreakClass;

namespace Dalamud.Interface.ImGuiSeStringRenderer.Internal.TextProcessing;

/// <summary>Enumerates line break offsets.</summary>
internal ref struct LineBreakEnumerator
{
    private readonly UtfEnumeratorFlags enumeratorFlags;

    private UtfEnumerator enumerator;
    private int dataLength;
    private int currentByteOffsetDelta;

    private Entry class1;
    private Entry class2;

    private Entry space1;
    private Entry space2;
    private bool spaceStreak;

    private int consecutiveRegionalIndicators;

    /// <summary>Initializes a new instance of the <see cref="LineBreakEnumerator"/> struct.</summary>
    /// <param name="data">UTF-N byte sequence.</param>
    /// <param name="enumeratorFlags">Flags to pass to sub-enumerator.</param>
    public LineBreakEnumerator(
        ReadOnlySpan<byte> data,
        UtfEnumeratorFlags enumeratorFlags = UtfEnumeratorFlags.Default)
    {
        this.enumerator = UtfEnumerator.From(data, enumeratorFlags);
        this.enumeratorFlags = enumeratorFlags;
        this.dataLength = data.Length;
    }

    private LineBreakEnumerator(
        int dataLength,
        UtfEnumerator enumerator,
        UtfEnumeratorFlags enumeratorFlags)
    {
        this.dataLength = dataLength;
        this.enumerator = enumerator;
        this.enumeratorFlags = enumeratorFlags;
    }

    private enum LineBreakMode : byte
    {
        Prohibited,
        Mandatory,
        Optional,
    }

    /// <inheritdoc cref="IEnumerator{T}.Current"/>
    public (int ByteOffset, bool Mandatory) Current { get; private set; }

    /// <summary>Gets a value indicating whether the end of the underlying span has been reached.</summary>
    public bool Finished { get; private set; }

    /// <summary>Resumes enumeration with the given data.</summary>
    /// <param name="data">The data.</param>
    /// <param name="offsetDelta">Offset to add to <see cref="Current"/>.<c>ByteOffset</c>.</param>
    public void ResumeWith(ReadOnlySpan<byte> data, int offsetDelta)
    {
        this.enumerator = UtfEnumerator.From(data, this.enumeratorFlags);
        this.dataLength = data.Length;
        this.currentByteOffsetDelta = offsetDelta;
        this.Finished = false;
    }

    /// <inheritdoc cref="IEnumerator.MoveNext"/>
    [SuppressMessage("ReSharper", "ConvertIfStatementToSwitchStatement", Justification = "No")]
    public bool MoveNext()
    {
        if (this.Finished)
            return false;

        while (this.enumerator.MoveNext())
        {
            var effectiveInt =
                this.enumerator.Current.IsSeStringPayload
                    ? UtfEnumerator.RepresentativeCharFor(this.enumerator.Current.MacroCode)
                    : this.enumerator.Current.EffectiveInt;
            if (effectiveInt == -1)
                continue;

            switch (this.HandleCharacter(effectiveInt))
            {
                case LineBreakMode.Mandatory:
                    this.Current = (this.enumerator.Current.ByteOffset + this.currentByteOffsetDelta, true);
                    return true;
                case LineBreakMode.Optional:
                    this.Current = (this.enumerator.Current.ByteOffset + this.currentByteOffsetDelta, false);
                    return true;
                case LineBreakMode.Prohibited:
                default:
                    continue;
            }
        }

        // Start and end of text:
        // LB3 Always break at the end of text.
        this.Current = (this.dataLength + this.currentByteOffsetDelta, true);
        this.Finished = true;
        return true;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public LineBreakEnumerator GetEnumerator() =>
        new(this.dataLength, this.enumerator.GetEnumerator(), this.enumeratorFlags);

    private LineBreakMode HandleCharacter(int c)
    {
        // https://unicode.org/reports/tr14/#Algorithm

        // 6.1 Non-tailorable Line Breaking Rules
        // Resolve line breaking classes:
        // LB1 Assign a line breaking class to each code point of the input.
        // => done inside Entry ctor
        var curr = new Entry(c);
        var (prev1, prev2) = (this.class1, this.class2);
        (this.class2, this.class1) = (this.class1, curr);

        if (curr.Class == RI)
            this.consecutiveRegionalIndicators++;
        else
            this.consecutiveRegionalIndicators = 0;

        var (prevSpaceStreak, prevSpace1, prevSpace2) = (this.spaceStreak, this.space1, this.space2);
        this.spaceStreak = curr.Class == SP;
        if (this.spaceStreak && !prevSpaceStreak)
            (this.space1, this.space2) = (prev1, prev2);

        if (!prevSpaceStreak)
            (prevSpace1, prevSpace2) = (prev1, prev2);

        // Start and end of text:
        // LB2 Never break at the start of text.
        if (prev1.Class is sot)
            return LineBreakMode.Prohibited;

        // Mandatory breaks:
        // LB4 Always break after hard line breaks.
        if (prev1.Class is BK)
            return LineBreakMode.Mandatory;

        // LB5 Treat CR followed by LF, as well as CR, LF, and NL as hard line breaks.
        if (prev2.Class is CR && prev1.Class is LF)
            return LineBreakMode.Mandatory;
        if (prev1.Class is CR && curr.Class is LF)
            return LineBreakMode.Prohibited;
        if (prev1.Class is CR or LF or NL)
            return LineBreakMode.Mandatory;

        // LB6 Do not break before hard line breaks.
        if (curr.Class is BK or CR or LF or NL)
            return LineBreakMode.Prohibited;

        // Explicit breaks and non-breaks:
        // LB7 Do not break before spaces or zero width space.
        if (curr.Class is SP or ZW)
            return LineBreakMode.Prohibited;

        // LB8 Break before any character following a zero-width space, even if one or more spaces intervene.
        if (prev1.Class is ZW)
            return LineBreakMode.Optional;
        if (prevSpaceStreak && prevSpace1.Class is ZW)
            return LineBreakMode.Optional;

        // LB8a Do not break after a zero width joiner.
        if (prev1.Class is ZWJ)
            return LineBreakMode.Prohibited;

        // Combining marks:
        // LB9 Do not break a combining character sequence; treat it as if it has the line breaking class of the base character in all of the following rules. Treat ZWJ as if it were CM.
        // ?

        // LB10 Treat any remaining combining mark or ZWJ as AL.
        if (curr.Class is CM or ZWJ)
            this.class1 = curr = new('A');

        // Word joiner:
        // LB11 Do not break before or after Word joiner and related characters.
        if (prev1.Class is WJ || curr.Class is WJ)
            return LineBreakMode.Prohibited;

        // Non-breaking characters:
        // LB12 Do not break after NBSP and related characters.
        if (prev1.Class is GL)
            return LineBreakMode.Prohibited;

        // 6.2 Tailorable Line Breaking Rules
        // Non-breaking characters:
        // LB12a Do not break before NBSP and related characters, except after spaces and hyphens.
        if (prev1.Class is not SP and not BA and not HY &&
            curr.Class is GL)
            return LineBreakMode.Prohibited;

        // Opening and closing:
        // LB13 Do not break before ‘]’ or ‘!’ or ‘;’ or ‘/’, even after spaces.
        if (curr.Class is CL or CP or EX or IS or SY)
            return LineBreakMode.Prohibited;

        // LB14 Do not break after ‘[’, even after spaces.
        if (prevSpace1.Class is OP)
            return LineBreakMode.Prohibited;

        // LB15a Do not break after an unresolved initial punctuation that lies at the start of the line, after a space, after opening punctuation, or after an unresolved quotation mark, even after spaces.
        if (prevSpace2.Class is sot or BK or CR or LF or NL or OP or QU or GL or SP or ZW &&
            prevSpace1.Class is QU &&
            prevSpace1.GeneralCategory is Pi)
            return LineBreakMode.Prohibited;

        var next = this.enumerator.TryPeekNext(out var nextSubsequence, out _)
                       ? new Entry(nextSubsequence.EffectiveChar)
                       : new(eot);

        // LB15b Do not break before an unresolved final punctuation that lies at the end of the line, before a space, before a prohibited break, or before an unresolved quotation mark, even after spaces.
        if (curr.Class is QU && curr.GeneralCategory is Pf &&
            next.Class is SP or GL or WJ or CL or QU or CP or EX or IS or SY or BK or CR or LF or NL or ZW or eot)
            return LineBreakMode.Prohibited;

        // LB16 Do not break between closing punctuation and a nonstarter (lb=NS), even with intervening spaces.
        if (prevSpace1.Class is CL or CP && next.Class is NS)
            return LineBreakMode.Prohibited;

        // LB17 Do not break within ‘——’, even with intervening spaces.
        if (prevSpace1.Class is B2 && next.Class is B2)
            return LineBreakMode.Prohibited;

        // Spaces:
        // LB18 Break after spaces.
        if (prev1.Class is SP)
            return LineBreakMode.Optional;

        // Special case rules:
        // LB19 Do not break before or after quotation marks, such as ‘ ” ’.
        if (prev1.Class is QU || curr.Class is QU)
            return LineBreakMode.Prohibited;

        // LB20 Break before and after unresolved CB.
        if (prev1.Class is CB || curr.Class is CB)
            return LineBreakMode.Optional;

        // LB21 Do not break before hyphen-minus, other hyphens, fixed-width spaces, small kana, and other non-starters, or after acute accents.
        if (curr.Class is BA or HY or NS || prev1.Class is BB)
            return LineBreakMode.Prohibited;

        // LB21a Don't break after Hebrew + Hyphen.
        if (prev2.Class is HL && prev1.Class is HY or BA)
            return LineBreakMode.Prohibited;

        // LB21b Don’t break between Solidus and Hebrew letters.
        if (prev1.Class is SY && curr.Class is HL)
            return LineBreakMode.Prohibited;

        // LB22 Do not break before ellipses.
        if (curr.Class is IN)
            return LineBreakMode.Prohibited;

        // Numbers:
        // LB23 Do not break between digits and letters.
        if (prev1.Class is AL or HL && curr.Class is NU)
            return LineBreakMode.Prohibited;
        if (prev1.Class is NU && curr.Class is AL or HL)
            return LineBreakMode.Prohibited;

        // LB23a Do not break between numeric prefixes and ideographs, or between ideographs and numeric postfixes.
        if (prev1.Class is PR && curr.Class is ID or EB or EM)
            return LineBreakMode.Prohibited;
        if (prev1.Class is ID or EB or EM && curr.Class is PR)
            return LineBreakMode.Prohibited;

        // LB24 Do not break between numeric prefix/postfix and letters, or between letters and prefix/postfix.
        if (prev1.Class is PR or PO && curr.Class is AL or HL)
            return LineBreakMode.Prohibited;
        if (prev1.Class is AL or HL && curr.Class is PR or PO)
            return LineBreakMode.Prohibited;

        // LB25 Do not break between the following pairs of classes relevant to numbers:
        if ((prev1.Class, curr.Class) is (CL, PO) or (CP, PO) or (CL, PR) or (CP, PR) or (NU, PO) or (NU, PR)
            or (PO, OP) or (PO, NU) or (PR, OP) or (PR, NU) or (HY, NU) or (IS, NU) or (NU, NU) or (SY, NU))
            return LineBreakMode.Prohibited;

        // Korean syllable blocks:
        // LB26 Do not break a Korean syllable.
        if (prev1.Class is JL && curr.Class is JL or JV or H2 or H3)
            return LineBreakMode.Prohibited;
        if (prev1.Class is JV or H2 && curr.Class is JV or JT)
            return LineBreakMode.Prohibited;

        // LB27 Treat a Korean Syllable Block the same as ID.
        if (prev1.Class is JL or JV or JT or H2 or H3 && curr.Class is PO)
            return LineBreakMode.Prohibited;
        if (prev1.Class is PR && curr.Class is JL or JV or JT or H2 or H3)
            return LineBreakMode.Prohibited;

        // Finally, join alphabetic letters into words and break everything else.
        // LB28 Do not break between alphabetics (“at”).
        if (prev1.Class is AL or HL && curr.Class is AL or HL)
            return LineBreakMode.Prohibited;

        // LB28a Do not break inside the orthographic syllables of Brahmic scripts.
        // TODO: what's "◌"?
        if (prev1.Class is AP && curr.Class is AK or AS)
            return LineBreakMode.Prohibited;
        if (prev1.Class is AK or AS && curr.Class is VF or VI)
            return LineBreakMode.Prohibited;
        if (prev2.Class is AK or AS && prev1.Class is VI && curr.Class is AK)
            return LineBreakMode.Prohibited;
        if (prev1.Class is AK or AS && curr.Class is AK or AS && next.Class is VF)
            return LineBreakMode.Prohibited;

        // LB29 Do not break between numeric punctuation and alphabetics (“e.g.”).
        if (prev1.Class is IS && curr.Class is AL or HL)
            return LineBreakMode.Prohibited;

        // LB30 Do not break between letters, numbers, or ordinary symbols and opening or closing parentheses.
        if (prev1.Class is AL or HL or NU &&
            curr.Class is OP && curr.EastAsianWidth is not F and not W and not H)
            return LineBreakMode.Prohibited;
        if (prev1.Class is CP && prev1.EastAsianWidth is not F and not W and not H &&
            curr.Class is AL or HL or NU)
            return LineBreakMode.Prohibited;

        // LB30a Break between two regional indicator symbols if and only if there are an even number of regional indicators preceding the position of the break.
        if (this.consecutiveRegionalIndicators % 2 == 0)
            return LineBreakMode.Optional;

        // LB30b Do not break between an emoji base (or potential emoji) and an emoji modifier.
        if (prev1.Class is EB && curr.Class is EM)
            return LineBreakMode.Prohibited;
        if (prev1.GeneralCategory is Cn &&
            (prev1.EmojiProperty & UnicodeEmojiProperty.Extended_Pictographic) != 0 &&
            curr.Class is EM)
            return LineBreakMode.Prohibited;

        // LB31 Break everywhere else.
        return LineBreakMode.Optional;
    }

    private readonly struct Entry
    {
        public readonly UnicodeLineBreakClass Class;
        public readonly UnicodeGeneralCategory GeneralCategory;
        public readonly UnicodeEastAsianWidthClass EastAsianWidth;
        public readonly UnicodeEmojiProperty EmojiProperty;

        public Entry(int c)
        {
            this.Class = UnicodeData.LineBreak[c] switch
            {
                AI or SG or XX => AL,
                SA when UnicodeData.GeneralCategory[c] is Mn or Mc => CM,
                SA => AL,
                CJ => NS,
                var x => x,
            };
            this.GeneralCategory = UnicodeData.GeneralCategory[c];
            this.EastAsianWidth = UnicodeData.EastAsianWidth[c];
            this.EmojiProperty = UnicodeData.EmojiProperty[c];
        }

        public Entry(
            UnicodeLineBreakClass lineBreakClass,
            UnicodeGeneralCategory generalCategory = Cn,
            UnicodeEastAsianWidthClass eastAsianWidth = N,
            UnicodeEmojiProperty emojiProperty = 0)
        {
            this.Class = lineBreakClass;
            this.GeneralCategory = generalCategory;
            this.EastAsianWidth = eastAsianWidth;
            this.EmojiProperty = emojiProperty;
        }
    }
}
