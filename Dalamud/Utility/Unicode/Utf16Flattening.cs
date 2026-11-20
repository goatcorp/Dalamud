using System.Text;

namespace Dalamud.Utility.Unicode;

/// <summary>Flattens runes into UTF-16 code units, remembering where each code unit came from.</summary>
internal static class Utf16Flattening
{
    /// <summary>Appends a rune as UTF-16 code units.</summary>
    /// <param name="rune">The rune.</param>
    /// <param name="source">The source offset to record for each code unit of the rune.</param>
    /// <param name="units">Receives the code units.</param>
    /// <param name="sources">Receives the source offset of each code unit.</param>
    /// <param name="written">The number of code units written so far.</param>
    /// <returns>The new number of code units written.</returns>
    public static int Append(Rune rune, int source, Span<char> units, Span<int> sources, int written)
    {
        var length = rune.EncodeToUtf16(units[written..]);
        sources.Slice(written, length).Fill(source);
        return written + length;
    }
}
