using System.Diagnostics;

using Dalamud.Utility;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Time spent by the font engine, per frame, for finding out what costs frame time.</summary>
/// <remarks><ul>
/// <li>Only the main thread is measured. Sections may nest, such as rasterizing within drawing text. Each counts its
/// own time in full, and nested calls of the same section count once. A section that throws before its end is not
/// counted for the rest of the frame.</li>
/// <li>Measuring costs time in every call, so it is off unless something shows the measurements.</li>
/// </ul></remarks>
/// <seealso cref="BeginProfile"/>
internal static class FontProfiler
{
    /// <summary>The number of frames kept.</summary>
    public const int FrameCount = 300;

    private const int SectionCount = (int)Section.Count;

    private static readonly Stat[] Frames = new Stat[FrameCount * SectionCount];
    private static readonly long[] FrameTicks = new long[FrameCount];
    private static readonly Stat[] Current = new Stat[SectionCount];
    private static readonly int[] Depths = new int[SectionCount];
    private static long frameStart = Stopwatch.GetTimestamp();
    private static int next;
    private static int filled;
    private static bool shown;
    private static long frameIndex;
    private static long shownUntil;

    /// <summary>What is measured.</summary>
    public enum Section
    {
        /// <summary>Drawing layouts into draw lists, including rasterizing the glyphs they need.</summary>
        Render,

        /// <summary>Within <see cref="Render"/>, the render hook, resolving styles and finding the items in view.
        /// </summary>
        RenderSetup,

        /// <summary>Within <see cref="Render"/>, getting the glyph images from the fonts.</summary>
        RenderImages,

        /// <summary>Within <see cref="Render"/>, drawing decorations.</summary>
        RenderDecorations,

        /// <summary>Making layouts that were not cached, of plain text or SeStrings.</summary>
        Layout,

        /// <summary>Within <see cref="Layout"/>, decoding text and finding graphemes and line break opportunities.
        /// </summary>
        LayoutAnalyze,

        /// <summary>Within <see cref="Layout"/>, shaping, including choosing the fonts.</summary>
        LayoutShape,

        /// <summary>Within <see cref="LayoutShape"/>, the shaping by the fonts themselves.</summary>
        LayoutShapeByFonts,

        /// <summary>Within <see cref="Layout"/>, breaking lines and placing items.</summary>
        LayoutFinish,

        /// <summary>Rasterizing glyphs that were not in the atlas, with their synthetic bold, on the main thread.
        /// </summary>
        Rasterize,

        /// <summary>Glyphs sent to the thread pool to be rasterized, as the frame was out of budget. No time.</summary>
        Deferred,

        /// <summary>Packing rasterized glyphs into the atlas.</summary>
        Pack,

        /// <summary>Uploading the changed parts of the atlas to the GPU.</summary>
        Upload,

        /// <summary>The number of sections.</summary>
        Count,
    }

    /// <summary>Gets a value indicating whether sections are measured.</summary>
    /// <seealso cref="BeginProfile"/>
    public static bool Enabled { get; private set; }

    // Checks Enabled first, so that it costs one read while off.
    private static bool IsMeasured => Enabled && ThreadSafety.IsMainThread;

    /// <summary>Measures this frame and the next, for a view of the measurements drawn every frame.</summary>
    /// <remarks>Measuring that starts again forgets the frames before.</remarks>
    public static void BeginProfile()
    {
        if (!shown)
        {
            shown = Enabled = true;
            next = filled = 0;
        }

        shownUntil = frameIndex + 1;
    }

    /// <summary>Starts a section.</summary>
    /// <param name="section">The section.</param>
    /// <returns>The time to pass to <see cref="End"/>, or 0 if the section is already open.</returns>
    public static long Begin(Section section) =>
        IsMeasured && Depths[(int)section]++ == 0 ? Stopwatch.GetTimestamp() : 0;

