using System.Numerics;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImAnim;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ImAnimVariationVec4
{
    public ImAnimVariationMode Mode;
    public Vector4 Amount;
    public Vector4 MinClamp;
    public Vector4 MaxClamp;
    public uint Seed;
    public delegate* unmanaged[Cdecl]<int, void*, Vector4> Callback;
    public void* User;
    public ImAnimVariationFloat X;
    public ImAnimVariationFloat Y;
    public ImAnimVariationFloat Z;
    public ImAnimVariationFloat W;

    public static ImAnimVariationVec4 None => new()
    {
        Mode = ImAnimVariationMode.None,
        Amount = Vector4.Zero,
        MinClamp = new Vector4(float.MinValue),
        MaxClamp = new Vector4(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
        Z = ImAnimVariationFloat.None,
        W = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationVec4 Inc(Vector4 amt) => new()
    {
        Mode = ImAnimVariationMode.Increment,
        Amount = amt,
        MinClamp = new Vector4(float.MinValue),
        MaxClamp = new Vector4(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
        Z = ImAnimVariationFloat.None,
        W = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationVec4 Inc(float x, float y, float z, float w) => new()
    {
        Mode = ImAnimVariationMode.Increment,
        Amount = new Vector4(x, y, z, w),
        MinClamp = new Vector4(float.MinValue),
        MaxClamp = new Vector4(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
        Z = ImAnimVariationFloat.None,
        W = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationVec4 Dec(Vector4 amt) => new()
    {
        Mode = ImAnimVariationMode.Decrement,
        Amount = amt,
        MinClamp = new Vector4(float.MinValue),
        MaxClamp = new Vector4(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
        Z = ImAnimVariationFloat.None,
        W = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationVec4 Dec(float x, float y, float z, float w) => new()
    {
        Mode = ImAnimVariationMode.Decrement,
        Amount = new Vector4(x, y, z, w),
        MinClamp = new Vector4(float.MinValue),
        MaxClamp = new Vector4(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
        Z = ImAnimVariationFloat.None,
        W = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationVec4 Mul(float f) => new()
    {
        Mode = ImAnimVariationMode.Multiply,
        Amount = new Vector4(f),
        MinClamp = new Vector4(float.MinValue),
        MaxClamp = new Vector4(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
        Z = ImAnimVariationFloat.None,
        W = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationVec4 Rand(Vector4 amt) => new()
    {
        Mode = ImAnimVariationMode.Random,
        Amount = amt,
        MinClamp = new Vector4(float.MinValue),
        MaxClamp = new Vector4(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
        Z = ImAnimVariationFloat.None,
        W = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationVec4 Rand(float x, float y, float z, float w) => new()
    {
        Mode = ImAnimVariationMode.Random,
        Amount = new Vector4(x, y, z, w),
        MinClamp = new Vector4(float.MinValue),
        MaxClamp = new Vector4(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
        Z = ImAnimVariationFloat.None,
        W = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationVec4 Fn(ImAnim.VariationVec4Fn fn, void* userData = null) => new()
    {
        Mode = ImAnimVariationMode.Callback,
        Amount = Vector4.Zero,
        MinClamp = new Vector4(float.MinValue),
        MaxClamp = new Vector4(float.MaxValue),
        Seed = 0,
        Callback = (delegate* unmanaged[Cdecl]<int, void*, Vector4>)Marshal.GetFunctionPointerForDelegate(fn),
        User = userData,
        X = ImAnimVariationFloat.None,
        Y = ImAnimVariationFloat.None,
        Z = ImAnimVariationFloat.None,
        W = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationVec4 Axis(ImAnimVariationFloat vx, ImAnimVariationFloat vy, ImAnimVariationFloat vz, ImAnimVariationFloat vw) => new()
    {
        Mode = ImAnimVariationMode.None,
        Amount = Vector4.Zero,
        MinClamp = new Vector4(float.MinValue),
        MaxClamp = new Vector4(float.MaxValue),
        Seed = 0,
        Callback = null,
        User = null,
        X = vx,
        Y = vy,
        Z = vz,
        W = vw,
    };
}
