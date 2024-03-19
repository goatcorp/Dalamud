using System.Numerics;
using System.Text;

using Dalamud.Game.Text;
using Dalamud.Interface.Internal;
using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Interface.SpannedStrings.Internal;
using Dalamud.Interface.SpannedStrings.Styles;
using Dalamud.Utility.Text;

namespace Dalamud.Interface.SpannedStrings;

/// <summary>A builder for spannables.</summary>
/// <typeparam name="TReturn">The return type.</typeparam>
public interface ISpannedStringBuilder<out TReturn>
{
    /// <summary>Adds the given UTF-16 char sequence.</summary>
    /// <param name="span">Text to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    TReturn Append(ReadOnlySpan<char> span, int repeat = 1);

    /// <summary>Adds the given UTF-8 byte sequence.</summary>
    /// <param name="span">Text to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    TReturn Append(ReadOnlySpan<byte> span, int repeat = 1);

    /// <summary>Adds the given UTF-16 char sequence.</summary>
    /// <param name="memory">Text to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    TReturn Append(ReadOnlyMemory<char> memory, int repeat = 1);

    /// <summary>Adds the given UTF-8 byte sequence.</summary>
    /// <param name="memory">Text to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    TReturn Append(ReadOnlyMemory<byte> memory, int repeat = 1);

    /// <summary>Adds the given UTF-16 char sequence.</summary>
    /// <param name="text">Text to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    TReturn Append(string? text, int repeat = 1);

    /// <summary>Adds the given unicode sequence.</summary>
    /// <param name="utfEnumerator">A codepoint enumerator.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    TReturn Append(UtfEnumerator utfEnumerator, int repeat = 1);

    /// <summary>Adds the string representation of the given value.</summary>
    /// <param name="value">The value to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    TReturn Append(ISpanFormattable? value, int repeat = 1);

    /// <summary>Adds the string representation of the given value.</summary>
    /// <param name="value">The value to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    TReturn Append(object? value, int repeat = 1);

    /// <summary>Appends the given UTF-16 codepoint.</summary>
    /// <param name="value">The character to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    TReturn Append(char value, int repeat = 1);

    /// <summary>Adds the given value.</summary>
    /// <param name="value">The value to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    TReturn Append<T>(T value, int repeat = 1) where T : struct;

    /// <summary>Adds a callback to be called upon rendering.</summary>
    /// <param name="callback">The callback to be called. If null is provided, a placeholder will be added.</param>
    /// <param name="sizeRatio">The width-to-height ratio. 2 means that it is twice as wide as its height.</param>
    /// <param name="id">The ID of this texture wrap, in the context of the built string.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    TReturn AppendCallback(SpannedStringCallbackDelegate? callback, float sizeRatio, out int id);

    /// <summary>Adds a callback to be called upon rendering.</summary>
    /// <param name="id">The ID of the callback, in the context of the built string. If providing a value not fetched
    /// from <see cref="AppendCallback(SpannedStringCallbackDelegate?, float, out int)"/>, avoid making the number too
    /// large.</param>
    /// <param name="sizeRatio">The width-to-height ratio. 2 means that it is twice as wide as its height.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.InsertionCallback, false, "cb", "callback")]
    TReturn AppendCallback(int id, float sizeRatio);

    /// <summary>Adds a codepoint.</summary>
    /// <param name="codepoint">The codepoint.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    TReturn AppendChar(int codepoint, int repeat = 1);

    /// <summary>Adds a rune.</summary>
    /// <param name="rune">The rune.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    TReturn AppendChar(Rune rune, int repeat = 1);

    /// <summary>Adds an icon from the GFD file.</summary>
    /// <param name="iconId">The icon ID.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.InsertionIcon, false, "icon")]
    TReturn AppendIconGfd(GfdIcon iconId);

