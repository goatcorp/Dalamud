using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImAnim;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct ImAnimSpringParams
{
    public float Mass;
    public float Stiffness;
    public float Damping;
    public float InitialVelocity;
}
