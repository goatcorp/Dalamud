using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.ImGuiBackend.Helpers;

/// <summary>
/// Peels ReShade off stuff.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed blocks")]
internal static unsafe class ReShadePeeler
{
    /// <summary>
    /// Peels <see cref="IDXGISwapChain"/> if it is wrapped by ReShade.
    /// </summary>
    /// <param name="comptr">[inout] The COM pointer to an instance of <see cref="IDXGISwapChain"/>.</param>
    /// <typeparam name="T">A COM type that is or extends <see cref="IDXGISwapChain"/>.</typeparam>
    /// <returns><c>true</c> if peeled.</returns>
    public static bool PeelSwapChain<T>(ComPtr<T>* comptr)
        where T : unmanaged, IDXGISwapChain.Interface =>
        PeelIUnknown(comptr, 0x10);

    /// <summary>
    /// Peels <see cref="ID3D12Device"/> if it is wrapped by ReShade.
    /// </summary>
    /// <param name="comptr">[inout] The COM pointer to an instance of <see cref="ID3D12Device"/>.</param>
    /// <typeparam name="T">A COM type that is or extends <see cref="ID3D12Device"/>.</typeparam>
    /// <returns><c>true</c> if peeled.</returns>
    public static bool PeelD3D12Device<T>(ComPtr<T>* comptr)
        where T : unmanaged, ID3D12Device.Interface =>
        PeelIUnknown(comptr, 0x10);

    /// <summary>
    /// Peels <see cref="ID3D12CommandQueue"/> if it is wrapped by ReShade.
    /// </summary>
    /// <param name="comptr">[inout] The COM pointer to an instance of <see cref="ID3D12CommandQueue"/>.</param>
    /// <typeparam name="T">A COM type that is or extends <see cref="ID3D12CommandQueue"/>.</typeparam>
    /// <returns><c>true</c> if peeled.</returns>
    public static bool PeelD3D12CommandQueue<T>(ComPtr<T>* comptr)
        where T : unmanaged, ID3D12CommandQueue.Interface =>
        PeelIUnknown(comptr, 0x10);

    private static bool PeelIUnknown<T>(ComPtr<T>* comptr, nint offset)
        where T : unmanaged, IUnknown.Interface
    {
        if (comptr->Get() == null || !IsReShadedComObject(comptr->Get()))
            return false;

        var punk = new ComPtr<IUnknown>(*(IUnknown**)((nint)comptr->Get() + offset));
        using var comptr2 = default(ComPtr<T>);
        if (punk.As(&comptr2).FAILED)
            return false;
        comptr2.Swap(comptr);
        return true;
    }

    private static bool BelongsInReShadeDll(nint ptr)
    {
        foreach (ProcessModule processModule in Process.GetCurrentProcess().Modules)
        {
            if (ptr < processModule.BaseAddress)
                continue;

            var dosh = (IMAGE_DOS_HEADER*)processModule.BaseAddress;
            var nth = (IMAGE_NT_HEADERS64*)(processModule.BaseAddress + dosh->e_lfanew);
            if (ptr >= processModule.BaseAddress + nth->OptionalHeader.SizeOfImage)
                continue;

            fixed (byte* pfn0 = "CreateDXGIFactory"u8)
            fixed (byte* pfn1 = "D2D1CreateDevice"u8)
            fixed (byte* pfn2 = "D3D10CreateDevice"u8)
            fixed (byte* pfn3 = "D3D11CreateDevice"u8)
            fixed (byte* pfn4 = "D3D12CreateDevice"u8)
            fixed (byte* pfn5 = "glBegin"u8)
            fixed (byte* pfn6 = "vkCreateDevice"u8)
            {
                if (GetProcAddress((HMODULE)dosh, (sbyte*)pfn0) == 0)
                    continue;
                if (GetProcAddress((HMODULE)dosh, (sbyte*)pfn1) == 0)
                    continue;
                if (GetProcAddress((HMODULE)dosh, (sbyte*)pfn2) == 0)
                    continue;
                if (GetProcAddress((HMODULE)dosh, (sbyte*)pfn3) == 0)
                    continue;
                if (GetProcAddress((HMODULE)dosh, (sbyte*)pfn4) == 0)
                    continue;
                if (GetProcAddress((HMODULE)dosh, (sbyte*)pfn5) == 0)
                    continue;
                if (GetProcAddress((HMODULE)dosh, (sbyte*)pfn6) == 0)
                    continue;
            }

            var fileInfo = FileVersionInfo.GetVersionInfo(processModule.FileName);

            if (fileInfo.FileDescription == null)
                continue;

            if (!fileInfo.FileDescription.Contains("GShade") && !fileInfo.FileDescription.Contains("ReShade"))
                continue;

            return true;
        }

        return false;
    }

    private static bool IsReShadedComObject<T>(T* obj)
        where T : unmanaged, IUnknown.Interface
    {
        try
        {
            var vtbl = (nint**)Marshal.ReadIntPtr((nint)obj);
            for (var i = 0; i < 3; i++)
            {
                if (!BelongsInReShadeDll(Marshal.ReadIntPtr((nint)(vtbl + i))))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