    /// <summary>Adds an icon from a texture.</summary>
    /// <param name="id">The ID of the texture, in the context of the built string. If providing a value not fetched
    /// from <see cref="AppendTexture(IDalamudTextureWrap?, out int)"/>, avoid making the number too large.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.InsertionTexture, false, "tex")]
    TReturn AppendTexture(int id);

    /// <summary>Adds an icon from a texture.</summary>
    /// <param name="id">The ID of the texture, in the context of the built string. If providing a value not fetched
    /// from <see cref="AppendTexture(IDalamudTextureWrap?, out int)"/>, avoid making the number too large.</param>
    /// <param name="uv0">The relative UV0.</param>
    /// <param name="uv1">The relative UV1.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.InsertionTexture, false, "tex")]
    TReturn AppendTexture(int id, Vector2 uv0, Vector2 uv1);

    /// <summary>Adds an icon from a texture.</summary>
    /// <param name="textureWrap">The texture wrap. If null is provided, a placeholder will be added.</param>
    /// <param name="id">The ID of this texture wrap, in the context of the built string.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    TReturn AppendTexture(IDalamudTextureWrap? textureWrap, out int id);

    /// <summary>Adds an icon from a texture.</summary>
    /// <param name="textureWrap">The texture wrap. If null is provided, a placeholder will be added.</param>
    /// <param name="uv0">The relative UV0.</param>
    /// <param name="uv1">The relative UV1.</param>
    /// <param name="id">The ID of this texture wrap, in the context of the built string.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    TReturn AppendTexture(IDalamudTextureWrap? textureWrap, Vector2 uv0, Vector2 uv1, out int id);

    /// <summary>Adds a line break.</summary>
    /// <param name="newLineType">The line break type. Multiple values may be specified.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    /// <remarks>Using multiple values are disallowed.</remarks>
    [SpannedParseInstruction(SpannedRecordType.InsertionManualNewLine, false, "br")]
    TReturn AppendLine(NewLineType newLineType = NewLineType.Manual);

    /// <summary>Adds the given UTF-16 char sequence.</summary>
    /// <param name="span">Text to add.</param>
    /// <param name="newLineType">The line break type. Multiple values may be specified.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    /// <remarks>Using multiple values are disallowed.</remarks>
    TReturn AppendLine(
        ReadOnlySpan<char> span,
        NewLineType newLineType = NewLineType.Manual);

    /// <summary>Adds the given UTF-8 byte sequence.</summary>
    /// <param name="span">Text to add.</param>
    /// <param name="newLineType">The line break type. Multiple values may be specified.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    /// <remarks>Using multiple values are disallowed.</remarks>
    TReturn AppendLine(
        ReadOnlySpan<byte> span,
        NewLineType newLineType = NewLineType.Manual);

    /// <summary>Pushes a link to be used from now on.</summary>
    /// <param name="value">The link data. Specify <c>default</c> to disable the lnk.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.Link, false, "link")]
    TReturn PushLink(ReadOnlySpan<byte> value);

    /// <summary>Pops a font link.</summary>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.Link, true, "/link")]
    TReturn PopLink();

    /// <summary>Pushes a font set to be used from now on.</summary>
    /// <param name="fontSet">The font set to use. Use <c>default</c> to use the current ImGui font at the point of
    /// rendering.</param>
    /// <param name="id">The ID of this font set, in the context of the built string.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    TReturn PushFontSet(FontHandleVariantSet fontSet, out int id);

    /// <summary>Pushes a previously added font set to be used from now on.</summary>
    /// <param name="id">The ID of the font set, in the context of the built string. If providing a value not fetched
    /// from <see cref="PushFontSet(FontHandleVariantSet, out int)"/>, avoid making the number too large.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.FontHandleSetIndex, false, "font")]
    TReturn PushFontSet(int id);

    /// <summary>Pops a font set.</summary>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.FontHandleSetIndex, true, "/font")]
    TReturn PopFontSet();

