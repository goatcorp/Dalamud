#define BORDER_MATCHING

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Dalamud.Utility;

#pragma warning disable SA1600
#pragma warning disable SA1602

internal enum MatchMode
{
    Simple,
    Fuzzy,
    FuzzyParts,
}

internal readonly ref struct FuzzyMatcher
{
    private static readonly (int, int)[] EmptySegArray = Array.Empty<(int, int)>();

    private readonly string needleString = string.Empty;
    private readonly ReadOnlySpan<char> needleSpan = ReadOnlySpan<char>.Empty;
    private readonly int needleFinalPosition = -1;
    private readonly (int Start, int End)[] needleSegments = EmptySegArray;
    private readonly MatchMode mode = MatchMode.Simple;

    public FuzzyMatcher(string term, MatchMode matchMode)
    {
        this.needleString = term;
        this.needleSpan = this.needleString.AsSpan();
        this.needleFinalPosition = this.needleSpan.Length - 1;
        this.mode = matchMode;

        switch (matchMode)
        {
            case MatchMode.FuzzyParts:
                this.needleSegments = FindNeedleSegments(this.needleSpan);
                break;
            case MatchMode.Fuzzy:
            case MatchMode.Simple:
                this.needleSegments = EmptySegArray;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(matchMode), matchMode, null);
        }
    }

    private static (int Start, int End)[] FindNeedleSegments(ReadOnlySpan<char> span)
    {
        var segments = new List<(int, int)>();
        var wordStart = -1;

        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] is not ' ' and not '\u3000')
            {
                if (wordStart < 0)
                {
                    wordStart = i;
                }
            }
            else if (wordStart >= 0)
            {
                segments.Add((wordStart, i - 1));
                wordStart = -1;
            }
        }

        if (wordStart >= 0)
        {
            segments.Add((wordStart, span.Length - 1));
        }

        return segments.ToArray();
    }

#pragma warning disable SA1202
    public int Matches(string value)
#pragma warning restore SA1202
    {
        if (this.needleFinalPosition < 0)
        {
            return 0;
        }

        if (this.mode == MatchMode.Simple)
        {
            return value.Contains(this.needleString) ? 1 : 0;
        }

        var haystack = value.AsSpan();

        if (this.mode == MatchMode.Fuzzy)
        {
            return this.GetRawScore(haystack, 0, this.needleFinalPosition);
        }

        if (this.mode == MatchMode.FuzzyParts)
        {
            if (this.needleSegments.Length < 2)
            {
                return this.GetRawScore(haystack, 0, this.needleFinalPosition);
            }

            var total = 0;
            for (var i = 0; i < this.needleSegments.Length; i++)
            {
                var (start, end) = this.needleSegments[i];
                var cur = this.GetRawScore(haystack, start, end);
                if (cur == 0)
                {
                    return 0;
                }

                total += cur;
            }

            return total;
        }

        return 8;
    }

    public int MatchesAny(params string[] values)
    {
        var max = 0;
        for (var i = 0; i < values.Length; i++)
        {
            var cur = this.Matches(values[i]);
            if (cur > max)
            {
                max = cur;
            }
        }

        return max;
    }

    private int GetRawScore(ReadOnlySpan<char> haystack, int needleStart, int needleEnd)
    {
        var (startPos, gaps, consecutive, borderMatches, endPos) = this.FindForward(haystack, needleStart, needleEnd);
        if (startPos < 0)
        {
            return 0;
        }

        var needleSize = needleEnd - needleStart + 1;

        var score = CalculateRawScore(needleSize, startPos, gaps, consecutive, borderMatches);
        // PluginLog.Debug(
        //     $"['{needleString.Substring(needleStart, needleEnd - needleStart + 1)}' in '{haystack}'] fwd: needleSize={needleSize} startPos={startPos} gaps={gaps} consecutive={consecutive} borderMatches={borderMatches} score={score}");

        (startPos, gaps, consecutive, borderMatches) = this.FindReverse(haystack, endPos, needleStart, needleEnd);
        var revScore = CalculateRawScore(needleSize, startPos, gaps, consecutive, borderMatches);
        // PluginLog.Debug(
        //     $"['{needleString.Substring(needleStart, needleEnd - needleStart + 1)}' in '{haystack}'] rev: needleSize={needleSize} startPos={startPos} gaps={gaps} consecutive={consecutive} borderMatches={borderMatches} score={revScore}");

        return int.Max(score, revScore);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable SA1204
    private static int CalculateRawScore(int needleSize, int startPos, int gaps, int consecutive, int borderMatches)
#pragma warning restore SA1204
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

    private (int StartPos, int Gaps, int Consecutive, int BorderMatches, int HaystackIndex) FindForward(
        ReadOnlySpan<char> haystack, int needleStart, int needleEnd)
    {
        var needleIndex = needleStart;
        var lastMatchIndex = -10;

        var startPos = 0;
        var gaps = 0;
        var consecutive = 0;
        var borderMatches = 0;

        for (var haystackIndex = 0; haystackIndex < haystack.Length; haystackIndex++)
        {
            if (haystack[haystackIndex] == this.needleSpan[needleIndex])
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

                if (needleIndex > needleEnd)
                {
                    return (startPos, gaps, consecutive, borderMatches, haystackIndex);
                }

                lastMatchIndex = haystackIndex;
            }
            else
            {
                if (needleIndex > needleStart)
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

    private (int StartPos, int Gaps, int Consecutive, int BorderMatches) FindReverse(
        ReadOnlySpan<char> haystack, int haystackLastMatchIndex, int needleStart, int needleEnd)
    {
        var needleIndex = needleEnd;
        var revLastMatchIndex = haystack.Length + 10;

        var gaps = 0;
        var consecutive = 0;
        var borderMatches = 0;

        for (var haystackIndex = haystackLastMatchIndex; haystackIndex >= 0; haystackIndex--)
        {
            if (haystack[haystackIndex] == this.needleSpan[needleIndex])
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

                if (needleIndex < needleStart)
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
}

#pragma warning restore SA1600
#pragma warning restore SA1602
