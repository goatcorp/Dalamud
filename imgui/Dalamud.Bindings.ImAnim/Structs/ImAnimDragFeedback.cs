using System.Numerics;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImAnim;

[StructLayout(LayoutKind.Sequential)]
public struct ImAnimDragFeedback
{
    public Vector2 Position;
    public Vector2 Offset;
    public Vector2 Velocity;
    public bool IsDragging;
    public bool IsSnapping;
    public float SnapProgress;
}
