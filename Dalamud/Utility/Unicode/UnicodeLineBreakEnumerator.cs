using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Lumina.Text;

using TerraFX.Interop.DirectX;

using static Dalamud.Utility.Unicode.UnicodeEastAsianWidthClass;
using static Dalamud.Utility.Unicode.UnicodeGeneralCategory;
using static Dalamud.Utility.Unicode.UnicodeLineBreakClass;

using IDWriteFactoryPtr = TerraFX.Interop.Windows.ComPtr<TerraFX.Interop.DirectX.IDWriteFactory>;
using IDWriteTextFormatPtr = TerraFX.Interop.Windows.ComPtr<TerraFX.Interop.DirectX.IDWriteTextFormat>;
using IDWriteTextLayoutPtr = TerraFX.Interop.Windows.ComPtr<TerraFX.Interop.DirectX.IDWriteTextLayout>;
using IID = TerraFX.Interop.Windows.IID;
using IUnknown = TerraFX.Interop.Windows.IUnknown;

namespace Dalamud.Utility.Unicode;

/// <summary>Enumerates line break offsets according to
/// <a href="https://www.unicode.org/reports/tr14/">UAX #14 (r57, Unicode 18.0)</a>.</summary>
/// <remarks>SeString payloads without a representative character are ignored. Otherwise, they're treated as U+FFFC
/// (object replacement character).</remarks>
public ref struct UnicodeLineBreakEnumerator
{
    private readonly UtfEnumeratorFlags enumeratorFlags;
    private readonly bool findWords;
    private readonly int dataLength;

    private UtfEnumerator enumerator;

    // For breaking South Asian text between words.
    private int complexContextEnd;
    private int[]? wordStarts;
    private int wordStartCount;
    private int nextWordStart;

    // The last two characters, as resolved by LB9 and LB10 (combining marks absorbed into their base).
    private Entry class1;
    private Entry class2;

    // The last two characters before the spaces, while class1 is SP.
    private Entry space1;
    private Entry space2;

    // Whether the last character, before resolving, was a ZWJ (LB8a).
    private bool afterZwj;

    private int consecutiveRegionalIndicators;
    private NumberState number;

    /// <summary>Initializes a new instance of the <see cref="UnicodeLineBreakEnumerator"/> struct.</summary>
    /// <param name="data">UTF-N byte sequence.</param>
    /// <param name="enumeratorFlags">Flags to pass to sub-enumerator.</param>
    /// <param name="findWords">Detect South Asian languages' words to break them in a better way.</param>
    public UnicodeLineBreakEnumerator(
        ReadOnlySpan<byte> data,
        UtfEnumeratorFlags enumeratorFlags = default,
        bool findWords = true)
    {
        this.enumerator = UtfEnumerator.From(data, enumeratorFlags);
        this.enumeratorFlags = enumeratorFlags;
        this.dataLength = data.Length;
        this.findWords = findWords;
    }

    private UnicodeLineBreakEnumerator(
        int dataLength,
        UtfEnumerator enumerator,
        UtfEnumeratorFlags enumeratorFlags,
        bool findWords)
    {
        this.dataLength = dataLength;
        this.enumerator = enumerator;
        this.enumeratorFlags = enumeratorFlags;
        this.findWords = findWords;
    }

    private enum LineBreakMode : byte
    {
        Prohibited,
        Mandatory,
        Optional,
    }

    /// <summary>Where the characters before are in a number, for LB25.</summary>
    private enum NumberState : byte
    {
        /// <summary>Not after <c>NU (SY | IS)*</c>.</summary>
        None,

        /// <summary>After <c>NU (SY | IS)*</c>.</summary>
        Number,

        /// <summary>After <c>NU (SY | IS)* (CL | CP)</c>.</summary>
        Closed,
    }

    /// <inheritdoc cref="IEnumerator{T}.Current"/>
    public UnicodeLineBreakItem Current { get; private set; }

    /// <summary>Gets a value indicating whether the end of the underlying span has been reached.</summary>
    public bool Finished { get; private set; }

    /// <inheritdoc cref="IEnumerator.MoveNext"/>
    public bool MoveNext()
    {
        if (this.Finished)
            return false;

        while (this.enumerator.MoveNext())
        {
            var effectiveInt = this.enumerator.Current.EffectiveInt;
            if (effectiveInt == char.MaxValue)
                continue;

            switch (this.HandleCharacter(effectiveInt))
            {
                case LineBreakMode.Mandatory:
                    this.Current = new(this.enumerator.Current.ByteOffset, true);
                    return true;
                case LineBreakMode.Optional:
                    this.Current = new(this.enumerator.Current.ByteOffset, false);
                    return true;
                case LineBreakMode.Prohibited:
                default:
                    continue;
            }
        }

        // Start and end of text:
        // LB3 Always break at the end of text.
        this.Current = new(this.dataLength, true);
        this.Finished = true;
        return true;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public UnicodeLineBreakEnumerator GetEnumerator() =>
        new(this.dataLength, this.enumerator.GetEnumerator(), this.enumeratorFlags, this.findWords);

    private static bool IsAksara(in Entry e) => e.Class is AK or AS || e.IsDottedCircle;

    private LineBreakMode HandleCharacter(int c)
    {
        // https://unicode.org/reports/tr14/#Algorithm

        // LB1 Assign a line breaking class to each code point of the input. Resolve AI, CB, CJ, SA, SG, and XX into
        // other line breaking classes depending on criteria outside the scope of this algorithm.
        // => done inside Entry ctor (CB is left unresolved, as LB20 handles it)
        var curr = new Entry(c, this.findWords && UnicodeData.LineBreak[c] is SA && this.IsWordStart());
        var currIsAfterZwj = this.afterZwj;
        this.afterZwj = curr.Class is ZWJ;

        if (curr.Class is CM or ZWJ)
        {
            // LB9 Do not break a combining character sequence; treat it as if it has the line breaking class of the
            // base character in all of the following rules. Treat ZWJ as if it were CM.
            if (this.class1.Class is not (sot or BK or CR or LF or NL or SP or ZW))
                return LineBreakMode.Prohibited;

            // LB10 Treat any remaining combining mark or ZWJ as AL.
            curr = Entry.LetterA;
        }

        var mode = this.Resolve(curr, currIsAfterZwj);

        if (curr.Class is SP && this.class1.Class is not SP)
            (this.space1, this.space2) = (this.class1, this.class2);
        this.consecutiveRegionalIndicators = curr.Class is RI ? this.consecutiveRegionalIndicators + 1 : 0;
        this.number = curr.Class switch
        {
            NU => NumberState.Number,
            SY or IS when this.number == NumberState.Number => NumberState.Number,
            CL or CP when this.number == NumberState.Number => NumberState.Closed,
            _ => NumberState.None,
        };
        (this.class2, this.class1) = (this.class1, curr);
        return mode;
    }

    /// <summary>Gets whether a line may break before a character, after the characters seen so far.</summary>
    /// <param name="curr">The character, resolved (not a CM or ZWJ).</param>
    /// <param name="currIsAfterZwj">Whether the character before was a ZWJ.</param>
    /// <returns>The break.</returns>
    private readonly LineBreakMode Resolve(in Entry curr, bool currIsAfterZwj)
    {
        var (prev1, prev2) = (this.class1, this.class2);

        // The last two characters before the spaces before this one, if any (for rules with SP*).
        var (prevSpace1, prevSpace2) = prev1.Class is SP ? (this.space1, this.space2) : (prev1, prev2);

        // Start and end of text:
        // LB2 Never break at the start of text.
        if (prev1.Class is sot)
            return LineBreakMode.Prohibited;

        // Mandatory breaks:
        // LB4 Always break after hard line breaks.
        if (prev1.Class is BK)
            return LineBreakMode.Mandatory;

        // LB5 Treat CR followed by LF, as well as CR, LF, and NL as hard line breaks.
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
        if (prevSpace1.Class is ZW)
            return LineBreakMode.Optional;

        // LB8a Do not break after a zero width joiner.
        if (currIsAfterZwj)
            return LineBreakMode.Prohibited;

        // Combining marks:
        // LB9, LB10 => done in HandleCharacter

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
        if (prev1.Class is not (SP or HY or HH) && curr.Class is GL)
            return LineBreakMode.Prohibited;

        // Opening and closing:
        // LB13 Do not break before ‘]’ or ‘!’ or ‘/’, even after spaces.
        if (curr.Class is CL or CP or EX or SY)
            return LineBreakMode.Prohibited;

        // LB14 Do not break after ‘[’, even after spaces.
        if (prevSpace1.Class is OP)
            return LineBreakMode.Prohibited;

        // LB15a Do not break after an unresolved initial punctuation that lies at the start of the line, after a space,
        // after opening punctuation, or after an unresolved quotation mark, even after spaces.
        if (prevSpace1 is { Class: QU, GeneralCategory: Pi } &&
            prevSpace2.Class is sot or BK or CR or LF or NL or OP or QU or GL or SP or ZW)
            return LineBreakMode.Prohibited;

        // LB15b Do not break before an unresolved final punctuation that lies at the end of the line, before a space,
        // before a prohibited break, or before an unresolved quotation mark, even after spaces.
        if (curr is { Class: QU, GeneralCategory: Pf } &&
            this.PeekNext(curr, 1).Class is SP or GL or WJ or CL or QU or CP or EX or IS or SY or BK or CR or LF or NL
                or ZW or eot)
            return LineBreakMode.Prohibited;

        // LB15c Break before a decimal mark that follows a space, for instance, in ‘subtract .5’.
        if (prev1.Class is SP && curr.Class is IS && this.PeekNext(curr, 1).Class is NU)
            return LineBreakMode.Optional;

        // LB15d Otherwise, do not break before ‘;’, ‘,’, or ‘.’, even after spaces.
        if (curr.Class is IS)
            return LineBreakMode.Prohibited;

        // LB16 Do not break between closing punctuation and a nonstarter (lb=NS), even with intervening spaces.
        if (prevSpace1.Class is CL or CP && curr.Class is NS)
            return LineBreakMode.Prohibited;

        // LB17 Do not break within ‘——’, even with intervening spaces.
        if (prevSpace1.Class is B2 && curr.Class is B2)
            return LineBreakMode.Prohibited;

        // Spaces:
        // LB18 Break after spaces.
        if (prev1.Class is SP)
            return LineBreakMode.Optional;

        // Special case rules:
        // LB19 Do not break before non-initial unresolved quotation marks, such as ‘ ” ’ or ‘ " ’, nor after non-final
        // unresolved quotation marks, such as ‘ “ ’ or ‘ " ’.
        if (curr is { Class: QU, GeneralCategory: not Pi } || prev1 is { Class: QU, GeneralCategory: not Pf })
            return LineBreakMode.Prohibited;

        // LB19a Unless surrounded by East Asian characters, do not break either side of any unresolved quotation marks.
        // eot, and sot (as prev2), are not East Asian.
        if (curr.Class is QU && (!prev1.IsEastAsian || !this.PeekNext(curr, 1).IsEastAsian))
            return LineBreakMode.Prohibited;
        if (prev1.Class is QU && (!curr.IsEastAsian || !prev2.IsEastAsian))
            return LineBreakMode.Prohibited;

        // LB20 Break before and after unresolved CB.
        if (prev1.Class is CB || curr.Class is CB)
            return LineBreakMode.Optional;

        // LB20a Do not break after a word-initial hyphen.
        if (prev2.Class is sot or BK or CR or LF or NL or SP or ZW or CB or GL &&
            prev1.Class is HY or HH &&
            curr.Class is AL or HL)
            return LineBreakMode.Prohibited;

        // LB21 Do not break before hyphen-minus, other hyphens, fixed-width spaces, small kana, and other non-starters,
        // or after acute accents.
        if (curr.Class is BA or HH or HY or NS || prev1.Class is BB)
            return LineBreakMode.Prohibited;

        // LB21a Do not break after the hyphen in Hebrew + Hyphen + non-Hebrew.
        if (prev2.Class is HL && prev1.Class is HY or HH && curr.Class is not HL)
            return LineBreakMode.Prohibited;

        // LB21b Do not break between Solidus and Hebrew letters.
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
        if (prev1.Class is ID or EB or EM && curr.Class is PO)
            return LineBreakMode.Prohibited;

        // LB24 Do not break between numeric prefix/postfix and letters, or between letters and prefix/postfix.
        if (prev1.Class is PR or PO && curr.Class is AL or HL)
            return LineBreakMode.Prohibited;
        if (prev1.Class is AL or HL && curr.Class is PR or PO)
            return LineBreakMode.Prohibited;

        // LB25 Do not break numbers:
        if (this.number is NumberState.Number or NumberState.Closed && curr.Class is PO or PR)
            return LineBreakMode.Prohibited;
        if (prev1.Class is PO or PR && curr.Class is OP &&
            this.PeekNext(curr, 1).Class switch
            {
                NU => true,
                IS => this.PeekNext(curr, 2).Class is NU,
                _ => false,
            })
            return LineBreakMode.Prohibited;
        if (prev1.Class is PO or PR or HY or IS && curr.Class is NU)
            return LineBreakMode.Prohibited;
        if (this.number is NumberState.Number && curr.Class is NU)
            return LineBreakMode.Prohibited;

        // Korean syllable blocks:
        // LB26 Do not break a Korean syllable.
        if (prev1.Class is JL && curr.Class is JL or JV or H2 or H3)
            return LineBreakMode.Prohibited;
        if (prev1.Class is JV or H2 && curr.Class is JV or JT)
            return LineBreakMode.Prohibited;
        if (prev1.Class is JT or H3 && curr.Class is JT)
            return LineBreakMode.Prohibited;

        // LB27 Treat a Korean Syllable Block the same as ID.
        if (prev1.Class is JL or JV or JT or H2 or H3 && curr.Class is PO)
            return LineBreakMode.Prohibited;
        if (prev1.Class is PR && curr.Class is JL or JV or JT or H2 or H3)
            return LineBreakMode.Prohibited;

        // Finally, join alphabetic letters into words and break everything else.
        if (curr.IsWordStart)
            return LineBreakMode.Optional;

        // LB28 Do not break between alphabetics (“at”).
        if (prev1.Class is AL or HL && curr.Class is AL or HL)
            return LineBreakMode.Prohibited;

        // LB28a Do not break inside the orthographic syllables of Brahmic scripts.
        // (AK | ◌ | AS), where ◌ is U+25CC DOTTED CIRCLE.
        if (prev1.Class is AP && IsAksara(curr))
            return LineBreakMode.Prohibited;
        if (IsAksara(prev1) && curr.Class is VF or VI)
            return LineBreakMode.Prohibited;
        if (IsAksara(prev2) && prev1.Class is VI && (curr.Class is AK || curr.IsDottedCircle))
            return LineBreakMode.Prohibited;
        if (IsAksara(prev1) && IsAksara(curr) && this.PeekNext(curr, 1).Class is VF)
            return LineBreakMode.Prohibited;

        // LB29 Do not break between numeric punctuation and alphabetics (“e.g.”).
        if (prev1.Class is IS && curr.Class is AL or HL)
            return LineBreakMode.Prohibited;

        // LB30 Do not break between letters, numbers, or ordinary symbols and opening or closing parentheses.
        if (prev1.Class is AL or HL or NU && curr is { Class: OP, IsEastAsian: false })
            return LineBreakMode.Prohibited;
        if (prev1 is { Class: CP, IsEastAsian: false } && curr.Class is AL or HL or NU)
            return LineBreakMode.Prohibited;

        // LB30a Break between two regional indicator symbols if and only if there are an even number of regional
        // indicators preceding the position of the break.
        if (prev1.Class is RI && curr.Class is RI && this.consecutiveRegionalIndicators % 2 == 1)
            return LineBreakMode.Prohibited;

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

    private readonly Entry PeekNext(Entry curr, int n)
    {
        var e = this.enumerator;
        var prev = curr;
        while (e.MoveNext())
        {
            var c = e.Current.EffectiveInt;
            if (c == char.MaxValue)
                continue;

            var next = new Entry(c);
            if (next.Class is CM or ZWJ)
            {
                if (prev.Class is not (BK or CR or LF or NL or SP or ZW))
                    continue;
                next = Entry.LetterA;
            }

            if (--n == 0)
                return next;
            prev = next;
        }

        return new(eot);
    }

    private bool IsWordStart()
    {
        var offset = this.enumerator.Current.ByteOffset;
        if (offset >= this.complexContextEnd)
            this.FindWords();

        while (this.nextWordStart < this.wordStartCount && this.wordStarts![this.nextWordStart] < offset)
            this.nextWordStart++;
        return this.nextWordStart < this.wordStartCount && this.wordStarts![this.nextWordStart] == offset;
    }

    private void FindWords()
    {
        const int maxRunLength = 1024;

        var pooledText = PooledSpan<char>.Rent(maxRunLength);
        var pooledTextOffsets = PooledSpan<int>.Rent(maxRunLength);
        var text = pooledText.Span;
        var textOffsets = pooledTextOffsets.Span;
        var length = 0;
        var e = this.enumerator;
        this.complexContextEnd = e.Current.ByteOffset;
        do
        {
            var c = e.Current.EffectiveInt;
            if (c == char.MaxValue)
                continue;
            if (UnicodeData.LineBreak[c] is not SA || length + 2 > maxRunLength)
                break;

            var written = new Rune(c).EncodeToUtf16(text[length..]);
            textOffsets.Slice(length, written).Fill(e.Current.ByteOffset);
            length += written;
            this.complexContextEnd = e.Current.ByteOffset + e.Current.ByteLength;
        }
        while (e.MoveNext());

        var starts = DirectWriteWordFinder.FindWordStarts(new(text[..length]));
        if (this.wordStarts is null || this.wordStarts.Length < starts.Length)
            this.wordStarts = new int[Math.Max(starts.Length, 16)];
        for (var i = 0; i < starts.Length; i++)
            this.wordStarts[i] = textOffsets[starts[i]];
        this.wordStartCount = starts.Length;
        this.nextWordStart = 0;
        pooledText.Recycle();
        pooledTextOffsets.Recycle();
    }

    private readonly struct Entry
    {
        // Treat any remaining CM or ZWJ as if it had the properties of U+0041 A LATIN CAPITAL LETTER A, that is,
        // Line_Break=AL, General_Category=Lu, East_Asian_Width=Na, Extended_Pictographic=N.
        public static readonly Entry LetterA = new(AL, Lu, Na);

        public readonly UnicodeLineBreakClass Class;
        public readonly UnicodeGeneralCategory GeneralCategory;
        public readonly UnicodeEastAsianWidthClass EastAsianWidth;
        public readonly UnicodeEmojiProperty EmojiProperty;
        public readonly bool IsDottedCircle;
        public readonly bool IsWordStart;

        public Entry(int c, bool isWordStart = false)
        {
            this.IsWordStart = isWordStart;
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
            this.IsDottedCircle = c == 0x25CC;
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

        public bool IsEastAsian => this.EastAsianWidth is F or W or H;
    }

    private static unsafe class DirectWriteWordFinder
    {
        private const int MaxCachedRuns = 1024;

        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<string, int[]> Cache = [];
        private static bool initialized;
        private static IDWriteFactoryPtr factory;
        private static IDWriteTextFormatPtr format;

        /// <summary>Finds where words start in a run of text of class SA.</summary>
        /// <param name="text">The run.</param>
        /// <returns>The UTF-16 indices where words start, other than 0, ascending, or none if DirectWrite is
        /// unavailable.</returns>
        public static int[] FindWordStarts(string text)
        {
            lock (SyncRoot)
            {
                if (Cache.TryGetValue(text, out var starts))
                    return starts;

                starts = Find(text);
                if (Cache.Count >= MaxCachedRuns)
                    Cache.Clear();
                Cache.Add(text, starts);
                return starts;
            }
        }

        private static int[] Find(string text)
        {
            if (text.Length < 2 || !Initialize())
                return [];

            using var layout = default(IDWriteTextLayoutPtr);
            fixed (char* p = text)
            {
                var hr = factory.Get()->CreateTextLayout(
                    p,
                    (uint)text.Length,
                    format.Get(),
                    float.MaxValue,
                    float.MaxValue,
                    layout.GetAddressOf());
                if (hr.FAILED)
                    return [];
            }

            uint count;
            layout.Get()->GetClusterMetrics(null, 0, &count);
            var metrics = PooledSpan<DWRITE_CLUSTER_METRICS>.Rent((int)count);
            try
            {
                fixed (DWRITE_CLUSTER_METRICS* m = metrics.Span)
                {
                    if (layout.Get()->GetClusterMetrics(m, count, &count).FAILED)
                        return [];
                }

                var starts = new List<int>();
                var position = 0;
                foreach (ref readonly var cluster in metrics.Span[..(int)count])
                {
                    position += cluster.length;
                    if (cluster.canWrapLineAfter != 0 && position < text.Length)
                        starts.Add(position);
                }

                return starts.ToArray();
            }
            finally
            {
                metrics.Recycle();
            }
        }

        private static bool Initialize()
        {
            if (initialized)
                return format.Get() != null;
            initialized = true;

            try
            {
                fixed (Guid* piid = &IID.IID_IDWriteFactory)
                {
                    DirectX.DWriteCreateFactory(
                        DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED,
                        piid,
                        (IUnknown**)factory.GetAddressOf()).ThrowOnError();
                }

#pragma warning disable SA1519 // Braces should not be omitted from multi-line child statement
                // just need some value to supply. It does not affect the word finder.
                fixed (char* family = "Segoe UI")
                fixed (char* locale = "en-us")
                {
                    factory.Get()->CreateTextFormat(
                        family,
                        null,
                        DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_NORMAL,
                        DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL,
                        DWRITE_FONT_STRETCH.DWRITE_FONT_STRETCH_NORMAL,
                        12,
                        locale,
                        format.GetAddressOf()).ThrowOnError();
                }
#pragma warning restore SA1519 // Braces should not be omitted from multi-line child statement

                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "DirectWrite is unavailable; SA text is not broken between words");
                factory.Dispose();
                format.Dispose();
                return false;
            }
        }
    }
}
