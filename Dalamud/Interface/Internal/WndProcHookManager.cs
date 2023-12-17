using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Hooking;
using Dalamud.Interface.Utility;
using Dalamud.Logging.Internal;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Internal;

/// <summary>
/// A manifestation of "I can't believe this is required".
/// </summary>
[ServiceManager.BlockingEarlyLoadedService]
internal sealed class WndProcHookManager : IServiceType, IDisposable
{
    private static readonly ModuleLog Log = new("WPHM");

    private readonly Hook<DispatchMessageWDelegate> dispatchMessageWHook;
    private readonly Dictionary<HWND, nint> wndProcNextDict = new();
    private readonly WndProcDelegate wndProcDelegate;
    private readonly uint unhookSelfMessage;
    private bool disposed;

    [ServiceManager.ServiceConstructor]
    private unsafe WndProcHookManager()
    {
        this.wndProcDelegate = this.WndProcDetour;
        this.dispatchMessageWHook = Hook<DispatchMessageWDelegate>.FromImport(
            null, "user32.dll", "DispatchMessageW", 0, this.DispatchMessageWDetour);
        this.dispatchMessageWHook.Enable();
        fixed (void* pMessageName = $"{nameof(WndProcHookManager)}.{nameof(this.unhookSelfMessage)}")
            this.unhookSelfMessage = RegisterWindowMessageW((ushort*)pMessageName);
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="WndProcHookManager"/> class.
    /// </summary>
    ~WndProcHookManager() => this.ReleaseUnmanagedResources();

    /// <summary>
    /// Delegate for overriding WndProc.
    /// </summary>
    /// <param name="args">The arguments.</param>
    public delegate void WndProcOverrideDelegate(ref WndProcOverrideEventArgs args);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate LRESULT WndProcDelegate(HWND hwnd, uint uMsg, WPARAM wParam, LPARAM lParam);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate nint DispatchMessageWDelegate(ref MSG msg);
    
    /// <summary>
    /// Called before WndProc.
    /// </summary>
    public event WndProcOverrideDelegate? PreWndProc;
    
    /// <summary>
    /// Called after WndProc.
    /// </summary>
    public event WndProcOverrideDelegate? PostWndProc;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.disposed = true;
        this.dispatchMessageWHook.Dispose();
        this.ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Detour for <see cref="DispatchMessageW"/>. Used to discover new windows to hook.
    /// </summary>
    /// <param name="msg">The message.</param>
    /// <returns>The original return value.</returns>
    private unsafe nint DispatchMessageWDetour(ref MSG msg)
    {
        lock (this.wndProcNextDict)
        {
            if (!this.disposed && ImGuiHelpers.FindViewportId(msg.hwnd) >= 0 &&
                !this.wndProcNextDict.ContainsKey(msg.hwnd))
            {
                this.wndProcNextDict[msg.hwnd] = SetWindowLongPtrW(
                    msg.hwnd,
                    GWLP.GWLP_WNDPROC,
                    Marshal.GetFunctionPointerForDelegate(this.wndProcDelegate));
            }
        }

        return this.dispatchMessageWHook.IsDisposed
                   ? DispatchMessageW((MSG*)Unsafe.AsPointer(ref msg))
                   : this.dispatchMessageWHook.Original(ref msg);
    }

    private unsafe LRESULT WndProcDetour(HWND hwnd, uint uMsg, WPARAM wParam, LPARAM lParam)
    {
        nint nextProc;
        lock (this.wndProcNextDict)
        {
            if (!this.wndProcNextDict.TryGetValue(hwnd, out nextProc))
            {
                // Something went wrong; prevent crash. Things will, regardless of the effort, break.
                return DefWindowProcW(hwnd, uMsg, wParam, lParam);
            }
        }

        if (uMsg == this.unhookSelfMessage)
        {
            // Even though this message is dedicated for our processing,
            // satisfy the expectations by calling the next window procedure.
            var rv = CallWindowProcW(
                (delegate* unmanaged<HWND, uint, WPARAM, LPARAM, LRESULT>)nextProc,
                hwnd,
                uMsg,
                wParam,
                lParam);

            // Remove self from the chain.
            SetWindowLongPtrW(hwnd, GWLP.GWLP_WNDPROC, nextProc);
            lock (this.wndProcNextDict)
                this.wndProcNextDict.Remove(hwnd);

            return rv;
        }

        var arg = new WndProcOverrideEventArgs(hwnd, ref uMsg, ref wParam, ref lParam);
        try
        {
            this.PreWndProc?.Invoke(ref arg);
        }
        catch (Exception e)
        {
            Log.Error(e, $"{nameof(this.PostWndProc)} error");
        }

        if (!arg.SuppressCall)
        {
            try
            {
                arg.ReturnValue = CallWindowProcW(
                    (delegate* unmanaged<HWND, uint, WPARAM, LPARAM, LRESULT>)nextProc,
                    hwnd,
                    uMsg,
                    wParam,
                    lParam);
            }
            catch (Exception e)
            {
                Log.Error(e, $"{nameof(CallWindowProcW)} error; probably some other software's fault");
            }

            try
            {
                this.PostWndProc?.Invoke(ref arg);
            }
            catch (Exception e)
            {
                Log.Error(e, $"{nameof(this.PostWndProc)} error");
            }
        }

        if (uMsg == WM.WM_NCDESTROY)
        {
            // The window will cease to exist, once we return.
            SetWindowLongPtrW(hwnd, GWLP.GWLP_WNDPROC, nextProc);
            lock (this.wndProcNextDict)
                this.wndProcNextDict.Remove(hwnd);
        }

        return arg.ReturnValue;
    }

    private void ReleaseUnmanagedResources()
    {
        this.disposed = true;

        // As wndProcNextDict will be touched on each SendMessageW call, make a copy of window list first.
        HWND[] windows;
        lock (this.wndProcNextDict)
            windows = this.wndProcNextDict.Keys.ToArray();

        // Unregister our hook from all the windows we hooked.
        foreach (var v in windows)
            SendMessageW(v, this.unhookSelfMessage, default, default);
    }

    /// <summary>
    /// Parameters for <see cref="WndProcOverrideDelegate"/>.
    /// </summary>
    public ref struct WndProcOverrideEventArgs
    {
        /// <summary>
        /// The handle of the target window of the message.
        /// </summary>
        public readonly HWND Hwnd;

        /// <summary>
        /// The message.
        /// </summary>
        public ref uint Message;

        /// <summary>
        /// The WPARAM.
        /// </summary>
        public ref WPARAM WParam;

        /// <summary>
        /// The LPARAM.
        /// </summary>
        public ref LPARAM LParam;

        /// <summary>
        /// Initializes a new instance of the <see cref="WndProcOverrideEventArgs"/> struct.
        /// </summary>
        /// <param name="hwnd">The handle of the target window of the message.</param>
        /// <param name="msg">The message.</param>
        /// <param name="wParam">The WPARAM.</param>
        /// <param name="lParam">The LPARAM.</param>
        public WndProcOverrideEventArgs(HWND hwnd, ref uint msg, ref WPARAM wParam, ref LPARAM lParam)
        {
            this.Hwnd = hwnd;
            this.LParam = ref lParam;
            this.WParam = ref wParam;
            this.Message = ref msg;
            this.ViewportId = ImGuiHelpers.FindViewportId(hwnd);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether to suppress calling the next WndProc in the chain.<br />
        /// Does nothing if changed from <see cref="WndProcHookManager.PostWndProc"/>.
        /// </summary>
        public bool SuppressCall { get; set; }
        
        /// <summary>
        /// Gets or sets the return value.<br />
        /// Has the return value from next window procedure, if accessed from <see cref="WndProcHookManager.PostWndProc"/>.
        /// </summary>
        public LRESULT ReturnValue { get; set; }

        /// <summary>
        /// Gets the ImGui viewport ID.
        /// </summary>
        public int ViewportId { get; init; }

        /// <summary>
        /// Gets a value indicating whether this message is for the game window (the first viewport).
        /// </summary>
        public bool IsGameWindow => this.ViewportId == 0;

        /// <summary>
        /// Sets <see cref="SuppressCall"/> to <c>true</c> and sets <see cref="ReturnValue"/>.
        /// </summary>
        /// <param name="returnValue">The new return value.</param>
        public void SuppressAndReturn(LRESULT returnValue)
        {
            this.ReturnValue = returnValue;
            this.SuppressCall = true;
        }

        /// <summary>
        /// Sets <see cref="SuppressCall"/> to <c>true</c> and calls <see cref="DefWindowProcW"/>.
        /// </summary>
        public void SuppressWithDefault()
        {
            this.ReturnValue = DefWindowProcW(this.Hwnd, this.Message, this.WParam, this.LParam);
            this.SuppressCall = true;
        }
    }
}
