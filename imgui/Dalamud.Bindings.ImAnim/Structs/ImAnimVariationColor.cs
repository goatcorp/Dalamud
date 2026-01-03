using System.Numerics;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImAnim;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ImAnimVariationColor
{
    public ImAnimVariationMode Mode;
    public Vector4 Amount;
    public Vector4 MinClamp;
    public Vector4 MaxClamp;
    public ImAnimColorSpace ColorSpace;
    public delegate* unmanaged[Cdecl]<int, void*, Vector4> Callback;
    public void* User;
    public ImAnimVariationFloat R;
    public ImAnimVariationFloat G;
    public ImAnimVariationFloat B;
    public ImAnimVariationFloat A;

    public static ImAnimVariationColor None => new()
    {
        Mode = ImAnimVariationMode.None,
        Amount = Vector4.Zero,
        MinClamp = Vector4.Zero,
        MaxClamp = Vector4.One,
        ColorSpace = ImAnimColorSpace.Oklab,
        Callback = null,
        User = null,
        R = ImAnimVariationFloat.None,
        G = ImAnimVariationFloat.None,
        B = ImAnimVariationFloat.None,
        A = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationColor Inc(Vector4 color) => new()
    {
        Mode = ImAnimVariationMode.Increment,
        Amount = color,
        MinClamp = Vector4.Zero,
        MaxClamp = Vector4.One,
        ColorSpace = ImAnimColorSpace.Oklab,
        Callback = null,
        User = null,
        R = ImAnimVariationFloat.None,
        G = ImAnimVariationFloat.None,
        B = ImAnimVariationFloat.None,
        A = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationColor Inc(float r, float g, float b, float a) => new()
    {
        Mode = ImAnimVariationMode.Increment,
        Amount = new Vector4(r, g, b, a),
        MinClamp = Vector4.Zero,
        MaxClamp = Vector4.One,
        ColorSpace = ImAnimColorSpace.Oklab,
        Callback = null,
        User = null,
        R = ImAnimVariationFloat.None,
        G = ImAnimVariationFloat.None,
        B = ImAnimVariationFloat.None,
        A = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationColor Dec(Vector4 color) => new()
    {
        Mode = ImAnimVariationMode.Decrement,
        Amount = color,
        MinClamp = Vector4.Zero,
        MaxClamp = Vector4.One,
        ColorSpace = ImAnimColorSpace.Oklab,
        Callback = null,
        User = null,
        R = ImAnimVariationFloat.None,
        G = ImAnimVariationFloat.None,
        B = ImAnimVariationFloat.None,
        A = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationColor Dec(float r, float g, float b, float a) => new()
    {
        Mode = ImAnimVariationMode.Decrement,
        Amount = new Vector4(r, g, b, a),
        MinClamp = Vector4.Zero,
        MaxClamp = Vector4.One,
        ColorSpace = ImAnimColorSpace.Oklab,
        Callback = null,
        User = null,
        R = ImAnimVariationFloat.None,
        G = ImAnimVariationFloat.None,
        B = ImAnimVariationFloat.None,
        A = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationColor Mul(float f) => new()
    {
        Mode = ImAnimVariationMode.Multiply,
        Amount = new Vector4(f, f, f, 1),
        MinClamp = Vector4.Zero,
        MaxClamp = Vector4.One,
        ColorSpace = ImAnimColorSpace.Oklab,
        Callback = null,
        User = null,
        R = ImAnimVariationFloat.None,
        G = ImAnimVariationFloat.None,
        B = ImAnimVariationFloat.None,
        A = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationColor Rand(Vector4 acolort) => new()
    {
        Mode = ImAnimVariationMode.Random,
        Amount = acolort,
        MinClamp = Vector4.Zero,
        MaxClamp = Vector4.One,
        ColorSpace = ImAnimColorSpace.Oklab,
        Callback = null,
        User = null,
        R = ImAnimVariationFloat.None,
        G = ImAnimVariationFloat.None,
        B = ImAnimVariationFloat.None,
        A = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationColor Rand(float r, float g, float b, float a) => new()
    {
        Mode = ImAnimVariationMode.Random,
        Amount = new Vector4(r, g, b, a),
        MinClamp = Vector4.Zero,
        MaxClamp = Vector4.One,
        ColorSpace = ImAnimColorSpace.Oklab,
        Callback = null,
        User = null,
        R = ImAnimVariationFloat.None,
        G = ImAnimVariationFloat.None,
        B = ImAnimVariationFloat.None,
        A = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationColor Fn(ImAnim.VariationVec4Fn fn, void* userData = null) => new()
    {
        Mode = ImAnimVariationMode.Callback,
        Amount = Vector4.Zero,
        MinClamp = Vector4.Zero,
        MaxClamp = Vector4.One,
        ColorSpace = ImAnimColorSpace.Oklab,
        Callback = (delegate* unmanaged[Cdecl]<int, void*, Vector4>)Marshal.GetFunctionPointerForDelegate(fn),
        User = userData,
        R = ImAnimVariationFloat.None,
        G = ImAnimVariationFloat.None,
        B = ImAnimVariationFloat.None,
        A = ImAnimVariationFloat.None,
    };

    public static ImAnimVariationColor Axis(ImAnimVariationFloat vr, ImAnimVariationFloat vg, ImAnimVariationFloat vb, ImAnimVariationFloat va) => new()
    {
        Mode = ImAnimVariationMode.None,
        Amount = Vector4.Zero,
        MinClamp = Vector4.Zero,
        MaxClamp = Vector4.One,
        ColorSpace = ImAnimColorSpace.Oklab,
        Callback = null,
        User = null,
        R = vr,
        G = vg,
        B = vb,
        A = va,
    };
}
