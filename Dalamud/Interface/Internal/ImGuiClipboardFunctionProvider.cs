using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using CheapLoc;

using Dalamud.Game.Gui.Toast;
using Dalamud.Interface.Utility;
using Dalamud.Logging.Internal;

using ImGuiNET;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Internal;

/// <summary>
/// Configures the ImGui clipboard behaviour to work nicely with XIV.
/// </summary>
/// <remarks>
/// <para>
/// XIV uses '\r' for line endings and will truncate all text after a '\n' character.
/// This means that copy/pasting multi-line text from ImGui to XIV will only copy the first line.
/// </para>
/// <para>
/// ImGui uses '\n' for line endings and will ignore '\r' entirely.
/// This means that copy/pasting multi-line text from XIV to ImGui will copy all the text
/// without line breaks.
/// </para>
/// <para>
/// To fix this we normalize all clipboard line endings entering/exiting ImGui to '\r\n' which
/// works for both ImGui and XIV.
/// </para>
/// </remarks>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class ImGuiClipboardFunctionProvider : IInternalDisposableService
{
    private static readonly ModuleLog Log = new(nameof(ImGuiClipboardFunctionProvider));
    private readonly nint clipboardUserDataOriginal;
    private readonly nint setTextOriginal;
    private readonly nint getTextOriginal;

    [ServiceManager.ServiceDependency]
    private readonly ToastGui toastGui = Service<ToastGui>.Get();
    
    private ImVectorWrapper<byte> clipboardData;
    private GCHandle clipboardUserData;

    [ServiceManager.ServiceConstructor]
    private ImGuiClipboardFunctionProvider(InterfaceManager.InterfaceManagerWithScene imws)
    {
        // Effectively waiting for ImGui to become available.
        Debug.Assert(ImGuiHelpers.IsImGuiInitialized, "IMWS initialized but IsImGuiInitialized is false?");

        var io = ImGui.GetIO();
        this.clipboardUserDataOriginal = io.ClipboardUserData;
        this.setTextOriginal = io.SetClipboardTextFn;
        this.getTextOriginal = io.GetClipboardTextFn;
        io.ClipboardUserData = GCHandle.ToIntPtr(this.clipboardUserData = GCHandle.Alloc(this));
        io.SetClipboardTextFn = (nint)(delegate* unmanaged<nint, byte*, void>)&StaticSetClipboardTextImpl;
        io.GetClipboardTextFn = (nint)(delegate* unmanaged<nint, byte*>)&StaticGetClipboardTextImpl;

        this.clipboardData = new(0);
        return;

        [UnmanagedCallersOnly]
        static void StaticSetClipboardTextImpl(nint userData, byte* text) =>
            ((ImGuiClipboardFunctionProvider)GCHandle.FromIntPtr(userData).Target)!.SetClipboardTextImpl(text);

        [UnmanagedCallersOnly]
        static byte* StaticGetClipboardTextImpl(nint userData) =>
            ((ImGuiClipboardFunctionProvider)GCHandle.FromIntPtr(userData).Target)!.GetClipboardTextImpl();
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        if (!this.clipboardUserData.IsAllocated)
            return;

        var io = ImGui.GetIO();
        io.SetClipboardTextFn = this.setTextOriginal;
        io.GetClipboardTextFn = this.getTextOriginal;
        io.ClipboardUserData = this.clipboardUserDataOriginal;

        this.clipboardUserData.Free();
        this.clipboardData.Dispose();
    }

    private bool OpenClipboardOrShowError()
    {
        if (!OpenClipboard(default))
        {
            this.toastGui.ShowError(
                Loc.Localize(
                    "ImGuiClipboardFunctionProviderClipboardInUse",
                    "Some other application is using the clipboard. Try again later."));
            return false;
        }

        return true;
    }

    private void SetClipboardTextImpl(byte* text)
    {
        if (!this.OpenClipboardOrShowError())
            return;

        try
        {
            var len = 0;
            while (text[len] != 0)
                len++;
            var str = Encoding.UTF8.GetString(text, len);
            str = str.ReplaceLineEndings("\r\n");
            var hMem = GlobalAlloc(GMEM.GMEM_MOVEABLE, (nuint)((str.Length + 1) * 2));
            if (hMem == 0)
                throw new OutOfMemoryException();
            
            var ptr = (char*)GlobalLock(hMem);
            if (ptr == null)
            {
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error())
                      ?? throw new InvalidOperationException($"{nameof(GlobalLock)} failed.");
            }

            str.AsSpan().CopyTo(new(ptr, str.Length));
            ptr[str.Length] = default;
            GlobalUnlock(hMem);

            EmptyClipboard();
            SetClipboardData(CF.CF_UNICODETEXT, hMem);
        }
        catch (Exception e)
        {
            Log.Error(e, $"Error in {nameof(this.SetClipboardTextImpl)}");
            this.toastGui.ShowError(
                Loc.Localize(
                    "ImGuiClipboardFunctionProviderErrorCopy",
                    "Failed to copy. See logs for details."));
        }
        finally
        {
            CloseClipboard();
        }
    }

    private byte* GetClipboardTextImpl()
    {
        this.clipboardData.Clear();
        
        var formats = stackalloc uint[] { CF.CF_UNICODETEXT, CF.CF_TEXT };
        if (GetPriorityClipboardFormat(formats, 2) < 1 || !this.OpenClipboardOrShowError())
        {
            this.clipboardData.Add(0);
            return this.clipboardData.Data;
        }

        var hMem = (HGLOBAL)GetClipboardData(CF.CF_UNICODETEXT);
        try
        {
            if (hMem != default)
            {
                var ptr = (char*)GlobalLock(hMem);
                if (ptr == null)
                {
                    throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error())
                          ?? throw new InvalidOperationException($"{nameof(GlobalLock)} failed.");
                }

                var str = new string(ptr);
                str = str.ReplaceLineEndings("\r\n");
                this.clipboardData.Resize(Encoding.UTF8.GetByteCount(str) + 1);
                Encoding.UTF8.GetBytes(str, this.clipboardData.DataSpan);
                this.clipboardData[^1] = 0;
            }
            else
            {
                this.clipboardData.Add(0);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, $"Error in {nameof(this.GetClipboardTextImpl)}");
            this.toastGui.ShowError(
                Loc.Localize(
                    "ImGuiClipboardFunctionProviderErrorPaste",
                    "Failed to paste. See logs for details."));
        }
        finally
        {
            if (hMem != default)
                GlobalUnlock(hMem);
            CloseClipboard();
        }

        return this.clipboardData.Data;
    }
}
