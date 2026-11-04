using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImGui;

public partial struct ImDrawVert
{
    /// <summary>Gets the extra data of the vertex. It is opaque, except that a first item of 0 means the rest is
    /// unused.</summary>
    [UnscopedRef]
    public Span<uint> Ext => MemoryMarshal.CreateSpan(ref this.Ext_0, 11);
}
