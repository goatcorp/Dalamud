using Dalamud.Logging.Internal;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <summary>What all instances of <see cref="ConcurrentGlyphQueue{TJob}"/> share. Main thread only.</summary>
internal static class ConcurrentGlyphQueue
{
    /// <summary>The most glyphs all queues hold at once.</summary>
    /// <remarks>Bounds the bitmaps held when text changes faster than glyphs are rasterized. It is about what
    /// <see cref="MaxWorkers"/> rasterize in a few frames.</remarks>
    public const int MaxPending = 1024;

    /// <summary>The most thread pool threads a queue rasterizes on at once.</summary>
    /// <remarks>A burst of new text then leaves the other threads to the game and to plugins.</remarks>
    public static readonly int MaxWorkers = Math.Clamp(Environment.ProcessorCount / 4, 1, 2);

    /// <summary>Logger for <see cref="ConcurrentGlyphQueue{T}"/>.</summary>
    public static readonly ModuleLog Log = new(typeof(ConcurrentGlyphQueue));

    /// <summary>Gets the number of glyphs of all queues being rasterized on the thread pool, or waiting to be added.
    /// </summary>
    public static int TotalPending { get; private set; }

    /// <summary>Changes <see cref="TotalPending"/>.</summary>
    /// <param name="delta">The change.</param>
    internal static void CountPending(int delta) => TotalPending += delta;
}
