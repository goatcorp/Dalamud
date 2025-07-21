using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

namespace Dalamud.Bindings.ImGui;

[StructLayout(LayoutKind.Sequential, Size = TotalBufferSize)]
[InterpolatedStringHandler]
public ref struct AutoUtf8Buffer : IDisposable
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

    public AutoUtf8Buffer(int literalLength, int formattedCount) : this(ReadOnlySpan<byte>.Empty)
    {
        this.state |= State.Interpolation;
        literalLength += formattedCount * 4;
        if (literalLength >= FixedBufferSize)
            IncreaseBuffer(out _, literalLength);
    }

    public AutoUtf8Buffer(int literalLength, int formattedCount, IFormatProvider? formatProvider)
        : this(literalLength, formattedCount)
    {
        this.formatProvider = formatProvider;
    }

    public AutoUtf8Buffer(ReadOnlySpan<byte> text)
    {
        this.state = State.Initialized;
        if (text.IsEmpty)
        {
            unsafe
            {
                this.span = MemoryMarshal.CreateSpan(ref this.fixedBuffer[0], 0);
                this.fixedBuffer[0] = 0;
            }

            this.state |= State.NullTerminated | State.OwnedSpan;
        }
        else
        {
            this.span = text;
            if (Unsafe.Add(ref Unsafe.AsRef(in text[0]), text.Length) == 0)
                this.state |= State.NullTerminated;
        }
    }

    public AutoUtf8Buffer(ReadOnlyMemory<byte> text) : this(text.Span)
    {
    }

    public unsafe AutoUtf8Buffer(ReadOnlySpan<char> text)
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

    public AutoUtf8Buffer(ReadOnlyMemory<char> text) : this(text.Span)
    {
    }

    public AutoUtf8Buffer(string? text) : this(text.AsSpan())
    {
    }

    public unsafe AutoUtf8Buffer(byte* text) : this(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(text))
    {
        this.state |= State.NullTerminated;
    }

    public unsafe AutoUtf8Buffer(char* text) : this(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(text))
    {
    }

    public readonly bool IsInitialized => (this.state & State.Initialized) != 0;

    public readonly unsafe ReadOnlySpan<byte> Span =>
        (this.state & State.OwnedSpan) != 0
            ? MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in this.fixedBuffer[0]), this.span.Length)
            : this.span;

    public readonly int Length => this.span.Length;

    public readonly bool IsEmpty => this.span.IsEmpty;

    public unsafe ReadOnlySpan<byte> NullTerminatedSpan
    {
        get
        {
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

            return this.span;
        }
    }

    private unsafe Span<byte> EffectiveBuffer =>
        this.rentedBuffer is { } rentedBuffer
            ? rentedBuffer.AsSpan()
            : MemoryMarshal.CreateSpan(ref this.fixedBuffer[0], FixedBufferSize);

    private Span<byte> RemainingBuffer => this.EffectiveBuffer[this.span.Length..];

    public static implicit operator AutoUtf8Buffer(ReadOnlySpan<byte> text) => new(text);
    public static implicit operator AutoUtf8Buffer(ReadOnlyMemory<byte> text) => new(text);
    public static implicit operator AutoUtf8Buffer(Span<byte> text) => new(text);
    public static implicit operator AutoUtf8Buffer(Memory<byte> text) => new(text);
    public static implicit operator AutoUtf8Buffer(ReadOnlySpan<char> text) => new(text);
    public static implicit operator AutoUtf8Buffer(ReadOnlyMemory<char> text) => new(text);
    public static implicit operator AutoUtf8Buffer(Span<char> text) => new(text);
    public static implicit operator AutoUtf8Buffer(Memory<char> text) => new(text);
    public static implicit operator AutoUtf8Buffer(string? text) => new(text);
    public static unsafe implicit operator AutoUtf8Buffer(byte* text) => new(text);
    public static unsafe implicit operator AutoUtf8Buffer(char* text) => new(text);

    public void Dispose()
    {
        if (this.rentedBuffer is { } rentedBuffer)
        {
            this.rentedBuffer = null;
            this.span = default;
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }

        this.state = State.None;
    }

    public AutoUtf8Buffer MoveOrDefault([InterpolatedStringHandlerArgument] AutoUtf8Buffer other)
    {
        if (this.IsInitialized)
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

    public void AppendFormatted<T>(T value) => this.AppendFormatted(value, null);

    public void AppendFormatted<T>(T value, string? format)
    {
        var remaining = this.RemainingBuffer;
        int written;
        while (!Utf8.TryWrite(remaining, this.formatProvider, $"{value}", out written))
            this.IncreaseBuffer(out remaining);

        var newSpan = this.EffectiveBuffer[..(this.span.Length + written + 1)];
        newSpan[^1] = 0;
        this.span = newSpan[..^1];
    }

    public void AppendFormatted<T>(T value, int alignment) => this.AppendFormatted(value, alignment, null);

    public void AppendFormatted<T>(T value, int alignment, string? format)
    {
        var startingPos = this.span.Length;
        this.AppendFormatted(value, format);
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
