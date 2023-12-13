using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

using ImGuiNET;

using TerraFX.Interop.Windows;

using static Dalamud.ImGuiScene.Helpers.ImGuiViewportHelpers;

using Win32 = TerraFX.Interop.Windows.Windows;

namespace Dalamud.ImGuiScene.Implementations;

/// <summary>
/// An implementation of <see cref="IImGuiInputHandler"/>, using Win32 APIs.<br />
/// Largely a port of https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_win32.cpp,
/// though some changes and wndproc hooking.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal sealed unsafe partial class Win32InputHandler : IImGuiInputHandler
{
    // private ImGuiMouseCursor _oldCursor = ImGuiMouseCursor.None;
    private readonly HWND hWnd;
    private readonly HCURSOR[] cursors;

    private readonly WndProcDelegate wndProcDelegate;
    private readonly bool[] imguiMouseIsDown;
    private readonly nint platformNamePtr;

    private ViewportHandler viewportHandler;

    private long lastTime;

    private nint iniPathPtr;

    private bool disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32InputHandler"/> class.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    public Win32InputHandler(HWND hWnd)
    {
        var io = ImGui.GetIO();
        if (ImGui.GetIO().NativePtr->BackendPlatformName is not null)
            throw new InvalidOperationException("ImGui backend platform seems to be have been already attached.");

        this.hWnd = hWnd;

        // hook wndproc
        // have to hold onto the delegate to keep it in memory for unmanaged code
        this.wndProcDelegate = this.WndProcDetour;

        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors |
                           ImGuiBackendFlags.HasSetMousePos |
                           ImGuiBackendFlags.RendererHasViewports |
                           ImGuiBackendFlags.PlatformHasViewports;

        this.platformNamePtr = Marshal.StringToHGlobalAnsi("imgui_impl_win32_c#");
        io.NativePtr->BackendPlatformName = (byte*)this.platformNamePtr;

        var mainViewport = ImGui.GetMainViewport();
        mainViewport.PlatformHandle = mainViewport.PlatformHandleRaw = hWnd;
        if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
            this.viewportHandler = new(this);

        this.imguiMouseIsDown = new bool[5];

        this.cursors = new HCURSOR[9];
        this.cursors[(int)ImGuiMouseCursor.Arrow] = Win32.LoadCursorW(default, IDC.IDC_ARROW);
        this.cursors[(int)ImGuiMouseCursor.TextInput] = Win32.LoadCursorW(default, IDC.IDC_IBEAM);
        this.cursors[(int)ImGuiMouseCursor.ResizeAll] = Win32.LoadCursorW(default, IDC.IDC_SIZEALL);
        this.cursors[(int)ImGuiMouseCursor.ResizeEW] = Win32.LoadCursorW(default, IDC.IDC_SIZEWE);
        this.cursors[(int)ImGuiMouseCursor.ResizeNS] = Win32.LoadCursorW(default, IDC.IDC_SIZENS);
        this.cursors[(int)ImGuiMouseCursor.ResizeNESW] = Win32.LoadCursorW(default, IDC.IDC_SIZENESW);
        this.cursors[(int)ImGuiMouseCursor.ResizeNWSE] = Win32.LoadCursorW(default, IDC.IDC_SIZENWSE);
        this.cursors[(int)ImGuiMouseCursor.Hand] = Win32.LoadCursorW(default, IDC.IDC_HAND);
        this.cursors[(int)ImGuiMouseCursor.NotAllowed] = Win32.LoadCursorW(default, IDC.IDC_NO);
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="Win32InputHandler"/> class.
    /// </summary>
    ~Win32InputHandler() => this.ReleaseUnmanagedResources();

    private delegate LRESULT WndProcDelegate(HWND hWnd, uint uMsg, WPARAM wparam, LPARAM lparam);

    private delegate BOOL MonitorEnumProcDelegate(HMONITOR monitor, HDC hdc, RECT* rect, LPARAM lparam);

    /// <inheritdoc/>
    public bool UpdateCursor { get; set; } = true;

    /// <inheritdoc/>
    public string? IniPath
    {
        get
        {
            var ptr = (byte*)this.iniPathPtr;
            if (ptr is null)
                return string.Empty;
            var len = 0;
            while (ptr![len] != 0)
                len++;
            return Encoding.UTF8.GetString(ptr, len);
        }

        set
        {
            if (this.iniPathPtr != 0)
                Marshal.FreeHGlobal(this.iniPathPtr);
            if (!string.IsNullOrEmpty(value))
            {
                var e = Encoding.UTF8.GetByteCount(value);
                var newAlloc = Marshal.AllocHGlobal(e + 2);
                try
                {
                    var span = new Span<byte>((void*)newAlloc, e + 2);
                    span[^1] = span[^2] = 0;
                    Encoding.UTF8.GetBytes(value, span);
                }
                catch
                {
                    Marshal.FreeHGlobal(newAlloc);
                    throw;
                }

                this.iniPathPtr = newAlloc;
            }

            ImGui.GetIO().NativePtr->IniFilename = (byte*)this.iniPathPtr;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public bool IsImGuiCursor(nint hCursor) => this.cursors.Contains((HCURSOR)hCursor);

    /// <inheritdoc/>
    public void NewFrame(int targetWidth, int targetHeight)
    {
        var io = ImGui.GetIO();

        io.DisplaySize.X = targetWidth;
        io.DisplaySize.Y = targetHeight;
        io.DisplayFramebufferScale.X = 1f;
        io.DisplayFramebufferScale.Y = 1f;

        var frequency = Stopwatch.Frequency;
        var currentTime = Stopwatch.GetTimestamp();
        io.DeltaTime = this.lastTime > 0 ? (float)((double)(currentTime - this.lastTime) / frequency) : 1f / 60;
        this.lastTime = currentTime;

        this.viewportHandler.UpdateMonitors();

        this.UpdateMousePos();

        this.ProcessKeyEventsWorkarounds();

        // TODO: need to figure out some way to unify all this
        // The bottom case works(?) if the caller hooks SetCursor, but otherwise causes fps issues
        // The top case more or less works if we use ImGui's software cursor (and ideally hide the
        // game's hardware cursor)
        // It would be nice if hooking WM_SETCURSOR worked as it 'should' so that external hooking
        // wasn't necessary

        // this is what imgui's example does, but it doesn't seem to work for us
        // this could be a timing issue.. or their logic could just be wrong for many applications
        // var cursor = io.MouseDrawCursor ? ImGuiMouseCursor.None : ImGui.GetMouseCursor();
        // if (_oldCursor != cursor)
        // {
        //    _oldCursor = cursor;
        //    UpdateMouseCursor();
        // }

        // hacky attempt to make cursors work how I think they 'should'
        if ((io.WantCaptureMouse || io.MouseDrawCursor) && this.UpdateCursor)
        {
            this.UpdateMouseCursor();
        }

        // Similar issue seen with overlapping mouse clicks
        // eg, right click and hold on imgui window, drag off, left click and hold
        //   release right click, release left click -> right click was 'stuck' and imgui
        //   would become unresponsive
        if (!io.WantCaptureMouse)
        {
            for (var i = 0; i < io.MouseDown.Count; i++)
            {
                io.MouseDown[i] = false;
            }
        }
    }

    /// <summary>
    /// Processes window messages. Supports both WndProcA and WndProcW.
    /// </summary>
    /// <param name="hWndCurrent">Handle of the window.</param>
    /// <param name="msg">Type of window message.</param>
    /// <param name="wParam">wParam.</param>
    /// <param name="lParam">lParam.</param>
    /// <returns>Return value, if not doing further processing.</returns>
    public LRESULT? ProcessWndProcW(HWND hWndCurrent, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (ImGui.GetCurrentContext() == nint.Zero)
            return null;

        var io = ImGui.GetIO();

        switch (msg)
        {
            case WM.WM_LBUTTONDOWN:
            case WM.WM_LBUTTONDBLCLK:
            case WM.WM_RBUTTONDOWN:
            case WM.WM_RBUTTONDBLCLK:
            case WM.WM_MBUTTONDOWN:
            case WM.WM_MBUTTONDBLCLK:
            case WM.WM_XBUTTONDOWN:
            case WM.WM_XBUTTONDBLCLK:
            {
                var button = GetButton(msg, wParam);
                if (io.WantCaptureMouse)
                {
                    if (!ImGui.IsAnyMouseDown() && Win32.GetCapture() == nint.Zero)
                        Win32.SetCapture(hWndCurrent);

                    io.MouseDown[button] = true;
                    this.imguiMouseIsDown[button] = true;
                    return default(LRESULT);
                }

                break;
            }

            case WM.WM_LBUTTONUP:
            case WM.WM_RBUTTONUP:
            case WM.WM_MBUTTONUP:
            case WM.WM_XBUTTONUP:
            {
                var button = GetButton(msg, wParam);
                if (io.WantCaptureMouse && this.imguiMouseIsDown[button])
                {
                    if (!ImGui.IsAnyMouseDown() && Win32.GetCapture() == hWndCurrent)
                        Win32.ReleaseCapture();

                    io.MouseDown[button] = false;
                    this.imguiMouseIsDown[button] = false;
                    return default(LRESULT);
                }

                break;
            }

            case WM.WM_MOUSEWHEEL:
                if (io.WantCaptureMouse)
                {
                    io.MouseWheel += Win32.GET_WHEEL_DELTA_WPARAM(wParam) / (float)Win32.WHEEL_DELTA;
                    return default(LRESULT);
                }

                break;
            case WM.WM_MOUSEHWHEEL:
                if (io.WantCaptureMouse)
                {
                    io.MouseWheelH += Win32.GET_WHEEL_DELTA_WPARAM(wParam) / (float)Win32.WHEEL_DELTA;
                    return default(LRESULT);
                }

                break;
            case WM.WM_KEYDOWN:
            case WM.WM_SYSKEYDOWN:
            case WM.WM_KEYUP:
            case WM.WM_SYSKEYUP:
            {
                var isKeyDown = msg is WM.WM_KEYDOWN or WM.WM_SYSKEYDOWN;
                if ((int)wParam >= 256)
                    break;

                // Submit modifiers
                UpdateKeyModifiers();

                // Obtain virtual key code
                // (keypad enter doesn't have its own... VK_RETURN with KF_EXTENDED flag means keypad enter, see IM_VK_KEYPAD_ENTER definition for details, it is mapped to ImGuiKey.KeyPadEnter.)
                var vk = (int)wParam;
                if (vk == VK.VK_RETURN && ((int)lParam & (256 << 16)) > 0)
                    vk = VK.VK_RETURN + 256;

                // Submit key event
                var key = VirtualKeyToImGuiKey(vk);
                var scancode = ((int)lParam & 0xff0000) >> 16;
                if (key != ImGuiKey.None && io.WantTextInput)
                {
                    AddKeyEvent(key, isKeyDown, vk, scancode);
                    return nint.Zero;
                }

                switch (vk)
                {
                    // Submit individual left/right modifier events
                    case VK.VK_SHIFT:
                        // Important: Shift keys tend to get stuck when pressed together, missing key-up events are corrected in OnProcessKeyEventsWorkarounds()
                        if (IsVkDown(VK.VK_LSHIFT) == isKeyDown)
                            AddKeyEvent(ImGuiKey.LeftShift, isKeyDown, VK.VK_LSHIFT, scancode);

                        if (IsVkDown(VK.VK_RSHIFT) == isKeyDown)
                            AddKeyEvent(ImGuiKey.RightShift, isKeyDown, VK.VK_RSHIFT, scancode);

                        break;

                    case VK.VK_CONTROL:
                        if (IsVkDown(VK.VK_LCONTROL) == isKeyDown)
                            AddKeyEvent(ImGuiKey.LeftCtrl, isKeyDown, VK.VK_LCONTROL, scancode);

                        if (IsVkDown(VK.VK_RCONTROL) == isKeyDown)
                            AddKeyEvent(ImGuiKey.RightCtrl, isKeyDown, VK.VK_RCONTROL, scancode);

                        break;

                    case VK.VK_MENU:
                        if (IsVkDown(VK.VK_LMENU) == isKeyDown)
                            AddKeyEvent(ImGuiKey.LeftAlt, isKeyDown, VK.VK_LMENU, scancode);

                        if (IsVkDown(VK.VK_RMENU) == isKeyDown)
                            AddKeyEvent(ImGuiKey.RightAlt, isKeyDown, VK.VK_RMENU, scancode);

                        break;
                }

                break;
            }

            case WM.WM_CHAR:
                if (io.WantTextInput)
                {
                    io.AddInputCharacter((uint)wParam);
                    return nint.Zero;
                }

                break;

            // this never seemed to work reasonably, but I'll leave it for now
            case WM.WM_SETCURSOR:
                if (io.WantCaptureMouse)
                {
                    if (Win32.LOWORD(lParam) == Win32.HTCLIENT && this.UpdateMouseCursor())
                    {
                        // this message returns 1 to block further processing
                        // because consistency is no fun
                        return 1;
                    }
                }

                break;
            // TODO: Decode why IME is miserable
            // case WM.WM_IME_NOTIFY:
            // return HandleImeMessage(hWnd, (long) wParam, (long) lParam);
            case WM.WM_DISPLAYCHANGE:
                this.viewportHandler.UpdateMonitors();
                break;
        }

        return null;
    }

    private void UpdateMousePos()
    {
        var io = ImGui.GetIO();
        var pt = default(POINT);

        // Depending on if Viewports are enabled, we have to change how we process
        // the cursor position. If viewports are enabled, we pass the absolute cursor
        // position to ImGui. Otherwise, we use the old method of passing client-local
        // mouse position to ImGui.
        if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
        {
            if (io.WantSetMousePos)
            {
                Win32.SetCursorPos((int)io.MousePos.X, (int)io.MousePos.Y);
            }

            if (Win32.GetCursorPos(&pt))
            {
                io.MousePos.X = pt.x;
                io.MousePos.Y = pt.y;
            }
            else
            {
                io.MousePos.X = float.MinValue;
                io.MousePos.Y = float.MinValue;
            }
        }
        else
        {
            if (io.WantSetMousePos)
            {
                pt.x = (int)io.MousePos.X;
                pt.y = (int)io.MousePos.Y;
                Win32.ClientToScreen(this.hWnd, &pt);
                Win32.SetCursorPos(pt.x, pt.y);
            }

            if (Win32.GetCursorPos(&pt) && Win32.ScreenToClient(this.hWnd, &pt))
            {
                io.MousePos.X = pt.x;
                io.MousePos.Y = pt.y;
            }
            else
            {
                io.MousePos.X = float.MinValue;
                io.MousePos.Y = float.MinValue;
            }
        }
    }

    // TODO This is kind of unnecessary unless we REALLY want viewport hovered support
    // It seems to mess with the mouse and get it stuck a lot. Do not know why
    // private void UpdateMousePos() {
    //     ImGuiIOPtr io = ImGui.GetIO();
    //
    //     // Set OS mouse position if requested (rarely used, only when ImGuiConfigFlags_NavEnableSetMousePos is enabled by user)
    //     // (When multi-viewports are enabled, all imgui positions are same as OS positions)
    //     if (io.WantSetMousePos) {
    //         POINT pos = new POINT() {x = (int) io.MousePos.X, y = (int) io.MousePos.Y};
    //         if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) == 0)
    //             Win32.ClientToScreen(_hWnd, ref pos);
    //         Win32.SetCursorPos(pos.x, pos.y);
    //     }
    //
    //     io.MousePos = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
    //     io.MouseHoveredViewport = 0;
    //
    //     // Set imgui mouse position
    //     if (!Win32.GetCursorPos(out POINT mouseScreenPos))
    //         return;
    //     nint focusedHwnd = Win32.GetForegroundWindow();
    //     if (focusedHwnd != nint.Zero) {
    //         if (Win32.IsChild(focusedHwnd, _hWnd))
    //             focusedHwnd = _hWnd;
    //         if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) == ImGuiConfigFlags.ViewportsEnable) {
    //             // Multi-viewport mode: mouse position in OS absolute coordinates (io.MousePos is (0,0) when the mouse is on the upper-left of the primary monitor)
    //             // This is the position you can get with GetCursorPos(). In theory adding viewport->Pos is also the reverse operation of doing ScreenToClient().
    //             ImGuiViewportPtr viewport = ImGui.FindViewportByPlatformHandle(focusedHwnd);
    //             unsafe {
    //                 if (viewport.NativePtr != null)
    //                     io.MousePos = new Vector2(mouseScreenPos.x, mouseScreenPos.y);
    //             }
    //         } else {
    //             // Single viewport mode: mouse position in client window coordinates (io.MousePos is (0,0) when the mouse is on the upper-left corner of the app window.)
    //             // This is the position you can get with GetCursorPos() + ScreenToClient() or from WM_MOUSEMOVE.
    //             if (focusedHwnd == _hWnd) {
    //                 POINT mouseClientPos = mouseScreenPos;
    //                 Win32.ScreenToClient(focusedHwnd, ref mouseClientPos);
    //                 io.MousePos = new Vector2(mouseClientPos.x, mouseClientPos.y);
    //             }
    //         }
    //     }
    //
    //     // (Optional) When using multiple viewports: set io.MouseHoveredViewport to the viewport the OS mouse cursor is hovering.
    //     // Important: this information is not easy to provide and many high-level windowing library won't be able to provide it correctly, because
    //     // - This is _ignoring_ viewports with the ImGuiViewportFlags_NoInputs flag (pass-through windows).
    //     // - This is _regardless_ of whether another viewport is focused or being dragged from.
    //     // If ImGuiBackendFlags_HasMouseHoveredViewport is not set by the backend, imgui will ignore this field and infer the information by relying on the
    //     // rectangles and last focused time of every viewports it knows about. It will be unaware of foreign windows that may be sitting between or over your windows.
    //     nint hovered_hwnd = Win32.WindowFromPoint(mouseScreenPos);
    //     if (hovered_hwnd != nint.Zero) {
    //         ImGuiViewportPtr viewport = ImGui.FindViewportByPlatformHandle(focusedHwnd);
    //         unsafe {
    //             if (viewport.NativePtr != null)
    //                 if ((viewport.Flags & ImGuiViewportFlags.NoInputs) == 0
    //                 ) // FIXME: We still get our NoInputs window with WM_NCHITTEST/HTTRANSPARENT code when decorated?
    //                     io.MouseHoveredViewport = viewport.ID;
    //         }
    //     }
    // }

    private bool UpdateMouseCursor()
    {
        var io = ImGui.GetIO();
        if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.NoMouseCursorChange))
            return false;

        var cur = ImGui.GetMouseCursor();
        if (cur == ImGuiMouseCursor.None || io.MouseDrawCursor)
            Win32.SetCursor(default);
        else
            Win32.SetCursor(this.cursors[(int)cur]);

        return true;
    }

    private void ProcessKeyEventsWorkarounds()
    {
        // Left & right Shift keys: when both are pressed together, Windows tend to not generate the WM_KEYUP event for the first released one.
        if (ImGui.IsKeyDown(ImGuiKey.LeftShift) && !IsVkDown(VK.VK_LSHIFT))
            AddKeyEvent(ImGuiKey.LeftShift, false, VK.VK_LSHIFT);
        if (ImGui.IsKeyDown(ImGuiKey.RightShift) && !IsVkDown(VK.VK_RSHIFT))
            AddKeyEvent(ImGuiKey.RightShift, false, VK.VK_RSHIFT);

        // Sometimes WM_KEYUP for Win key is not passed down to the app (e.g. for Win+V on some setups, according to GLFW).
        if (ImGui.IsKeyDown(ImGuiKey.LeftSuper) && !IsVkDown(VK.VK_LWIN))
            AddKeyEvent(ImGuiKey.LeftSuper, false, VK.VK_LWIN);
        if (ImGui.IsKeyDown(ImGuiKey.RightSuper) && !IsVkDown(VK.VK_RWIN))
            AddKeyEvent(ImGuiKey.RightSuper, false, VK.VK_RWIN);

        // From ImGui's FAQ:
        // Note: Text input widget releases focus on "Return KeyDown", so the subsequent "Return KeyUp" event
        // that your application receive will typically have io.WantCaptureKeyboard == false. Depending on your
        // application logic it may or not be inconvenient.
        //
        // With how the local wndproc works, this causes the key up event to be missed when exiting ImGui text entry
        // (eg, from hitting enter or escape.  There may be other ways as well)
        // This then causes the key to appear 'stuck' down, which breaks subsequent attempts to use the input field.
        // This is something of a brute force fix that basically makes key up events irrelevant
        // Holding a key will send repeated key down events and (re)set these where appropriate, so this should be ok.
        var io = ImGui.GetIO();
        if (!io.WantTextInput)
        {
            for (var i = (int)ImGuiKey.NamedKey_BEGIN; i < (int)ImGuiKey.NamedKey_END; i++)
            {
                // Skip raising modifier keys if the game is focused.
                // This allows us to raise the keys when one is held and the window becomes unfocused,
                // but if we do not skip them, they will only be held down every 4th frame or so.
                if (Win32.GetForegroundWindow() == this.hWnd && (IsGamepadKey((ImGuiKey)i) || IsModKey((ImGuiKey)i)))
                    continue;
                io.AddKeyEvent((ImGuiKey)i, false);
            }
        }
    }

    // TODO: Decode why IME is miserable
    // private int HandleImeMessage(nint hWnd, long wParam, long lParam) {
    //
    //     int result = -1;
    //     // if (io.WantCaptureKeyboard)
    //     result = (int) Win32.DefWindowProc(hWnd, WM.WM_IME_NOTIFY, (nint) wParam, (nint) lParam);
    //     System.Diagnostics.Debug.WriteLine($"ime command {(Win32.ImeCommand) wParam} result {result}");
    //
    //     return result;
    // }

    /// <summary>
    /// This WndProc is called for ImGuiScene windows. WndProc for main window will be called back from somewhere else.
    /// </summary>
    private LRESULT WndProcDetour(HWND hWndCurrent, uint msg, WPARAM wParam, LPARAM lParam)
    {
        // Attempt to process the result of this window message
        // We will return the result here if we consider the message handled
        var processResult = this.ProcessWndProcW(hWndCurrent, msg, wParam, lParam);

        if (processResult != null) return processResult.Value;

        // The message wasn't handled, but it's a platform window
        // So we have to handle some messages ourselves
        // BUT we might have disposed the context, so check that
        if (ImGui.GetCurrentContext() == nint.Zero)
            return Win32.DefWindowProcW(hWndCurrent, msg, wParam, lParam);

        var viewport = ImGui.FindViewportByPlatformHandle(hWndCurrent);
        if (viewport.NativePtr == null)
            return Win32.DefWindowProcW(hWndCurrent, msg, wParam, lParam);

        switch (msg)
        {
            case WM.WM_CLOSE:
                viewport.PlatformRequestClose = true;
                return 0;
            case WM.WM_MOVE:
                viewport.PlatformRequestMove = true;
                return 0;
            case WM.WM_SIZE:
                viewport.PlatformRequestResize = true;
                return 0;
            case WM.WM_MOUSEACTIVATE:
                // We never want our platform windows to be active, or else Windows will think we
                // want messages dispatched with its hWnd. We don't. The only way to activate a platform
                // window is via clicking, it does not appear on the taskbar or alt-tab, so we just
                // brute force behavior here.

                // Make the game the foreground window. This prevents ImGui windows from becoming
                // choppy when users have the "limit FPS" option enabled in-game
                Win32.SetForegroundWindow(this.hWnd);

                // Also set the window capture to the main window, as focus will not cause
                // future messages to be dispatched to the main window unless it is receiving capture
                Win32.SetCapture(this.hWnd);

                // We still want to return MA_NOACTIVATE
                // https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-mouseactivate
                return 0x3;
            case WM.WM_NCHITTEST:
                // Let mouse pass-through the window. This will allow the backend to set io.MouseHoveredViewport properly (which is OPTIONAL).
                // The ImGuiViewportFlags_NoInputs flag is set while dragging a viewport, as want to detect the window behind the one we are dragging.
                // If you cannot easily access those viewport flags from your windowing/event code: you may manually synchronize its state e.g. in
                // your main loop after calling UpdatePlatformWindows(). Iterate all viewports/platform windows and pass the flag to your windowing system.
                if (viewport.Flags.HasFlag(ImGuiViewportFlags.NoInputs))
                {
                    // https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-nchittest
                    return -1;
                }

                break;
        }

        return Win32.DefWindowProcW(hWndCurrent, msg, wParam, lParam);
    }

    private void ReleaseUnmanagedResources()
    {
        if (this.disposedValue)
            return;

        this.viewportHandler.Dispose();

        this.cursors.AsSpan().Clear();

        if (ImGui.GetIO().NativePtr->BackendPlatformName == (void*)this.platformNamePtr)
            ImGui.GetIO().NativePtr->BackendPlatformName = null;
        if (this.platformNamePtr != nint.Zero)
            Marshal.FreeHGlobal(this.platformNamePtr);

        if (this.iniPathPtr != nint.Zero)
        {
            ImGui.GetIO().NativePtr->IniFilename = null;
            Marshal.FreeHGlobal(this.iniPathPtr);
            this.iniPathPtr = nint.Zero;
        }

        this.disposedValue = true;
    }

    private struct ViewportHandler : IDisposable
    {
        [SuppressMessage("ReSharper", "CollectionNeverQueried.Local", Justification = "Keeping references alive")]
        private readonly List<object> delegateReferences = new();

        private Win32InputHandler input;
        private nint classNamePtr;

        private bool wantUpdateMonitors = true;

        public ViewportHandler(Win32InputHandler input)
        {
            this.input = input;
            this.classNamePtr = Marshal.StringToHGlobalUni("ImGui Platform");

            var pio = ImGui.GetPlatformIO();
            pio.Platform_CreateWindow = this.RegisterFunctionPointer<CreateWindowDelegate>(this.OnCreateWindow);
            pio.Platform_DestroyWindow = this.RegisterFunctionPointer<DestroyWindowDelegate>(this.OnDestroyWindow);
            pio.Platform_ShowWindow = this.RegisterFunctionPointer<ShowWindowDelegate>(this.OnShowWindow);
            pio.Platform_SetWindowPos = this.RegisterFunctionPointer<SetWindowPosDelegate>(this.OnSetWindowPos);
            pio.Platform_GetWindowPos = this.RegisterFunctionPointer<GetWindowPosDelegate>(this.OnGetWindowPos);
            pio.Platform_SetWindowSize = this.RegisterFunctionPointer<SetWindowSizeDelegate>(this.OnSetWindowSize);
            pio.Platform_GetWindowSize = this.RegisterFunctionPointer<GetWindowSizeDelegate>(this.OnGetWindowSize);
            pio.Platform_SetWindowFocus = this.RegisterFunctionPointer<SetWindowFocusDelegate>(this.OnSetWindowFocus);
            pio.Platform_GetWindowFocus = this.RegisterFunctionPointer<GetWindowFocusDelegate>(this.OnGetWindowFocus);
            pio.Platform_GetWindowMinimized =
                this.RegisterFunctionPointer<GetWindowMinimizedDelegate>(this.OnGetWindowMinimized);
            pio.Platform_SetWindowTitle = this.RegisterFunctionPointer<SetWindowTitleDelegate>(this.OnSetWindowTitle);
            pio.Platform_SetWindowAlpha = this.RegisterFunctionPointer<SetWindowAlphaDelegate>(this.OnSetWindowAlpha);
            pio.Platform_UpdateWindow = this.RegisterFunctionPointer<UpdateWindowDelegate>(this.OnUpdateWindow);
            // pio.Platform_SetImeInputPos = this.RegisterFunctionPointer<SetImeInputPosDelegate>(this.OnSetImeInputPos);
            // pio.Platform_GetWindowDpiScale = this.RegisterFunctionPointer<GetWindowDpiScaleDelegate>(this.OnGetWindowDpiScale);
            // pio.Platform_ChangedViewport = this.RegisterFunctionPointer<ChangedViewportDelegate>(this.OnChangedViewport);

            var wcex = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                style = CS.CS_HREDRAW | CS.CS_VREDRAW,
                hInstance = Win32.GetModuleHandleW(null),
                hbrBackground = (HBRUSH)(1 + COLOR.COLOR_BACKGROUND),
                lpfnWndProc = (delegate* unmanaged<HWND, uint, WPARAM, LPARAM, LRESULT>)Marshal
                    .GetFunctionPointerForDelegate(this.input.wndProcDelegate),
                lpszClassName = (ushort*)this.classNamePtr,
            };

            if (Win32.RegisterClassExW(&wcex) == 0)
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()) ?? new("RegisterClassEx Fail");

            // Register main window handle (which is owned by the main application, not by us)
            // This is mostly for simplicity and consistency, so that our code (e.g. mouse handling etc.) can use same logic for main and secondary viewports.
            var mainViewport = ImGui.GetMainViewport();

            var data = (ImGuiViewportDataWin32*)Marshal.AllocHGlobal(Marshal.SizeOf<ImGuiViewportDataWin32>());
            mainViewport.PlatformUserData = (nint)data;
            data->Hwnd = this.input.hWnd;
            data->HwndOwned = false;
            mainViewport.PlatformHandle = this.input.hWnd;
        }

        public void Dispose()
        {
            if (this.input is null)
                return;

            var pio = ImGui.GetPlatformIO();

            if (ImGui.GetPlatformIO().NativePtr->Monitors.Data != 0)
            {
                // We allocated the platform monitor data in OnUpdateMonitors ourselves,
                // so we have to free it ourselves to ImGui doesn't try to, or else it will crash
                Marshal.FreeHGlobal(ImGui.GetPlatformIO().NativePtr->Monitors.Data);
                ImGui.GetPlatformIO().NativePtr->Monitors = default;
            }

            if (this.classNamePtr != 0)
            {
                Win32.UnregisterClassW((ushort*)this.classNamePtr, Win32.GetModuleHandleW(null));
                Marshal.FreeHGlobal(this.classNamePtr);
                this.classNamePtr = 0;
            }

            pio.Platform_CreateWindow = nint.Zero;
            pio.Platform_DestroyWindow = nint.Zero;
            pio.Platform_ShowWindow = nint.Zero;
            pio.Platform_SetWindowPos = nint.Zero;
            pio.Platform_GetWindowPos = nint.Zero;
            pio.Platform_SetWindowSize = nint.Zero;
            pio.Platform_GetWindowSize = nint.Zero;
            pio.Platform_SetWindowFocus = nint.Zero;
            pio.Platform_GetWindowFocus = nint.Zero;
            pio.Platform_GetWindowMinimized = nint.Zero;
            pio.Platform_SetWindowTitle = nint.Zero;
            pio.Platform_SetWindowAlpha = nint.Zero;
            pio.Platform_UpdateWindow = nint.Zero;
            // pio.Platform_SetImeInputPos = nint.Zero;
            // pio.Platform_GetWindowDpiScale = nint.Zero;
            // pio.Platform_ChangedViewport = nint.Zero;

            this.input = null!;
        }

        public void UpdateMonitors()
        {
            if (!this.wantUpdateMonitors || this.input is null)
                return;

            this.wantUpdateMonitors = false;

            // Set up platformIO monitor structures
            // Here we use a manual ImVector overload, free the existing monitor data,
            // and allocate our own, as we are responsible for telling ImGui about monitors
            var pio = ImGui.GetPlatformIO();
            var numMonitors = Win32.GetSystemMetrics(SM.SM_CMONITORS);
            var data = Marshal.AllocHGlobal(Marshal.SizeOf<ImGuiPlatformMonitor>() * numMonitors);
            if (pio.NativePtr->Monitors.Data != 0)
                Marshal.FreeHGlobal(pio.NativePtr->Monitors.Data);
            pio.NativePtr->Monitors = new(numMonitors, numMonitors, data);

            // ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
            // Marshal.FreeHGlobal(platformIO.NativePtr->Monitors.Data);
            // int numMonitors = Win32.GetSystemMetrics(Win32.SystemMetric.SM_CMONITORS);
            // nint data = Marshal.AllocHGlobal(Marshal.SizeOf<ImGuiPlatformMonitor>() * numMonitors);
            // platformIO.NativePtr->Monitors = new ImVector(numMonitors, numMonitors, data);

            var monitorIndex = -1;
            var enumfn = new MonitorEnumProcDelegate(
                (hMonitor, _, _, _) =>
                {
                    monitorIndex++;
                    var info = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
                    if (!Win32.GetMonitorInfoW(hMonitor, &info))
                        return true;

                    var monitorLt = new Vector2(info.rcMonitor.left, info.rcMonitor.top);
                    var monitorRb = new Vector2(info.rcMonitor.right, info.rcMonitor.bottom);
                    var workLt = new Vector2(info.rcWork.left, info.rcWork.top);
                    var workRb = new Vector2(info.rcWork.right, info.rcWork.bottom);
                    // Give ImGui the info for this display

                    var imMonitor = ImGui.GetPlatformIO().Monitors[monitorIndex];
                    imMonitor.MainPos = monitorLt;
                    imMonitor.MainSize = monitorRb - monitorLt;
                    imMonitor.WorkPos = workLt;
                    imMonitor.WorkSize = workRb - workLt;
                    imMonitor.DpiScale = 1f;
                    return true;
                });
            Win32.EnumDisplayMonitors(
                default,
                null,
                (delegate* unmanaged<HMONITOR, HDC, RECT*, LPARAM, BOOL>)Marshal.GetFunctionPointerForDelegate(enumfn),
                default);
        }

        private nint RegisterFunctionPointer<T>(T obj)
        {
            this.delegateReferences.Add(obj);
            return Marshal.GetFunctionPointerForDelegate(obj);
        }

        private void OnCreateWindow(ImGuiViewportPtr viewport)
        {
            var data = (ImGuiViewportDataWin32*)Marshal.AllocHGlobal(Marshal.SizeOf<ImGuiViewportDataWin32>());
            viewport.PlatformUserData = (nint)data;
            viewport.Flags =
                ImGuiViewportFlags.NoTaskBarIcon |
                ImGuiViewportFlags.NoFocusOnClick |
                ImGuiViewportFlags.NoFocusOnAppearing |
                viewport.Flags;
            ViewportFlagsToWin32Styles(viewport.Flags, out data->DwStyle, out data->DwExStyle);

            var parentWindow = default(HWND);
            if (viewport.ParentViewportId != 0)
            {
                var parentViewport = ImGui.FindViewportByID(viewport.ParentViewportId);
                parentWindow = (HWND)parentViewport.PlatformHandle;
            }

            // Create window
            var rect = new RECT
            {
                left = (int)viewport.Pos.X,
                top = (int)viewport.Pos.Y,
                right = (int)(viewport.Pos.X + viewport.Size.X),
                bottom = (int)(viewport.Pos.Y + viewport.Size.Y),
            };
            Win32.AdjustWindowRectEx(&rect, (uint)data->DwStyle, false, (uint)data->DwExStyle);

            fixed (char* pwszWindowTitle = "Untitled")
            {
                data->Hwnd = Win32.CreateWindowExW(
                    (uint)data->DwExStyle,
                    (ushort*)this.classNamePtr,
                    (ushort*)pwszWindowTitle,
                    (uint)data->DwStyle,
                    rect.left,
                    rect.top,
                    rect.right - rect.left,
                    rect.bottom - rect.top,
                    parentWindow,
                    default,
                    Win32.GetModuleHandleW(null),
                    default);
            }

            data->HwndOwned = true;
            viewport.PlatformRequestResize = false;
            viewport.PlatformHandle = viewport.PlatformHandleRaw = data->Hwnd;
        }

        private void OnDestroyWindow(ImGuiViewportPtr viewport)
        {
            // This is also called on the main viewport for some reason, and we never set that viewport's PlatformUserData
            if (viewport.PlatformUserData == nint.Zero) return;

            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;

            if (Win32.GetCapture() == data->Hwnd)
            {
                // Transfer capture so if we started dragging from a window that later disappears, we'll still receive the MOUSEUP event.
                Win32.ReleaseCapture();
                Win32.SetCapture(this.input.hWnd);
            }

            if (data->Hwnd != nint.Zero && data->HwndOwned)
            {
                var result = Win32.DestroyWindow(data->Hwnd);
                if (result == false && Win32.GetLastError() == ERROR.ERROR_ACCESS_DENIED)
                {
                    // We are disposing, and we're doing it from a different thread because of course we are
                    // Just send the window the close message
                    Win32.PostMessageW(data->Hwnd, WM.WM_CLOSE, default, default);
                }
            }

            data->Hwnd = default;
            Marshal.FreeHGlobal(viewport.PlatformUserData);
            viewport.PlatformUserData = viewport.PlatformHandle = nint.Zero;
        }

        private void OnShowWindow(ImGuiViewportPtr viewport)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;

            if (viewport.Flags.HasFlag(ImGuiViewportFlags.NoFocusOnAppearing))
                Win32.ShowWindow(data->Hwnd, SW.SW_SHOWNA);
            else
                Win32.ShowWindow(data->Hwnd, SW.SW_SHOW);
        }

        private void OnUpdateWindow(ImGuiViewportPtr viewport)
        {
            // (Optional) Update Win32 style if it changed _after_ creation.
            // Generally they won't change unless configuration flags are changed, but advanced uses (such as manually rewriting viewport flags) make this useful.
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;

            viewport.Flags =
                ImGuiViewportFlags.NoTaskBarIcon |
                ImGuiViewportFlags.NoFocusOnClick |
                ImGuiViewportFlags.NoFocusOnAppearing |
                viewport.Flags;
            ViewportFlagsToWin32Styles(viewport.Flags, out var newStyle, out var newExStyle);

            // Only reapply the flags that have been changed from our point of view (as other flags are being modified by Windows)
            if (data->DwStyle != newStyle || data->DwExStyle != newExStyle)
            {
                // (Optional) Update TopMost state if it changed _after_ creation
                var topMostChanged = (data->DwExStyle & WS.WS_EX_TOPMOST) !=
                                     (newExStyle & WS.WS_EX_TOPMOST);

                var insertAfter = default(HWND);
                if (topMostChanged)
                {
                    insertAfter = viewport.Flags.HasFlag(ImGuiViewportFlags.TopMost)
                                      ? HWND.HWND_TOPMOST
                                      : HWND.HWND_NOTOPMOST;
                }

                var swpFlag = topMostChanged ? 0 : SWP.SWP_NOZORDER;

                // Apply flags and position (since it is affected by flags)
                data->DwStyle = newStyle;
                data->DwExStyle = newExStyle;

                _ = Win32.SetWindowLongW(data->Hwnd, GWL.GWL_STYLE, data->DwStyle);
                _ = Win32.SetWindowLongW(data->Hwnd, GWL.GWL_EXSTYLE, data->DwExStyle);

                // Create window
                var rect = new RECT
                {
                    left = (int)viewport.Pos.X,
                    top = (int)viewport.Pos.Y,
                    right = (int)(viewport.Pos.X + viewport.Size.X),
                    bottom = (int)(viewport.Pos.Y + viewport.Size.Y),
                };
                Win32.AdjustWindowRectEx(&rect, (uint)data->DwStyle, false, (uint)data->DwExStyle);
                Win32.SetWindowPos(
                    data->Hwnd,
                    insertAfter,
                    rect.left,
                    rect.top,
                    rect.right - rect.left,
                    rect.bottom - rect.top,
                    (uint)(swpFlag | SWP.SWP_NOACTIVATE | SWP.SWP_FRAMECHANGED));

                // This is necessary when we alter the style
                Win32.ShowWindow(data->Hwnd, SW.SW_SHOWNA);
                viewport.PlatformRequestMove = viewport.PlatformRequestResize = true;
            }
        }

        private Vector2* OnGetWindowPos(Vector2* returnStorage, ImGuiViewportPtr viewport)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;
            var pt = new POINT { x = 0, y = 0 };
            Win32.ClientToScreen(data->Hwnd, &pt);
            returnStorage->X = pt.x;
            returnStorage->Y = pt.y;
            return returnStorage;
        }

        private void OnSetWindowPos(ImGuiViewportPtr viewport, Vector2 pos)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;
            var rect = new RECT((int)pos.X, (int)pos.Y, (int)pos.X, (int)pos.Y);
            Win32.AdjustWindowRectEx(&rect, (uint)data->DwStyle, false, (uint)data->DwExStyle);
            Win32.SetWindowPos(
                data->Hwnd,
                default,
                rect.left,
                rect.top,
                0,
                0,
                SWP.SWP_NOZORDER |
                SWP.SWP_NOSIZE |
                SWP.SWP_NOACTIVATE);
        }

        private Vector2* OnGetWindowSize(Vector2* returnStorage, ImGuiViewportPtr viewport)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;
            RECT rect;
            Win32.GetClientRect(data->Hwnd, &rect);
            returnStorage->X = rect.right - rect.left;
            returnStorage->Y = rect.bottom - rect.top;
            return returnStorage;
        }

        private void OnSetWindowSize(ImGuiViewportPtr viewport, Vector2 size)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;

            var rect = new RECT(0, 0, (int)size.X, (int)size.Y);
            Win32.AdjustWindowRectEx(&rect, (uint)data->DwStyle, false, (uint)data->DwExStyle);
            Win32.SetWindowPos(
                data->Hwnd,
                default,
                0,
                0,
                rect.right - rect.left,
                rect.bottom - rect.top,
                SWP.SWP_NOZORDER |
                SWP.SWP_NOMOVE |
                SWP.SWP_NOACTIVATE);
        }

        private void OnSetWindowFocus(ImGuiViewportPtr viewport)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;

            Win32.BringWindowToTop(data->Hwnd);
            Win32.SetForegroundWindow(data->Hwnd);
            Win32.SetFocus(data->Hwnd);
        }

        private bool OnGetWindowFocus(ImGuiViewportPtr viewport)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;
            return Win32.GetForegroundWindow() == data->Hwnd;
        }

        private bool OnGetWindowMinimized(ImGuiViewportPtr viewport)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;
            return Win32.IsIconic(data->Hwnd);
        }

        private void OnSetWindowTitle(ImGuiViewportPtr viewport, string title)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;
            fixed (char* pwszTitle = title)
                Win32.SetWindowTextW(data->Hwnd, (ushort*)pwszTitle);
        }

        private void OnSetWindowAlpha(ImGuiViewportPtr viewport, float alpha)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;
            var style = Win32.GetWindowLongW(data->Hwnd, GWL.GWL_EXSTYLE);

            alpha = Math.Clamp(alpha, 0f, 1f);
            if (alpha < 1.0f)
            {
                style |= WS.WS_EX_LAYERED;
                _ = Win32.SetWindowLongW(data->Hwnd, GWL.GWL_EXSTYLE, style);
            }
            else
            {
                style &= ~WS.WS_EX_LAYERED;
                _ = Win32.SetWindowLongW(data->Hwnd, GWL.GWL_EXSTYLE, style);
            }

            _ = Win32.SetLayeredWindowAttributes(data->Hwnd, 0, (byte)(255 * alpha), LWA.LWA_ALPHA);
        }

        // TODO: Decode why IME is miserable
        // private void OnSetImeInputPos(ImGuiViewportPtr viewport, Vector2 pos) {
        //     Win32.COMPOSITIONFORM cs = new Win32.COMPOSITIONFORM(
        //         0x20,
        //         new Win32.POINT(
        //             (int) (pos.X - viewport.Pos.X),
        //             (int) (pos.Y - viewport.Pos.Y)),
        //         new Win32.RECT(0, 0, 0, 0)
        //     );
        //     var hwnd = viewport.PlatformHandle;
        //     if (hwnd != nint.Zero) {
        //         var himc = Win32.ImmGetContext(hwnd);
        //         if (himc != nint.Zero) {
        //             Win32.ImmSetCompositionWindow(himc, ref cs);
        //             Win32.ImmReleaseContext(hwnd, himc);
        //         }
        //     }
        // }

        // Helper structure we store in the void* RenderUserData field of each ImGuiViewport to easily retrieve our backend data->
        private struct ImGuiViewportDataWin32
        {
            public HWND Hwnd;
            public bool HwndOwned;
            public int DwStyle;
            public int DwExStyle;
        }
    }
}
