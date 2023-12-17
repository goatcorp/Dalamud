using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Interface.Utility;
using Dalamud.Logging.Internal;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Hooking.WndProcHook;

/// <summary>
/// Manages WndProc hooks for game main window and extra ImGui viewport windows.
/// </summary>
[ServiceManager.BlockingEarlyLoadedService]
internal sealed class WndProcHookManager : IServiceType, IDisposable
{
    private static readonly ModuleLog Log = new(nameof(WndProcHookManager));

    private readonly Hook<DispatchMessageWDelegate> dispatchMessageWHook;
    private readonly Dictionary<HWND, WndProcEventArgs> wndProcOverrides = new();

    [ServiceManager.ServiceConstructor]
    private unsafe WndProcHookManager()
    {
        this.dispatchMessageWHook = Hook<DispatchMessageWDelegate>.FromImport(
            null,
            "user32.dll",
            "DispatchMessageW",
            0,
            this.DispatchMessageWDetour);
        this.dispatchMessageWHook.Enable();
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private unsafe delegate nint DispatchMessageWDelegate(MSG* msg);

    /// <summary>
    /// Called before WndProc.
    /// </summary>
    public event WndProcEventDelegate? PreWndProc;

    /// <summary>
    /// Called after WndProc.
    /// </summary>
    public event WndProcEventDelegate? PostWndProc;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.dispatchMessageWHook.Dispose();
        foreach (var v in this.wndProcOverrides.Values)
            v.InternalRelease();
        this.wndProcOverrides.Clear();
    }

    /// <summary>
    /// Invokes <see cref="PreWndProc"/>.
    /// </summary>
    /// <param name="args">The arguments.</param>
    internal void InvokePreWndProc(WndProcEventArgs args)
    {
        try
        {
            this.PreWndProc?.Invoke(args);
        }
        catch (Exception e)
        {
            Log.Error(e, $"{nameof(this.PreWndProc)} error");
        }
    }

    /// <summary>
    /// Invokes <see cref="PostWndProc"/>.
    /// </summary>
    /// <param name="args">The arguments.</param>
    internal void InvokePostWndProc(WndProcEventArgs args)
    {
        try
        {
            this.PostWndProc?.Invoke(args);
        }
        catch (Exception e)
        {
            Log.Error(e, $"{nameof(this.PostWndProc)} error");
        }
    }

    /// <summary>
    /// Removes <paramref name="args"/> from the list of known WndProc overrides.
    /// </summary>
    /// <param name="args">Object to remove.</param>
    internal void OnHookedWindowRemoved(WndProcEventArgs args)
    {
        if (!this.dispatchMessageWHook.IsDisposed)
            this.wndProcOverrides.Remove(args.Hwnd);
    }

    /// <summary>
    /// Detour for <see cref="DispatchMessageW"/>. Used to discover new windows to hook.
    /// </summary>
    /// <param name="msg">The message.</param>
    /// <returns>The original return value.</returns>
    private unsafe nint DispatchMessageWDetour(MSG* msg)
    {
        if (!this.wndProcOverrides.ContainsKey(msg->hwnd)
            && ImGuiHelpers.FindViewportId(msg->hwnd) is var vpid and >= 0)
        {
            this.wndProcOverrides[msg->hwnd] = new(this, msg->hwnd, vpid);
        }

        return this.dispatchMessageWHook.Original(msg);
    }
}