    /// <summary>Pushes a float value indicating the font size to use from now on.</summary>
    /// <param name="value">The font size.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.FontSize, false, "size")]
    TReturn PushFontSize(float value);

    /// <summary>Pops a float value indicating the font size to use from now on.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.FontSize, true, "/size")]
    TReturn PopFontSize();

    /// <summary>Pushes a float value indicating the line height to use from now on.</summary>
    /// <param name="value">The line height.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.LineHeight, false, "lh", "line-height")]
    TReturn PushLineHeight(float value);

    /// <summary>Pops a float value indicating the line height to use from now on.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.LineHeight, true, "/lh", "/line-height")]
    TReturn PopLineHeight();

    /// <summary>Pushes a float value indicating the line offset to use from now on.</summary>
    /// <param name="value">The line offset.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.HorizontalOffset, false, "ho", "horizontal-offset")]
    TReturn PushHorizontalOffset(float value);

    /// <summary>Pops a float value indicating the line offset to use from now on.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.HorizontalOffset, true, "/ho", "/horizontal-offset")]
    TReturn PopHorizontalOffset();

    /// <summary>Pushes a Horizontal alignment mode to use from now on.</summary>
    /// <param name="value">The line offset.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.HorizontalAlignment, false, "ha", "horizontal-align")]
    TReturn PushHorizontalAlignment(HorizontalAlignment value);

    /// <summary>Pops a Horizontal alignment mode to use from now on.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.HorizontalAlignment, true, "/ha", "/horizontal-align")]
    TReturn PopHorizontalAlignment();

    /// <summary>Pushes a float value indicating the line offset to use from now on.</summary>
    /// <param name="value">The line offset.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.VerticalOffset, false, "vo", "vertical-offset")]
    TReturn PushVerticalOffset(float value);

    /// <summary>Pops a float value indicating the line offset to use from now on.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.VerticalOffset, true, "/vo", "/vertical-offset")]
    TReturn PopVerticalOffset();

    /// <summary>Pushes a vertical alignment mode to use from now on.</summary>
    /// <param name="value">The line offset.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.VerticalAlignment, false, "va", "vertical-align")]
    TReturn PushVerticalAlignment(VerticalAlignment value);

    /// <summary>Pops a vertical alignment mode to use from now on.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.VerticalAlignment, true, "/va", "/vertical-align")]
    TReturn PopVerticalAlignment();

    /// <summary>Pushes a boolean value indicating whether to use italics from now on.</summary>
    /// <param name="mode">Whether to use italics.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.Italic, false, "i", "italic")]
    TReturn PushItalic(BoolOrToggle mode = BoolOrToggle.Change);

    /// <inheritdoc cref="PushItalic(BoolOrToggle)"/>
    [SpannedParseInstruction(SpannedRecordType.Italic, false, "i", "italic")]
    TReturn PushItalic(bool mode);

    /// <summary>Pops a boolean value indicating whether to use italics from now on.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.Italic, true, "/i", "/italic")]
    TReturn PopItalic();

    /// <summary>Pushes a boolean value indicating whether to use bolds from now on.</summary>
    /// <param name="mode">Whether to use bolds. If <c>null</c>, then this function will toggle the state.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.Bold, false, "b", "bold")]
    TReturn PushBold(BoolOrToggle mode = BoolOrToggle.Change);

    /// <inheritdoc cref="PushBold(BoolOrToggle)"/>
    [SpannedParseInstruction(SpannedRecordType.Bold, false, "b", "bold")]
    TReturn PushBold(bool mode);

    /// <summary>Pops a boolean value indicating whether to use bolds from now on.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.Bold, true, "/b", "/bold")]
    TReturn PopBold();

    /// <summary>Pushes a text decoration to use from now on.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.TextDecoration, false, "td", "text-decoration")]
    TReturn PushTextDecoration(TextDecoration value);
    
