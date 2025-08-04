using System.Runtime.CompilerServices;
using System.Text;

namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImGuiTextBuffer
{
    public readonly Span<byte> Span => new(this.Buf.Data, this.Buf.Size);

    public override readonly string ToString() => Encoding.UTF8.GetString(this.Span);

    public void append(ImU8String str)
    {
        fixed (ImGuiTextBuffer* thisPtr = &this)
            ImGui.append(thisPtr, str);
    }
}

public unsafe partial struct ImGuiTextBufferPtr
{
    public readonly Span<byte> Span => new(Unsafe.AsRef(in this).Buf.Data, Unsafe.AsRef(in this).Buf.Size);

    public override readonly string ToString() => Encoding.UTF8.GetString(this.Span);

    public void append(ImU8String str) => ImGui.append(this, str);
}
