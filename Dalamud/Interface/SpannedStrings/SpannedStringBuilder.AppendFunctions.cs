using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Game.Text;
using Dalamud.Interface.Internal;
using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Interface.SpannedStrings.Internal;
using Dalamud.Utility;
using Dalamud.Utility.Text;

namespace Dalamud.Interface.SpannedStrings;

/// <summary>A custom text renderer implementation.</summary>
public sealed partial class SpannedStringBuilder
{
    /// <summary>A dictionary for exclusive use with <see cref="Append{T}"/>.</summary>
    private Dictionary<Type, Delegate>? appendSpanFormattableDelegates;

    /// <summary>A buffer for exclusive use with <see cref="AppendSpanFormattable{T}"/>.</summary>
    private MemoryStream? appendBuffer;

    private delegate SpannedStringBuilder AppendSpanFormattableDelegate<in T>(T value, int repeat = 1)
        where T : struct;

    /// <inheritdoc/>
    public SpannedStringBuilder Append(ReadOnlySpan<char> span, int repeat = 1)
    {
        if (repeat < 1 || span.IsEmpty)
            return this;

        var numBytes = Encoding.UTF8.GetByteCount(span);
        var reserved = this.ReserveBytes(numBytes * repeat);
        var template = reserved;
        template = template[..Encoding.UTF8.GetBytes(span, template)];
        reserved = reserved[template.Length..];
        for (; !reserved.IsEmpty; reserved = reserved[template.Length..])
            template.CopyTo(reserved);
        return this;
    }

    /// <inheritdoc/>
    public SpannedStringBuilder Append(ReadOnlySpan<byte> span, int repeat = 1)
    {
        if (repeat < 1 || span.IsEmpty)
            return this;

        var reserved = this.ReserveBytes(span.Length * repeat);
        while (!reserved.IsEmpty)
        {
            span.CopyTo(reserved);
            reserved = reserved[span.Length..];
        }

        return this;
    }

    /// <inheritdoc/>
    public SpannedStringBuilder Append(ReadOnlyMemory<char> memory, int repeat = 1) =>
        this.Append(memory.Span, repeat);

    /// <inheritdoc/>
    public SpannedStringBuilder Append(ReadOnlyMemory<byte> memory, int repeat = 1) =>
        this.Append(memory.Span, repeat);

    /// <inheritdoc/>
    public SpannedStringBuilder Append(string? text, int repeat = 1) =>
        this.Append((text ?? "null").AsSpan(), repeat);

    /// <inheritdoc/>
    public SpannedStringBuilder Append(UtfEnumerator utfEnumerator, int repeat = 1)
    {
        while (repeat-- > 0)
        {
            foreach (var c in utfEnumerator)
            {
                if (c.IsValid())
                    this.AppendChar(c.Value);
                else
                    this.AppendChar(Rune.ReplacementChar);
            }
        }

        return this;
    }

    /// <inheritdoc/>
    public SpannedStringBuilder Append(ISpanFormattable? value, int repeat = 1) =>
        value is null ? this.Append("null"u8) : this.AppendSpanFormattable(value, repeat);

    /// <inheritdoc/>
    public SpannedStringBuilder Append(object? value, int repeat = 1) =>
        this.Append((value?.ToString() ?? "null").AsSpan(), repeat);

    /// <inheritdoc/>
    public SpannedStringBuilder Append(char value, int repeat = 1) => this.AppendChar(value, repeat);

