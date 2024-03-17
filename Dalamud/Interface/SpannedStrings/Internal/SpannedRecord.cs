using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

using Dalamud.Utility;
using Dalamud.Utility.Text;

namespace Dalamud.Interface.SpannedStrings.Internal;

/// <summary>Represents a spanned region in a text stream.</summary>
internal struct SpannedRecord
{
    /// <summary>The start offset of this span, in the text stream.</summary>
    public int TextStart;

    /// <summary>The start offset of this span, in the data stream.</summary>
    public int DataStart;

    /// <summary>The length of this span, in the data stream.</summary>
    public int DataLength;

    /// <summary>The type of this span.</summary>
    public SpannedRecordType Type;

    /// <summary>Storage for <see cref="IsRevert"/>.</summary>
    public byte IsRevertByte;

    /// <summary>Initializes a new instance of the <see cref="SpannedRecord"/> struct.</summary>
    /// <param name="textStart">The start offset in the text stream.</param>
    /// <param name="dataStart">The start offset in the data stream.</param>
    /// <param name="dataLength">The length in the data stream.</param>
    /// <param name="type">The type of the span.</param>
    /// <param name="isRevert">Whether to revert to the default option.</param>
    public SpannedRecord(
        int textStart,
        int dataStart,
        int dataLength,
        SpannedRecordType type,
        bool isRevert = false)
    {
        Debug.Assert(!isRevert || dataLength == 0, "Revert should not have data attached.");

        this.TextStart = textStart;
        this.DataStart = dataStart;
        this.DataLength = dataLength;
        this.Type = type;
        this.IsRevert = isRevert;
    }

