using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Interface.Fonts.Internal.DirectWrite;
using Dalamud.Logging.Internal;
using Dalamud.Utility;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <summary>Offloads glyph rasterization beyond <see cref="GlyphRasterBudget"/> to the thread pool to avoid frame
/// hitches. One instance serves the fonts of one owner.</summary>
/// <remarks><ul>
/// <li>Deferred glyphs return an empty non-final image until committed on a later frame.</li>
/// <li>Up to <see cref="MaxWorkers"/> thread-pool workers rasterize concurrently.</li>
/// <li>Main thread only, except worker calls to <see cref="DirectWriteFontFace.Rasterize(in GlyphRasterRequest)"/>.
/// </li>
/// </ul></remarks>
internal sealed class ConcurrentGlyphQueue : IDisposable
{
    /// <summary>Maximum pending glyphs across all queues.</summary>
    /// <remarks>Caps queued bitmaps when text changes faster than glyphs are rasterized.</remarks>
    public const int MaxPending = 1024;

    /// <summary>Maximum concurrent thread-pool workers per queue.</summary>
    /// <remarks>Caps worker concurrency so bursts of new text do not starve the game or plugins.</remarks>
    public static readonly int MaxWorkers = Math.Clamp(Environment.ProcessorCount / 4, 1, 2);

    private static readonly ModuleLog Log = new(typeof(ConcurrentGlyphQueue));

    // Frame in which each pending glyph was last requested.
    private readonly Dictionary<(DirectWriteFontFace Face, ulong Key), long> pending = [];
    private readonly ConcurrentQueue<GlyphRasterRequest> waiting = new();
    private readonly ConcurrentQueue<(GlyphRasterRequest Request, GlyphBitmap? Bitmap)> finished = new();
    private long drainedFrame = -1;

    // Active thread-pool worker count.
    private int workers;

    // Read on worker threads as well.
    private volatile bool disposed;

    /// <summary>Gets the number of glyphs across all queues being rasterized or waiting to be committed.</summary>
    public static int TotalPending { get; private set; }

    /// <summary>Defers rasterization to the thread pool when the frame budget is exhausted or the glyph is already
    /// pending.</summary>
    /// <param name="request">What to rasterize.</param>
    /// <param name="costly">Whether the glyph may exceed the frame budget on its own.</param>
    /// <returns><see langword="true"/> if deferred, or <see langword="false"/> if the caller should rasterize inline
    /// and record time via <see cref="GlyphRasterBudget.CountRasterize"/>.</returns>
    /// <remarks>Already-pending glyphs stay deferred. Above <see cref="MaxPending"/>, glyphs are skipped without
    /// queuing. Returns <see langword="false"/> once disposed.</remarks>
    public bool TryDefer(in GlyphRasterRequest request, bool costly = false)
    {
        if (this.disposed || GlyphRasterBudget.Synchronous || !ThreadSafety.IsMainThread)
            return false;
        var key = (request.Face, request.Key);
        ref var asked = ref CollectionsMarshal.GetValueRefOrNullRef(this.pending, key);
        if (!Unsafe.IsNullRef(ref asked))
        {
            asked = FontClock.Frame;
            return true;
        }

        if (GlyphRasterBudget.HasTimeFor(costly))
            return false;
        if (TotalPending >= MaxPending)
            return true;

        this.pending.Add(key, FontClock.Frame);
        TotalPending++;
        FontProfiler.Count(FontProfiler.Section.Deferred);
        this.waiting.Enqueue(request);
        this.StartWorker();
        return true;
    }

    /// <summary>Commits completed thread-pool glyphs once per frame before rendering.</summary>
    /// <remarks>Commits glyphs requested within the last two frames (at least one, then bounded by
    /// <see cref="GlyphRasterBudget.AddBudgetTicks"/>) and discards older ones.</remarks>
    public void DrainOncePerFrame()
    {
        if (this.drainedFrame == FontClock.Frame || !ThreadSafety.IsMainThread)
            return;
        this.drainedFrame = FontClock.Frame;
        if (!this.finished.IsEmpty)
            this.Drain();
    }

    /// <summary>Clears pending requests and subtracts their count from <see cref="TotalPending"/>.</summary>
    /// <remarks>In-flight worker results are discarded when they finish. Subsequent calls on this queue do not defer.
    /// </remarks>
    public void Dispose()
    {
        if (this.disposed)
            return;
        this.disposed = true;
        TotalPending -= this.pending.Count;
        this.pending.Clear();
        this.waiting.Clear();
        this.finished.Clear();
    }

    private void Drain()
    {
        // Always commit at least one glyph so heavy GPU uploads cannot stall the queue.
        var start = Stopwatch.GetTimestamp();
        var budget = GlyphRasterBudget.AddBudgetTicks;
        var added = false;
        while (this.finished.TryPeek(out var f))
        {
            var key = (f.Request.Face, f.Request.Key);
            var removed = this.pending.Remove(key, out var asked);
            var wanted = removed && FontClock.Frame - asked <= 2;
            if (wanted && added && Stopwatch.GetTimestamp() - start >= budget)
            {
                this.pending.Add(key, asked);
                break;
            }

            _ = this.finished.TryDequeue(out _);
            if (removed)
                TotalPending--;
            if (wanted)
            {
                f.Request.Face.AddRasterized(f.Request.Key, f.Bitmap);
                added = true;
            }
        }
    }

    private void StartWorker()
    {
        var n = Volatile.Read(ref this.workers);
        while (n < MaxWorkers)
        {
            var seen = Interlocked.CompareExchange(ref this.workers, n + 1, n);
            if (seen == n)
            {
                ThreadPool.UnsafeQueueUserWorkItem(static q => q.Work(), this, false);
                return;
            }

            n = seen;
        }
    }

    private void Work()
    {
        while (this.waiting.TryDequeue(out var w))
            this.Rasterize(w);

        // Re-check after decrementing to avoid a lost wakeup if work was enqueued just before exiting.
        Interlocked.Decrement(ref this.workers);
        if (!this.waiting.IsEmpty)
            this.StartWorker();
    }

    // Catch exceptions on the thread pool and cache failures as empty so they are not retried every frame.
    private void Rasterize(in GlyphRasterRequest request)
    {
        if (this.disposed)
            return;

        GlyphBitmap? bitmap;
        try
        {
            bitmap = request.Face.Rasterize(request);
        }
        catch (Exception e)
        {
            Log.Verbose(e, "A glyph of {Source} could not be rasterized, and is drawn as nothing", request.Face);
            bitmap = null;
        }

        this.finished.Enqueue((request, bitmap));
    }
}
