#define BORDER_MATCHING

using System.Globalization;
using System.Runtime.CompilerServices;

namespace Dalamud.Utility;
#pragma warning disable SA1600
#pragma warning disable SA1602

/// <summary>
/// Specify fuzzy match mode.
/// </summary>
internal enum MatchMode
{
    Simple,

    /// <summary>
    /// The string is considered for fuzzy matching as a whole.
    /// </summary>
    Fuzzy,

    /// <summary>
    /// Each part of the string, separated by whitespace, is considered for fuzzy matching; each part must match in a fuzzy way.
    /// </summary>
    FuzzyParts,
}

/// <summary>
/// Matches a string in a fuzzy way.
/// </summary>
internal static class FuzzyMatcher
{
    /// <summary>
    /// Specify fuzzy match mode.
    /// </summary>
    internal enum Mode
    {
        /// <summary>
        /// The string is considered for fuzzy matching as a whole.
        /// </summary>
        Fuzzy,

        /// <summary>
        /// Each part of the string, separated by whitespace, is considered for fuzzy matching; each part must match in a fuzzy way.
        /// </summary>
        FuzzyParts,
    }

    /// <summary>
    /// Determines if <paramref name="needle"/> can be found in <paramref name="haystack"/> in a fuzzy way.
    /// </summary>
    /// <param name="haystack">The string to search from.</param>
    /// <param name="needle">The substring to search for.</param>
    /// <param name="mode">Fuzzy match mode.</param>
    /// <param name="cultureInfo">Culture info for case insensitive matching.</param>
    /// <param name="score">The score. 0 means that the string did not match. The scores are meaningful only across matches using the same <paramref name="needle"/> value.</param>
    /// <returns><c>true</c> if matches.</returns>
    public static bool FuzzyMatches(
        this ReadOnlySpan<char> haystack,
        ReadOnlySpan<char> needle,
        Mode mode,
        CultureInfo cultureInfo,
        out int score)
    {
        score = 0;
        switch (mode)
        {
            case var _ when needle.Length == 0:
                score = 0;
                break;

            case Mode.Fuzzy:
                score = GetRawScore(haystack, needle, cultureInfo);
                break;

            case Mode.FuzzyParts:
                foreach (var needleSegment in new WordEnumerator(needle))
                {
                    var cur = GetRawScore(haystack, needleSegment, cultureInfo);
                    if (cur == 0)
                    {
                        score = 0;
                        break;
                    }

                    score += cur;
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }

        return score > 0;
    }

    /// <inheritdoc cref="FuzzyMatches(ReadOnlySpan{char},ReadOnlySpan{char},Mode,CultureInfo,out int)"/>
    public static bool FuzzyMatches(
        this string? haystack,
        ReadOnlySpan<char> needle,
        Mode mode,
        CultureInfo cultureInfo,
        out int score) => haystack.AsSpan().FuzzyMatches(needle, mode, cultureInfo, out score);

    /// <summary>
    /// Determines if <paramref name="needle"/> can be found in <paramref name="haystack"/> using the mode
    /// <see cref="Mode.FuzzyParts"/>.
    /// </summary>
    /// <param name="haystack">The string to search from.</param>
    /// <param name="needle">The substring to search for.</param>
    /// <returns><c>true</c> if matches.</returns>
    public static bool FuzzyMatchesParts(this string? haystack, ReadOnlySpan<char> needle) =>
        haystack.FuzzyMatches(needle, Mode.FuzzyParts, CultureInfo.InvariantCulture, out _);

    private static int GetRawScore(ReadOnlySpan<char> haystack, ReadOnlySpan<char> needle, CultureInfo cultureInfo)
    {
        var (startPos, gaps, consecutive, borderMatches, endPos) = FindForward(haystack, needle, cultureInfo);
        if (startPos < 0)
        {
            return 0;
        }

        var score = CalculateRawScore(needle.Length, startPos, gaps, consecutive, borderMatches);
        // PluginLog.Debug(
        //     $"['{needle.Substring(needleStart, needleEnd - needleStart + 1)}' in '{haystack}'] fwd: needleSize={needleSize} startPos={startPos} gaps={gaps} consecutive={consecutive} borderMatches={borderMatches} score={score}");

        (startPos, gaps, consecutive, borderMatches) = FindReverse(haystack[..(endPos + 1)], needle, cultureInfo);
        var revScore = CalculateRawScore(needle.Length, startPos, gaps, consecutive, borderMatches);
        // PluginLog.Debug(
        //     $"['{needle.Substring(needleStart, needleEnd - needleStart + 1)}' in '{haystack}'] rev: needleSize={needleSize} startPos={startPos} gaps={gaps} consecutive={consecutive} borderMatches={borderMatches} score={revScore}");

        return int.Max(score, revScore);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CalculateRawScore(int needleSize, int startPos, int gaps, int consecutive, int borderMatches)
    {
        var score = 100
                    + needleSize * 3
                    + borderMatches * 3
                    + consecutive * 5
                    - startPos
                    - gaps * 2;
        if (startPos == 0)
            score += 5;
        return score < 1 ? 1 : score;
    }

    private static (int StartPos, int Gaps, int Consecutive, int BorderMatches, int HaystackIndex) FindForward(
        ReadOnlySpan<char> haystack,
        ReadOnlySpan<char> needle,
        CultureInfo cultureInfo)
    {
        var needleIndex = 0;
        var lastMatchIndex = -10;

        var startPos = 0;
        var gaps = 0;
        var consecutive = 0;
        var borderMatches = 0;

        for (var haystackIndex = 0; haystackIndex < haystack.Length; haystackIndex++)
        {
            if (char.ToLower(haystack[haystackIndex], cultureInfo) == char.ToLower(needle[needleIndex], cultureInfo))
            {
#if BORDER_MATCHING
                if (haystackIndex > 0)
                {
                    if (!char.IsLetterOrDigit(haystack[haystackIndex - 1]))
                    {
                        borderMatches++;
                    }
                }
#endif

                needleIndex++;

                if (haystackIndex == lastMatchIndex + 1)
                {
                    consecutive++;
                }

                if (needleIndex >= needle.Length)
                {
                    return (startPos, gaps, consecutive, borderMatches, haystackIndex);
                }

                lastMatchIndex = haystackIndex;
            }
            else
            {
                if (needleIndex > 0)
                {
                    gaps++;
                }
                else
                {
                    startPos++;
                }
            }
        }

        return (-1, 0, 0, 0, 0);
    }

    private static (int StartPos, int Gaps, int Consecutive, int BorderMatches) FindReverse(
        ReadOnlySpan<char> haystack,
        ReadOnlySpan<char> needle,
        CultureInfo cultureInfo)
    {
        var needleIndex = needle.Length - 1;
        var revLastMatchIndex = haystack.Length + 10;

        var gaps = 0;
        var consecutive = 0;
        var borderMatches = 0;

        for (var haystackIndex = haystack.Length - 1; haystackIndex >= 0; haystackIndex--)
        {
            if (char.ToLower(haystack[haystackIndex], cultureInfo) == char.ToLower(needle[needleIndex], cultureInfo))
            {
#if BORDER_MATCHING
                if (haystackIndex > 0)
                {
                    if (!char.IsLetterOrDigit(haystack[haystackIndex - 1]))
                    {
                        borderMatches++;
                    }
                }
#endif

                needleIndex--;

                if (haystackIndex == revLastMatchIndex - 1)
                {
                    consecutive++;
                }

                if (needleIndex < 0)
                {
                    return (haystackIndex, gaps, consecutive, borderMatches);
                }

                revLastMatchIndex = haystackIndex;
            }
            else
            {
                gaps++;
            }
        }

        return (-1, 0, 0, 0);
    }

    private ref struct WordEnumerator
    {
        private readonly ReadOnlySpan<char> fullNeedle;
        private int start = -1;
        private int end = 0;

        public WordEnumerator(ReadOnlySpan<char> fullNeedle)
        {
            this.fullNeedle = fullNeedle;
        }

        public ReadOnlySpan<char> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.fullNeedle[this.start..this.end];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (this.start >= this.fullNeedle.Length - 1)
                return false;

            this.start = this.end;

            // Skip the spaces
            while (this.start < this.fullNeedle.Length && char.IsWhiteSpace(this.fullNeedle[this.start]))
                this.start++;

            this.end = this.start;
            while (this.end < this.fullNeedle.Length && !char.IsWhiteSpace(this.fullNeedle[this.end]))
                this.end++;

            return this.start != this.end;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WordEnumerator GetEnumerator() => this;
    }
}
