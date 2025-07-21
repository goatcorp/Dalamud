using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Bindings.ImGui;

[StructLayout(LayoutKind.Sequential)]
public ref struct AutoUtf8Buffer : IDisposable
{
    private const int StackBufferSize = 1024 - 1 - 8 - 8;

    private byte[]? RentedBuffer;
    private ReadOnlySpan<byte> span;
    private State state;
    private unsafe fixed byte buffer[StackBufferSize];

    [Flags]
    private enum State
    {
        None = 0,
        Initialized = 1 << 0,
        NullTerminated = 1 << 1,
    }

    public AutoUtf8Buffer(ReadOnlySpan<byte> text)
    {
        this.state = State.Initialized;
        if (text.IsEmpty)
        {
            unsafe
            {
                this.span = MemoryMarshal.CreateSpan(ref this.buffer[0], 0);
                this.buffer[0] = 0;
            }

            this.state |= State.NullTerminated;
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

    public AutoUtf8Buffer(ReadOnlySpan<char> text)
    {
        this.state = State.Initialized | State.NullTerminated;
        var cb = Encoding.UTF8.GetByteCount(text);
        if (cb + 1 < StackBufferSize)
        {
            unsafe
            {
                var newSpan = MemoryMarshal.CreateSpan(ref this.buffer[0], cb);
                this.span = newSpan;
                Encoding.UTF8.GetBytes(text, newSpan);
                this.buffer[cb] = 0;
            }
        }
        else
        {
            this.RentedBuffer = ArrayPool<byte>.Shared.Rent(cb + 1);
            var newSpan = this.RentedBuffer.AsSpan(0, cb);
            this.span = newSpan;
            Encoding.UTF8.GetBytes(text, newSpan);
            this.RentedBuffer[cb] = 0;
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

    public readonly ReadOnlySpan<byte> Span => this.span;

    public readonly int Length => this.span.Length;

    public readonly bool IsEmpty => this.span.IsEmpty;

    public ReadOnlySpan<byte> NullTerminatedSpan
    {
        get
        {
            if ((this.state & State.NullTerminated) != 0)
            {
                if (this.Span.Length + 1 < StackBufferSize)
                {
                    unsafe
                    {
                        var newSpan = MemoryMarshal.CreateSpan(ref this.buffer[0], this.span.Length);
                        this.span.CopyTo(newSpan);
                        this.buffer[newSpan.Length] = 0;
                        this.span = newSpan;
                    }
                }
                else
                {
                    this.RentedBuffer = ArrayPool<byte>.Shared.Rent(this.span.Length + 1);
                    var newSpan = this.RentedBuffer.AsSpan(0, this.span.Length);
                    this.span.CopyTo(newSpan);
                    this.RentedBuffer[newSpan.Length] = 0;
                }

                this.state |= State.NullTerminated;
            }

            return this.span;
        }
    }

    public static implicit operator AutoUtf8Buffer(ReadOnlySpan<byte> text) => new(text);
    public static implicit operator AutoUtf8Buffer(ReadOnlyMemory<byte> text) => new(text);
    public static implicit operator AutoUtf8Buffer(ReadOnlySpan<char> text) => new(text);
    public static implicit operator AutoUtf8Buffer(ReadOnlyMemory<char> text) => new(text);
    public static implicit operator AutoUtf8Buffer(string? text) => new(text);
    public static unsafe implicit operator AutoUtf8Buffer(byte* text) => new(text);
    public static unsafe implicit operator AutoUtf8Buffer(char* text) => new(text);

    public void Dispose()
    {
        if (this.RentedBuffer is { } rentedBuffer)
        {
            this.RentedBuffer = null;
            this.span = default;
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }

        this.state = State.None;
    }

    public AutoUtf8Buffer MoveOrDefault(AutoUtf8Buffer other)
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

    public override readonly string ToString() => Encoding.UTF8.GetString(this.span);
}
