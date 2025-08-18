using System.Numerics;

namespace Dalamud.Interface.Windowing;

public struct WindowSizeConstraints
{
    public Vector2 MinimumSize { get; set; }
    public Vector2 MaximumSize { get; set; }
}
