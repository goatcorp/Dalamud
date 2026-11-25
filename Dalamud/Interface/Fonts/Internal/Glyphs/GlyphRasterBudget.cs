using System.Diagnostics;

using Dalamud.Logging.Internal;
using Dalamud.Utility;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <summary>Per-frame main-thread time budget for synchronous glyph rasterization, shared across all fonts.</summary>
/// <remarks>Main thread only. Glyphs beyond the budget are rasterized on the thread pool and omitted until ready.
/// </remarks>
/// <seealso cref="ConcurrentGlyphQueue"/>
internal static class GlyphRasterBudget
{
    /// <summary>Maximum font line height in device pixels rasterized inline on the main thread while the frame budget is
    /// finite.</summary>
    /// <remarks>Larger glyphs can exceed the entire frame budget on their own, so they are always deferred.</remarks>
    public const float MaxInlineSize = 64;

    // Budget up to 1/10 of a display refresh, capped at 1 ms, to avoid frame drops.
    private const int PartOfRefresh = 10;

    private static readonly ModuleLog Log = new(typeof(GlyphRasterBudget));
    private static readonly TimeSpan MaxFrameBudget = TimeSpan.FromMilliseconds(1);

    // Rasterizing on the main thread for longer than this in a frame is logged.
    private static readonly long SlowRasterizeTicks = (Stopwatch.Frequency * 4) / 1000;

    private static long spentTicks;
    private static int spentGlyphs;
    private static int synchronousDepth;
    private static long uploadTicks;
    private static long lastUploadTicks;

    /// <summary>Gets or sets the per-frame main-thread glyph rasterization budget.</summary>
    /// <remarks>Unlimited by default. <see cref="FrameBudgetOverride"/> takes precedence when set.</remarks>
    public static TimeSpan FrameBudget
    {
        get => FrameBudgetOverride ?? field;
        set;
    }

        = TimeSpan.MaxValue;

    /// <summary>Gets or sets an override for <see cref="FrameBudget"/>, or <see langword="null"/> for none.</summary>
    public static TimeSpan? FrameBudgetOverride { get; set; }

    /// <summary>Gets a value indicating whether synchronous rasterization is forced regardless of budget.</summary>
    /// <seealso cref="BeginSynchronous"/>
    public static bool Synchronous => synchronousDepth > 0;

    /// <summary>Gets the remaining time budget this frame for committing thread-pool glyphs, in <see cref="Stopwatch"/>
    /// ticks.</summary>
    /// <remarks>Starts at 0.5 ms minus the previous frame's GPU upload time, and may be zero or negative.</remarks>
    public static long AddBudgetTicks => (Stopwatch.Frequency / 2000) - lastUploadTicks;

    /// <summary>Tests whether a glyph may be rasterized on the main thread this frame.</summary>
    /// <param name="costly">Whether the glyph may exceed the budget on its own, such as a color glyph or one larger than
    /// <see cref="MaxInlineSize"/>.</param>
    /// <returns><see langword="true"/> if inline rasterization is within budget. Costly glyphs are allowed only when the
    /// budget is unlimited.</returns>
    /// <remarks>Does not check <see cref="Synchronous"/>.</remarks>
    public static bool HasTimeFor(bool costly) =>
        FrameBudget == TimeSpan.MaxValue ||
        (!costly && spentTicks < FrameBudget.TotalSeconds * Stopwatch.Frequency);

    /// <summary>Sets <see cref="FrameBudget"/> from the game display's refresh rate.</summary>
    /// <param name="refreshHz">Display refresh rate in Hz.</param>
    public static void SetFromRefreshRate(uint refreshHz)
    {
        var budget = TimeSpan.FromSeconds(1.0 / Math.Max(refreshHz, 1) / PartOfRefresh);
        FrameBudget = budget < MaxFrameBudget ? budget : MaxFrameBudget;
    }

    /// <summary>Forces synchronous glyph rasterization until the returned scope is disposed. Supports nesting.
    /// </summary>
    /// <returns>A disposable that ends the scope.</returns>
    /// <exception cref="InvalidOperationException">If called outside the main thread.</exception>
    public static ImPopFontDisposable BeginSynchronous()
    {
        ThreadSafety.AssertMainThread();
        synchronousDepth++;
        return new(completeGlyphs: true);
    }

    /// <summary>Ends a scope of <see cref="BeginSynchronous"/>.</summary>
    public static void EndSynchronous()
    {
        if (synchronousDepth > 0)
            synchronousDepth--;
    }

    /// <summary>Counts time spent rasterizing a glyph, against <see cref="FrameBudget"/>.</summary>
    /// <param name="ticks">The time in <see cref="Stopwatch"/> ticks.</param>
    /// <remarks>Only the main thread counts.</remarks>
    public static void CountRasterize(long ticks)
    {
        if (!ThreadSafety.IsMainThread)
            return;
        spentTicks += ticks;
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
