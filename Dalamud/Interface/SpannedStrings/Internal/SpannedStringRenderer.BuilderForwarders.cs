using System.Numerics;

using Dalamud.Game.Text;
using Dalamud.Interface.Internal;
using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Interface.SpannedStrings.Styles;

namespace Dalamud.Interface.SpannedStrings.Internal;

/// <summary>A custom text renderer implementation.</summary>
internal sealed partial class SpannedStringRenderer
{
    /// <inheritdoc/>
    public ISpannedStringRenderer Append(ReadOnlySpan<char> span, int repeat = 1)
    {
        this.builder.Append(span, repeat);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer Append(ReadOnlySpan<byte> span, int repeat = 1)
    {
        this.builder.Append(span, repeat);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer Append(ReadOnlyMemory<char> memory, int repeat = 1)
    {
        this.builder.Append(memory, repeat);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer Append(ReadOnlyMemory<byte> memory, int repeat = 1)
    {
        this.builder.Append(memory, repeat);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer Append(string? text, int repeat = 1)
    {
        this.builder.Append(text, repeat);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer Append(ISpanFormattable? value, int repeat = 1)
    {
        this.builder.Append(value, repeat);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer Append(object? value, int repeat = 1)
    {
        this.builder.Append(value, repeat);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer Append(char value, int repeat = 1)
    {
        this.builder.Append(value, repeat);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer Append<T>(T value, int repeat = 1) where T : struct
    {
        this.builder.Append(value, repeat);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer AppendCallback(SpannedStringCallbackDelegate? callback, float sizeRatio, out int id)
    {
        this.builder.AppendCallback(callback, sizeRatio, out id);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer AppendCallback(int id, float sizeRatio)
    {
        this.builder.AppendCallback(id, sizeRatio);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer AppendChar(int codepoint, int repeat = 1)
    {
        this.builder.AppendChar(codepoint, repeat);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer AppendIconGfd(GfdIcon iconId)
    {
        this.builder.AppendIconGfd(iconId);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer AppendTexture(int id)
    {
        this.builder.AppendTexture(id);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer AppendTexture(int id, Vector2 uv0, Vector2 uv1)
    {
        this.builder.AppendTexture(id, uv0, uv1);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer AppendTexture(IDalamudTextureWrap? textureWrap, out int id)
    {
        this.builder.AppendTexture(textureWrap, out id);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer AppendTexture(
        IDalamudTextureWrap? textureWrap, Vector2 uv0, Vector2 uv1, out int id)
    {
        this.builder.AppendTexture(textureWrap, uv0, uv1, out id);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer AppendLine(NewLineType newLineType = NewLineType.Manual)
    {
        this.builder.AppendLine(newLineType);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer AppendLine(ReadOnlySpan<char> span, NewLineType newLineType = NewLineType.Manual)
    {
        this.builder.AppendLine(span, newLineType);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer AppendLine(ReadOnlySpan<byte> span, NewLineType newLineType = NewLineType.Manual)
    {
        this.builder.AppendLine(span, newLineType);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushLink(ReadOnlySpan<byte> value)
    {
        this.builder.PushLink(value);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PopLink()
    {
        this.builder.PopLink();
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushFontSet(FontHandleVariantSet fontSet, out int id)
    {
        this.builder.PushFontSet(fontSet, out id);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushFontSet(int id)
    {
        this.builder.PushFontSet(id);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PopFontSet()
    {
        this.builder.PopFontSet();
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushFontSize(float value)
    {
        this.builder.PushFontSize(value);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PopFontSize()
    {
        this.builder.PopFontSize();
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushLineHeight(float value)
    {
        this.builder.PushLineHeight(value);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PopLineHeight()
    {
        this.builder.PopLineHeight();
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushHorizontalOffset(float value)
    {
        this.builder.PushHorizontalOffset(value);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PopHorizontalOffset()
    {
        this.builder.PopHorizontalOffset();
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushHorizontalAlignment(HorizontalAlignment value)
    {
        this.builder.PushHorizontalAlignment(value);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PopHorizontalAlignment()
    {
        this.builder.PopHorizontalAlignment();
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushVerticalOffset(float value)
    {
        this.builder.PushVerticalOffset(value);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PopVerticalOffset()
    {
        this.builder.PopVerticalOffset();
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushVerticalAlignment(VerticalAlignment value)
    {
        this.builder.PushVerticalAlignment(value);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PopVerticalAlignment()
    {
        this.builder.PopVerticalAlignment();
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushItalic(BoolOrToggle mode = BoolOrToggle.Change)
    {
        this.builder.PushItalic(mode);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushItalic(bool mode)
    {
        this.builder.PushItalic(mode);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PopItalic()
    {
        this.builder.PopItalic();
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushBold(BoolOrToggle mode = BoolOrToggle.Change)
    {
        this.builder.PushBold(mode);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushBold(bool mode)
    {
        this.builder.PushBold(mode);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PopBold()
    {
        this.builder.PopBold();
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushBackColor(Rgba32 color)
    {
        this.builder.PushBackColor(color);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PopBackColor()
    {
        this.builder.PopBackColor();
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushShadowColor(Rgba32 color)
    {
        this.builder.PushShadowColor(color);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PopShadowColor()
    {
        this.builder.PopShadowColor();
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushEdgeColor(Rgba32 color)
    {
        this.builder.PushEdgeColor(color);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PopEdgeColor()
    {
        this.builder.PopEdgeColor();
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushForeColor(Rgba32 color)
    {
        this.builder.PushForeColor(color);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PopForeColor()
    {
        this.builder.PopForeColor();
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushBorderWidth(float value)
    {
        this.builder.PushBorderWidth(value);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PopBorderWidth()
    {
        this.builder.PopBorderWidth();
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushShadowOffset(Vector2 value)
    {
        this.builder.PushShadowOffset(value);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PopShadowOffset()
    {
        this.builder.PopShadowOffset();
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PushAll(in SpanStyle value)
    {
        this.builder.PushAll(in value);
        return this;
    }

    /// <inheritdoc/>
    public ISpannedStringRenderer PopAll()
    {
        this.builder.PopAll();
        return this;
    }

    /// <inheritdoc/>
    public SpannedString Build() => this.builder.Build();
}
