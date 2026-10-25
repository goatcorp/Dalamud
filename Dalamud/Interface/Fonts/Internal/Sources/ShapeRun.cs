using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Utility.Unicode;

using Lumina.Text;

namespace Dalamud.Interface.Fonts.Internal.Sources;

/// <summary>A run of text to shape.</summary>
/// <remarks>One font, size and style, within one line, without inline objects or controls. Fonts shape only
/// [<see cref="Begin"/>, <see cref="End"/>), without the text around it.</remarks>
internal readonly ref struct ShapeRun
{
    /// <summary>Initializes a new instance of the <see cref="ShapeRun"/> struct.</summary>
    /// <param name="text">The text the run is in.</param>
    /// <param name="begin">Start of the run in <paramref name="text"/>, at a grapheme boundary.</param>
    /// <param name="end">End of the run in <paramref name="text"/>, at a grapheme boundary.</param>
    /// <param name="size">Size in logical pixels.</param>
    /// <param name="density">Device pixels per logical pixel.</param>
    /// <param name="graphemeStarts">Per codepoint of <paramref name="text"/>, whether a grapheme (UAX #29) starts
    /// there, if the caller knows. Empty to find them when needed.</param>
    public ShapeRun(
        ReadOnlySpan<uint> text, int begin, int end, float size, float density,
        ReadOnlySpan<bool> graphemeStarts = default)
    {
        if (begin < 0 || end < begin || end > text.Length)
            throw new ArgumentOutOfRangeException(nameof(end), end, "Not a range of the text.");
        if (!graphemeStarts.IsEmpty && graphemeStarts.Length < end)
            throw new ArgumentOutOfRangeException(nameof(graphemeStarts), "Shorter than the run.");
        this.Text = text;
        this.Begin = begin;
        this.End = end;
        this.Size = size;
        this.Density = density;
        this.GraphemeStarts = graphemeStarts;
    }

    /// <summary>Gets the text the run is in.</summary>
    public ReadOnlySpan<uint> Text { get; }

    /// <summary>Gets the start of the run in <see cref="Text"/>.</summary>
    public int Begin { get; }

    /// <summary>Gets the end of the run in <see cref="Text"/>.</summary>
    public int End { get; }

    /// <summary>Gets the size in logical pixels.</summary>
    public float Size { get; }

    /// <summary>Gets the device pixels per logical pixel.</summary>
    public float Density { get; }

    // Per codepoint of Text, whether a grapheme starts there, or empty if not known.
    private ReadOnlySpan<bool> GraphemeStarts { get; }

    /// <summary>Gets a part of the run, in the same text.</summary>
    /// <param name="begin">Start in <see cref="Text"/>.</param>
    /// <param name="end">End in <see cref="Text"/>.</param>
    /// <returns>The part.</returns>
    public ShapeRun Slice(int begin, int end) =>
        new(this.Text, begin, end, this.Size, this.Density, this.GraphemeStarts);

    /// <summary>Gets the run at another size.</summary>
    /// <param name="size">Size in logical pixels.</param>
    /// <returns>The run.</returns>
    public ShapeRun WithSize(float size) =>
        new(this.Text, this.Begin, this.End, size, this.Density, this.GraphemeStarts);

    /// <summary>Finds the grapheme clusters (UAX #29) of the run.</summary>
    /// <param name="starts">Receives the index into <see cref="Text"/> of the start of each grapheme, in order.</param>
    /// <remarks>Takes them from <see cref="GraphemeStarts"/> if the caller gave them.</remarks>
    public void FindGraphemes(List<int> starts)
    {
        starts.Clear();
        if (this.End <= this.Begin)
            return;
        starts.Add(this.Begin);
        if (!this.GraphemeStarts.IsEmpty)
        {
            var known = this.GraphemeStarts;
            for (var i = this.Begin + 1; i < this.End; i++)
            {
                if (known[i])
                    starts.Add(i);
            }

            return;
        }

        var bytes = MemoryMarshal.AsBytes(this.Text[this.Begin..this.End]);
        const UtfEnumeratorFlags utf32 = UtfEnumeratorFlags.Utf32 | UtfEnumeratorFlags.LittleEndian;
        foreach (var boundary in new UnicodeGraphemeClusterEnumerator(bytes, utf32))
        {
            if (boundary > 0 && boundary < bytes.Length)
                starts.Add(this.Begin + (boundary / 4));
        }
    }
}