    /// <summary>Gets or sets a value indicating whether to revert to the default option.</summary>
    /// <remarks>Only applicable for style changing spans.</remarks>
    public bool IsRevert
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => this.IsRevertByte != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => this.IsRevertByte = value ? (byte)1 : (byte)0;
    }

    /// <summary>Gets the encoded length for this span.</summary>
    public readonly int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var res = 0
                      + Utf8Value.GetEncodedLength(this.TextStart)
                      + Utf8Value.GetEncodedLength(this.DataStart)
                      + Utf8Value.GetEncodedLength(this.DataLength)
                      + Utf8Value.GetEncodedLength((byte)this.Type)
                      + Utf8Value.GetEncodedLength(this.IsRevertByte);
            return res;
        }
    }

    /// <inheritdoc cref="TryDecode(ReadOnlySpan{byte},out SpannedRecord, out int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryDecode(ReadOnlySpan<byte> textStream, out SpannedRecord spannedRecord, out int length) =>
        TryDecode(ref textStream, out spannedRecord, out length);

    /// <summary>Decodes a byte sequence to a <see cref="SpannedRecord"/>.</summary>
    /// <param name="textStream">The text stream to decode from.</param>
    /// <param name="spannedRecord">The retrieved spanned.</param>
    /// <param name="length">The length of the spanned.</param>
    /// <returns><c>true</c> if decoded.</returns>
    public static bool TryDecode(ref ReadOnlySpan<byte> textStream, out SpannedRecord spannedRecord, out int length)
    {
        length = 0;
        spannedRecord = default;
        if (!Utf8Value.TryDecode(ref textStream, out var v, out _))
            return false;
        spannedRecord.TextStart = v;
        if (!Utf8Value.TryDecode(ref textStream, out v, out _))
            return false;
        spannedRecord.DataStart = v;
        if (!Utf8Value.TryDecode(ref textStream, out v, out _))
            return false;
        spannedRecord.DataLength = v;
        if (!Utf8Value.TryDecode(ref textStream, out v, out _))
            return false;
        spannedRecord.Type = (SpannedRecordType)v.IntValue;
        if (!Utf8Value.TryDecode(ref textStream, out v, out _))
            return false;
        spannedRecord.IsRevertByte = (byte)v.IntValue;
        return true;
    }

    /// <inheritdoc cref="Encode(ref Span{byte})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int Encode(Span<byte> textStream) => this.Encode(ref textStream);

    /// <summary>Encodes the codepoint to the target.</summary>
    /// <param name="textStream">The text stream to encode to.</param>
    /// <returns>The length of the encoded data.</returns>
    public readonly int Encode(ref Span<byte> textStream)
    {
        var length = 0;
        length += Utf8Value.Encode(ref textStream, this.TextStart);
        length += Utf8Value.Encode(ref textStream, this.DataStart);
        length += Utf8Value.Encode(ref textStream, this.DataLength);
        length += Utf8Value.Encode(ref textStream, (byte)this.Type);
        length += Utf8Value.Encode(ref textStream, this.IsRevertByte);
        return length;
    }

    /// <inheritdoc/>
    public override readonly string ToString() =>
        this.DataLength == 0
            ? this.IsRevert
                  ? $"{this.TextStart}({this.Type}, revert)"
                  : $"{this.TextStart}({this.Type}, set)"
            : $"{this.TextStart}({this.Type}, {this.DataStart}, {this.DataLength})";

    /// <summary>Write parameters for use with <see cref="ISpannedStringBuilder{TReturn}"/> functions.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="data">The data attached to the record.</param>
    /// <param name="formatProvider">The format provider.</param>
    internal readonly void WritePushParameters(
        StringBuilder sb,
        ReadOnlySpan<byte> data,
        IFormatProvider? formatProvider)
    {
        switch (this.Type)
        {
            case SpannedRecordType.Link:
                if (SpannedRecordCodec.TryDecodeLink(data, out var link))
                {
                    sb.Append(" \"");
                    foreach (var c in link.AsUtf8Enumerable())
                    {
                        if (c.Value.TryGetRune(out var rune))
                        {
                            switch (rune.Value)
                            {
                                case '"':
                                    sb.Append("\\\"");
                                    break;
                                case '\\':
                                    sb.Append(@"\\");
                                    break;
                                default:
                                    sb.Append(rune.ToString());
                                    break;
                            }
                        }
                        else
                        {
                            sb.Append($"\\x{c.Value.UIntValue:X02}");
                        }
                    }

                    sb.Append('"');
                }

                break;
            case SpannedRecordType.FontHandleSetIndex:
                if (SpannedRecordCodec.TryDecodeFontHandleSetIndex(data, out var i32))
                    sb.Append(' ').Append(i32);
                break;
            case SpannedRecordType.FontSize:
                if (SpannedRecordCodec.TryDecodeFontSize(data, out var f32))
                    sb.Append(formatProvider, $" {f32:g}");
                break;
            case SpannedRecordType.LineHeight:
                if (SpannedRecordCodec.TryDecodeLineHeight(data, out f32))
                    sb.Append(formatProvider, $" {f32:g}");
                break;
            case SpannedRecordType.HorizontalOffset:
                if (SpannedRecordCodec.TryDecodeHorizontalOffset(data, out f32))
                    sb.Append(formatProvider, $" {f32:g}");
                break;
            case SpannedRecordType.HorizontalAlignment:
                if (SpannedRecordCodec.TryDecodeHorizontalAlignment(data, out var horizontalAlignment))
                    sb.Append(' ').Append(horizontalAlignment);
                break;
            case SpannedRecordType.VerticalOffset:
                if (SpannedRecordCodec.TryDecodeVerticalOffset(data, out f32))
                    sb.Append(formatProvider, $" {f32:g}");
                break;
            case SpannedRecordType.VerticalAlignment:
                if (SpannedRecordCodec.TryDecodeVerticalAlignment(data, out var verticalAlignment))
                    sb.Append(' ').Append(verticalAlignment);
                break;
            case SpannedRecordType.Italic:
                if (SpannedRecordCodec.TryDecodeItalic(data, out var toggle))
                    sb.Append(' ').Append(toggle);
                break;
            case SpannedRecordType.Bold:
                if (SpannedRecordCodec.TryDecodeBold(data, out toggle))
                    sb.Append(' ').Append(toggle);
                break;
            case SpannedRecordType.BackColor:
                if (SpannedRecordCodec.TryDecodeBackColor(data, out var color))
                    sb.Append(' ').Append(color.ToString());
                break;
            case SpannedRecordType.ShadowColor:
                if (SpannedRecordCodec.TryDecodeShadowColor(data, out color))
                    sb.Append(' ').Append(color.ToString());
                break;
            case SpannedRecordType.EdgeColor:
                if (SpannedRecordCodec.TryDecodeEdgeColor(data, out color))
                    sb.Append(' ').Append(color.ToString());
                break;
            case SpannedRecordType.ForeColor:
                if (SpannedRecordCodec.TryDecodeForeColor(data, out color))
                    sb.Append(' ').Append(color.ToString());
                break;
            case SpannedRecordType.BorderWidth:
                if (SpannedRecordCodec.TryDecodeBorderWidth(data, out f32))
                    sb.Append(formatProvider, $" {f32:g}");
                break;
            case SpannedRecordType.ShadowOffset:
                if (SpannedRecordCodec.TryDecodeShadowOffset(data, out var v2))
                    sb.Append(formatProvider, $" {v2.X:g} {v2.Y:g}");
                break;
            case SpannedRecordType.InsertionIcon:
                if (SpannedRecordCodec.TryDecodeInsertionIcon(data, out var icon))
                    sb.Append(' ').Append(icon);
                break;
            case SpannedRecordType.InsertionTexture:
                if (SpannedRecordCodec.TryDecodeInsertionTexture(data, out i32, out var uv0, out var uv1))
                    sb.Append(formatProvider, $" {i32} {uv0.X:g} {uv0.Y:g} {uv1.X:g} {uv1.Y:g}");
                break;
            case SpannedRecordType.InsertionManualNewLine:
                _ = SpannedRecordCodec.TryDecodeInsertManualNewLine(data);
                break;
            case SpannedRecordType.InsertionCallback:
                if (SpannedRecordCodec.TryDecodeInsertionCallback(data, out i32, out f32))
                    sb.Append(formatProvider, $" {i32:g} {f32:g}");
                break;
            case SpannedRecordType.None:
            default:
                break;
        }
    }
}
