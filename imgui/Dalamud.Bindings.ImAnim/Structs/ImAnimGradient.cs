using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Bindings.ImAnim;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ImAnimGradient
{
    public ImVector<Vector2> Positions;
    public ImVector<Vector2> Colors;
}
