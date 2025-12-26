using System.Numerics;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImAnim;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ImAnimDragOpts
{
    public Vector2 SnapGrid;
    public Vector2* SnapPoints;
    public int SnapPointsCount;
    public float SnapDuration;
    public float Overshoot;
    public ImAnimEaseType EaseType;

    public static ImAnimDragOpts Default() => new() { SnapDuration = 0.2f, EaseType = ImAnimEaseType.OutCubic };
}
