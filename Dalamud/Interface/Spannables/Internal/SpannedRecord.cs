using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.Spannables.Strings.Internal;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility;
using Dalamud.Utility.Text;

using Newtonsoft.Json;

namespace Dalamud.Interface.Spannables.Internal;

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
                      + UtfValue.GetEncodedLength8(this.TextStart)
                      + UtfValue.GetEncodedLength8(this.DataStart)
                      + UtfValue.GetEncodedLength8(this.DataLength)
                      + UtfValue.GetEncodedLength8((byte)this.Type)
                      + UtfValue.GetEncodedLength8(this.IsRevertByte);
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
        if (!UtfValue.TryDecode8(ref textStream, out var v, out _))
            return false;
        spannedRecord.TextStart = v;
        if (!UtfValue.TryDecode8(ref textStream, out v, out _))
            return false;
        spannedRecord.DataStart = v;
        if (!UtfValue.TryDecode8(ref textStream, out v, out _))
            return false;
        spannedRecord.DataLength = v;
        if (!UtfValue.TryDecode8(ref textStream, out v, out _))
            return false;
        spannedRecord.Type = (SpannedRecordType)v.IntValue;
        if (!UtfValue.TryDecode8(ref textStream, out v, out _))
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
        length += UtfValue.Encode8(ref textStream, this.TextStart);
        length += UtfValue.Encode8(ref textStream, this.DataStart);
        length += UtfValue.Encode8(ref textStream, this.DataLength);
        length += UtfValue.Encode8(ref textStream, (byte)this.Type);
        length += UtfValue.Encode8(ref textStream, this.IsRevertByte);
        return length;
    }

    /// <inheritdoc/>
    public override readonly string ToString() =>
        this.DataLength == 0
            ? this.IsRevert
                  ? $"{this.TextStart}({this.Type}, revert)"
                  : $"{this.TextStart}({this.Type}, set)"
            : $"{this.TextStart}({this.Type}, {this.DataStart}, {this.DataLength})";

    /// <summary>Write parameters for use with <see cref="ISpannedStringBuilder"/> functions.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="data">The data attached to the record.</param>
    /// <param name="fontSets">The font sets.</param>
    /// <param name="formatProvider">The format provider.</param>
    internal readonly void WritePushParameters(
        StringBuilder sb,
        ReadOnlySpan<byte> data,
        FontHandleVariantSet[] fontSets,
        IFormatProvider? formatProvider)
    {
        switch (this.Type)
        {
            case SpannedRecordType.Link when SpannedRecordCodec.TryDecodeLink(data, out var link):
                AppendString(sb, link.EnumerateUtf(UtfEnumeratorFlags.Utf8));
                return;
            case SpannedRecordType.FontHandleSetIndex
                when SpannedRecordCodec.TryDecodeFontHandleSetIndex(data, out var i32):
                sb.Append(' ').Append(i32);
                switch (fontSets[i32].FontFamilyId)
                {
                    case DalamudDefaultFontAndFamilyId:
                    case null:
                        // no extra parameter required
                        return;
                    case DalamudAssetFontAndFamilyId ff:
                        sb.Append(' ').Append(ff.Asset);
                        return;
                    case GameFontAndFamilyId ff:
                        sb.Append(' ').Append(ff.Family);
                        return;
                    case SystemFontFamilyId ff:
                        sb.Append(' ').Append(ff.EnglishName);
                        return;
                    default:
                        sb.Append(' ')
                          .Append(
                              JsonConvert.SerializeObject(
                                  fontSets[i32].FontFamilyId,
                                  SpannedRecordCodec.FontFamilyJsonSerdeSettings));
                        return;
                }

            case SpannedRecordType.FontSize when SpannedRecordCodec.TryDecodeFontSize(data, out var f32):
                sb.Append(formatProvider, $" {f32:g}");
                return;
            case SpannedRecordType.LineHeight when SpannedRecordCodec.TryDecodeLineHeight(data, out var f32):
                sb.Append(formatProvider, $" {f32:g}");
                return;
            case SpannedRecordType.HorizontalOffset
                when SpannedRecordCodec.TryDecodeHorizontalOffset(data, out var f32):
                sb.Append(formatProvider, $" {f32:g}");
                return;
            case SpannedRecordType.HorizontalAlignment
                when SpannedRecordCodec.TryDecodeHorizontalAlignment(data, out var horizontalAlignment):
                sb.Append(' ').Append(horizontalAlignment);
                return;
            case SpannedRecordType.VerticalOffset when SpannedRecordCodec.TryDecodeVerticalOffset(data, out var f32):
                sb.Append(formatProvider, $" {f32:g}");
                return;
            case SpannedRecordType.VerticalAlignment
                when SpannedRecordCodec.TryDecodeVerticalAlignment(data, out var verticalAlignment):
                sb.Append(' ').Append(verticalAlignment);
                return;
            case SpannedRecordType.Italic when SpannedRecordCodec.TryDecodeItalic(data, out var toggle):
                sb.Append(' ').Append(toggle);
                return;
            case SpannedRecordType.Bold when SpannedRecordCodec.TryDecodeBold(data, out var toggle):
                sb.Append(' ').Append(toggle);
                return;
            case SpannedRecordType.TextDecoration
                when SpannedRecordCodec.TryDecodeTextDecoration(data, out var textDecorationLine):
                sb.Append(' ').Append(textDecorationLine);
                return;
            case SpannedRecordType.TextDecorationStyle
                when SpannedRecordCodec.TryDecodeTextDecorationStyle(data, out var textDecorationStyle):
                sb.Append(' ').Append(textDecorationStyle);
                return;
            case SpannedRecordType.BackColor when SpannedRecordCodec.TryDecodeBackColor(data, out var color):
                sb.Append(' ').Append(color.ToString());
                return;
            case SpannedRecordType.ShadowColor when SpannedRecordCodec.TryDecodeShadowColor(data, out var color):
                sb.Append(' ').Append(color.ToString());
                return;
            case SpannedRecordType.EdgeColor when SpannedRecordCodec.TryDecodeEdgeColor(data, out var color):
                sb.Append(' ').Append(color.ToString());
                return;
            case SpannedRecordType.ForeColor when SpannedRecordCodec.TryDecodeForeColor(data, out var color):
                sb.Append(' ').Append(color.ToString());
                return;
            case SpannedRecordType.TextDecorationColor
                when SpannedRecordCodec.TryDecodeTextDecorationColor(data, out var color):
                sb.Append(' ').Append(color.ToString());
                return;
            case SpannedRecordType.BorderWidth when SpannedRecordCodec.TryDecodeBorderWidth(data, out var f32):
                sb.Append(formatProvider, $" {f32:g}");
                return;
            case SpannedRecordType.ShadowOffset when SpannedRecordCodec.TryDecodeShadowOffset(data, out var v2):
                sb.Append(formatProvider, $" {v2.X:g} {v2.Y:g}");
                return;
            case SpannedRecordType.TextDecorationThickness
                when SpannedRecordCodec.TryDecodeTextDecorationThickness(data, out var f32):
                sb.Append(formatProvider, $" {f32:g}");
                return;
            case SpannedRecordType.ObjectIcon when SpannedRecordCodec.TryDecodeObjectIcon(data, out var icon):
                sb.Append(' ').Append(icon);
                return;
            case SpannedRecordType.ObjectTexture
                when SpannedRecordCodec.TryDecodeObjectTexture(data, out var i32, out var uv0, out var uv1):
                sb.Append(formatProvider, $" {i32} {uv0.X:g} {uv0.Y:g} {uv1.X:g} {uv1.Y:g}");
                return;
            case SpannedRecordType.ObjectNewLine:
                _ = SpannedRecordCodec.TryDecodeInsertManualNewLine(data);
                return;
            case SpannedRecordType.ObjectSpannable
                when SpannedRecordCodec.TryDecodeObjectSpannable(data, out var i32):
                sb.Append(formatProvider, $" {i32:g}");
                return;
            case SpannedRecordType.None:
            default:
                return;
        }

        static void AppendString(StringBuilder sb, UtfEnumerator enumerator)
        {
            sb.Append(" \"");
            foreach (var c in enumerator)
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
                else if (c.Value.UIntValue < 0x100)
                {
                    sb.Append($"\\x{c.Value.UIntValue:X02}");
                }
                else if (c.Value.UIntValue < 0x10000)
                {
                    sb.Append($"\\u{c.Value.UIntValue:X04}");
                }
                else
                {
                    sb.Append($"\\U{c.Value.UIntValue:X08}");
                }
            }

            sb.Append('"');
        }
    }
}
