using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility;

namespace Dalamud.Interface.Spannables.Text;

/// <summary>A custom text renderer implementation.</summary>
public sealed partial class TextSpannableBuilder
{
    /// <inheritdoc/>
    public TextSpannableBuilder PushLink(ReadOnlySpan<byte> value)
    {
        var len = SpannedRecordCodec.EncodeLink(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.Link, len, out var data);
        SpannedRecordCodec.EncodeLink(data, value);
        return this.PushHelper(ref this.stackLink, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PopLink() =>
        this.PopHelper(this.stackLink, SpannedRecordType.Link);

    /// <inheritdoc/>
    public TextSpannableBuilder PushFontSize(float value)
    {
        var len = SpannedRecordCodec.EncodeFontSize(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.FontSize, len, out var data);
        SpannedRecordCodec.EncodeFontSize(data, value);
        return this.PushHelper(ref this.stackFontSize, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PopFontSize() =>
        this.PopHelper(this.stackFontSize, SpannedRecordType.FontSize);

    /// <inheritdoc/>
    public TextSpannableBuilder PushDefaultFontFamily() =>
        this.PushFontSet(new(DalamudDefaultFontAndFamilyId.Instance), out _);

    /// <inheritdoc/>
    public TextSpannableBuilder PushAssetFontFamily(DalamudAsset asset) =>
        this.PushFontSet(new(DalamudAssetFontAndFamilyId.From(asset)), out _);

    /// <inheritdoc/>
    public TextSpannableBuilder PushGameFontFamily(GameFontFamily family) =>
        this.PushFontSet(new(GameFontAndFamilyId.From(family)), out _);

    /// <inheritdoc/>
    public TextSpannableBuilder PushSystemFontFamilyIfAvailable(string name) =>
        SystemFontFamilyId.TryGet(name, out var familyId)
            ? this.PushFontSet(new(familyId), out _)
            : this.PushFontSet(default);

    /// <inheritdoc/>
    public TextSpannableBuilder PushFontFamily(IFontFamilyId family) =>
        this.PushFontSet(new(family), out _);

    /// <inheritdoc/>
    public TextSpannableBuilder PushFontSet(FontHandleVariantSet fontSet, out int id)
    {
        id = this.fontSets.Count;
        this.fontSets.Add(fontSet);
        return this.PushFontSet(id);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PushFontSet(int id)
    {
        this.fontSets.EnsureCapacity(id + 1);
        while (this.fontSets.Count <= id)
            this.fontSets.Add(default);
        var len = SpannedRecordCodec.EncodeFontHandleSetIndex(default, id, this.fontSets[id].FontFamilyId);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.FontHandleSetIndex, len, out var data);
        SpannedRecordCodec.EncodeFontHandleSetIndex(data, id, this.fontSets[id].FontFamilyId);
        return this.PushHelper(ref this.stackFont, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PopFontSet() =>
        this.PopHelper(this.stackFont, SpannedRecordType.FontHandleSetIndex);

    /// <inheritdoc/>
    public TextSpannableBuilder PushLineHeight(float value)
    {
        var len = SpannedRecordCodec.EncodeLineHeight(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.LineHeight, len, out var data);
        SpannedRecordCodec.EncodeLineHeight(data, value);
        return this.PushHelper(ref this.stackLineHeight, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PopLineHeight() =>
        this.PopHelper(this.stackLineHeight, SpannedRecordType.LineHeight);

    /// <inheritdoc/>
    public TextSpannableBuilder PushHorizontalOffset(float value)
    {
        var len = SpannedRecordCodec.EncodeHorizontalOffset(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.HorizontalOffset, len, out var data);
        SpannedRecordCodec.EncodeHorizontalOffset(data, value);
        return this.PushHelper(ref this.stackHorizontalOffset, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PopHorizontalOffset() =>
        this.PopHelper(this.stackHorizontalOffset, SpannedRecordType.HorizontalOffset);

    /// <inheritdoc/>
    public TextSpannableBuilder PushHorizontalAlignment(float value)
    {
        var len = SpannedRecordCodec.EncodeHorizontalAlignment(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.HorizontalAlignment, len, out var data);
        SpannedRecordCodec.EncodeHorizontalAlignment(data, value);
        return this.PushHelper(ref this.stackHorizontalAlignment, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PushHorizontalAlignment(HorizontalAlignment value) =>
        this.PushHorizontalAlignment(
            value switch
            {
                HorizontalAlignment.Left => 0f,
                HorizontalAlignment.Center => 0.5f,
                HorizontalAlignment.Right => 1f,
                _ => 0f,
            });

    /// <inheritdoc/>
    public TextSpannableBuilder PopHorizontalAlignment() =>
        this.PopHelper(this.stackHorizontalAlignment, SpannedRecordType.HorizontalAlignment);

    /// <inheritdoc/>
    public TextSpannableBuilder PushVerticalOffset(float value)
    {
        var len = SpannedRecordCodec.EncodeVerticalOffset(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.VerticalOffset, len, out var data);
        SpannedRecordCodec.EncodeVerticalOffset(data, value);
        return this.PushHelper(ref this.stackVerticalOffset, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PopVerticalOffset() =>
        this.PopHelper(this.stackVerticalOffset, SpannedRecordType.VerticalOffset);

    /// <inheritdoc/>
    public TextSpannableBuilder PushVerticalAlignment(float value)
    {
        var len = SpannedRecordCodec.EncodeVerticalAlignment(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.VerticalAlignment, len, out var data);
        SpannedRecordCodec.EncodeVerticalAlignment(data, value);
        return this.PushHelper(ref this.stackVerticalAlignment, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PushVerticalAlignment(VerticalAlignment value) =>
        this.PushVerticalAlignment(
            value switch
            {
                VerticalAlignment.Top => 0f,
                VerticalAlignment.Middle => 0.5f,
                VerticalAlignment.Bottom => 1f,
                VerticalAlignment.Baseline => -1f,
                _ => -1f,
            });

    /// <inheritdoc/>
    public TextSpannableBuilder PopVerticalAlignment() =>
        this.PopHelper(this.stackVerticalAlignment, SpannedRecordType.VerticalAlignment);

    /// <inheritdoc/>
    public TextSpannableBuilder PushItalic(BoolOrToggle mode = BoolOrToggle.Change)
    {
        mode = ResolveToggleValue(this.stackItalicMode, mode);
        var len = SpannedRecordCodec.EncodeItalic(default, mode);
        this.AddRecordAndReserveData(SpannedRecordType.Italic, len, out var data);
        SpannedRecordCodec.EncodeItalic(data, mode);
        return this.PushHelper(ref this.stackItalicMode, mode);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PushItalic(bool mode) => this.PushItalic(mode ? BoolOrToggle.On : BoolOrToggle.Off);

    /// <inheritdoc/>
    public TextSpannableBuilder PopItalic()
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
    public TextSpannableBuilder PushBold(BoolOrToggle mode = BoolOrToggle.Change)
    {
        mode = ResolveToggleValue(this.stackBoldMode, mode);
        var len = SpannedRecordCodec.EncodeBold(default, mode);
        this.AddRecordAndReserveData(SpannedRecordType.Bold, len, out var data);
        SpannedRecordCodec.EncodeBold(data, mode);
        return this.PushHelper(ref this.stackBoldMode, mode);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PushBold(bool mode) => this.PushBold(mode ? BoolOrToggle.On : BoolOrToggle.Off);

    /// <inheritdoc/>
    public TextSpannableBuilder PopBold()
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
    public TextSpannableBuilder PushTextDecoration(TextDecoration value)
    {
        var len = SpannedRecordCodec.EncodeTextDecoration(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.TextDecoration, len, out var data);
        SpannedRecordCodec.EncodeTextDecoration(data, value);
        return this.PushHelper(ref this.stackTextDecoration, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PopTextDecoration() =>
        this.PopHelper(this.stackTextDecoration, SpannedRecordType.TextDecoration);

    /// <inheritdoc/>
    public TextSpannableBuilder PushTextDecorationStyle(TextDecorationStyle value)
    {
        var len = SpannedRecordCodec.EncodeTextDecorationStyle(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.TextDecorationStyle, len, out var data);
        SpannedRecordCodec.EncodeTextDecorationStyle(data, value);
        return this.PushHelper(ref this.stackTextDecorationStyle, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PopTextDecorationStyle() =>
        this.PopHelper(this.stackTextDecorationStyle, SpannedRecordType.TextDecorationStyle);

    /// <inheritdoc/>
    public TextSpannableBuilder PushBackColor(Rgba32 color)
    {
        var len = SpannedRecordCodec.EncodeBackColor(default, color);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.BackColor, len, out var data);
        SpannedRecordCodec.EncodeBackColor(data, color);
        return this.PushHelper(ref this.stackBackColor, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PopBackColor() =>
        this.PopHelper(this.stackBackColor, SpannedRecordType.BackColor);

    /// <inheritdoc/>
    public TextSpannableBuilder PushShadowColor(Rgba32 color)
    {
        var len = SpannedRecordCodec.EncodeShadowColor(default, color);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.ShadowColor, len, out var data);
        SpannedRecordCodec.EncodeShadowColor(data, color);
        return this.PushHelper(ref this.stackShadowColor, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PopShadowColor() =>
        this.PopHelper(this.stackShadowColor, SpannedRecordType.ShadowColor);

    /// <inheritdoc/>
    public TextSpannableBuilder PushEdgeColor(Rgba32 color)
    {
        var len = SpannedRecordCodec.EncodeEdgeColor(default, color);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.EdgeColor, len, out var data);
        SpannedRecordCodec.EncodeEdgeColor(data, color);
        return this.PushHelper(ref this.stackEdgeColor, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PopEdgeColor() =>
        this.PopHelper(this.stackEdgeColor, SpannedRecordType.EdgeColor);

    /// <inheritdoc/>
    public TextSpannableBuilder PushTextDecorationColor(Rgba32 color)
    {
        var len = SpannedRecordCodec.EncodeTextDecorationColor(default, color);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.TextDecorationColor, len, out var data);
        SpannedRecordCodec.EncodeTextDecorationColor(data, color);
        return this.PushHelper(ref this.stackTextDecorationColor, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PopTextDecorationColor() =>
        this.PopHelper(this.stackTextDecorationColor, SpannedRecordType.TextDecorationColor);

    /// <inheritdoc/>
    public TextSpannableBuilder PushForeColor(Rgba32 color)
    {
        var len = SpannedRecordCodec.EncodeForeColor(default, color);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.ForeColor, len, out var data);
        SpannedRecordCodec.EncodeForeColor(data, color);
        return this.PushHelper(ref this.stackForeCoor, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PopForeColor() =>
        this.PopHelper(this.stackForeCoor, SpannedRecordType.ForeColor);

    /// <inheritdoc/>
    public TextSpannableBuilder PushEdgeWidth(float value)
    {
        var len = SpannedRecordCodec.EncodeEdgeWidth(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.EdgeWidth, len, out var data);
        SpannedRecordCodec.EncodeEdgeWidth(data, value);
        return this.PushHelper(ref this.stackEdgeWidth, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PopEdgeWidth() =>
        this.PopHelper(this.stackEdgeWidth, SpannedRecordType.EdgeWidth);

    /// <inheritdoc/>
    public TextSpannableBuilder PushShadowOffset(Vector2 value)
    {
        var len = SpannedRecordCodec.EncodeShadowOffset(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.ShadowOffset, len, out var data);
        SpannedRecordCodec.EncodeShadowOffset(data, value);
        return this.PushHelper(ref this.stackShadowOffset, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PopShadowOffset() =>
        this.PopHelper(this.stackShadowOffset, SpannedRecordType.ShadowOffset);

    /// <inheritdoc/>
    public TextSpannableBuilder PushTextDecorationThickness(float value)
    {
        var len = SpannedRecordCodec.EncodeTextDecorationThickness(default, value);
        var recordIndex = this.AddRecordAndReserveData(SpannedRecordType.TextDecorationThickness, len, out var data);
        SpannedRecordCodec.EncodeTextDecorationThickness(data, value);
        return this.PushHelper(ref this.stackTextDecorationThickness, recordIndex);
    }

    /// <inheritdoc/>
    public TextSpannableBuilder PopTextDecorationThickness() =>
        this.PopHelper(this.stackTextDecorationThickness, SpannedRecordType.TextDecorationThickness);

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
    private TextSpannableBuilder PushHelper<T>(ref Stack<T>? stack, T value) where T : unmanaged
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
    private TextSpannableBuilder PopHelper(Stack<int>? stack, SpannedRecordType spannedRecordType)
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
