using System.Numerics;
using System.Text;

using Dalamud.Game.Text;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility;
using Dalamud.Utility.Text;

namespace Dalamud.Interface.Spannables.Strings.Internal;

/// <summary>A builder for <see cref="SpannedString"/>.</summary>
/// <remarks>This is an interface just for the sake of managing doccomments. Nothing else than
/// <see cref="SpannedStringBuilder"/> is expected to implement this.</remarks>
internal interface ISpannedStringBuilder
{
    /// <summary>Adds the given UTF-16 char sequence.</summary>
    /// <param name="span">Text to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    SpannedStringBuilder Append(ReadOnlySpan<char> span, int repeat = 1);

    /// <summary>Adds the given UTF-8 byte sequence.</summary>
    /// <param name="span">Text to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    SpannedStringBuilder Append(ReadOnlySpan<byte> span, int repeat = 1);

    /// <summary>Adds the given UTF-16 char sequence.</summary>
    /// <param name="memory">Text to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    SpannedStringBuilder Append(ReadOnlyMemory<char> memory, int repeat = 1);

    /// <summary>Adds the given UTF-8 byte sequence.</summary>
    /// <param name="memory">Text to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    SpannedStringBuilder Append(ReadOnlyMemory<byte> memory, int repeat = 1);

    /// <summary>Adds the given UTF-16 char sequence.</summary>
    /// <param name="text">Text to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    SpannedStringBuilder Append(string? text, int repeat = 1);

    /// <summary>Adds the given unicode sequence.</summary>
    /// <param name="utfEnumerator">A codepoint enumerator.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    SpannedStringBuilder Append(UtfEnumerator utfEnumerator, int repeat = 1);

    /// <summary>Adds the string representation of the given value.</summary>
    /// <param name="value">The value to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    SpannedStringBuilder Append(ISpanFormattable? value, int repeat = 1);

    /// <summary>Adds the string representation of the given value.</summary>
    /// <param name="value">The value to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    SpannedStringBuilder Append(IUtf8SpanFormattable? value, int repeat = 1);

    /// <summary>Adds the string representation of the given value.</summary>
    /// <param name="value">The value to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    SpannedStringBuilder Append(object? value, int repeat = 1);

    /// <summary>Appends the given UTF-16 codepoint.</summary>
    /// <param name="value">The character to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    SpannedStringBuilder Append(char value, int repeat = 1);

    /// <summary>Adds the given value.</summary>
    /// <param name="value">The value to add.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    SpannedStringBuilder Append<T>(T value, int repeat = 1) where T : struct;

    /// <summary>Adds a callback to be called upon rendering.</summary>
    /// <param name="callback">The callback to be called. If null is provided, a placeholder will be added.</param>
    /// <param name="id">The ID of this texture wrap, in the context of the built string.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    SpannedStringBuilder AppendSpannable(ISpannable? callback, out int id);

    /// <summary>Adds a callback to be called upon rendering.</summary>
    /// <param name="id">The ID of the callback, in the context of the built string. If providing a value not fetched
    /// from <see cref="AppendSpannable(ISpannable?, out int)"/>, avoid making the number too large.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.ObjectSpannable, false, "sp", "spannable")]
    SpannedStringBuilder AppendSpannable(int id);

    /// <summary>Adds a codepoint.</summary>
    /// <param name="codepoint">The codepoint.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    SpannedStringBuilder AppendChar(int codepoint, int repeat = 1);

    /// <summary>Adds a rune.</summary>
    /// <param name="rune">The rune.</param>
    /// <param name="repeat">Number of times to repeat.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    SpannedStringBuilder AppendChar(Rune rune, int repeat = 1);

    /// <summary>Adds an icon.</summary>
    /// <param name="iconId">The icon ID.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.ObjectIcon, false, "icon")]
    SpannedStringBuilder AppendIcon(int iconId);

