using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

namespace Dalamud.Bindings.ImGui;

[InterpolatedStringHandler]
public ref struct ImU8String
{
    public const int AllocFreeBufferSize = 512;
    private const int MinimumRentSize = AllocFreeBufferSize * 2;

    private IFormatProvider? formatProvider;
    private byte[]? rentedBuffer;
    private ref readonly byte externalFirstByte;
    private State state;
    private FixedBufferContainer fixedBuffer;

    [Flags]
    private enum State : byte
    {
        None = 0,
        Initialized = 1 << 0,
        NullTerminated = 1 << 1,
        Interpolation = 1 << 2,
    }

    public ImU8String()
    {
        Unsafe.SkipInit(out this.fixedBuffer);
        this.FixedBufferByteRef = 0;
    }

    public ImU8String(int literalLength, int formattedCount)
        : this(""u8)
    {
        this.state |= State.Interpolation;
        literalLength += formattedCount * 4;
        this.Reserve(literalLength);
    }

    public ImU8String(int literalLength, int formattedCount, IFormatProvider? formatProvider)
        : this(literalLength, formattedCount)
    {
        this.formatProvider = formatProvider;
    }

    public ImU8String(ReadOnlySpan<byte> text, bool ensureNullTermination = false)
        : this()
    {
        if (Unsafe.IsNullRef(in MemoryMarshal.GetReference(text)))
        {
            this.state = State.None;
            return;
        }

        this.state = State.Initialized;
        if (text.IsEmpty)
        {
            this.state |= State.NullTerminated;
        }
        else if (ensureNullTermination)
        {
            this.Reserve(text.Length + 1);
            var buffer = this.Buffer;
            text.CopyTo(buffer);
            buffer[^1] = 0;
            this.Length = text.Length;
            this.state |= State.NullTerminated;
        }
        else
        {
            this.externalFirstByte = ref text[0];
            this.Length = text.Length;
            if (Unsafe.Add(ref Unsafe.AsRef(in this.externalFirstByte), this.Length) == 0)
                this.state |= State.NullTerminated;
        }
    }

    public ImU8String(ReadOnlyMemory<byte> text, bool ensureNullTermination = false)
        : this(text.Span, ensureNullTermination)
    {
    }

    public ImU8String(ReadOnlySpan<char> text)
        : this()
    {
        if (Unsafe.IsNullRef(in MemoryMarshal.GetReference(text)))
        {
            this.state = State.None;
            return;
        }

        this.state = State.Initialized | State.NullTerminated;
        this.Length = Encoding.UTF8.GetByteCount(text);
        if (this.Length + 1 < AllocFreeBufferSize)
        {
            var newSpan = this.FixedBufferSpan[..this.Length];
            Encoding.UTF8.GetBytes(text, newSpan);
            this.FixedBufferSpan[this.Length] = 0;
        }
        else
        {
            this.rentedBuffer = ArrayPool<byte>.Shared.Rent(this.Length + 1);
            var newSpan = this.rentedBuffer.AsSpan(0, this.Length);
            Encoding.UTF8.GetBytes(text, newSpan);
            this.rentedBuffer[this.Length] = 0;
        }
    }

    public ImU8String(ReadOnlyMemory<char> text)
        : this(text.Span)
    {
    }

    public ImU8String(string? text)
        : this(text.AsSpan())
    {
    }

    public unsafe ImU8String(byte* text)
        : this(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(text))
    {
        this.state |= State.NullTerminated;
    }

    public unsafe ImU8String(char* text)
        : this(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(text))
    {
    }

    public static ImU8String Empty => default;

    public readonly ReadOnlySpan<byte> Span =>
        !Unsafe.IsNullRef(in this.externalFirstByte)
            ? MemoryMarshal.CreateReadOnlySpan(in this.externalFirstByte, this.Length)
            : this.rentedBuffer is { } rented
                ? rented.AsSpan(0, this.Length)
                : Unsafe.AsRef(in this).FixedBufferSpan[..this.Length];

    public int Length { get; private set; }

    public readonly bool IsNull => (this.state & State.Initialized) == 0;

    public readonly bool IsEmpty => this.Length == 0;

    internal Span<byte> Buffer
    {
        get
        {
            if (Unsafe.IsNullRef(in this.externalFirstByte))
                this.ConvertToOwned();

            return this.rentedBuffer is { } buf
                       ? buf.AsSpan()
                       : MemoryMarshal.Cast<FixedBufferContainer, byte>(new(ref Unsafe.AsRef(ref this.fixedBuffer)));
        }
    }

    private Span<byte> RemainingBuffer => this.Buffer[this.Length..];

    private ref byte FixedBufferByteRef => ref this.FixedBufferSpan[0];

    private Span<byte> FixedBufferSpan =>
        MemoryMarshal.Cast<FixedBufferContainer, byte>(new(ref Unsafe.AsRef(ref this.fixedBuffer)));

    public static implicit operator ImU8String(ReadOnlySpan<byte> text) => new(text);
    public static implicit operator ImU8String(ReadOnlyMemory<byte> text) => new(text);
    public static implicit operator ImU8String(Span<byte> text) => new(text);
    public static implicit operator ImU8String(Memory<byte> text) => new(text);
    public static implicit operator ImU8String(byte[]? text) => new(text.AsSpan());
    public static implicit operator ImU8String(ReadOnlySpan<char> text) => new(text);
    public static implicit operator ImU8String(ReadOnlyMemory<char> text) => new(text);
    public static implicit operator ImU8String(Span<char> text) => new(text);
    public static implicit operator ImU8String(Memory<char> text) => new(text);
    public static implicit operator ImU8String(char[]? text) => new(text.AsSpan());
    public static implicit operator ImU8String(string? text) => new(text);
    public static unsafe implicit operator ImU8String(byte* text) => new(text);
    public static unsafe implicit operator ImU8String(char* text) => new(text);

    public ref readonly byte GetPinnableReference()
    {
        if (this.IsNull)
            return ref Unsafe.NullRef<byte>();

        if (this.IsEmpty)
            return ref this.FixedBufferSpan[0];

        return ref this.Span.GetPinnableReference();
    }

    public ref readonly byte GetPinnableReference(ReadOnlySpan<byte> defaultValue)
    {
        if (this.IsNull)
            return ref defaultValue.GetPinnableReference();

        if (this.IsEmpty)
            return ref this.FixedBufferSpan[0];

        return ref this.Span.GetPinnableReference();
    }

    public ref readonly byte GetPinnableNullTerminatedReference(ReadOnlySpan<byte> defaultValue = default)
    {
        if (this.IsNull)
            return ref defaultValue.GetPinnableReference();

        if (this.IsEmpty)
        {
            ref var t = ref this.FixedBufferSpan[0];
            t = 0;
            return ref t;
        }

        if ((this.state & State.NullTerminated) == 0)
            this.ConvertToOwned();

        return ref this.Span[0];
    }

    private void ConvertToOwned()
    {
        if (Unsafe.IsNullRef(in this.externalFirstByte))
            return;

        Debug.Assert(this.rentedBuffer is null);

        if (this.Length + 1 < AllocFreeBufferSize)
        {
            var fixedBufferSpan = this.FixedBufferSpan;
            this.Span.CopyTo(fixedBufferSpan);
            fixedBufferSpan[this.Length] = 0;
        }
        else
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(this.Length + 1);
            this.Span.CopyTo(newBuffer);

            newBuffer[this.Length] = 0;
            this.rentedBuffer = newBuffer;
        }

        this.state |= State.NullTerminated;
        this.externalFirstByte = ref Unsafe.NullRef<byte>();
    }

    public void Recycle()
    {
        if (this.rentedBuffer is { } buf)
        {
            this.rentedBuffer = null;
            ArrayPool<byte>.Shared.Return(buf);
        }
    }

    public ImU8String MoveOrDefault(ImU8String other)
    {
        if (!this.IsNull)
        {
            other.Recycle();
            var res = this;
            this = default;
            return res;
        }

        return other;
    }

    public override readonly string ToString() => Encoding.UTF8.GetString(this.Span);

    public void AppendLiteral(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        var remaining = this.RemainingBuffer;
        var len = Encoding.UTF8.GetByteCount(value);
        if (remaining.Length <= len)
            this.IncreaseBuffer(out remaining, this.Length + len + 1);
        this.Buffer[this.Length += Encoding.UTF8.GetBytes(value.AsSpan(), remaining)] = 0;
    }

    public void AppendFormatted(ReadOnlySpan<byte> value) => this.AppendFormatted(value, null);

    public void AppendFormatted(ReadOnlySpan<byte> value, string? format)
    {
        var remaining = this.RemainingBuffer;
        if (remaining.Length < value.Length + 1)
            this.IncreaseBuffer(out remaining, this.Length + value.Length + 1);
        value.CopyTo(remaining);
        this.Buffer[this.Length += value.Length] = 0;
    }

    public void AppendFormatted(ReadOnlySpan<byte> value, int alignment) =>
        this.AppendFormatted(value, alignment, null);

    public void AppendFormatted(ReadOnlySpan<byte> value, int alignment, string? format)
    {
        var startingPos = this.Length;
        this.AppendFormatted(value, format);
        this.FixAlignment(startingPos, alignment);
    }

    public void AppendFormatted(ReadOnlySpan<char> value) => this.AppendFormatted(value, null);

    public void AppendFormatted(ReadOnlySpan<char> value, string? format)
    {
        var remaining = this.RemainingBuffer;
        var len = Encoding.UTF8.GetByteCount(value);
        if (remaining.Length < len + 1)
            this.IncreaseBuffer(out remaining, this.Length + len + 1);
        this.Buffer[this.Length += Encoding.UTF8.GetBytes(value, remaining)] = 0;
    }

    public void AppendFormatted(ReadOnlySpan<char> value, int alignment) =>
        this.AppendFormatted(value, alignment, null);

    public void AppendFormatted(ReadOnlySpan<char> value, int alignment, string? format)
    {
        var startingPos = this.Length;
        this.AppendFormatted(value, format);
        this.FixAlignment(startingPos, alignment);
    }

    public void AppendFormatted(object? value) => this.AppendFormatted<object>(value!);
    public void AppendFormatted(object? value, string? format) => this.AppendFormatted<object>(value!, format);
    public void AppendFormatted(object? value, int alignment) => this.AppendFormatted<object>(value!, alignment);
    public void AppendFormatted(object? value, int alignment, string? format) =>
        this.AppendFormatted<object>(value!, alignment, format);

    public void AppendFormatted<T>(T value) => this.AppendFormatted(value, null);

    public void AppendFormatted<T>(T value, string? format)
    {
        var remaining = this.RemainingBuffer;
        if (remaining.Length < 1)
            this.IncreaseBuffer(out remaining);

        int written;
        while (true)
        {
            var handler = new Utf8.TryWriteInterpolatedStringHandler(1, 1, remaining[..^1], this.formatProvider, out _);
            handler.AppendFormatted(value, format);
            if (Utf8.TryWrite(remaining, this.formatProvider, ref handler, out written))
                break;
            this.IncreaseBuffer(out remaining);
        }

        this.Buffer[this.Length += written] = 0;
    }

    public void AppendFormatted<T>(T value, int alignment) => this.AppendFormatted(value, alignment, null);

    public void AppendFormatted<T>(T value, int alignment, string? format)
    {
        var startingPos = this.Length;
        this.AppendFormatted(value, format);
        this.FixAlignment(startingPos, alignment);
    }

    public void Reserve(int length)
    {
        if (length >= AllocFreeBufferSize)
            this.IncreaseBuffer(out _, length);
    }

    private void FixAlignment(int startingPos, int alignment)
    {
        var appendedLength = this.Length - startingPos;

        var leftAlign = alignment < 0;
        if (leftAlign)
            alignment = -alignment;

        var fillLength = alignment - appendedLength;
        if (fillLength <= 0)
            return;

        var destination = this.Buffer;
        if (fillLength > destination.Length - this.Length)
        {
            this.IncreaseBuffer(out _, fillLength + 1);
            destination = this.Buffer;
        }

        if (leftAlign)
        {
            destination.Slice(this.Length, fillLength).Fill((byte)' ');
        }
        else
        {
            destination.Slice(startingPos, appendedLength).CopyTo(destination[(startingPos + fillLength)..]);
            destination.Slice(startingPos, fillLength).Fill((byte)' ');
        }

        this.Buffer[this.Length += fillLength] = 0;
    }

    private void IncreaseBuffer(out Span<byte> remaining, int minCapacity = 0)
    {
        minCapacity = Math.Max(minCapacity, Math.Max(this.Buffer.Length * 2, MinimumRentSize));
        var newBuffer = ArrayPool<byte>.Shared.Rent(minCapacity);
        this.Span.CopyTo(newBuffer);
        newBuffer[this.Length] = 0;
        if (this.rentedBuffer is not null)
            ArrayPool<byte>.Shared.Return(this.rentedBuffer);

        this.rentedBuffer = newBuffer;
        this.externalFirstByte = ref Unsafe.NullRef<byte>();
        remaining = newBuffer.AsSpan(this.Length);
    }

    [StructLayout(LayoutKind.Sequential, Size = AllocFreeBufferSize)]
    private struct FixedBufferContainer;
}
