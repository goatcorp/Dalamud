#define BORDER_MATCHING

namespace Dalamud.Utility;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

internal readonly ref struct FuzzyMatcher
{
    private static readonly (int, int)[] EmptySegArray = Array.Empty<(int, int)>();

    private readonly string needleString = string.Empty;
    private readonly ReadOnlySpan<char> needleSpan = ReadOnlySpan<char>.Empty;
    private readonly int needleFinalPosition = -1;
    private readonly (int start, int end)[] needleSegments = EmptySegArray;
    private readonly MatchMode mode = MatchMode.Simple;

    public FuzzyMatcher(string term, MatchMode matchMode)
    {
        needleString = term;
        needleSpan = needleString.AsSpan();
        needleFinalPosition = needleSpan.Length - 1;
        mode = matchMode;

        switch (matchMode)
        {
            case MatchMode.FuzzyParts:
                needleSegments = FindNeedleSegments(needleSpan);
                break;
            case MatchMode.Fuzzy:
            case MatchMode.Simple:
                needleSegments = EmptySegArray;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(matchMode), matchMode, null);
        }
    }

    private static (int start, int end)[] FindNeedleSegments(ReadOnlySpan<char> span)
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

    public int Matches(string value)
    {
        if (needleFinalPosition < 0)
        {
            return 0;
        }

        if (mode == MatchMode.Simple)
        {
            return value.Contains(needleString) ? 1 : 0;
        }

        var haystack = value.AsSpan();

        if (mode == MatchMode.Fuzzy)
        {
            return GetRawScore(haystack, 0, needleFinalPosition);
        }

        if (mode == MatchMode.FuzzyParts)
        {
            if (needleSegments.Length < 2)
            {
                return GetRawScore(haystack, 0, needleFinalPosition);
            }

            var total = 0;
            for (var i = 0; i < needleSegments.Length; i++)
            {
                var (start, end) = needleSegments[i];
                var cur = GetRawScore(haystack, start, end);
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
            var cur = Matches(values[i]);
            if (cur > max)
            {
                max = cur;
            }
        }

        return max;
    }

    private int GetRawScore(ReadOnlySpan<char> haystack, int needleStart, int needleEnd)
    {
        var (startPos, gaps, consecutive, borderMatches, endPos) = FindForward(haystack, needleStart, needleEnd);
        if (startPos < 0)
        {
            return 0;
        }

        var needleSize = needleEnd - needleStart + 1;

        var score = CalculateRawScore(needleSize, startPos, gaps, consecutive, borderMatches);
        // PluginLog.Debug(
        //     $"['{needleString.Substring(needleStart, needleEnd - needleStart + 1)}' in '{haystack}'] fwd: needleSize={needleSize} startPos={startPos} gaps={gaps} consecutive={consecutive} borderMatches={borderMatches} score={score}");

        (startPos, gaps, consecutive, borderMatches) = FindReverse(haystack, endPos, needleStart, needleEnd);
        var revScore = CalculateRawScore(needleSize, startPos, gaps, consecutive, borderMatches);
        // PluginLog.Debug(
        //     $"['{needleString.Substring(needleStart, needleEnd - needleStart + 1)}' in '{haystack}'] rev: needleSize={needleSize} startPos={startPos} gaps={gaps} consecutive={consecutive} borderMatches={borderMatches} score={revScore}");

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

    private (int startPos, int gaps, int consecutive, int borderMatches, int haystackIndex) FindForward(
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
            if (haystack[haystackIndex] == needleSpan[needleIndex])
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

    private (int startPos, int gaps, int consecutive, int borderMatches) FindReverse(ReadOnlySpan<char> haystack,
        int haystackLastMatchIndex, int needleStart, int needleEnd)
    {
        var needleIndex = needleEnd;
        var revLastMatchIndex = haystack.Length + 10;

        var gaps = 0;
        var consecutive = 0;
        var borderMatches = 0;

        for (var haystackIndex = haystackLastMatchIndex; haystackIndex >= 0; haystackIndex--)
        {
            if (haystack[haystackIndex] == needleSpan[needleIndex])
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

public enum MatchMode
{
    Simple,
    Fuzzy,
    FuzzyParts
}