    /// <summary>Pops a text decoration.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.TextDecoration, true, "/td", "/text-decoration")]
    TReturn PopTextDecoration();

    /// <summary>Pushes a text decoration style to use from now on.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.TextDecorationStyle, false, "tds", "text-decoration-style")]
    TReturn PushTextDecorationStyle(TextDecorationStyle value);
    
    /// <summary>Pops a text decoration style.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.TextDecorationStyle, true, "/tds", "/text-decoration-style")]
    TReturn PopTextDecorationStyle();

    /// <summary>Pushes a background color to use from now on.</summary>
    /// <param name="color">The new color.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.BackColor, false, "bc", "back-color", "background-color")]
    TReturn PushBackColor(Rgba32 color);

    /// <summary>Pops a background color.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.BackColor, true, "/bc", "/back-color", "/background-color")]
    TReturn PopBackColor();

    /// <summary>Pushes a shadow color to use from now on.</summary>
    /// <param name="color">The new color.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.ShadowColor, false, "sc", "shadow-color")]
    TReturn PushShadowColor(Rgba32 color);

    /// <summary>Pops a shadow color.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.ShadowColor, true, "/sc", "/shadow-color")]
    TReturn PopShadowColor();

    /// <summary>Pushes an edge color to use from now on.</summary>
    /// <param name="color">The new color.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.EdgeColor, false, "ec", "edge-color")]
    TReturn PushEdgeColor(Rgba32 color);

    /// <summary>Pops an edge color.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.EdgeColor, true, "/ec", "/edge-color")]
    TReturn PopEdgeColor();

    /// <summary>Pushes an text decoration color to use from now on.</summary>
    /// <param name="color">The new color.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.TextDecorationColor, false, "tdc", "text-decoration-color")]
    TReturn PushTextDecorationColor(Rgba32 color);

    /// <summary>Pops an text decoration color.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.TextDecorationColor, true, "/tdc", "/text-decoration-color")]
    TReturn PopTextDecorationColor();

    /// <summary>Pushes a foreground color to use from now on.</summary>
    /// <param name="color">The new color.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.ForeColor, false, "fc", "fore-color", "foreground-color")]
    TReturn PushForeColor(Rgba32 color);

    /// <summary>Pops a foreground color.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.ForeColor, true, "/fc", "/fore-color", "/foreground-color")]
    TReturn PopForeColor();

    /// <summary>Pushes a border width (thickness) to use from now on.</summary>
    /// <param name="value">The new width (thickness).</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.BorderWidth, false, "bw", "border-width")]
    TReturn PushBorderWidth(float value);

    /// <summary>Pops a border width.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.BorderWidth, true, "/bw", "/border-width")]
    TReturn PopBorderWidth();

    /// <summary>Pushes a shadow offset to use from now on.</summary>
    /// <param name="value">The new shadow offset.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.ShadowOffset, false, "so", "shadow-offset")]
    TReturn PushShadowOffset(Vector2 value);

    /// <summary>Pops a shadow offset.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.ShadowOffset, true, "/so", "/shadow-offset")]
    TReturn PopShadowOffset();

    /// <summary>Pushes a text decoration stroke thickness to use from now on.</summary>
    /// <param name="value">The new thickness.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.TextDecorationThickness, false, "tdt", "text-decoration-thickness")]
    TReturn PushTextDecorationThickness(float value);

    /// <summary>Pops a text decoration stroke thickness.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.TextDecorationThickness, true, "/tdt", "/text-decoration-thickness")]
    TReturn PopTextDecorationThickness();

    /// <summary>Pushes all values from the given span style.</summary>
    /// <param name="value">The new span styles.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    TReturn PushAll(in SpanStyle value);

    /// <summary>Pops all values pushed from <see cref="PushAll"/>.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    TReturn PopAll();

    /// <summary>Builds a spannable.</summary>
    /// <returns>The built spannable.</returns>
    SpannedString Build();
}
