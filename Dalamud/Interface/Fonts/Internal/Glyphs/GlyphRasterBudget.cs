using System.Diagnostics;

using Dalamud.Logging.Internal;
using Dalamud.Utility;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <summary>The time glyphs may be rasterized on the main thread for in a frame, shared by all fonts.</summary>
/// <remarks>Main thread only. Fonts rasterize the glyphs beyond it on the thread pool, and draw them as nothing until
/// they are ready.</remarks>
/// <seealso cref="ConcurrentGlyphQueue{TJob}"/>
internal static class GlyphRasterBudget
{
    /// <summary>The largest em size in device pixels of a glyph rasterized on the main thread while the budget is
    /// limited.</summary>
    /// <remarks>The budget cannot tell what the next glyph costs, and larger ones may take longer than all of it.
    /// </remarks>
    public const float MaxInlineSize = 64;

    // At most a tenth of a refresh of the display, and at most 1 ms, so that new text does not drop frames.
    private const int PartOfRefresh = 10;

    private static readonly ModuleLog Log = new(typeof(GlyphRasterBudget));
    private static readonly TimeSpan MaxFrameBudget = TimeSpan.FromMilliseconds(1);

    // Rasterizing on the main thread for longer than this in a frame is logged.
    private static readonly long SlowRasterizeTicks = (Stopwatch.Frequency * 4) / 1000;

    // The time glyphs rasterized on the thread pool may be added for in a frame, less the uploads of the frame before.

    private static long spentTicks;
    private static int spentGlyphs;
    private static int synchronousDepth;
    private static long uploadTicks;
    private static long lastUploadTicks;

    /// <summary>Gets or sets the time glyphs may be rasterized on the main thread for in a frame.</summary>
    /// <remarks>Unlimited by default. <see cref="FrameBudgetOverride"/> takes its place if set.</remarks>
    public static TimeSpan FrameBudget
    {
        get => FrameBudgetOverride ?? field;
        set;
    }

        = TimeSpan.MaxValue;

    /// <summary>Gets or sets the time to use instead of <see cref="FrameBudget"/>, or null for none.</summary>
    /// <remarks>For tests and the font service widget.</remarks>
    public static TimeSpan? FrameBudgetOverride { get; set; }

    /// <summary>Gets a value indicating whether glyphs are to be rasterized when asked for, however long it takes.
    /// </summary>
    /// <seealso cref="BeginSynchronous"/>
    public static bool Synchronous => synchronousDepth > 0;

    /// <summary>Gets the time glyphs rasterized on the thread pool may be added for in this frame, in
    /// <see cref="Stopwatch"/> ticks.</summary>
    /// <remarks>The time the frame before spent uploading glyphs to the GPU is taken off. It may be 0 or less.
    /// </remarks>
    public static long AddBudgetTicks
    {
        get => field - lastUploadTicks;
    }

        = Stopwatch.Frequency / 2000;

    /// <summary>Tests whether a glyph may be rasterized on the main thread in this frame.</summary>
    /// <param name="costly">Whether it may take longer than the budget, such as a color glyph or one larger than
    /// <see cref="MaxInlineSize"/>.</param>
    /// <returns>Whether it may. Costly glyphs may only while the budget is unlimited.</returns>
    /// <remarks>It does not tell about <see cref="Synchronous"/>.</remarks>
    public static bool HasTimeFor(bool costly) =>
        FrameBudget == TimeSpan.MaxValue ||
        (!costly && spentTicks < FrameBudget.TotalSeconds * Stopwatch.Frequency);

    /// <summary>Sets <see cref="FrameBudget"/> for the display the game is on.</summary>
    /// <param name="refreshHz">How many times a second the display refreshes.</param>
    public static void SetFromRefreshRate(uint refreshHz)
    {
        var budget = TimeSpan.FromSeconds(1.0 / Math.Max(refreshHz, 1) / PartOfRefresh);
        FrameBudget = budget < MaxFrameBudget ? budget : MaxFrameBudget;
    }

    /// <summary>Makes glyphs be rasterized when asked for, until disposed. Nests.</summary>
    /// <returns>A disposable that ends the scope.</returns>
    /// <exception cref="InvalidOperationException">If called outside the main thread.</exception>
    public static ImPopFontDisposable BeginSynchronous()
    {
        ThreadSafety.AssertMainThread();
        synchronousDepth++;
        return new ImPopFontDisposable(completeGlyphs: true);
    }

    /// <summary>Ends a scope of <see cref="BeginSynchronous"/>.</summary>
    public static void EndSynchronous()
    {
        if (synchronousDepth > 0)
            synchronousDepth--;
    }

    /// <summary>Starts measuring a glyph rasterized on the main thread, against <see cref="FrameBudget"/>.</summary>
    /// <returns>The time to pass to <see cref="EndMeasure"/>.</returns>
    public static long Measure() => Stopwatch.GetTimestamp();

    /// <summary>Ends measuring a glyph rasterized on the main thread.</summary>
    /// <param name="start">What <see cref="Measure"/> returned.</param>
    public static void EndMeasure(long start)
    {
        if (!ThreadSafety.IsMainThread)
            return;
        spentTicks += Stopwatch.GetTimestamp() - start;
        spentGlyphs++;
    }

    /// <summary>Counts time spent uploading glyphs to the GPU, against <see cref="AddBudgetTicks"/> of the next frame.
    /// </summary>
    /// <param name="ticks">The time in <see cref="Stopwatch"/> ticks.</param>
    public static void CountUpload(long ticks) => uploadTicks += ticks;

    /// <summary>Starts the budget of the next frame.</summary>
    /// <remarks>Synchronous scopes left open are ended.</remarks>
    public static void EndFrame()
    {
        lastUploadTicks = uploadTicks;
        uploadTicks = 0;
        if (spentTicks >= SlowRasterizeTicks)
        {
            Log.Debug(
                "Rasterizing {Count} glyphs took {Ms:0.00}ms in one frame",
                spentGlyphs,
                (spentTicks * 1000.0) / Stopwatch.Frequency);
        }

        spentTicks = 0;
        spentGlyphs = 0;
        if (synchronousDepth > 0)
        {
            Log.Verbose(
                "{Count} scopes of complete glyphs were not ended in the frame, and are ended now",
                synchronousDepth);
            synchronousDepth = 0;
        }
    }
}
