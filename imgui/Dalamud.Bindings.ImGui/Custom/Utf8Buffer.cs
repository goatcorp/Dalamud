using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

namespace Dalamud.Bindings.ImGui;

[StructLayout(LayoutKind.Sequential, Size = TotalBufferSize)]
[InterpolatedStringHandler]
public ref struct Utf8Buffer : IDisposable
{
    private const int TotalBufferSize = 1024;
    private const int FixedBufferSize = TotalBufferSize - 8 - 8 - 8 - 1;
    private const int MinimumRentSize = TotalBufferSize * 2;
    private byte[]? rentedBuffer;
    private ReadOnlySpan<byte> span;
    private IFormatProvider? formatProvider;
    private State state;
    private unsafe fixed byte fixedBuffer[FixedBufferSize];

    [Flags]
    private enum State : byte
    {
        None = 0,
        Initialized = 1 << 0,
        NullTerminated = 1 << 1,
        Interpolation = 1 << 2,
        OwnedSpan = 1 << 3,
    }

    public Utf8Buffer(int literalLength, int formattedCount) : this(ReadOnlySpan<byte>.Empty)
    {
        this.state |= State.Interpolation;
        literalLength += formattedCount * 4;
        if (literalLength >= FixedBufferSize)
            IncreaseBuffer(out _, literalLength);
    }

    public Utf8Buffer(int literalLength, int formattedCount, IFormatProvider? formatProvider) : this(
        literalLength,
        formattedCount)
    {
        this.formatProvider = formatProvider;
    }

    public unsafe Utf8Buffer(ReadOnlySpan<byte> text)
    {
        this.state = State.Initialized;
        if (text.IsEmpty)
        {
            this.span = MemoryMarshal.CreateSpan(ref this.fixedBuffer[0], 0);
            this.fixedBuffer[0] = 0;
            this.state |= State.NullTerminated | State.OwnedSpan;
        }
        else
        {
            this.span = text;
            if (Unsafe.Add(ref Unsafe.AsRef(in text[0]), text.Length) == 0) this.state |= State.NullTerminated;
        }
    }

    public Utf8Buffer(ReadOnlyMemory<byte> text) : this(text.Span)
    {
    }

    public unsafe Utf8Buffer(ReadOnlySpan<char> text)
    {
        this.state = State.Initialized | State.NullTerminated;
        var cb = Encoding.UTF8.GetByteCount(text);
        if (cb + 1 < FixedBufferSize)
        {
            var newSpan = MemoryMarshal.CreateSpan(ref this.fixedBuffer[0], cb);
            this.span = newSpan;
            Encoding.UTF8.GetBytes(text, newSpan);
            this.fixedBuffer[cb] = 0;
            this.state |= State.OwnedSpan;
        }
        else
        {
            this.rentedBuffer = ArrayPool<byte>.Shared.Rent(cb + 1);
            var newSpan = this.rentedBuffer.AsSpan(0, cb);
            this.span = newSpan;
            Encoding.UTF8.GetBytes(text, newSpan);
            this.rentedBuffer[cb] = 0;
        }
    }

    public Utf8Buffer(ReadOnlyMemory<char> text) : this(text.Span)
    {
    }

    public Utf8Buffer(string? text) : this(text.AsSpan())
    {
    }

    public unsafe Utf8Buffer(byte* text) : this(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(text))
    {
        this.state |= State.NullTerminated;
    }

    public unsafe Utf8Buffer(char* text) : this(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(text))
    {
    }

    public static Utf8Buffer Empty => default;

    public readonly unsafe ReadOnlySpan<byte> Span =>
        (this.state & State.OwnedSpan) != 0
            ? MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in this.fixedBuffer[0]), this.span.Length)
            : this.span;

    public readonly int Length => this.span.Length;

    public readonly bool IsNull => (this.state & State.Initialized) == 0;

    public readonly bool IsEmpty => this.span.IsEmpty;

    public unsafe ref readonly byte GetPinnableNullTerminatedReference(ReadOnlySpan<byte> defaultValue = default)
    {
        if (this.IsNull)
            return ref defaultValue.GetPinnableReference();

        if (this.IsEmpty)
        {
            this.fixedBuffer[0] = 0;
            return ref this.fixedBuffer[0];
        }

        if ((this.state & State.OwnedSpan) != 0)
            this.span = MemoryMarshal.CreateSpan(ref this.fixedBuffer[0], this.span.Length);

        if ((this.state & State.NullTerminated) == 0)
        {
            if (this.span.Length + 1 < FixedBufferSize)
            {
                var newSpan = MemoryMarshal.CreateSpan(ref this.fixedBuffer[0], this.span.Length);
                this.span.CopyTo(newSpan);
                this.fixedBuffer[newSpan.Length] = 0;
                this.span = newSpan;
            }
            else
            {
                this.rentedBuffer = ArrayPool<byte>.Shared.Rent(this.span.Length + 1);
                var newSpan = this.rentedBuffer.AsSpan(0, this.span.Length);
                this.span.CopyTo(newSpan);
                this.rentedBuffer[newSpan.Length] = 0;
            }

            this.state |= State.NullTerminated;
        }

        return ref this.span[0];
    }

    private unsafe Span<byte> EffectiveBuffer =>
        this.rentedBuffer is { } buf
            ? buf.AsSpan()
            : MemoryMarshal.CreateSpan(ref this.fixedBuffer[0], FixedBufferSize);

    private Span<byte> RemainingBuffer => this.EffectiveBuffer[this.span.Length..];

    public static implicit operator Utf8Buffer(ReadOnlySpan<byte> text) => new(text);
    public static implicit operator Utf8Buffer(ReadOnlyMemory<byte> text) => new(text);
    public static implicit operator Utf8Buffer(Span<byte> text) => new(text);
    public static implicit operator Utf8Buffer(Memory<byte> text) => new(text);
    public static implicit operator Utf8Buffer(byte[]? text) => new(text.AsSpan());
    public static implicit operator Utf8Buffer(ReadOnlySpan<char> text) => new(text);
    public static implicit operator Utf8Buffer(ReadOnlyMemory<char> text) => new(text);
    public static implicit operator Utf8Buffer(Span<char> text) => new(text);
    public static implicit operator Utf8Buffer(Memory<char> text) => new(text);
    public static implicit operator Utf8Buffer(char[]? text) => new(text.AsSpan());
    public static implicit operator Utf8Buffer(string? text) => new(text);
    public static unsafe implicit operator Utf8Buffer(byte* text) => new(text);
    public static unsafe implicit operator Utf8Buffer(char* text) => new(text);

    public void Dispose()
    {
        if (this.rentedBuffer is { } buf)
        {
            this.rentedBuffer = null;
            this.span = default;
            ArrayPool<byte>.Shared.Return(buf);
        }

        this.state = State.None;
    }

    public Utf8Buffer MoveOrDefault(Utf8Buffer other)
    {
        if (!this.IsNull)
        {
            other.Dispose();
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
            this.IncreaseBuffer(out remaining, this.span.Length + len + 1);
        Encoding.UTF8.GetBytes(value.AsSpan(), remaining);
        var newSpan = this.EffectiveBuffer[..(this.span.Length + len + 1)];
        newSpan[^1] = 0;
        this.span = newSpan[..^1];
    }

    public void AppendFormatted(ReadOnlySpan<byte> value) => this.AppendFormatted(value, null);

    public void AppendFormatted(ReadOnlySpan<byte> value, string? format)
    {
        var remaining = this.RemainingBuffer;
        if (remaining.Length < value.Length + 1)
            this.IncreaseBuffer(out remaining, this.span.Length + value.Length + 1);
        value.CopyTo(remaining);
        var newSpan = this.EffectiveBuffer[..(this.span.Length + value.Length + 1)];
        newSpan[^1] = 0;
        this.span = newSpan[..^1];
    }

    public void AppendFormatted(ReadOnlySpan<byte> value, int alignment) => this.AppendFormatted(value, alignment, null);

    public void AppendFormatted(ReadOnlySpan<byte> value, int alignment, string? format)
    {
        var startingPos = this.span.Length;
        this.AppendFormatted(value, format);
        FixAlignment(startingPos, alignment);
    }

    public void AppendFormatted(ReadOnlySpan<char> value) => this.AppendFormatted(value, null);

    public void AppendFormatted(ReadOnlySpan<char> value, string? format)
    {
        var remaining = this.RemainingBuffer;
        var len = Encoding.UTF8.GetByteCount(value);
        if (remaining.Length < len + 1)
            this.IncreaseBuffer(out remaining, this.span.Length + len + 1);
        Encoding.UTF8.GetBytes(value, remaining);

        var newSpan = this.EffectiveBuffer[..(this.span.Length + len + 1)];
        newSpan[^1] = 0;
        this.span = newSpan[..^1];
    }

    public void AppendFormatted(ReadOnlySpan<char> value, int alignment) => this.AppendFormatted(value, alignment, null);

    public void AppendFormatted(ReadOnlySpan<char> value, int alignment, string? format)
    {
        var startingPos = this.span.Length;
        this.AppendFormatted(value, format);
        FixAlignment(startingPos, alignment);
    }

    public void AppendFormatted<T>(T value) => this.AppendFormatted(value, null);

    public void AppendFormatted<T>(T value, string? format)
    {
        var remaining = this.RemainingBuffer;
        int written;
        while (!Utf8.TryWrite(remaining, this.formatProvider, $"{value}\0", out written))
            this.IncreaseBuffer(out remaining);

        this.span = this.EffectiveBuffer[..(this.span.Length + written - 1)];
    }

    public void AppendFormatted<T>(T value, int alignment) => this.AppendFormatted(value, alignment, null);

    public void AppendFormatted<T>(T value, int alignment, string? format)
    {
        var startingPos = this.span.Length;
        this.AppendFormatted(value, format);
        FixAlignment(startingPos, alignment);
    }

    private void FixAlignment(int startingPos, int alignment)
    {
        var appendedLength = this.span.Length - startingPos;

        var leftAlign = alignment < 0;
        if (leftAlign)
            alignment = -alignment;

        var fillLength = alignment - appendedLength;
        if (fillLength <= 0)
            return;

        var destination = this.EffectiveBuffer;
        if (fillLength > destination.Length - this.span.Length)
        {
            this.IncreaseBuffer(out _, fillLength + 1);
            destination = this.EffectiveBuffer;
        }

        if (leftAlign)
        {
            destination.Slice(this.span.Length, fillLength).Fill((byte)' ');
        }
        else
        {
            destination.Slice(startingPos, appendedLength).CopyTo(destination[(startingPos + fillLength)..]);
            destination.Slice(startingPos, fillLength).Fill((byte)' ');
        }

        var newSpan = destination[..(this.span.Length + fillLength + 1)];
        newSpan[^1] = 0;
        this.span = newSpan[..^1];
    }

    private void IncreaseBuffer(out Span<byte> remaining, int minCapacity = 0)
    {
        minCapacity = Math.Max(minCapacity, Math.Max(this.EffectiveBuffer.Length * 2, MinimumRentSize));
        var newBuffer = ArrayPool<byte>.Shared.Rent(minCapacity);
        this.Span.CopyTo(newBuffer);
        newBuffer[this.span.Length] = 0;
        this.span = newBuffer.AsSpan(0, this.span.Length);
        if (this.rentedBuffer is not null)
            ArrayPool<byte>.Shared.Return(this.rentedBuffer);

        this.rentedBuffer = newBuffer;
        this.state &= ~State.OwnedSpan;
        remaining = newBuffer.AsSpan(this.span.Length);
    }
}
