using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImAnim;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ImAnimVariationFloat
{
    public ImAnimVariationMode Mode;
    public float Amount;
    public float MinClamp;
    public float MaxClamp;
    public uint Seed;
    public delegate* unmanaged[Cdecl]<int, void*, float> Callback;
    public void* User;

    public static ImAnimVariationFloat None => new()
    {
        Mode = ImAnimVariationMode.None,
        Amount = 0,
        MinClamp = float.MinValue,
        MaxClamp = float.MaxValue,
        Seed = 0,
        Callback = null,
        User = null
    };

    public static ImAnimVariationFloat Inc(float amt) => new()
    {
        Mode = ImAnimVariationMode.Increment,
        Amount = amt,
        MinClamp = float.MinValue,
        MaxClamp = float.MaxValue,
        Seed = 0,
        Callback = null,
        User = null
    };

    public static ImAnimVariationFloat Dec(float amt) => new()
    {
        Mode = ImAnimVariationMode.Decrement,
        Amount = amt,
        MinClamp = float.MinValue,
        MaxClamp = float.MaxValue,
        Seed = 0,
        Callback = null,
        User = null
    };

    public static ImAnimVariationFloat Mul(float f) => new()
    {
        Mode = ImAnimVariationMode.Multiply,
        Amount = f,
        MinClamp = float.MinValue,
        MaxClamp = float.MaxValue,
        Seed = 0,
        Callback = null,
        User = null
    };

    public static ImAnimVariationFloat Rand(float r) => new()
    {
        Mode = ImAnimVariationMode.Random,
        Amount = r,
        MinClamp = float.MinValue,
        MaxClamp = float.MaxValue,
        Seed = 0,
        Callback = null,
        User = null
    };

    public static ImAnimVariationFloat RandAbs(float r) => new()
    {
        Mode = ImAnimVariationMode.RandomAbs,
        Amount = r,
        MinClamp = float.MinValue,
        MaxClamp = float.MaxValue,
        Seed = 0,
        Callback = null,
        User = null
    };

    public static ImAnimVariationFloat PingPong(float amt) => new()
    {
        Mode = ImAnimVariationMode.PingPong,
        Amount = amt,
        MinClamp = float.MinValue,
        MaxClamp = float.MaxValue,
        Seed = 0,
        Callback = null,
        User = null
    };

    public static ImAnimVariationFloat Fn(ImAnim.VariationFloatFn fn, void* userData = null) => new()
    {
        Mode = ImAnimVariationMode.Callback,
        Amount = 0,
        MinClamp = float.MinValue,
        MaxClamp = float.MaxValue,
        Seed = 0,
        Callback = (delegate* unmanaged[Cdecl]<int, void*, float>)Marshal.GetFunctionPointerForDelegate(fn),
        User = userData
    };
}
