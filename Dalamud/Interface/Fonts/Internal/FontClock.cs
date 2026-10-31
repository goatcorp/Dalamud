using Dalamud.Interface.Fonts.Internal.Glyphs;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>For tracking when to free fonts.</summary>
internal static class FontClock
{
    /// <summary>Gets the number of frames ended so far.</summary>
    /// <seealso cref="GlyphAtlasTextResources.OnFrameRendered"/>
    public static long Frame { get; private set; }

    /// <summary>Counts a frame as ended.</summary>
    public static void Advance() => Frame++;
}
