using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility;
using Dalamud.Utility.Text;

using Newtonsoft.Json;

namespace Dalamud.Interface.Spannables.Internal;

/// <summary>Codec for data stream associated with types of <see cref="SpannedRecordType"/>.</summary>
internal sealed class SpannedRecordCodec
{
    /// <summary>JSON serialization configuration for de/serializing <see cref="IFontFamilyId"/>.</summary>
    public static readonly JsonSerializerSettings FontFamilyJsonSerdeSettings = new()
    {
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        TypeNameHandling = TypeNameHandling.Objects,
        Formatting = Formatting.None,
    };

    /// <summary>Decodes data for <see cref="SpannedRecordType.Link"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="link">The link.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeLink(ReadOnlySpan<byte> dataStream, out ReadOnlySpan<byte> link) =>
        TryDecodeRawBytes(ref dataStream, out link);

    /// <summary>Encodes data for <see cref="SpannedRecordType.Link"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="link">The link.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeLink(Span<byte> dataStream, ReadOnlySpan<byte> link) =>
        EncodeRawBytes(ref dataStream, link);

    /// <summary>Decodes data for <see cref="SpannedRecordType.FontHandleSetIndex"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="index">The font handle set index.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeFontHandleSetIndex(ReadOnlySpan<byte> dataStream, out int index) =>
        TryDecode(ref dataStream, out index);