    /// <inheritdoc cref="AppendIcon(int)"/>
    [SpannedParseInstruction(SpannedRecordType.ObjectIcon, false, "icon")]
    SpannedStringBuilder AppendIcon(GfdIcon iconId);

    /// <summary>Adds an icon from a texture.</summary>
    /// <param name="id">The ID of the texture, in the context of the built string. If providing a value not fetched
    /// from <see cref="AppendTexture(IDalamudTextureWrap?, out int)"/>, avoid making the number too large.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.ObjectTexture, false, "tex")]
    SpannedStringBuilder AppendTexture(int id);

    /// <summary>Adds an icon from a texture.</summary>
    /// <param name="id">The ID of the texture, in the context of the built string. If providing a value not fetched
    /// from <see cref="AppendTexture(IDalamudTextureWrap?, out int)"/>, avoid making the number too large.</param>
    /// <param name="uv0">The relative UV0.</param>
    /// <param name="uv1">The relative UV1.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.ObjectTexture, false, "tex")]
    SpannedStringBuilder AppendTexture(int id, Vector2 uv0, Vector2 uv1);

    /// <summary>Adds an icon from a texture.</summary>
    /// <param name="textureWrap">The texture wrap. If null is provided, a placeholder will be added.</param>
    /// <param name="id">The ID of this texture wrap, in the context of the built string.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    SpannedStringBuilder AppendTexture(IDalamudTextureWrap? textureWrap, out int id);

    /// <summary>Adds an icon from a texture.</summary>
    /// <param name="textureWrap">The texture wrap. If null is provided, a placeholder will be added.</param>
    /// <param name="uv0">The relative UV0.</param>
    /// <param name="uv1">The relative UV1.</param>
    /// <param name="id">The ID of this texture wrap, in the context of the built string.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    SpannedStringBuilder AppendTexture(IDalamudTextureWrap? textureWrap, Vector2 uv0, Vector2 uv1, out int id);

    /// <summary>Adds a line break.</summary>
    /// <param name="newLineType">The line break type. Multiple values may be specified.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    /// <remarks>Using multiple values are disallowed.</remarks>
    [SpannedParseInstruction(SpannedRecordType.ObjectNewLine, false, "br")]
    SpannedStringBuilder AppendLine(NewLineType newLineType = NewLineType.Manual);

    /// <summary>Adds the given UTF-16 char sequence.</summary>
    /// <param name="span">Text to add.</param>
    /// <param name="newLineType">The line break type. Multiple values may be specified.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    /// <remarks>Using multiple values are disallowed.</remarks>
    SpannedStringBuilder AppendLine(
        ReadOnlySpan<char> span,
        NewLineType newLineType = NewLineType.Manual);

    /// <summary>Adds the given UTF-8 byte sequence.</summary>
    /// <param name="span">Text to add.</param>
    /// <param name="newLineType">The line break type. Multiple values may be specified.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    /// <remarks>Using multiple values are disallowed.</remarks>
    SpannedStringBuilder AppendLine(
        ReadOnlySpan<byte> span,
        NewLineType newLineType = NewLineType.Manual);

    /// <summary>Pushes a link to be used from now on.</summary>
    /// <param name="value">The link data. Specify <c>default</c> to disable the lnk.</param>
    /// <returns>A reference of this instance after the append operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.Link, false, "link")]
    SpannedStringBuilder PushLink(ReadOnlySpan<byte> value);

    /// <summary>Pops a font link.</summary>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.Link, true, "/link")]
    SpannedStringBuilder PopLink();

    /// <summary>Pushes the Dalamud default font to be used from now on.</summary>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.FontHandleSetIndex, false, "font-default")]
    SpannedStringBuilder PushDefaultFontFamily();

    /// <summary>Pushes a Dalamud asset font to be used from now on.</summary>
    /// <param name="asset">The asset font.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.FontHandleSetIndex, false, "font-asset")]
    SpannedStringBuilder PushAssetFontFamily(DalamudAsset asset);

