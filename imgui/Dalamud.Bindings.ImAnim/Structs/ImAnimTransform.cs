using System.Numerics;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImAnim;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ImAnimTransform
{
    public Vector2 Position;
    public Vector2 Scale;
    public float Rotation;
}
