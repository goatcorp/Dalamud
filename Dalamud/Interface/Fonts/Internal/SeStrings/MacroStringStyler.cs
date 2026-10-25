using System.Collections.Generic;

using Dalamud.Interface.Fonts.Internal.Layout;
using Dalamud.Interface.Fonts.Internal.Markup;

using Lumina.Text.Parse;
using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.Fonts.Internal.SeStrings;

/// <summary>Reads the macro representation of SeStrings.</summary>
/// <remarks>Each macro is compiled on its own, and the SeString is read as <see cref="SeStringStyler"/> does. Offsets
/// of layouts are into the macro string.</remarks>
internal sealed class MacroStringStyler : SeStringStyler
{
    private readonly List<byte> compiled = [];
    private readonly List<int> compiledSource = [];

    /// <summary>Initializes a new instance of the <see cref="MacroStringStyler"/> class, with the game's icons and
    /// colors.</summary>
    /// <seealso cref="SeStringGameData"/>
    public MacroStringStyler()
    {
    }

    /// <inheritdoc/>
    public override TextMarkup Markup => TextMarkup.MacroString;

    /// <summary>Compiles a macro string, and maps each byte of the SeString and its end to the offset in the macro
    /// string it came from.</summary>
    /// <param name="source">The macro string.</param>
    /// <param name="output">Buffer for the SeString.</param>
    /// <param name="outputSource">Receives the offset in <paramref name="source"/> of each byte of the SeString, and
    /// of its end.</param>
    /// <returns>The SeString.</returns>
    /// <remarks>Characters after <c>\</c> are literal, and each macro is compiled on its own.</remarks>
    internal static byte[] Compile(ReadOnlySpan<byte> source, List<byte> output, List<int> outputSource)
    {
        output.Clear();
        outputSource.Clear();
        for (var i = 0; i < source.Length;)
        {
            int end;
            ReadOnlySpan<byte> bytes;
            switch (source[i])
            {
                case (byte)'\\' when i + 1 < source.Length:
                    end = Math.Min(i + 1 + Utf8Length(source[i + 1]), source.Length);
                    bytes = source[(i + 1)..end];
                    break;
                case (byte)'<':
                    end = MacroEnd(source, i);
                    bytes = ReadOnlySeString.FromMacroString(
                        source[i..end],
                        new() { ExceptionMode = MacroStringParseExceptionMode.EmbedError }).Data.Span;
                    break;
                default:
                    end = i + 1;
                    bytes = source.Slice(i, 1);
                    break;
            }

            foreach (var b in bytes)
            {
                output.Add(b);
                outputSource.Add(i);
            }

            i = end;
        }

        outputSource.Add(source.Length);
        return [.. output];
    }

    /// <inheritdoc/>
    protected override void Parse(
        FontImpl font, in TextLayoutDesc desc, ReadOnlySpan<byte> text, ITextMarkupCallback[] callbacks, bool nested,
        StyledText output)
    {
        var toMacro = nested ? [] : this.compiledSource;
        var seString = Compile(text, nested ? [] : this.compiled, toMacro);
        this.ParseSeString(font, desc, seString, callbacks, nested, output);
        output.SourceToDrawn = [.. toMacro];
    }

    // Finds where a macro of a macro string ends. The end is past its '>', counting nested macros. Characters after
    // '\' are literal, so '<' and '>' that are not of macros are escaped within macros. Within '[' and ']' they are
    // comparisons, as in <if([gnum1>0],a,b)>.
    private static int MacroEnd(ReadOnlySpan<byte> source, int begin)
    {
        var nesting = 0;
        var brackets = 0;
        for (var i = begin; i < source.Length; i++)
        {
            switch (source[i])
            {
                case (byte)'\\':
                    i++;
                    break;
                case (byte)'[':
                    brackets++;
                    break;
                case (byte)']' when brackets > 0:
                    brackets--;
                    break;
                case (byte)'<' when brackets == 0:
                    nesting++;
                    break;
                case (byte)'>' when brackets == 0 && --nesting == 0:
                    return i + 1;
            }
        }

        return source.Length;
    }

    private static int Utf8Length(byte lead) =>
        lead < 0x80 ? 1 : (lead & 0xE0) == 0xC0 ? 2 : (lead & 0xF0) == 0xE0 ? 3 : (lead & 0xF8) == 0xF0 ? 4 : 1;
}
