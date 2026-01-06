using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImAnim;

[StructLayout(LayoutKind.Sequential)]
public struct ImAnimEaseDesc
{
    public ImAnimEaseType Type;
    public float P0;
    public float P1;
    public float P2;
    public float P3;
}