    /// <summary>Pushes a game font family to be used from now on.</summary>
    /// <param name="family">The game font family.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.FontHandleSetIndex, false, "font-game")]
    SpannedStringBuilder PushGameFontFamily(GameFontFamily family);

    /// <summary>Pushes a system font family to be used from now on.</summary>
    /// <param name="name">The system font family name.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    /// <remarks>If a corresponding system font does not exist, then an empty instance of
    /// <see cref="FontHandleVariantSet"/> will be pushed.</remarks>
    [SpannedParseInstruction(SpannedRecordType.FontHandleSetIndex, false, "font-system")]
    SpannedStringBuilder PushSystemFontFamilyIfAvailable(string name);

    /// <summary>Pushes a font family to be used from now on.</summary>
    /// <param name="family">The font family.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.FontHandleSetIndex, false, "font-family")]
    SpannedStringBuilder PushFontFamily(IFontFamilyId family);

    /// <summary>Pushes a font set to be used from now on.</summary>
    /// <param name="fontSet">The font set to use. Use <c>default</c> to use the current ImGui font at the point of
    /// rendering.</param>
    /// <param name="id">The ID of this font set, in the context of the built string.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    SpannedStringBuilder PushFontSet(FontHandleVariantSet fontSet, out int id);

    /// <summary>Pushes a previously added font set to be used from now on.</summary>
    /// <param name="id">The ID of the font set, in the context of the built string. If providing a value not fetched
    /// from <see cref="PushFontSet(FontHandleVariantSet, out int)"/>, avoid making the number too large.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.FontHandleSetIndex, false, "font")]
    SpannedStringBuilder PushFontSet(int id);

    /// <summary>Pops a font set.</summary>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.FontHandleSetIndex, true, "/font")]
    SpannedStringBuilder PopFontSet();

    /// <summary>Pushes a float value indicating the font size to use from now on.</summary>
    /// <param name="value">The font size.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    /// <remarks>See <see cref="TextStyle.FontSize"/> for meaning of values.</remarks>
    [SpannedParseInstruction(SpannedRecordType.FontSize, false, "size")]
    SpannedStringBuilder PushFontSize(float value);

    /// <summary>Pops a float value indicating the font size to use from now on.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.FontSize, true, "/size")]
    SpannedStringBuilder PopFontSize();

    /// <summary>Pushes a float value indicating the line height to use from now on.</summary>
    /// <param name="value">The line height.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.LineHeight, false, "lh", "line-height")]
    SpannedStringBuilder PushLineHeight(float value);

    /// <summary>Pops a float value indicating the line height to use from now on.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.LineHeight, true, "/lh", "/line-height")]
    SpannedStringBuilder PopLineHeight();

    /// <summary>Pushes a float value indicating the line offset to use from now on.</summary>
    /// <param name="value">The line offset.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.HorizontalOffset, false, "ho", "horizontal-offset")]
    SpannedStringBuilder PushHorizontalOffset(float value);

    /// <summary>Pops a float value indicating the line offset to use from now on.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.HorizontalOffset, true, "/ho", "/horizontal-offset")]
    SpannedStringBuilder PopHorizontalOffset();

    /// <summary>Pushes a horizontal alignment mode to use from now on, with respect to the whole alloted region
    /// specified from <see cref="RenderContext.MaxSize"/>, or <see cref="ISpannableRenderPass.Boundary"/> if no maximum
    /// width is specified.</summary>
    /// <param name="value">The horizontal alignment.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.HorizontalAlignment, false, "ha", "horizontal-align")]
    SpannedStringBuilder PushHorizontalAlignment(float value);

    /// <summary>Pushes a horizontal alignment mode to use from now on, with respect to the whole alloted region
    /// specified from <see cref="RenderContext.MaxSize"/>, or <see cref="ISpannableRenderPass.Boundary"/> if no maximum
    /// width is specified.</summary>
    /// <param name="value">The horizontal alignment.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.HorizontalAlignment, false, "ha", "horizontal-align")]
    SpannedStringBuilder PushHorizontalAlignment(HorizontalAlignment value);

