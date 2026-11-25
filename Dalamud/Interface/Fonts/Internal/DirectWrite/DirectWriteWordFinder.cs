using System.Diagnostics.CodeAnalysis;

using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <summary>Finds word-break positions in spaceless scripts using DirectWrite's line-breaking analyzer.</summary>
internal static unsafe class DirectWriteWordFinder
{
    /// <summary>Finds where words start in a run of text, other than at the start of the run.</summary>
    /// <param name="text">The run, in UTF-16.</param>
    /// <param name="offsets">The offset of each code unit of <paramref name="text"/> in the text being enumerated.
    /// On return, its first elements hold the word starts in ascending order.</param>
    /// <returns>The number of word starts.</returns>
    public static int FindWordStarts(ReadOnlySpan<char> text, Span<int> offsets)
    {
        var pooledBreakpoints = PooledSpan<DWRITE_LINE_BREAKPOINT>.Rent(text.Length);
        var breakpoints = pooledBreakpoints.Span;
        breakpoints.Clear();
        var count = 0;
        if (Analyze(text, breakpoints))
        {
            // count is always below i, so this does not overwrite offsets still to be read.
            for (var i = 1; i < text.Length; i++)
            {
                if (CanBreakBetween(breakpoints[i - 1], breakpoints[i]))
                    offsets[count++] = offsets[i];
            }
        }

        pooledBreakpoints.Recycle();
        return count;
    }

    private static bool CanBreakBetween(in DWRITE_LINE_BREAKPOINT before, in DWRITE_LINE_BREAKPOINT after)
    {
        var a = (DWRITE_BREAK_CONDITION)before.breakConditionAfter;
        var b = (DWRITE_BREAK_CONDITION)after.breakConditionBefore;
        if (a == DWRITE_BREAK_CONDITION.DWRITE_BREAK_CONDITION_MUST_BREAK ||
            b == DWRITE_BREAK_CONDITION.DWRITE_BREAK_CONDITION_MUST_BREAK)
        {
            return true;
        }

        return a != DWRITE_BREAK_CONDITION.DWRITE_BREAK_CONDITION_MAY_NOT_BREAK &&
               b != DWRITE_BREAK_CONDITION.DWRITE_BREAK_CONDITION_MAY_NOT_BREAK &&
               (a == DWRITE_BREAK_CONDITION.DWRITE_BREAK_CONDITION_CAN_BREAK ||
                b == DWRITE_BREAK_CONDITION.DWRITE_BREAK_CONDITION_CAN_BREAK);
    }

    [SuppressMessage(
        "StyleCop.CSharp.LayoutRules",
        "SA1519:Braces should not be omitted from multi-line child statement",
        Justification = "No")]
    private static bool Analyze(ReadOnlySpan<char> text, Span<DWRITE_LINE_BREAKPOINT> breakpoints)
    {
        fixed (char* pText = text)
        fixed (char* locale = "en-us")
        fixed (DWRITE_LINE_BREAKPOINT* pBreakpoints = breakpoints)
        {
            var source = new DirectWriteTextSource(pText, (uint)text.Length, locale);
            var sink = new DirectWriteScriptSink(pBreakpoints, breakpoints.Length);
            return DirectWriteUtilities.TextAnalyzer->AnalyzeLineBreakpoints(
                    (IDWriteTextAnalysisSource*)&source,
                    0,
                    (uint)text.Length,
                    (IDWriteTextAnalysisSink*)&sink)
                .SUCCEEDED;
        }
    }
}
