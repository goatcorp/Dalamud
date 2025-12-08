using System.Numerics;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImAnim;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ImAnimVariationVec2
{
    public ImAnimVariationMode Mode;
    public Vector2 Amount;
    public Vector2 MinClamp;
    public Vector2 MaxClamp;
    public uint Seed;
    public delegate* unmanaged[Cdecl]<int, void*, Vector2> Callback;
    public void* User;
    public ImAnimVariationFloat X;
    public ImAnimVariationFloat Y;

    public static ImAnimVariationVec2 None => new()
    {
        Mode = ImAnimVariationMode.None,
        Amount = Vector2.Zero,
        MinClamp = new Vector2(float.MinValue),
        MaxClamp = new Vector2(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationVec2 Inc(Vector2 amt) => new()
    {
        Mode = ImAnimVariationMode.Increment,
        Amount = amt,
        MinClamp = new Vector2(float.MinValue),
        MaxClamp = new Vector2(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationVec2 Inc(float x, float y) => new()
    {
        Mode = ImAnimVariationMode.Increment,
        Amount = new Vector2(x, y),
        MinClamp = new Vector2(float.MinValue),
        MaxClamp = new Vector2(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationVec2 Dec(Vector2 amt) => new()
    {
        Mode = ImAnimVariationMode.Decrement,
        Amount = amt,
        MinClamp = new Vector2(float.MinValue),
        MaxClamp = new Vector2(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationVec2 Dec(float x, float y) => new()
    {
        Mode = ImAnimVariationMode.Decrement,
        Amount = new Vector2(x, y),
        MinClamp = new Vector2(float.MinValue),
        MaxClamp = new Vector2(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationVec2 Mul(float f) => new()
    {
        Mode = ImAnimVariationMode.Multiply,
        Amount = new Vector2(f),
        MinClamp = new Vector2(float.MinValue),
        MaxClamp = new Vector2(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationVec2 Rand(Vector2 amt) => new()
    {
        Mode = ImAnimVariationMode.Random,
        Amount = amt,
        MinClamp = new Vector2(float.MinValue),
        MaxClamp = new Vector2(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationVec2 Rand(float x, float y) => new()
    {
        Mode = ImAnimVariationMode.Random,
        Amount = new Vector2(x, y),
        MinClamp = new Vector2(float.MinValue),
        MaxClamp = new Vector2(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
    };

    // TODO: iam_variation_vec2_fn
    public static ImAnimVariationVec2 Fn(ImAnim.VariationVec2Fn fn, void* userData = null) => new()
    {
        Mode = ImAnimVariationMode.Callback,
        Amount = Vector2.Zero,
        MinClamp = new Vector2(float.MinValue),
        MaxClamp = new Vector2(float.MaxValue),
        Seed = 0,
        Callback = (delegate* unmanaged[Cdecl]<int, void*, Vector2>)Marshal.GetFunctionPointerForDelegate(fn),
        User = userData,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationVec2 Axis(ImAnimVariationFloat vx, ImAnimVariationFloat vy) => new()
    {
        Mode = ImAnimVariationMode.None,
        Amount = Vector2.Zero,
        MinClamp = new Vector2(float.MinValue),
        MaxClamp = new Vector2(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = vx,
        Y = vy,
    };
}