    /// <summary>Pops a horizontal alignment mode to use from now on, with respect to the whole alloted region
    /// specified from <see cref="RenderContext.MaxSize"/>, or <see cref="ISpannableRenderPass.Boundary"/> if no maximum
    /// width is specified.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.HorizontalAlignment, true, "/ha", "/horizontal-align")]
    SpannedStringBuilder PopHorizontalAlignment();

    /// <summary>Pushes a float value indicating the line offset to use from now on.</summary>
    /// <param name="value">The line offset.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.VerticalOffset, false, "vo", "vertical-offset")]
    SpannedStringBuilder PushVerticalOffset(float value);

    /// <summary>Pops a float value indicating the line offset to use from now on.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.VerticalOffset, true, "/vo", "/vertical-offset")]
    SpannedStringBuilder PopVerticalOffset();

    /// <summary>Pushes a vertical alignment mode to use from now on, with respect to the current line.</summary>
    /// <param name="value">The vertical alignment.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.VerticalAlignment, false, "va", "vertical-align")]
    SpannedStringBuilder PushVerticalAlignment(float value);

    /// <summary>Pushes a vertical alignment mode to use from now on, with respect to the current line.</summary>
    /// <param name="value">The vertical alignment.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.VerticalAlignment, false, "va", "vertical-align")]
    SpannedStringBuilder PushVerticalAlignment(VerticalAlignment value);

    /// <summary>Pops a vertical alignment mode to use from now on, with respect to the current line.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.VerticalAlignment, true, "/va", "/vertical-align")]
    SpannedStringBuilder PopVerticalAlignment();

    /// <summary>Pushes a boolean value indicating whether to use italics from now on.</summary>
    /// <param name="mode">Whether to use italics.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.Italic, false, "i", "italic")]
    SpannedStringBuilder PushItalic(BoolOrToggle mode = BoolOrToggle.Change);

    /// <inheritdoc cref="PushItalic(BoolOrToggle)"/>
    [SpannedParseInstruction(SpannedRecordType.Italic, false, "i", "italic")]
    SpannedStringBuilder PushItalic(bool mode);

    /// <summary>Pops a boolean value indicating whether to use italics from now on.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.Italic, true, "/i", "/italic")]
    SpannedStringBuilder PopItalic();

    /// <summary>Pushes a boolean value indicating whether to use bolds from now on.</summary>
    /// <param name="mode">Whether to use bolds. If <c>null</c>, then this function will toggle the state.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.Bold, false, "b", "bold")]
    SpannedStringBuilder PushBold(BoolOrToggle mode = BoolOrToggle.Change);

    /// <inheritdoc cref="PushBold(BoolOrToggle)"/>
    [SpannedParseInstruction(SpannedRecordType.Bold, false, "b", "bold")]
    SpannedStringBuilder PushBold(bool mode);

    /// <summary>Pops a boolean value indicating whether to use bolds from now on.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.Bold, true, "/b", "/bold")]
    SpannedStringBuilder PopBold();

    /// <summary>Pushes a text decoration to use from now on.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.TextDecoration, false, "td", "text-decoration")]
    SpannedStringBuilder PushTextDecoration(TextDecoration value);

    /// <summary>Pops a text decoration.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.TextDecoration, true, "/td", "/text-decoration")]
    SpannedStringBuilder PopTextDecoration();

    /// <summary>Pushes a text decoration style to use from now on.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.TextDecorationStyle, false, "tds", "text-decoration-style")]
    SpannedStringBuilder PushTextDecorationStyle(TextDecorationStyle value);

    /// <summary>Pops a text decoration style.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.TextDecorationStyle, true, "/tds", "/text-decoration-style")]
    SpannedStringBuilder PopTextDecorationStyle();

