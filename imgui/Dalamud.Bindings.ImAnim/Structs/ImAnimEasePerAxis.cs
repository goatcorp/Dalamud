using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImAnim;

[StructLayout(LayoutKind.Sequential)]
public struct ImAnimEasePerAxis
{
    public ImAnimEaseDesc X;
    public ImAnimEaseDesc Y;
    public ImAnimEaseDesc Z;
    public ImAnimEaseDesc W;

    public static ImAnimEasePerAxis From(ImAnimEaseDesc all) => new()
    {
        X = all,
        Y = all,
        Z = all,
        W = all
    };

    public static ImAnimEasePerAxis From(ImAnimEaseDesc ex, ImAnimEaseDesc ey) => new()
    {
        X = ex,
        Y = ey,
        Z = new ImAnimEaseDesc { Type = ImAnimEaseType.Linear },
        W = new ImAnimEaseDesc { Type = ImAnimEaseType.Linear }
    };
}