    /// <summary>Decodes data for <see cref="SpannedRecordType.FontHandleSetIndex"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="index">The font handle set index.</param>
    /// <param name="familyId">The family id.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeFontHandleSetIndex(
        ReadOnlySpan<byte> dataStream,
        out int index,
        out IFontFamilyId? familyId) =>
        TryDecode(ref dataStream, out index) & TryDecode(ref dataStream, out familyId);

    /// <summary>Encodes data for <see cref="SpannedRecordType.FontHandleSetIndex"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="index">The font handle set index.</param>
    /// <param name="familyId">The family id.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeFontHandleSetIndex(Span<byte> dataStream, int index, IFontFamilyId? familyId) =>
        Encode(ref dataStream, index) + Encode(ref dataStream, familyId);

    /// <summary>Decodes data for <see cref="SpannedRecordType.FontSize"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="size">The font size.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeFontSize(ReadOnlySpan<byte> dataStream, out float size) =>
        TryDecode(ref dataStream, out size);

    /// <summary>Encodes data for <see cref="SpannedRecordType.FontSize"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="size">The font size.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeFontSize(Span<byte> dataStream, float size) =>
        Encode(ref dataStream, size);

    /// <summary>Decodes data for <see cref="SpannedRecordType.LineHeight"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="size">The font size.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeLineHeight(ReadOnlySpan<byte> dataStream, out float size) =>
        TryDecode(ref dataStream, out size);

    /// <summary>Encodes data for <see cref="SpannedRecordType.LineHeight"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="size">The font size.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeLineHeight(Span<byte> dataStream, float size) =>
        Encode(ref dataStream, size);

    /// <summary>Decodes data for <see cref="SpannedRecordType.HorizontalOffset"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="size">The font size.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeHorizontalOffset(ReadOnlySpan<byte> dataStream, out float size) =>
        TryDecode(ref dataStream, out size);

    /// <summary>Encodes data for <see cref="SpannedRecordType.HorizontalOffset"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="size">The font size.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeHorizontalOffset(Span<byte> dataStream, float size) =>
        Encode(ref dataStream, size);

    /// <summary>Decodes data for <see cref="SpannedRecordType.HorizontalAlignment"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeHorizontalAlignment(ReadOnlySpan<byte> dataStream, out float value) =>
        TryDecode(ref dataStream, out value);

    /// <summary>Encodes data for <see cref="SpannedRecordType.HorizontalAlignment"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="value">The value.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeHorizontalAlignment(Span<byte> dataStream, float value) =>
        Encode(ref dataStream, value);

    /// <summary>Decodes data for <see cref="SpannedRecordType.VerticalOffset"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="size">The font size.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeVerticalOffset(ReadOnlySpan<byte> dataStream, out float size) =>
        TryDecode(ref dataStream, out size);

    /// <summary>Encodes data for <see cref="SpannedRecordType.VerticalOffset"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="size">The font size.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeVerticalOffset(Span<byte> dataStream, float size) =>
        Encode(ref dataStream, size);

    /// <summary>Decodes data for <see cref="SpannedRecordType.VerticalAlignment"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeVerticalAlignment(ReadOnlySpan<byte> dataStream, out float value) =>
        TryDecode(ref dataStream, out value);

    /// <summary>Encodes data for <see cref="SpannedRecordType.VerticalAlignment"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="value">The value.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeVerticalAlignment(Span<byte> dataStream, float value) =>
        Encode(ref dataStream, value);

    /// <summary>Decodes data for <see cref="SpannedRecordType.Italic"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeItalic(ReadOnlySpan<byte> dataStream, out BoolOrToggle value) =>
        TryDecode(ref dataStream, out value);

    /// <summary>Encodes data for <see cref="SpannedRecordType.Italic"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="value">The value.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeItalic(Span<byte> dataStream, BoolOrToggle value) =>
        Encode(ref dataStream, value);

    /// <summary>Decodes data for <see cref="SpannedRecordType.Bold"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeBold(ReadOnlySpan<byte> dataStream, out BoolOrToggle value) =>
        TryDecode(ref dataStream, out value);

    /// <summary>Encodes data for <see cref="SpannedRecordType.Bold"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="value">The value.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeBold(Span<byte> dataStream, BoolOrToggle value) =>
        Encode(ref dataStream, value);

    /// <summary>Decodes data for <see cref="SpannedRecordType.TextDecoration"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeTextDecoration(ReadOnlySpan<byte> dataStream, out TextDecoration value) =>
        TryDecode(ref dataStream, out value);

    /// <summary>Encodes data for <see cref="SpannedRecordType.TextDecoration"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="value">The value.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeTextDecoration(Span<byte> dataStream, TextDecoration value) =>
        Encode(ref dataStream, value);

    /// <summary>Decodes data for <see cref="SpannedRecordType.TextDecorationStyle"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeTextDecorationStyle(ReadOnlySpan<byte> dataStream, out TextDecorationStyle value) =>
        TryDecode(ref dataStream, out value);

    /// <summary>Encodes data for <see cref="SpannedRecordType.TextDecorationStyle"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="value">The value.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeTextDecorationStyle(Span<byte> dataStream, TextDecorationStyle value) =>
        Encode(ref dataStream, value);

    /// <summary>Decodes data for <see cref="SpannedRecordType.BackColor"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="color">The RGBA color value.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeBackColor(ReadOnlySpan<byte> dataStream, out Rgba32 color) =>
        TryDecode(ref dataStream, out color);

    /// <summary>Encodes data for <see cref="SpannedRecordType.BackColor"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="color">The RGBA color value.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeBackColor(Span<byte> dataStream, Rgba32 color) =>
        Encode(ref dataStream, color);

    /// <summary>Decodes data for <see cref="SpannedRecordType.ShadowColor"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="color">The RGBA color value.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeShadowColor(ReadOnlySpan<byte> dataStream, out Rgba32 color) =>
        TryDecode(ref dataStream, out color);

    /// <summary>Encodes data for <see cref="SpannedRecordType.ShadowColor"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="color">The RGBA color value.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeShadowColor(Span<byte> dataStream, Rgba32 color) =>
        Encode(ref dataStream, color);

    /// <summary>Decodes data for <see cref="SpannedRecordType.EdgeColor"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="color">The RGBA color value.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeEdgeColor(ReadOnlySpan<byte> dataStream, out Rgba32 color) =>
        TryDecode(ref dataStream, out color);

    /// <summary>Encodes data for <see cref="SpannedRecordType.EdgeColor"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="color">The RGBA color value.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeEdgeColor(Span<byte> dataStream, Rgba32 color) =>
        Encode(ref dataStream, color);

    /// <summary>Decodes data for <see cref="SpannedRecordType.TextDecorationColor"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="color">The RGBA color value.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeTextDecorationColor(ReadOnlySpan<byte> dataStream, out Rgba32 color) =>
        TryDecode(ref dataStream, out color);

    /// <summary>Encodes data for <see cref="SpannedRecordType.TextDecorationColor"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="color">The RGBA color value.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeTextDecorationColor(Span<byte> dataStream, Rgba32 color) =>
        Encode(ref dataStream, color);

    /// <summary>Decodes data for <see cref="SpannedRecordType.ForeColor"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="color">The RGBA color value.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeForeColor(ReadOnlySpan<byte> dataStream, out Rgba32 color) =>
        TryDecode(ref dataStream, out color);

    /// <summary>Encodes data for <see cref="SpannedRecordType.ForeColor"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="color">The RGBA color value.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeForeColor(Span<byte> dataStream, Rgba32 color) =>
        Encode(ref dataStream, color);

    /// <summary>Decodes data for <see cref="SpannedRecordType.BorderWidth"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="width">The border width.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeBorderWidth(ReadOnlySpan<byte> dataStream, out float width) =>
        TryDecode(ref dataStream, out width);

    /// <summary>Encodes data for <see cref="SpannedRecordType.BorderWidth"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="width">The border width.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeBorderWidth(Span<byte> dataStream, float width) =>
        Encode(ref dataStream, width);

    /// <summary>Decodes data for <see cref="SpannedRecordType.ShadowOffset"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="offset">The shadow offset.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeShadowOffset(ReadOnlySpan<byte> dataStream, out Vector2 offset) =>
        TryDecode(ref dataStream, out offset);

    /// <summary>Encodes data for <see cref="SpannedRecordType.ShadowOffset"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="offset">The shadow offset.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeShadowOffset(Span<byte> dataStream, Vector2 offset) =>
        Encode(ref dataStream, offset);

    /// <summary>Decodes data for <see cref="SpannedRecordType.TextDecorationThickness"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeTextDecorationThickness(ReadOnlySpan<byte> dataStream, out float value) =>
        TryDecode(ref dataStream, out value);

    /// <summary>Encodes data for <see cref="SpannedRecordType.TextDecorationThickness"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="value">The value.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeTextDecorationThickness(Span<byte> dataStream, float value) =>
        Encode(ref dataStream, value);

    /// <summary>Decodes data for <see cref="SpannedRecordType.ObjectIcon"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="icon">The icon.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeObjectIcon(ReadOnlySpan<byte> dataStream, out int icon) =>
        TryDecode(ref dataStream, out icon);

    /// <summary>Encodes data for <see cref="SpannedRecordType.ObjectIcon"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="icon">The icon.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeObjectIcon(Span<byte> dataStream, int icon) =>
        Encode(ref dataStream, icon);

    /// <summary>Decodes data for <see cref="SpannedRecordType.ObjectTexture"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="index">The texture index.</param>
    /// <param name="uv0">The UV0.</param>
    /// <param name="uv1">The UV1.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeObjectTexture(
        ReadOnlySpan<byte> dataStream,
        out int index,
        out Vector2 uv0,
        out Vector2 uv1) =>
        TryDecode(ref dataStream, out index)
        & TryDecode(ref dataStream, out uv0)
        & TryDecode(ref dataStream, out uv1);

    /// <summary>Encodes data for <see cref="SpannedRecordType.ObjectTexture"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="index">The texture index.</param>
    /// <param name="uv0">The UV0.</param>
    /// <param name="uv1">The UV1.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeObjectTexture(Span<byte> dataStream, int index, Vector2 uv0, Vector2 uv1) =>
        Encode(ref dataStream, index)
        + Encode(ref dataStream, uv0)
        + Encode(ref dataStream, uv1);

    /// <summary>Decodes data for <see cref="SpannedRecordType.ObjectSpannable"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="index">The callback index.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryDecodeObjectSpannable(ReadOnlySpan<byte> dataStream, out int index) =>
        TryDecode(ref dataStream, out index);

    /// <summary>Encodes data for <see cref="SpannedRecordType.ObjectSpannable"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="index">The callback index.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    public static int EncodeObjectSpannable(Span<byte> dataStream, int index) =>
        Encode(ref dataStream, index);

    /// <summary>Decodes data for <see cref="SpannedRecordType.ObjectNewLine"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <returns><c>true</c> on success.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool TryDecodeInsertManualNewLine(ReadOnlySpan<byte> dataStream)
    {
        _ = dataStream;
        return true;
    }

    /// <summary>Encodes data for <see cref="SpannedRecordType.ObjectNewLine"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int EncodeInsertManualNewLine(Span<byte> dataStream)
    {
        _ = dataStream;
        return 0;
    }

    /// <summary>Encodes data of arbitrary unmanaged type.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="value">The value to encode.</param>
    /// <typeparam name="T">An unmanaged type that can be fit into 4 bytes.</typeparam>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe int Encode<T>(ref Span<byte> dataStream, T value)
        where T : unmanaged => UtfValue.Encode8(
        ref dataStream,
        BitConverter.IsLittleEndian
            ? (int)(sizeof(T) switch
                       {
                           1 => *(byte*)&value,
                           2 => *(ushort*)&value,
                           4 => *(uint*)&value,
                           _ => throw new NotSupportedException(),
                       })
            : (int)(sizeof(T) switch
                       {
                           1 => *((byte*)&value + 3),
                           2 => *(ushort*)((byte*)&value + 2),
                           4 => *(uint*)&value,
                           _ => throw new NotSupportedException(),
                       }));

    /// <summary>Decodes data of arbitrary unmanaged type.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="value">The retrieved value.</param>
    /// <typeparam name="T">An unmanaged type that can be fit into 4 bytes.</typeparam>
    /// <returns><c>true</c> on success.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe bool TryDecode<T>(ref ReadOnlySpan<byte> dataStream, out T value)
        where T : unmanaged
    {
        value = default;
        if (!UtfValue.TryDecode8(ref dataStream, out var v, out _))
            return false;

        if (BitConverter.IsLittleEndian || sizeof(T) == 4)
            value = *(T*)&v;
        else if (sizeof(T) == 1)
            value = *(T*)((byte*)&v + 3);
        else if (sizeof(T) == 2)
            value = *(T*)((byte*)&v + 2);
        else
            throw new NotSupportedException();
        return true;
    }

    /// <summary>Encodes data of arbitrary length.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="data">The retrieved data.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int EncodeRawBytes(ref Span<byte> dataStream, ReadOnlySpan<byte> data)
    {
        var length = 0;
        length += UtfValue.Encode8(ref dataStream, data.Length + 1);

        length += data.Length + 1;
        if (!dataStream.IsEmpty)
        {
            data.CopyTo(dataStream);
            dataStream[data.Length] = 0; // add zero terminator
            dataStream = dataStream[(data.Length + 1)..];
        }

        return length;
    }

    /// <summary>Decodes data of arbitrary length.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="value">The data to encode.</param>
    /// <returns><c>true</c> on success.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool TryDecodeRawBytes(scoped ref ReadOnlySpan<byte> dataStream, out ReadOnlySpan<byte> value)
    {
        value = default;
        if (!UtfValue.TryDecode8(ref dataStream, out var v, out _) || dataStream.Length < v.IntValue || v.IntValue < 1)
            return false;
        value = dataStream[..(v.IntValue - 1)]; // ignore zero terminator
        dataStream = dataStream[v.IntValue..];
        return true;
    }

    /// <summary>Encodes a <see cref="Vector2"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int Encode(ref Span<byte> dataStream, Vector2 value) =>
        Encode(ref dataStream, value.X) + Encode(ref dataStream, value.Y);

    /// <summary>Decodes a <see cref="Vector2"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="value">The retrieved value.</param>
    /// <returns><c>true</c> on success.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool TryDecode(ref ReadOnlySpan<byte> dataStream, out Vector2 value)
    {
        if (TryDecode(ref dataStream, out float x) && TryDecode(ref dataStream, out float y))
        {
            value = new(x, y);
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Encodes a <see cref="string"/>.</summary>
    /// <param name="dataStream">The optional data stream to encode to.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The remaning region of <paramref name="dataStream"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int Encode(ref Span<byte> dataStream, string? value)
    {
        var len = Encode(ref dataStream, value?.Length ?? -1);
        if (!string.IsNullOrEmpty(value))
        {
            var len2 =
                dataStream.IsEmpty
                    ? Encoding.UTF8.GetByteCount(value)
                    : Encoding.UTF8.GetBytes(value, dataStream);
            len += len2;
        }

        return len;
    }

    /// <summary>Decodes a <see cref="string"/>.</summary>
    /// <param name="dataStream">The data stream to decode from.</param>
    /// <param name="value">The retrieved value.</param>
    /// <returns><c>true</c> on success.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool TryDecode(ref ReadOnlySpan<byte> dataStream, out string? value)
    {
        if (!TryDecode(ref dataStream, out int len) || len < -1 || (len > 0 && dataStream.Length < len))
        {
            value = null;
            return false;
        }

        if (len == -1)
        {
            value = null;
            return true;
        }

        value = Encoding.UTF8.GetString(dataStream[..len]);
        dataStream = dataStream[len..];
        return true;
    }

    /// <summary>Encodes a <see cref="IFontFamilyId"/>.</summary>
    /// <param name="target">The optional target span.</param>
    /// <param name="familyId">The family id.</param>
    /// <returns>The length of the encoded data.</returns>
    public static int Encode(ref Span<byte> target, IFontFamilyId? familyId)
    {
        var length = 0;
        switch (familyId)
        {
            case null:
                length += UtfValue.Encode8(ref target, 0);
                break;
            case DalamudDefaultFontAndFamilyId:
                length += UtfValue.Encode8(ref target, 1);
                break;
            case DalamudAssetFontAndFamilyId assetFamily:
                length += UtfValue.Encode8(ref target, 2);
                length += UtfValue.Encode8(ref target, (int)assetFamily.Asset);
                break;
            case GameFontAndFamilyId gameFamily:
                length += UtfValue.Encode8(ref target, 3);
                length += UtfValue.Encode8(ref target, (int)gameFamily.GameFontFamily);
                break;
            case SystemFontFamilyId systemFamily:
                length += UtfValue.Encode8(ref target, 4);

                length += Encode(ref target, systemFamily.EnglishName);

                length += UtfValue.Encode8(ref target, systemFamily.LocaleNames?.Count ?? 0);
                if (systemFamily.LocaleNames is not null)
                {
                    foreach (var f2 in systemFamily.LocaleNames)
                    {
                        length += Encode(ref target, f2.Key);
                        length += Encode(ref target, f2.Value);
                    }
                }

                break;
            default:
                length += UtfValue.Encode8(ref target, 5);
                var js = JsonConvert.SerializeObject(familyId, FontFamilyJsonSerdeSettings);
                length += Encode(ref target, js);
                break;
        }

        return length;
    }

    /// <summary>Attempts to decode an instance of <see cref="FontHandleVariantSet"/> from a byte sequence.</summary>
    /// <param name="source">The source byte sequence.</param>
    /// <param name="familyId">The retrieved font set.</param>
    /// <returns><c>true</c> if decoded.</returns>
    public static bool TryDecode(ref ReadOnlySpan<byte> source, out IFontFamilyId? familyId)
    {
        switch (UtfValue.TryDecode8(ref source, out var fontFamilyType, out _) ? fontFamilyType.IntValue : -1)
        {
            case 0:
                familyId = null;
                return true;
            case 1:
                familyId = DalamudDefaultFontAndFamilyId.Instance;
                return true;
            case 2
                when UtfValue.TryDecode8(ref source, out var assetInt, out _)
                     && DalamudAssetFontAndFamilyId.TryGetInstance((DalamudAsset)assetInt.IntValue, out var v):
                familyId = v;
                return true;
            case 3
                when UtfValue.TryDecode8(ref source, out var assetInt, out _)
                     && GameFontAndFamilyId.TryGetInstance((GameFontFamily)assetInt.IntValue, out var v):
                familyId = v;
                return true;
            case 4:
            {
                if (!TryDecode(ref source, out string englishName) || englishName is null)
                    goto default;
                if (!UtfValue.TryDecode8(ref source, out var numLocaleNames, out _))
                    goto default;
                var n = numLocaleNames.IntValue;
                var localeNames = new Dictionary<string, string>(numLocaleNames);
                while (n-- > 0)
                {
                    if (!TryDecode(ref source, out string? k) || k is null)
                        goto default;
                    if (!TryDecode(ref source, out string? v) || v is null)
                        goto default;
                    localeNames[k] = v;
                }

                familyId = new SystemFontFamilyId(englishName, localeNames);
                return true;
            }

            case 5:
            {
                if (!TryDecode(ref source, out string json) || json is null)
                    goto default;
                familyId = JsonConvert.DeserializeObject<IFontFamilyId>(json);
                return familyId is not null;
            }

            default:
                familyId = default;
                return false;
        }
    }
}