    /// <summary>Ends a section.</summary>
    /// <param name="section">The section.</param>
    /// <param name="start">What <see cref="Begin"/> returned.</param>
    /// <param name="bytes">What was processed, if any, in the unit of <see cref="Stat.Bytes"/>.</param>
    /// <param name="calls">The number of calls it counts as, such as glyphs rasterized together.</param>
    public static void End(Section section, long start, long bytes = 0, long calls = 1)
    {
        if (!IsMeasured)
            return;
        Depths[(int)section]--;
        if (start == 0)
            return;
        ref var s = ref Current[(int)section];
        s.Ticks += Stopwatch.GetTimestamp() - start;
        s.Calls += calls;
        s.Bytes += bytes;
    }

    /// <summary>Counts calls of a section that takes no time of its own.</summary>
    /// <param name="section">The section.</param>
    /// <param name="calls">The number of calls.</param>
    public static void Count(Section section, long calls = 1)
    {
        if (IsMeasured)
            Current[(int)section].Calls += calls;
    }

    /// <summary>Ends the frame, as it has been rendered.</summary>
    /// <remarks>Measuring stops here once nothing has shown the measurements in this frame or the one before.
    /// </remarks>
    public static void EndFrame()
    {
        var now = Stopwatch.GetTimestamp();
        var ticks = now - frameStart;
        frameStart = now;
        if (shown && frameIndex >= shownUntil)
        {
            shown = Enabled = false;
        }

        frameIndex++;

        // Every section has ended by now, unless one threw before its end, or measuring started within it.
        Array.Clear(Depths);
        if (Enabled)
        {
            FrameTicks[next] = ticks;
            Current.CopyTo(Frames.AsSpan(next * SectionCount, SectionCount));
            next = (next + 1) % FrameCount;
            filled = Math.Min(filled + 1, FrameCount);
        }

        Array.Clear(Current);
    }

    /// <summary>Gets a section of the frame most recently ended.</summary>
    /// <param name="section">The section.</param>
    /// <returns>The time and calls.</returns>
    public static Stat Last(Section section) =>
        filled == 0 ? default : Frames[((((next + FrameCount) - 1) % FrameCount) * SectionCount) + (int)section];

    /// <summary>Gets the average and the worst of the frames kept.</summary>
    /// <param name="section">The section.</param>
    /// <returns>The average per frame, the frame with the most time in the section, and how long that frame took.
    /// </returns>
    public static (Stat Average, Stat Worst, long WorstFrameTicks) Summarize(Section section)
    {
        var sum = default(Stat);
        var worst = default(Stat);
        var worstFrame = 0L;
        for (var i = 0; i < filled; i++)
        {
            var s = Frames[(i * SectionCount) + (int)section];
            sum.Ticks += s.Ticks;
            sum.Calls += s.Calls;
            sum.Bytes += s.Bytes;
            if (s.Ticks > worst.Ticks)
                (worst, worstFrame) = (s, FrameTicks[i]);
        }

        if (filled > 0)
            sum = new() { Ticks = sum.Ticks / filled, Calls = sum.Calls / filled, Bytes = sum.Bytes / filled };
        return (sum, worst, worstFrame);
    }

    /// <summary>Gets the average frame time of the frames kept.</summary>
    /// <returns>The time in <see cref="Stopwatch"/> ticks.</returns>
    public static long AverageFrameTicks()
    {
        var sum = 0L;
        for (var i = 0; i < filled; i++)
            sum += FrameTicks[i];
        return filled == 0 ? 0 : sum / filled;
    }

    /// <summary>Converts ticks to milliseconds.</summary>
    /// <param name="ticks">The <see cref="Stopwatch"/> ticks.</param>
    /// <returns>The milliseconds.</returns>
    public static double ToMs(long ticks) => (ticks * 1000.0) / Stopwatch.Frequency;

    /// <summary>Time and calls of a section in a frame.</summary>
    public struct Stat
    {
        /// <summary>The time, in <see cref="Stopwatch"/> ticks.</summary>
        public long Ticks;

        /// <summary>The number of calls, such as glyphs rasterized.</summary>
        public long Calls;

        /// <summary>What was processed. Bytes of text or uploaded, codepoints shaped, or quads drawn for
        /// <see cref="Section.Render"/>.</summary>
        public long Bytes;
    }
}