    /// <inheritdoc/>
    public SpannedStringBuilder Append<T>(T value, int repeat = 1) where T : struct
    {
        this.appendSpanFormattableDelegates ??= new(8);
        if (!this.appendSpanFormattableDelegates.TryGetValue(typeof(T), out var @delegate))
        {
            @delegate = this
                        .GetType()
                        .GetMethod(nameof(this.AppendSpanFormattable), BindingFlags.Instance | BindingFlags.NonPublic)!
                        .MakeGenericMethod(typeof(T))
                        .CreateDelegate<AppendSpanFormattableDelegate<T>>(this);
            this.appendSpanFormattableDelegates[typeof(T)] = @delegate;
        }

        return ((AppendSpanFormattableDelegate<T>)@delegate).Invoke(value, repeat);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder AppendCallback(SpannedStringCallbackDelegate? callback, float sizeRatio, out int id)
    {
        id = this.callbacks.Count;
        this.callbacks.Add(callback);
        return this.AppendCallback(id, sizeRatio);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder AppendCallback(int id, float sizeRatio)
    {
        this.callbacks.EnsureCapacity(id + 1);
        while (this.callbacks.Count <= id)
            this.callbacks.Add(null);
        var len = SpannedRecordCodec.EncodeInsertionCallback(default, id, sizeRatio);
        this.AddRecordAndReserveData(SpannedRecordType.InsertionCallback, len, out var data);
        SpannedRecordCodec.EncodeInsertionCallback(data, id, sizeRatio);
        return this;
    }

    /// <inheritdoc/>
    public SpannedStringBuilder AppendChar(int codepoint, int repeat = 1)
    {
        if (repeat < 1)
            return this;

        var len = UtfValue.GetEncodedLength8(codepoint);
        var target = this.ReserveBytes(len * repeat);
        var template = target[..len];
        UtfValue.Encode8(template, codepoint, out _);
        for (; !target.IsEmpty; target = target[len..])
            template.CopyTo(target);
        return this;
    }

    /// <inheritdoc/>
    public SpannedStringBuilder AppendChar(Rune rune, int repeat = 1)
    {
        if (repeat < 1)
            return this;

        var len = rune.Utf8SequenceLength;
        var target = this.ReserveBytes(len * repeat);
        var template = target[..len];
        rune.EncodeToUtf8(template);
        for (; !target.IsEmpty; target = target[len..])
            template.CopyTo(target);
        return this;
    }

    /// <inheritdoc/>
    public SpannedStringBuilder AppendIconGfd(GfdIcon iconId)
    {
        var len = SpannedRecordCodec.EncodeInsertionIcon(default, iconId);
        this.AddRecordAndReserveData(SpannedRecordType.InsertionIcon, len, out var data);
        SpannedRecordCodec.EncodeInsertionIcon(data, iconId);
        return this;
    }

    /// <inheritdoc/>
    public SpannedStringBuilder AppendTexture(int id) => this.AppendTexture(id, Vector2.Zero, Vector2.One);

    /// <inheritdoc/>
    public SpannedStringBuilder AppendTexture(int id, Vector2 uv0, Vector2 uv1)
    {
        this.textures.EnsureCapacity(id + 1);
        while (this.textures.Count <= id)
            this.textures.Add(null);
        var len = SpannedRecordCodec.EncodeInsertionTexture(default, id, uv0, uv1);
        this.AddRecordAndReserveData(SpannedRecordType.InsertionTexture, len, out var data);
        SpannedRecordCodec.EncodeInsertionTexture(data, id, uv0, uv1);
        return this;
    }

    /// <inheritdoc/>
    public SpannedStringBuilder AppendTexture(IDalamudTextureWrap? textureWrap, out int id) =>
        this.AppendTexture(textureWrap, Vector2.Zero, Vector2.One, out id);

    /// <inheritdoc/>
    public SpannedStringBuilder AppendTexture(IDalamudTextureWrap? textureWrap, Vector2 uv0, Vector2 uv1, out int id)
    {
        id = this.textures.Count;
        this.textures.Add(textureWrap);
        return this.AppendTexture(id, uv0, uv1);
    }

    /// <inheritdoc/>
    public SpannedStringBuilder AppendLine(NewLineType newLineType = NewLineType.Manual)
    {
        switch (newLineType)
        {
            case NewLineType.None:
                return this;
            case NewLineType.Manual:
            {
                var len = SpannedRecordCodec.EncodeInsertManualNewLine(default);
                this.AddRecordAndReserveData(SpannedRecordType.InsertionManualNewLine, len, out var data);
                SpannedRecordCodec.EncodeInsertManualNewLine(data);
                return this;
            }

            case NewLineType.Cr:
                this.textStream.WriteByte((byte)'\r');
                return this;
            case NewLineType.Lf:
                this.textStream.WriteByte((byte)'\n');
                return this;
            case NewLineType.CrLf:
                this.textStream.Write("\r\n"u8);
                return this;
            default:
                throw
                    byte.PopCount((byte)newLineType) > 1
                        ? throw new ArgumentOutOfRangeException(
                              nameof(newLineType),
                              newLineType,
                              "Multiple flags are disallowed.")
                        : new InvalidEnumArgumentException(
                            nameof(newLineType),
                            (int)newLineType,
                            typeof(NewLineType));
        }
    }

    /// <inheritdoc/>
    public SpannedStringBuilder AppendLine(
        ReadOnlySpan<char> span,
        NewLineType newLineType = NewLineType.Manual)
    {
        this.Append(span);
        this.AppendLine(newLineType);
        return this;
    }

    /// <inheritdoc/>
    public SpannedStringBuilder AppendLine(
        ReadOnlySpan<byte> span,
        NewLineType newLineType = NewLineType.Manual)
    {
        this.Append(span);
        this.AppendLine(newLineType);
        return this;
    }

    private SpannedStringBuilder AppendSpanFormattable<T>(T value, int repeat = 1) where T : ISpanFormattable
    {
        if (repeat < 1)
            return this;

        this.appendBuffer ??= new();
        while (true)
        {
            var buf = MemoryMarshal.Cast<byte, char>(this.appendBuffer.GetDataSpan());
            if (value.TryFormat(buf, out var written, default, null))
                return this.Append(buf[..written], repeat);

            this.appendBuffer.SetLength(Math.Min(Array.MaxLength, Math.Max(64, this.appendBuffer.Length * 2)));
        }
    }
}
