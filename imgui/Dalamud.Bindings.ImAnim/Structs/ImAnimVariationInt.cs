using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImAnim;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ImAnimVariationInt
{
    public ImAnimVariationMode Mode;
    public int Amount;
    public int MinClamp;
    public int MaxClamp;
    public uint Seed;
    public delegate* unmanaged[Cdecl]<int, void*, int> Callback;
    public void* User;

    public static ImAnimVariationInt None => new()
    {
        Mode = ImAnimVariationMode.None,
        Amount = 0,
        MinClamp = int.MinValue,
        MaxClamp = int.MaxValue,
        Seed = 0,
        Callback = null,
        User = null
    };

    public static ImAnimVariationInt Inc(int amt) => new()
    {
        Mode = ImAnimVariationMode.Increment,
        Amount = amt,
        MinClamp = int.MinValue,
        MaxClamp = int.MaxValue,
        Seed = 0,
        Callback = null,
        User = null
    };

    public static ImAnimVariationInt Dec(int amt) => new()
    {
        Mode = ImAnimVariationMode.Decrement,
        Amount = amt,
        MinClamp = int.MinValue,
        MaxClamp = int.MaxValue,
        Seed = 0,
        Callback = null,
        User = null
    };

    public static ImAnimVariationInt Mul(int f) => new()
    {
        Mode = ImAnimVariationMode.Multiply,
        Amount = f,
        MinClamp = int.MinValue,
        MaxClamp = int.MaxValue,
        Seed = 0,
        Callback = null,
        User = null
    };

    public static ImAnimVariationInt Rand(int r) => new()
    {
        Mode = ImAnimVariationMode.Random,
        Amount = r,
        MinClamp = int.MinValue,
        MaxClamp = int.MaxValue,
        Seed = 0,
        Callback = null,
        User = null
    };

    public static ImAnimVariationInt RandAbs(int r) => new()
    {
        Mode = ImAnimVariationMode.RandomAbs,
        Amount = r,
        MinClamp = int.MinValue,
        MaxClamp = int.MaxValue,
        Seed = 0,
        Callback = null,
        User = null
    };

    public static ImAnimVariationInt Fn(ImAnim.VariationIntFn fn, void* userData = null) => new()
    {
        Mode = ImAnimVariationMode.Callback,
        Amount = 0,
        MinClamp = int.MinValue,
        MaxClamp = int.MaxValue,
        Seed = 0,
        Callback = (delegate* unmanaged[Cdecl]<int, void*, int>)Marshal.GetFunctionPointerForDelegate(fn),
        User = userData
    };
}
