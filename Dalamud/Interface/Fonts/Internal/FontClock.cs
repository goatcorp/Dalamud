namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Tracks the rendered frame counter used for frame-scoped <see cref="FontImpl"/> pointers, caches, and eviction.</summary>
internal static class FontClock
{
    /// <summary>Gets the number of frames ended so far.</summary>
    public static long Frame { get; private set; }

    /// <summary>Counts a frame as ended.</summary>
    public static void Advance() => Frame++;
}
