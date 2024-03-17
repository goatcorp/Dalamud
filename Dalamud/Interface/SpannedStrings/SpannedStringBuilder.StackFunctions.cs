using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Interface.SpannedStrings.Internal;
using Dalamud.Interface.SpannedStrings.Styles;

namespace Dalamud.Interface.SpannedStrings;

/// <summary>A custom text renderer implementation.</summary>
public sealed partial class SpannedStringBuilder
{
    /// <inheritdoc/>
    public SpannedStringBuilder PushLink(ReadOnlySpan<byte> value)
    {
        var len = SpannedRecordCodec.EncodeLink(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.Link, len, out var data);
        SpannedRecordCodec.EncodeLink(data, value);
        return this.PushHelper(ref this.stackLink, recordIndex);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PopLink() =>
        this.PopHelper(this.stackLink, SpannedRecordType.Link);

    /// <inheritdoc/>
    public SpannedStringBuilder PushFontSize(float value)
    {
        var len = SpannedRecordCodec.EncodeFontSize(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.FontSize, len, out var data);
        SpannedRecordCodec.EncodeFontSize(data, value);
        return this.PushHelper(ref this.stackFontSize, recordIndex);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PopFontSize() =>
        this.PopHelper(this.stackFontSize, SpannedRecordType.FontSize);

    /// <inheritdoc/>
    public SpannedStringBuilder PushFontSet(FontHandleVariantSet fontSet, out int id)
    {
        id = this.fontSets.Count;
        this.fontSets.Add(fontSet);
        return this.PushFontSet(id);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PushFontSet(int id)
    {
        this.fontSets.EnsureCapacity(id + 1);
        while (this.fontSets.Count <= id)
            this.fontSets.Add(default);
        var len = SpannedRecordCodec.EncodeFontHandleSetIndex(default, id);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.FontHandleSetIndex, len, out var data);
        SpannedRecordCodec.EncodeFontHandleSetIndex(data, id);
        return this.PushHelper(ref this.stackFont, recordIndex);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PopFontSet() =>
        this.PopHelper(this.stackFont, SpannedRecordType.FontHandleSetIndex);

    /// <inheritdoc/>
    public SpannedStringBuilder PushLineHeight(float value)
    {
        var len = SpannedRecordCodec.EncodeLineHeight(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.LineHeight, len, out var data);
        SpannedRecordCodec.EncodeLineHeight(data, value);
        return this.PushHelper(ref this.stackLineHeight, recordIndex);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PopLineHeight() =>
        this.PopHelper(this.stackLineHeight, SpannedRecordType.LineHeight);

    /// <inheritdoc/>
    public SpannedStringBuilder PushHorizontalOffset(float value)
    {
        var len = SpannedRecordCodec.EncodeHorizontalOffset(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.HorizontalOffset, len, out var data);
        SpannedRecordCodec.EncodeHorizontalOffset(data, value);
        return this.PushHelper(ref this.stackHorizontalOffset, recordIndex);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PopHorizontalOffset() =>
        this.PopHelper(this.stackHorizontalOffset, SpannedRecordType.HorizontalOffset);

    /// <inheritdoc/>
    public SpannedStringBuilder PushHorizontalAlignment(HorizontalAlignment value)
    {
        var len = SpannedRecordCodec.EncodeHorizontalAlignment(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.HorizontalAlignment, len, out var data);
        SpannedRecordCodec.EncodeHorizontalAlignment(data, value);
        return this.PushHelper(ref this.stackHorizontalAlignment, recordIndex);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PopHorizontalAlignment() =>
        this.PopHelper(this.stackHorizontalAlignment, SpannedRecordType.HorizontalAlignment);

    /// <inheritdoc/>
    public SpannedStringBuilder PushVerticalOffset(float value)
    {
        var len = SpannedRecordCodec.EncodeVerticalOffset(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.VerticalOffset, len, out var data);
        SpannedRecordCodec.EncodeVerticalOffset(data, value);
        return this.PushHelper(ref this.stackVerticalOffset, recordIndex);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PopVerticalOffset() =>
        this.PopHelper(this.stackVerticalOffset, SpannedRecordType.VerticalOffset);

    /// <inheritdoc/>
    public SpannedStringBuilder PushVerticalAlignment(VerticalAlignment value)
    {
        var len = SpannedRecordCodec.EncodeVerticalAlignment(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.VerticalAlignment, len, out var data);
        SpannedRecordCodec.EncodeVerticalAlignment(data, value);
        return this.PushHelper(ref this.stackVerticalAlignment, recordIndex);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PopVerticalAlignment() =>
        this.PopHelper(this.stackVerticalAlignment, SpannedRecordType.VerticalAlignment);

    /// <inheritdoc/>
    public SpannedStringBuilder PushItalic(BoolOrToggle mode = BoolOrToggle.Change)
    {
        mode = ResolveToggleValue(this.stackItalicMode, mode);
        var len = SpannedRecordCodec.EncodeItalic(default, mode);
        this.AddRecordAndReserveData(SpannedRecordType.Italic, len, out var data);
        SpannedRecordCodec.EncodeItalic(data, mode);
        return this.PushHelper(ref this.stackItalicMode, mode);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PushItalic(bool mode) => this.PushItalic(mode ? BoolOrToggle.On : BoolOrToggle.Off);

    /// <inheritdoc/>
    public SpannedStringBuilder PopItalic()
    {
        if (this.PopHelper(this.stackItalicMode, SpannedRecordType.Italic, out var mode))
        {
            var len = SpannedRecordCodec.EncodeItalic(default, mode);
            this.AddRecordAndReserveData(SpannedRecordType.Italic, len, out var data);
            SpannedRecordCodec.EncodeItalic(data, mode);
        }

        return this;
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PushBold(BoolOrToggle mode = BoolOrToggle.Change)
    {
        mode = ResolveToggleValue(this.stackBoldMode, mode);
        var len = SpannedRecordCodec.EncodeBold(default, mode);
        this.AddRecordAndReserveData(SpannedRecordType.Bold, len, out var data);
        SpannedRecordCodec.EncodeBold(data, mode);
        return this.PushHelper(ref this.stackBoldMode, mode);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PushBold(bool mode) => this.PushBold(mode ? BoolOrToggle.On : BoolOrToggle.Off);

    /// <inheritdoc/>
    public SpannedStringBuilder PopBold()
    {
        if (this.PopHelper(this.stackBoldMode, SpannedRecordType.Bold, out var mode))
        {
            var len = SpannedRecordCodec.EncodeBold(default, mode);
            this.AddRecordAndReserveData(SpannedRecordType.Bold, len, out var data);
            SpannedRecordCodec.EncodeBold(data, mode);
        }

        return this;
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PushBackColor(Rgba32 color)
    {
        var len = SpannedRecordCodec.EncodeBackColor(default, color);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.BackColor, len, out var data);
        SpannedRecordCodec.EncodeBackColor(data, color);
        return this.PushHelper(ref this.stackBackColor, recordIndex);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PopBackColor() =>
        this.PopHelper(this.stackBackColor, SpannedRecordType.BackColor);

    /// <inheritdoc/>
    public SpannedStringBuilder PushShadowColor(Rgba32 color)
    {
        var len = SpannedRecordCodec.EncodeShadowColor(default, color);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.ShadowColor, len, out var data);
        SpannedRecordCodec.EncodeShadowColor(data, color);
        return this.PushHelper(ref this.stackShadowColor, recordIndex);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PopShadowColor() =>
        this.PopHelper(this.stackShadowColor, SpannedRecordType.ShadowColor);

    /// <inheritdoc/>
    public SpannedStringBuilder PushEdgeColor(Rgba32 color)
    {
        var len = SpannedRecordCodec.EncodeEdgeColor(default, color);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.EdgeColor, len, out var data);
        SpannedRecordCodec.EncodeEdgeColor(data, color);
        return this.PushHelper(ref this.stackEdgeColor, recordIndex);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PopEdgeColor() =>
        this.PopHelper(this.stackEdgeColor, SpannedRecordType.EdgeColor);

    /// <inheritdoc/>
    public SpannedStringBuilder PushForeColor(Rgba32 color)
    {
        var len = SpannedRecordCodec.EncodeForeColor(default, color);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.ForeColor, len, out var data);
        SpannedRecordCodec.EncodeForeColor(data, color);
        return this.PushHelper(ref this.stackForeCoor, recordIndex);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PopForeColor() =>
        this.PopHelper(this.stackForeCoor, SpannedRecordType.ForeColor);

    /// <inheritdoc/>
    public SpannedStringBuilder PushBorderWidth(float value)
    {
        var len = SpannedRecordCodec.EncodeBorderWidth(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.BorderWidth, len, out var data);
        SpannedRecordCodec.EncodeBorderWidth(data, value);
        return this.PushHelper(ref this.stackBorderWidth, recordIndex);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PopBorderWidth() =>
        this.PopHelper(this.stackBorderWidth, SpannedRecordType.BorderWidth);

    /// <inheritdoc/>
    public SpannedStringBuilder PushShadowOffset(Vector2 value)
    {
        var len = SpannedRecordCodec.EncodeShadowOffset(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.ShadowOffset, len, out var data);
        SpannedRecordCodec.EncodeShadowOffset(data, value);
        return this.PushHelper(ref this.stackShadowOffset, recordIndex);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder PopShadowOffset() =>
        this.PopHelper(this.stackShadowOffset, SpannedRecordType.ShadowOffset);

    /// <inheritdoc/>
    public SpannedStringBuilder PushAll(in SpanStyle value) =>
        this.PushFontSet(value.Font, out _)
            .PushItalic(value.Italic)
            .PushBold(value.Bold)
            .PushBackColor(value.BackColorU32)
            .PushShadowColor(value.ShadowColorU32)
            .PushEdgeColor(value.EdgeColorU32)
            .PushForeColor(value.ForeColorU32)
            .PushBorderWidth(value.BorderWidth)
            .PushShadowOffset(value.ShadowOffset);

    /// <inheritdoc/>
    public SpannedStringBuilder PopAll() =>
        this.PopFontSet()
            .PopItalic()
            .PopBold()
            .PopBackColor()
            .PopShadowColor()
            .PopEdgeColor()
            .PopForeColor()
            .PopBorderWidth()
            .PopShadowOffset();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BoolOrToggle ResolveToggleValue(Stack<BoolOrToggle>? stack, BoolOrToggle mode) =>
        mode switch
        {
            BoolOrToggle.On => BoolOrToggle.On,
            BoolOrToggle.Off => BoolOrToggle.Off,
            BoolOrToggle.Change => stack?.TryPeek(out var last) is true
                                       ? last switch
                                       {
                                           BoolOrToggle.On => BoolOrToggle.Off,
                                           BoolOrToggle.Off => BoolOrToggle.On,
                                           BoolOrToggle.NoChange => BoolOrToggle.Change,
                                           BoolOrToggle.Change => BoolOrToggle.NoChange,
                                           _ => BoolOrToggle.NoChange,
                                       }
                                       : BoolOrToggle.Change,
            BoolOrToggle.NoChange => BoolOrToggle.NoChange,
            _ => BoolOrToggle.NoChange,
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SpannedStringBuilder PushHelper<T>(ref Stack<T>? stack, T value) where T : unmanaged
    {
        stack ??= new(8);
        stack.Push(value);
        return this;
    }

    /// <summary>Pops a state from a stack.</summary>
    /// <param name="stack">The stack.</param>
    /// <param name="spannedRecordType">The record type.</param>
    /// <returns><c>this</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SpannedStringBuilder PopHelper(Stack<int>? stack, SpannedRecordType spannedRecordType)
    {
        if (stack is null)
            return this;

        stack.TryPop(out _);
        if (stack.TryPeek(out var val))
            this.AddRecordCopy(val);
        else
            this.AddRecordRevert(spannedRecordType);

        return this;
    }

    /// <summary>Pops a state from a stack.</summary>
    /// <param name="stack">The stack.</param>
    /// <param name="spannedRecordType">The record type.</param>
    /// <param name="prev">The previous value that has to be written.</param>
    /// <returns><c>true</c> if restoring record has to be written.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool PopHelper(Stack<BoolOrToggle>? stack, SpannedRecordType spannedRecordType, out BoolOrToggle prev)
    {
        prev = default;
        if (stack is null)
            return false;

        stack.TryPop(out _);
        if (stack.TryPeek(out var val))
        {
            prev = val;
            return true;
        }

        this.AddRecordRevert(spannedRecordType);
        return false;
    }
}
