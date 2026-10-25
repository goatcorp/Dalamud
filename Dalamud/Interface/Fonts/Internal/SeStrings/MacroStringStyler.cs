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

    /// <inheritdoc/>
    public override TextMarkup Markup => TextMarkup.MacroString;

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

    // Compiles a macro string to a SeString while recording each output byte's source offset in outputSource.
    // Backslash escapes the next UTF-8 character, and raw 0x00 and 0x02 bytes outside macros become U+FFFD so they are
    // not misparsed as SeString framing bytes.
    private static byte[] Compile(ReadOnlySpan<byte> source, List<byte> output, List<int> outputSource)
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
                {
                    var maxEnd = Math.Min(i + 1 + Utf8Length(source[i + 1]), source.Length);
                    end = i + 2;
                    while (end < maxEnd && (source[end] & 0xC0) == 0x80)
                        end++;
                    bytes = source[(i + 1)..end];
                    if (bytes is [0x00 or 0x02])
                        bytes = "�"u8;
                    break;
                }

                case 0x00 or 0x02:
                    end = i + 1;
                    bytes = "�"u8;
                    break;

                case (byte)'<' when MacroEnd(source, i) is var macroEnd and >= 0:
                    end = macroEnd;
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

    // Returns the index after the closing '>' of a macro starting at begin, accounting for nested macros, backslash
    // escapes, and comparisons inside '[' and ']' such as <if([gnum1>0],a,b)>.
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

        return -1;
    }

    private static int Utf8Length(byte lead) =>
        lead < 0x80 ? 1 : (lead & 0xE0) == 0xC0 ? 2 : (lead & 0xF0) == 0xE0 ? 3 : (lead & 0xF8) == 0xF0 ? 4 : 1;
}
