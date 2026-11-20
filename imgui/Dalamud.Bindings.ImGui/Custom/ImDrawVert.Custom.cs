using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImGui;

public partial struct ImDrawVert
{
    /// <summary>Gets the custom backend payload for the vertex.</summary>
    /// <remarks><c>Ext[0] == 0</c> marks an ordinary vertex with <c>Ext[1..4]</c> unused.</remarks>
    [UnscopedRef]
    public Span<uint> Ext => MemoryMarshal.CreateSpan(ref this.Ext_0, 5);
}
