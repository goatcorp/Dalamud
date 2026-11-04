using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Utility;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <summary>Rasterizes the glyphs beyond <see cref="GlyphRasterBudget"/> on the thread pool, so that new text does not
/// hold up frames. One instance serves the fonts of one owner.</summary>
/// <remarks><ul>
/// <li>Deferred glyphs are drawn as nothing, and not as final, until they are added in a later frame.</li>
/// <li>Up to <see cref="ConcurrentGlyphQueue.MaxWorkers"/> thread pool threads rasterize at once.</li>
/// <li>Main thread only, but for <paramref name="rasterize"/>.</li>
/// </ul></remarks>
/// <param name="rasterize">Rasterizes a job, with its synthetic bold, or gives null for an empty glyph. Called on the
/// thread pool.</param>
/// <param name="add">Caches what a job rasterized. Called on the main thread.</param>
/// <typeparam name="TJob">What to rasterize.</typeparam>
internal sealed class ConcurrentGlyphQueue<TJob>(Func<TJob, GlyphBitmap?> rasterize, Action<TJob, GlyphBitmap?> add)
    : IDisposable
    where TJob : struct
{
    // The frame each pending glyph was last asked for in.
    private readonly Dictionary<(object Source, ulong Key), long> pending = [];
    private readonly ConcurrentQueue<(object Source, ulong Key, TJob Job)> waiting = new();
    private readonly ConcurrentQueue<(object Source, ulong Key, TJob Job, GlyphBitmap? Bitmap)> finished = new();
    private long drainedFrame = -1;

    // The thread pool work items taking jobs from waiting.
    private int workers;

    // Read on the thread pool too.
    private volatile bool disposed;

    /// <summary>Tests whether a glyph is to be drawn as nothing for now, as it is rasterized on the thread pool.
    /// </summary>
    /// <param name="source">The font or cache that <paramref name="key"/> belongs to.</param>
    /// <param name="key">The key of the glyph in <paramref name="source"/>.</param>
    /// <param name="job">What to rasterize.</param>
    /// <param name="costly">Whether the glyph may take longer than the budget.</param>
    /// <returns>Whether it is deferred. Otherwise, the caller rasterizes it, within
    /// <see cref="GlyphRasterBudget.Measure"/>.</returns>
    /// <remarks>A glyph already pending stays deferred. Past <see cref="ConcurrentGlyphQueue.MaxPending"/>, glyphs
    /// are deferred without being rasterized. Once disposed, nothing is deferred.</remarks>
    public bool TryDefer(object source, ulong key, in TJob job, bool costly = false)
    {
        if (this.disposed || GlyphRasterBudget.Synchronous || !ThreadSafety.IsMainThread)
            return false;
        ref var asked = ref CollectionsMarshal.GetValueRefOrNullRef(this.pending, (source, key));
        if (!Unsafe.IsNullRef(ref asked))
        {
            asked = FontClock.Frame;
            return true;
        }

        if (GlyphRasterBudget.HasTimeFor(costly))
            return false;
        if (ConcurrentGlyphQueue.TotalPending >= ConcurrentGlyphQueue.MaxPending)
            return true;

        this.pending.Add((source, key), FontClock.Frame);
        ConcurrentGlyphQueue.CountPending(1);
        FontProfiler.Count(FontProfiler.Section.Deferred);
        this.waiting.Enqueue((source, key, job));
        this.StartWorker();
        return true;
    }

    /// <summary>Adds the glyphs finished on the thread pool, once per frame. Called before the glyphs of the frame are
    /// asked for.</summary>
    /// <remarks>Only glyphs asked for in the two frames before are added, within
    /// <see cref="GlyphRasterBudget.AddBudgetTicks"/> but at least one. The others are dropped.</remarks>
    public void DrainOncePerFrame()
    {
        if (this.drainedFrame == FontClock.Frame || !ThreadSafety.IsMainThread)
            return;
        this.drainedFrame = FontClock.Frame;
        if (!this.finished.IsEmpty)
            this.Drain();
    }

    /// <summary>Drops the pending glyphs and gives back their count to
    /// <see cref="ConcurrentGlyphQueue.TotalPending"/>.</summary>
    /// <remarks>Glyphs still being rasterized are dropped when they finish. The fonts of the owner may keep using the
    /// queue, which then defers nothing.</remarks>
    public void Dispose()
    {
        if (this.disposed)
            return;
        this.disposed = true;
        ConcurrentGlyphQueue.CountPending(-this.pending.Count);
        this.pending.Clear();
        this.waiting.Clear();
        this.finished.Clear();
    }

    private void Drain()
    {
        // One glyph is added in any case, so that uploads that took all of the budget do not stop the queue.
        var start = Stopwatch.GetTimestamp();
        var budget = GlyphRasterBudget.AddBudgetTicks;
        var added = false;
        while (this.finished.TryPeek(out var f))
        {
            var key = (f.Source, f.Key);
            var removed = this.pending.Remove(key, out var asked);
            var wanted = removed && FontClock.Frame - asked <= 2;
            if (wanted && added && Stopwatch.GetTimestamp() - start >= budget)
            {
                this.pending.Add(key, asked);
                break;
            }

            _ = this.finished.TryDequeue(out _);
            if (removed)
                ConcurrentGlyphQueue.CountPending(-1);
            if (wanted)
            {
                add(f.Job, f.Bitmap);
                added = true;
            }
        }
    }

    private void StartWorker()
    {
        var n = Volatile.Read(ref this.workers);
        while (n < ConcurrentGlyphQueue.MaxWorkers)
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
            this.Rasterize(w.Source, w.Key, w.Job);

        // A job queued after the loop found none, while this worker was still counted, would wait otherwise.
        Interlocked.Decrement(ref this.workers);
        if (!this.waiting.IsEmpty)
            this.StartWorker();
    }

    // On the thread pool, where an exception would end the process. A glyph that fails is added as empty, so that it
    // is not asked for again.
    private void Rasterize(object source, ulong key, in TJob job)
    {
        // Nothing waits for the glyphs of a disposed queue.
        if (this.disposed)
            return;

        GlyphBitmap? bitmap;
        try
        {
            bitmap = rasterize(job);
        }
        catch (Exception e)
        {
            ConcurrentGlyphQueue.Log.Verbose(
                e,
                "A glyph of {Source} could not be rasterized, and is drawn as nothing",
                source);
            bitmap = null;
        }

        this.finished.Enqueue((source, key, job, bitmap));
    }
}
