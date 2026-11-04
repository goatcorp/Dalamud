using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImGuiViewport
{
    /// <summary>Gets the debug name of the viewport.</summary>
    /// <returns>The name as UTF-8 bytes.</returns>
    public ReadOnlySpan<byte> GetDebugNameU8()
    {
        fixed (ImGuiViewport* thisPtr = &this)
            return MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ImGuiNative.GetDebugName(thisPtr));
    }

    /// <summary>Gets the debug name of the viewport.</summary>
    /// <returns>The name.</returns>
    public string GetDebugName() => Encoding.UTF8.GetString(this.GetDebugNameU8());
}

public unsafe partial struct ImGuiViewportPtr
{
    /// <summary>Gets the debug name of the viewport.</summary>
    /// <returns>The name as UTF-8 bytes.</returns>
    public ReadOnlySpan<byte> GetDebugNameU8() =>
        MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ImGuiNative.GetDebugName(this.Handle));

    /// <summary>Gets the debug name of the viewport.</summary>
    /// <returns>The name.</returns>
    public string GetDebugName() => Encoding.UTF8.GetString(this.GetDebugNameU8());
}