    /// <summary>Pushes a background color to use from now on.</summary>
    /// <param name="color">The new color.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.BackColor, false, "bc", "back-color", "background-color")]
    SpannedStringBuilder PushBackColor(Rgba32 color);

    /// <summary>Pops a background color.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.BackColor, true, "/bc", "/back-color", "/background-color")]
    SpannedStringBuilder PopBackColor();

    /// <summary>Pushes a shadow color to use from now on.</summary>
    /// <param name="color">The new color.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.ShadowColor, false, "sc", "shadow-color")]
    SpannedStringBuilder PushShadowColor(Rgba32 color);

    /// <summary>Pops a shadow color.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.ShadowColor, true, "/sc", "/shadow-color")]
    SpannedStringBuilder PopShadowColor();

    /// <summary>Pushes an edge color to use from now on.</summary>
    /// <param name="color">The new color.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.EdgeColor, false, "ec", "edge-color")]
    SpannedStringBuilder PushEdgeColor(Rgba32 color);

    /// <summary>Pops an edge color.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.EdgeColor, true, "/ec", "/edge-color")]
    SpannedStringBuilder PopEdgeColor();

    /// <summary>Pushes an text decoration color to use from now on.</summary>
    /// <param name="color">The new color.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.TextDecorationColor, false, "tdc", "text-decoration-color")]
    SpannedStringBuilder PushTextDecorationColor(Rgba32 color);

    /// <summary>Pops an text decoration color.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.TextDecorationColor, true, "/tdc", "/text-decoration-color")]
    SpannedStringBuilder PopTextDecorationColor();

    /// <summary>Pushes a foreground color to use from now on.</summary>
    /// <param name="color">The new color.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.ForeColor, false, "fc", "fore-color", "foreground-color")]
    SpannedStringBuilder PushForeColor(Rgba32 color);

    /// <summary>Pops a foreground color.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.ForeColor, true, "/fc", "/fore-color", "/foreground-color")]
    SpannedStringBuilder PopForeColor();

    /// <summary>Pushes a border width (thickness) to use from now on.</summary>
    /// <param name="value">The new width (thickness).</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.EdgeWidth, false, "ew", "edge-width")]
    SpannedStringBuilder PushEdgeWidth(float value);

    /// <summary>Pops a border width.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.EdgeWidth, true, "/ew", "/edge-width")]
    SpannedStringBuilder PopEdgeWidth();

    /// <summary>Pushes a shadow offset to use from now on.</summary>
    /// <param name="value">The new shadow offset.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.ShadowOffset, false, "so", "shadow-offset")]
    SpannedStringBuilder PushShadowOffset(Vector2 value);

    /// <summary>Pops a shadow offset.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.ShadowOffset, true, "/so", "/shadow-offset")]
    SpannedStringBuilder PopShadowOffset();

    /// <summary>Pushes a text decoration stroke thickness to use from now on.</summary>
    /// <param name="value">The new thickness.</param>
    /// <returns>A reference of this instance after the push operation is completed.</returns>
    [SpannedParseInstruction(SpannedRecordType.TextDecorationThickness, false, "tdt", "text-decoration-thickness")]
    SpannedStringBuilder PushTextDecorationThickness(float value);

    /// <summary>Pops a text decoration stroke thickness.</summary>
    /// <returns>A reference of this instance after the pop operation is completed.</returns>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    [SpannedParseInstruction(SpannedRecordType.TextDecorationThickness, true, "/tdt", "/text-decoration-thickness")]
    SpannedStringBuilder PopTextDecorationThickness();

    /// <summary>Clears everything.</summary>
    /// <returns>A reference of this instance after the clear operation is completed.</returns>
    SpannedStringBuilder Clear();

    /// <summary>Builds a spannable.</summary>
    /// <returns>The built spannable.</returns>
    SpannedString Build();
}
