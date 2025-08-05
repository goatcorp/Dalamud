#define BORDER_MATCHING

using System.Globalization;
using System.Runtime.CompilerServices;

using Dalamud.Configuration.Internal;

namespace Dalamud.Utility;

/// <summary>
/// Matches a string in a fuzzy way.
/// </summary>
internal static class FuzzyMatcher
{
    /// <summary>
    /// Scores how well <paramref name="needle"/> can be found in <paramref name="haystack"/> in a fuzzy way.
    /// </summary>
    /// <param name="haystack">The string to search in.</param>
    /// <param name="needle">The substring to search for.</param>
    /// <param name="mode">Fuzzy match mode.</param>
    /// <param name="cultureInfo">Culture info for case-insensitive matching. Defaults to the culture corresponding to Dalamud language.</param>
    /// <returns>The score. 0 means that the string did not match. The scores are meaningful only across matches using the same <paramref name="needle"/> value.</returns>
    public static int FuzzyScore(
        this ReadOnlySpan<char> haystack,
        ReadOnlySpan<char> needle,
        FuzzyMatcherMode mode = FuzzyMatcherMode.Simple,
        CultureInfo? cultureInfo = null)
    {
        cultureInfo ??=
            Service<DalamudConfiguration>.GetNullable().EffectiveLanguage is { } effectiveLanguage
                ? Localization.GetCultureInfoFromLangCode(effectiveLanguage)
                : CultureInfo.CurrentCulture;

        switch (mode)
        {
            case var _ when needle.Length == 0:
                return 0;

            case FuzzyMatcherMode.Simple:
                return cultureInfo.CompareInfo.IndexOf(haystack, needle, CompareOptions.IgnoreCase) != -1 ? 1 : 0;

            case FuzzyMatcherMode.Fuzzy:
                return GetRawScore(haystack, needle, cultureInfo);

            case FuzzyMatcherMode.FuzzyParts:
                var score = 0;
                foreach (var needleSegment in new WordEnumerator(needle))
                {
                    var cur = GetRawScore(haystack, needleSegment, cultureInfo);
                    if (cur == 0)
                        return 0;

                    score += cur;
                }

                return score;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
    }

    /// <inheritdoc cref="FuzzyScore(ReadOnlySpan{char},ReadOnlySpan{char},FuzzyMatcherMode,CultureInfo?)"/>
    public static int FuzzyScore(
        this string? haystack,
        ReadOnlySpan<char> needle,
        FuzzyMatcherMode mode = FuzzyMatcherMode.Simple,
        CultureInfo? cultureInfo = null) => haystack.AsSpan().FuzzyScore(needle, mode, cultureInfo);

    /// <summary>
    /// Determines if <paramref name="needle"/> can be found in <paramref name="haystack"/> in a fuzzy way.
    /// </summary>
    /// <param name="haystack">The string to search from.</param>
    /// <param name="needle">The substring to search for.</param>
    /// <param name="mode">Fuzzy match mode.</param>
    /// <param name="cultureInfo">Culture info for case-insensitive matching. Defaults to the culture corresponding to Dalamud language.</param>
    /// <returns><c>true</c> if matches.</returns>
    public static bool FuzzyMatches(
        this ReadOnlySpan<char> haystack,
        ReadOnlySpan<char> needle,
        FuzzyMatcherMode mode = FuzzyMatcherMode.Simple,
        CultureInfo? cultureInfo = null) => haystack.FuzzyScore(needle, mode, cultureInfo) > 0;

    /// <summary>
    /// Determines if <paramref name="needle"/> can be found in <paramref name="haystack"/> in a fuzzy way.
    /// </summary>
    /// <param name="haystack">The string to search from.</param>
    /// <param name="needle">The substring to search for.</param>
    /// <param name="mode">Fuzzy match mode.</param>
    /// <param name="cultureInfo">Culture info for case-insensitive matching. Defaults to the culture corresponding to Dalamud language.</param>
    /// <returns><c>true</c> if matches.</returns>
    public static bool FuzzyMatches(
        this string? haystack,
        ReadOnlySpan<char> needle,
        FuzzyMatcherMode mode = FuzzyMatcherMode.Simple,
        CultureInfo? cultureInfo = null) => haystack.FuzzyScore(needle, mode, cultureInfo) > 0;

    private static int GetRawScore(ReadOnlySpan<char> haystack, ReadOnlySpan<char> needle, CultureInfo cultureInfo)
    {
        var (startPos, gaps, consecutive, borderMatches, endPos) = FindForward(haystack, needle, cultureInfo);
        if (startPos < 0)
            return 0;

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
        var score = 100;
        score += needleSize * 3;
        score += borderMatches * 3;
        score += consecutive * 5;
        score -= startPos;
        score -= gaps * 2;
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
                        borderMatches++;
                }
#endif

                needleIndex++;

                if (haystackIndex == lastMatchIndex + 1)
                    consecutive++;

                if (needleIndex >= needle.Length)
                    return (startPos, gaps, consecutive, borderMatches, haystackIndex);

                lastMatchIndex = haystackIndex;
            }
            else
            {
                if (needleIndex > 0)
                    gaps++;
                else
                    startPos++;
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
                        borderMatches++;
                }
#endif

                needleIndex--;

                if (haystackIndex == revLastMatchIndex - 1)
                    consecutive++;

                if (needleIndex < 0)
                    return (haystackIndex, gaps, consecutive, borderMatches);

                revLastMatchIndex = haystackIndex;
            }
            else
            {
                gaps++;
            }
        }

        return (-1, 0, 0, 0);
    }

    private ref struct WordEnumerator(ReadOnlySpan<char> fullNeedle)
    {
        private readonly ReadOnlySpan<char> fullNeedle = fullNeedle;
        private int start = -1;
        private int end = 0;

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
