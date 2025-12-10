using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImAnim;

[StructLayout(LayoutKind.Sequential)]
public struct ImAnimMorphOpts
{
    public int Samples;
    public bool MatchEndpoints;
    public bool UseArcLength;

    public static ImAnimMorphOpts Default() => new() { Samples = 64, MatchEndpoints = true, UseArcLength = true };
}
