using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Bindings.ImGui;
using Dalamud.Console;
using Dalamud.Memory;
using Dalamud.Utility;

using Serilog;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

using ERROR = TerraFX.Interop.Windows.ERROR;

namespace Dalamud.Interface.ImGuiBackend.InputHandler;

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
    private readonly HWND hWnd;
    private readonly HCURSOR[] cursors;

    private readonly WndProcDelegate wndProcDelegate;
    private readonly nint platformNamePtr;

    private readonly IConsoleVariable<bool> cvLogMouseEvents;

    private ViewportHandler viewportHandler;

    private int mouseButtonsDown;
    private bool mouseTracked;
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
        if (ImGui.GetIO().Handle->BackendPlatformName is not null)
            throw new InvalidOperationException("ImGui backend platform seems to be have been already attached.");

        this.hWnd = hWnd;

        // hook wndproc
        // have to hold onto the delegate to keep it in memory for unmanaged code
        this.wndProcDelegate = this.WndProcDetour;

        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors |
                           ImGuiBackendFlags.HasSetMousePos |
                           ImGuiBackendFlags.RendererHasViewports |
                           ImGuiBackendFlags.PlatformHasViewports |
                           ImGuiBackendFlags.HasMouseHoveredViewport;

        this.platformNamePtr = Marshal.StringToHGlobalAnsi("imgui_impl_win32_c#");
        io.Handle->BackendPlatformName = (byte*)this.platformNamePtr;

        var mainViewport = ImGui.GetMainViewport();
        mainViewport.PlatformHandle = mainViewport.PlatformHandleRaw = hWnd;
        if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
            this.viewportHandler = new(this);

        this.cursors = new HCURSOR[9];
        this.cursors[(int)ImGuiMouseCursor.Arrow] = LoadCursorW(default, IDC.IDC_ARROW);
        this.cursors[(int)ImGuiMouseCursor.TextInput] = LoadCursorW(default, IDC.IDC_IBEAM);
        this.cursors[(int)ImGuiMouseCursor.ResizeAll] = LoadCursorW(default, IDC.IDC_SIZEALL);
        this.cursors[(int)ImGuiMouseCursor.ResizeEw] = LoadCursorW(default, IDC.IDC_SIZEWE);
        this.cursors[(int)ImGuiMouseCursor.ResizeNs] = LoadCursorW(default, IDC.IDC_SIZENS);
        this.cursors[(int)ImGuiMouseCursor.ResizeNesw] = LoadCursorW(default, IDC.IDC_SIZENESW);
        this.cursors[(int)ImGuiMouseCursor.ResizeNwse] = LoadCursorW(default, IDC.IDC_SIZENWSE);
        this.cursors[(int)ImGuiMouseCursor.Hand] = LoadCursorW(default, IDC.IDC_HAND);
        this.cursors[(int)ImGuiMouseCursor.NotAllowed] = LoadCursorW(default, IDC.IDC_NO);

        this.cvLogMouseEvents = Service<ConsoleManager>.Get().AddVariable(
            "imgui.log_mouse_events",
            "Log mouse events to console for debugging",
            false);
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="Win32InputHandler"/> class.
    /// </summary>
    ~Win32InputHandler() => this.ReleaseUnmanagedResources();

    private delegate LRESULT WndProcDelegate(HWND hWnd, uint uMsg, WPARAM wparam, LPARAM lparam);

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

            ImGui.GetIO().Handle->IniFilename = (byte*)this.iniPathPtr;
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
        var focusedWindow = GetForegroundWindow();

        io.DisplaySize.X = targetWidth;
        io.DisplaySize.Y = targetHeight;
        io.DisplayFramebufferScale.X = 1f;
        io.DisplayFramebufferScale.Y = 1f;

        var frequency = Stopwatch.Frequency;
        var currentTime = Stopwatch.GetTimestamp();
        io.DeltaTime = this.lastTime > 0 ? (float)((double)(currentTime - this.lastTime) / frequency) : 1f / 60;
        this.lastTime = currentTime;

        this.viewportHandler.UpdateMonitors();

        this.UpdateMouseData(focusedWindow);

        this.ProcessKeyEventsWorkarounds(focusedWindow);

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
            for (var i = 0; i < io.MouseDown.Length; i++)
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
        if (ImGui.GetCurrentContext().IsNull)
            return null;

        var io = ImGui.GetIO();

        switch (msg)
        {
            case WM.WM_MOUSEMOVE:
            {
                if (!this.mouseTracked)
                {
                    var tme = new TRACKMOUSEEVENT
                    {
                        cbSize = (uint)sizeof(TRACKMOUSEEVENT),
                        dwFlags = TME.TME_LEAVE,
                        hwndTrack = hWndCurrent,
                    };
                    this.mouseTracked = TrackMouseEvent(&tme);
                }

                var mousePos = new POINT(GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam));
                if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
                    ClientToScreen(hWndCurrent, &mousePos);
                io.AddMousePosEvent(mousePos.x, mousePos.y);
                break;
            }

            case WM.WM_MOUSELEAVE:
            {
                this.mouseTracked = false;
                var mouseScreenPos = new POINT(GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam));
                ClientToScreen(hWndCurrent, &mouseScreenPos);
                if (this.ViewportFromPoint(mouseScreenPos).IsNull)
                {
                    var fltMax = ImGuiNative.GETFLTMAX();
                    io.AddMousePosEvent(-fltMax, -fltMax);
                }

                break;
            }

            case WM.WM_LBUTTONDOWN:
            case WM.WM_LBUTTONDBLCLK:
            case WM.WM_RBUTTONDOWN:
            case WM.WM_RBUTTONDBLCLK:
            case WM.WM_MBUTTONDOWN:
            case WM.WM_MBUTTONDBLCLK:
            case WM.WM_XBUTTONDOWN:
            case WM.WM_XBUTTONDBLCLK:
            {
                if (this.cvLogMouseEvents.Value)
                {
                    Log.Verbose(
                        "Handle MouseDown {Btn} WantCaptureMouse: {Want} mouseButtonsDown: {Down}",
                        GetButton(msg, wParam),
                        io.WantCaptureMouse,
                        this.mouseButtonsDown);
                }

                var button = GetButton(msg, wParam);
                if (io.WantCaptureMouse)
                {
                    if (this.mouseButtonsDown == 0 && GetCapture() == nint.Zero)
                    {
                        SetCapture(hWndCurrent);
                    }

                    this.mouseButtonsDown |= 1 << button;
                    io.AddMouseButtonEvent(button, true);
                    return default(LRESULT);
                }

                if (!ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow))
                    ImGui.ClearWindowFocus();

                break;
            }

            case WM.WM_LBUTTONUP:
            case WM.WM_RBUTTONUP:
            case WM.WM_MBUTTONUP:
            case WM.WM_XBUTTONUP:
            {
                if (this.cvLogMouseEvents.Value)
                {
                    Log.Verbose(
                        "Handle MouseUp   {Btn} WantCaptureMouse: {Want} mouseButtonsDown: {Down}",
                        GetButton(msg, wParam),
                        io.WantCaptureMouse,
                        this.mouseButtonsDown);
                }

                var button = GetButton(msg, wParam);

                // Need to check if we captured the button event away from the game here, otherwise the game might get
                // a down event but no up event, causing the cursor to get stuck.
                // Can happen if WantCaptureMouse becomes true in between down and up
                if (io.WantCaptureMouse && (this.mouseButtonsDown & (1 << button)) != 0)
                {
                    this.mouseButtonsDown &= ~(1 << button);
                    if (this.mouseButtonsDown == 0 && GetCapture() == hWndCurrent)
                    {
                        ReleaseCapture();
                    }

                    io.AddMouseButtonEvent(button, false);
                    return default(LRESULT);
                }

                break;
            }

            case WM.WM_MOUSEWHEEL:
                if (io.WantCaptureMouse)
                {
                    io.AddMouseWheelEvent(0, GET_WHEEL_DELTA_WPARAM(wParam) / (float)WHEEL_DELTA);
                    return default(LRESULT);
                }

                break;
            case WM.WM_MOUSEHWHEEL:
                if (io.WantCaptureMouse)
                {
                    io.AddMouseWheelEvent(GET_WHEEL_DELTA_WPARAM(wParam) / (float)WHEEL_DELTA, 0);
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
                    io.AddInputCharacter(new Rune((uint)wParam));
                    return nint.Zero;
                }

                break;

            // this never seemed to work reasonably, but I'll leave it for now
            case WM.WM_SETCURSOR:
                if (io.WantCaptureMouse)
                {
                    if (LOWORD(lParam) == HTCLIENT && this.UpdateMouseCursor())
                    {
                        // this message returns 1 to block further processing
                        // because consistency is no fun
                        return 1;
                    }
                }

                break;

            case WM.WM_DISPLAYCHANGE:
                this.viewportHandler.UpdateMonitors();
                break;

            case WM.WM_SETFOCUS when hWndCurrent == this.hWnd:
                io.AddFocusEvent(true);
                break;

            case WM.WM_KILLFOCUS when hWndCurrent == this.hWnd:
                io.AddFocusEvent(false);
                // if (!ImGui.IsAnyMouseDown() && GetCapture() == hWndCurrent)
                //     ReleaseCapture();
                //
                // ImGui.GetIO().WantCaptureMouse = false;
                // ImGui.ClearWindowFocus();
                break;
        }

        return null;
    }

    private void UpdateMouseData(HWND focusedWindow)
    {
        var io = ImGui.GetIO();

        var mouseScreenPos = default(POINT);
        var hasMouseScreenPos = GetCursorPos(&mouseScreenPos) != 0;

        var isAppFocused =
            focusedWindow != default
            && (focusedWindow == this.hWnd
                || IsChild(focusedWindow, this.hWnd)
                || !ImGui.FindViewportByPlatformHandle(focusedWindow).IsNull);

        if (isAppFocused)
        {
            // (Optional) Set OS mouse position from Dear ImGui if requested (rarely used, only when ImGuiConfigFlags_NavEnableSetMousePos is enabled by user)
            // When multi-viewports are enabled, all Dear ImGui positions are same as OS positions.
            if (io.WantSetMousePos)
            {
                var pos = new POINT((int)io.MousePos.X, (int)io.MousePos.Y);
                if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
                    ClientToScreen(this.hWnd, &pos);
                SetCursorPos(pos.x, pos.y);
            }
        }

        // (Optional) Fallback to provide mouse position when focused (WM_MOUSEMOVE already provides this when hovered or captured)
        if (!io.WantSetMousePos && !this.mouseTracked && hasMouseScreenPos)
        {
            // Single viewport mode: mouse position in client window coordinates (io.MousePos is (0,0) when the mouse is on the upper-left corner of the app window)
            // (This is the position you can get with ::GetCursorPos() + ::ScreenToClient() or WM_MOUSEMOVE.)
            // Multi-viewport mode: mouse position in OS absolute coordinates (io.MousePos is (0,0) when the mouse is on the upper-left of the primary monitor)
            // (This is the position you can get with ::GetCursorPos() or WM_MOUSEMOVE + ::ClientToScreen(). In theory adding viewport->Pos to a client position would also be the same.)
            var mousePos = mouseScreenPos;
            if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) == 0)
            {
                // Use game window, otherwise, positions are calculated based on the focused window which might not be the game.
                // Leads to offsets.
                ClientToScreen(this.hWnd, &mousePos);
            }

            io.AddMousePosEvent(mousePos.x, mousePos.y);
        }

        // (Optional) When using multiple viewports: call io.AddMouseViewportEvent() with the viewport the OS mouse cursor is hovering.
        // If ImGuiBackendFlags_HasMouseHoveredViewport is not set by the backend, Dear imGui will ignore this field and infer the information using its flawed heuristic.
        // - [X] Win32 backend correctly ignore viewports with the _NoInputs flag (here using ::WindowFromPoint with WM_NCHITTEST + HTTRANSPARENT in WndProc does that)
        //       Some backend are not able to handle that correctly. If a backend report an hovered viewport that has the _NoInputs flag (e.g. when dragging a window
        //       for docking, the viewport has the _NoInputs flag in order to allow us to find the viewport under), then Dear ImGui is forced to ignore the value reported
        //       by the backend, and use its flawed heuristic to guess the viewport behind.
        // - [X] Win32 backend correctly reports this regardless of another viewport behind focused and dragged from (we need this to find a useful drag and drop target).
        if (hasMouseScreenPos)
        {
            var viewport = this.ViewportFromPoint(mouseScreenPos);
            io.AddMouseViewportEvent(!viewport.IsNull ? viewport.ID : 0u);
        }
        else
        {
            io.AddMouseViewportEvent(0);
        }
    }

    private ImGuiViewportPtr ViewportFromPoint(POINT mouseScreenPos)
    {
        var hoveredHwnd = WindowFromPoint(mouseScreenPos);
        return hoveredHwnd != default ? ImGui.FindViewportByPlatformHandle(hoveredHwnd) : default;
    }

    private bool UpdateMouseCursor()
    {
        var io = ImGui.GetIO();
        if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.NoMouseCursorChange))
            return false;

        var cur = ImGui.GetMouseCursor();
        if (cur == ImGuiMouseCursor.None || io.MouseDrawCursor)
            SetCursor(default);
        else
            SetCursor(this.cursors[(int)cur]);

        return true;
    }

    private void ProcessKeyEventsWorkarounds(HWND focusedWindow)
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
            // See: https://github.com/goatcorp/ImGuiScene/pull/13
            // > GetForegroundWindow from winuser.h is a surprisingly expensive function.
            var isForeground = focusedWindow == this.hWnd;
            for (var i = (int)ImGuiKey.NamedKeyBegin; i < (int)ImGuiKey.NamedKeyEnd; i++)
            {
                // Skip raising modifier keys if the game is focused.
                // This allows us to raise the keys when one is held and the window becomes unfocused,
                // but if we do not skip them, they will only be held down every 4th frame or so.
                if (isForeground && (IsGamepadKey((ImGuiKey)i) || IsModKey((ImGuiKey)i)))
                    continue;
                io.AddKeyEvent((ImGuiKey)i, false);
            }
        }
    }

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
        if (ImGui.GetCurrentContext().IsNull)
            return DefWindowProcW(hWndCurrent, msg, wParam, lParam);

        var viewport = ImGui.FindViewportByPlatformHandle(hWndCurrent);
        if (viewport.Handle == null)
            return DefWindowProcW(hWndCurrent, msg, wParam, lParam);

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
                SetForegroundWindow(this.hWnd);

                // Also set the window capture to the main window, as focus will not cause
                // future messages to be dispatched to the main window unless it is receiving capture
                SetCapture(this.hWnd);

                // We still want to return MA_NOACTIVATE
                // https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-mouseactivate
                return MA.MA_NOACTIVATE;
            case WM.WM_NCHITTEST:
                // Let mouse pass-through the window. This will allow the backend to set io.MouseHoveredViewport properly (which is OPTIONAL).
                // The ImGuiViewportFlags_NoInputs flag is set while dragging a viewport, as want to detect the window behind the one we are dragging.
                // If you cannot easily access those viewport flags from your windowing/event code: you may manually synchronize its state e.g. in
                // your main loop after calling UpdatePlatformWindows(). Iterate all viewports/platform windows and pass the flag to your windowing system.
                if (viewport.Flags.HasFlag(ImGuiViewportFlags.NoInputs))
                {
                    return HTTRANSPARENT;
                }

                break;
        }

        return DefWindowProcW(hWndCurrent, msg, wParam, lParam);
    }

    private void ReleaseUnmanagedResources()
    {
        if (this.disposedValue)
            return;

        this.viewportHandler.Dispose();

        this.cursors.AsSpan().Clear();

        if (ImGui.GetIO().Handle->BackendPlatformName == (void*)this.platformNamePtr)
            ImGui.GetIO().Handle->BackendPlatformName = null;
        if (this.platformNamePtr != nint.Zero)
            Marshal.FreeHGlobal(this.platformNamePtr);

        if (this.iniPathPtr != nint.Zero)
        {
            ImGui.GetIO().Handle->IniFilename = null;
            Marshal.FreeHGlobal(this.iniPathPtr);
            this.iniPathPtr = nint.Zero;
        }

        this.disposedValue = true;
    }

    private struct ViewportHandler : IDisposable
    {
        private static readonly string WindowClassName = typeof(ViewportHandler).FullName!;

        private Win32InputHandler input;

        private bool wantUpdateMonitors = true;

        public ViewportHandler(Win32InputHandler input)
        {
            this.input = input;

            var pio = ImGui.GetPlatformIO();
            pio.PlatformCreateWindow = (delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void>)&OnCreateWindow;
            pio.PlatformDestroyWindow = (delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void>)&OnDestroyWindow;
            pio.PlatformShowWindow = (delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void>)&OnShowWindow;
            pio.PlatformSetWindowPos = (delegate* unmanaged[Cdecl]<ImGuiViewportPtr, Vector2, void>)&OnSetWindowPos;
            pio.PlatformGetWindowPos = (delegate* unmanaged[Cdecl]<Vector2*, ImGuiViewportPtr, Vector2*>)&OnGetWindowPos;
            pio.PlatformSetWindowSize = (delegate* unmanaged[Cdecl]<ImGuiViewportPtr, Vector2, void>)&OnSetWindowSize;
            pio.PlatformGetWindowSize = (delegate* unmanaged[Cdecl]<Vector2*, ImGuiViewportPtr, Vector2*>)&OnGetWindowSize;
            pio.PlatformSetWindowFocus = (delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void>)&OnSetWindowFocus;
            pio.PlatformGetWindowFocus = (delegate* unmanaged[Cdecl]<ImGuiViewportPtr, byte>)&OnGetWindowFocus;
            pio.PlatformGetWindowMinimized = (delegate* unmanaged[Cdecl]<ImGuiViewportPtr, byte>)&OnGetWindowMinimized;
            pio.PlatformSetWindowTitle = (delegate* unmanaged[Cdecl]<ImGuiViewportPtr, byte*, void>)&OnSetWindowTitle;
            pio.PlatformSetWindowAlpha = (delegate* unmanaged[Cdecl]<ImGuiViewportPtr, float, void>)&OnSetWindowAlpha;
            pio.PlatformUpdateWindow = (delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void>)&OnUpdateWindow;
            // pio.Platform_SetImeInputPos = this.RegisterFunctionPointer<SetImeInputPosDelegate>(this.OnSetImeInputPos);
            // pio.Platform_GetWindowDpiScale = this.RegisterFunctionPointer<GetWindowDpiScaleDelegate>(this.OnGetWindowDpiScale);
            // pio.Platform_ChangedViewport = this.RegisterFunctionPointer<ChangedViewportDelegate>(this.OnChangedViewport);

            fixed (char* windowClassNamePtr = WindowClassName)
            {
                var wcex = new WNDCLASSEXW
                {
                    cbSize = (uint)sizeof(WNDCLASSEXW),
                    style = CS.CS_HREDRAW | CS.CS_VREDRAW,
                    hInstance = (HINSTANCE)Marshal.GetHINSTANCE(typeof(ViewportHandler).Module),
                    hbrBackground = (HBRUSH)(1 + COLOR.COLOR_BACKGROUND),
                    lpfnWndProc = (delegate* unmanaged<HWND, uint, WPARAM, LPARAM, LRESULT>)Marshal
                        .GetFunctionPointerForDelegate(this.input.wndProcDelegate),
                    lpszClassName = (ushort*)windowClassNamePtr,
                };

                if (RegisterClassExW(&wcex) == 0)
                    throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()) ?? new("RegisterClassEx Fail");
            }

            // Register main window handle (which is owned by the main application, not by us)
            // This is mostly for simplicity and consistency, so that our code (e.g. mouse handling etc.) can use same logic for main and secondary viewports.
            var mainViewport = ImGui.GetMainViewport();

            var data = (ImGuiViewportDataWin32*)Marshal.AllocHGlobal(Marshal.SizeOf<ImGuiViewportDataWin32>());
            mainViewport.PlatformUserData = data;
            data->Hwnd = this.input.hWnd;
            data->HwndOwned = false;
            mainViewport.PlatformHandle = this.input.hWnd;
        }

        public void Dispose()
        {
            if (this.input is null)
                return;

            var pio = ImGui.GetPlatformIO();
            ImGui.GetPlatformIO().Handle->Monitors.Free();

            fixed (char* windowClassNamePtr = WindowClassName)
            {
                UnregisterClassW(
                    (ushort*)windowClassNamePtr,
                    (HINSTANCE)Marshal.GetHINSTANCE(typeof(ViewportHandler).Module));
            }

            pio.PlatformCreateWindow = null;
            pio.PlatformDestroyWindow = null;
            pio.PlatformShowWindow = null;
            pio.PlatformSetWindowPos = null;
            pio.PlatformGetWindowPos = null;
            pio.PlatformSetWindowSize = null;
            pio.PlatformGetWindowSize = null;
            pio.PlatformSetWindowFocus = null;
            pio.PlatformGetWindowFocus = null;
            pio.PlatformGetWindowMinimized = null;
            pio.PlatformSetWindowTitle = null;
            pio.PlatformSetWindowAlpha = null;
            pio.PlatformUpdateWindow = null;
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
            pio.Handle->Monitors.Resize(0);

            EnumDisplayMonitors(default, null, &EnumDisplayMonitorsCallback, default);

            Log.Information("Monitors set up!");
            foreach (ref var monitor in pio.Handle->Monitors)
            {
                Log.Information(
                    "Monitor: {MainPos} {MainSize} {WorkPos} {WorkSize}",
                    monitor.MainPos,
                    monitor.MainSize,
                    monitor.WorkPos,
                    monitor.WorkSize);
            }

            return;

            [UnmanagedCallersOnly]
            static BOOL EnumDisplayMonitorsCallback(HMONITOR hMonitor, HDC hdc, RECT* rect, LPARAM lParam)
            {
                var info = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
                if (!GetMonitorInfoW(hMonitor, &info))
                    return true;

                var monitorLt = new Vector2(info.rcMonitor.left, info.rcMonitor.top);
                var monitorRb = new Vector2(info.rcMonitor.right, info.rcMonitor.bottom);
                var workLt = new Vector2(info.rcWork.left, info.rcWork.top);
                var workRb = new Vector2(info.rcWork.right, info.rcWork.bottom);

                // Give ImGui the info for this display
                var imMonitor = new ImGuiPlatformMonitor
                {
                    MainPos = monitorLt,
                    MainSize = monitorRb - monitorLt,
                    WorkPos = workLt,
                    WorkSize = workRb - workLt,
                    DpiScale = 1f,
                };
                if ((info.dwFlags & MONITORINFOF_PRIMARY) != 0)
                    ImGui.GetPlatformIO().Monitors.PushFront(imMonitor);
                else
                    ImGui.GetPlatformIO().Monitors.PushBack(imMonitor);
                return true;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void OnCreateWindow(ImGuiViewportPtr viewport)
        {
            var data = (ImGuiViewportDataWin32*)Marshal.AllocHGlobal(Marshal.SizeOf<ImGuiViewportDataWin32>());
            viewport.PlatformUserData = data;
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
            AdjustWindowRectEx(&rect, (uint)data->DwStyle, false, (uint)data->DwExStyle);

            fixed (char* windowClassNamePtr = WindowClassName)
            {
                data->Hwnd = CreateWindowExW(
                    (uint)data->DwExStyle,
                    (ushort*)windowClassNamePtr,
                    (ushort*)windowClassNamePtr,
                    (uint)data->DwStyle,
                    rect.left,
                    rect.top,
                    rect.right - rect.left,
                    rect.bottom - rect.top,
                    parentWindow,
                    default,
                    (HINSTANCE)Marshal.GetHINSTANCE(typeof(ViewportHandler).Module),
                    null);
            }

            if (data->Hwnd == 0)
                Util.Fatal($"CreateWindowExW failed: {GetLastError()}", "ImGui Viewport error");

            data->HwndOwned = true;
            viewport.PlatformRequestResize = false;
            viewport.PlatformHandle = viewport.PlatformHandleRaw = data->Hwnd;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void OnDestroyWindow(ImGuiViewportPtr viewport)
        {
            // This is also called on the main viewport for some reason, and we never set that viewport's PlatformUserData
            if (viewport.PlatformUserData == null) return;

            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;

            if (GetCapture() == data->Hwnd)
            {
                // Transfer capture so if we started dragging from a window that later disappears, we'll still receive the MOUSEUP event.
                ReleaseCapture();
                if (viewport.ParentViewportId != 0)
                {
                    var parentViewport = ImGui.FindViewportByID(viewport.ParentViewportId);
                    SetCapture((HWND)parentViewport.PlatformHandle);
                }
            }

            if (data->Hwnd != nint.Zero && data->HwndOwned)
            {
                var result = DestroyWindow(data->Hwnd);
                if (result == false && GetLastError() == ERROR.ERROR_ACCESS_DENIED)
                {
                    // We are disposing, and we're doing it from a different thread because of course we are
                    // Just send the window the close message
                    PostMessageW(data->Hwnd, WM.WM_CLOSE, default, default);
                }
            }

            data->Hwnd = default;
            Marshal.FreeHGlobal(new IntPtr(viewport.PlatformUserData));
            viewport.PlatformUserData = viewport.PlatformHandle = null;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void OnShowWindow(ImGuiViewportPtr viewport)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;

            if (viewport.Flags.HasFlag(ImGuiViewportFlags.NoFocusOnAppearing))
                ShowWindow(data->Hwnd, SW.SW_SHOWNA);
            else
                ShowWindow(data->Hwnd, SW.SW_SHOW);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void OnUpdateWindow(ImGuiViewportPtr viewport)
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

                _ = SetWindowLongW(data->Hwnd, GWL.GWL_STYLE, data->DwStyle);
                _ = SetWindowLongW(data->Hwnd, GWL.GWL_EXSTYLE, data->DwExStyle);

                // Create window
                var rect = new RECT
                {
                    left = (int)viewport.Pos.X,
                    top = (int)viewport.Pos.Y,
                    right = (int)(viewport.Pos.X + viewport.Size.X),
                    bottom = (int)(viewport.Pos.Y + viewport.Size.Y),
                };
                AdjustWindowRectEx(&rect, (uint)data->DwStyle, false, (uint)data->DwExStyle);
                SetWindowPos(
                    data->Hwnd,
                    insertAfter,
                    rect.left,
                    rect.top,
                    rect.right - rect.left,
                    rect.bottom - rect.top,
                    (uint)(swpFlag | SWP.SWP_NOACTIVATE | SWP.SWP_FRAMECHANGED));

                // This is necessary when we alter the style
                ShowWindow(data->Hwnd, SW.SW_SHOWNA);
                viewport.PlatformRequestMove = viewport.PlatformRequestResize = true;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Vector2* OnGetWindowPos(Vector2* returnValueStorage, ImGuiViewportPtr viewport)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;
            var pt = new POINT { x = 0, y = 0 };
            ClientToScreen(data->Hwnd, &pt);
            *returnValueStorage = new(pt.x, pt.y);
            return returnValueStorage;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void OnSetWindowPos(ImGuiViewportPtr viewport, Vector2 pos)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;
            var rect = new RECT((int)pos.X, (int)pos.Y, (int)pos.X, (int)pos.Y);
            AdjustWindowRectEx(&rect, (uint)data->DwStyle, false, (uint)data->DwExStyle);
            SetWindowPos(
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

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Vector2* OnGetWindowSize(Vector2* returnValueStorage, ImGuiViewportPtr viewport)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;
            RECT rect;
            GetClientRect(data->Hwnd, &rect);
            *returnValueStorage = new(rect.right - rect.left, rect.bottom - rect.top);
            return returnValueStorage;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void OnSetWindowSize(ImGuiViewportPtr viewport, Vector2 size)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;

            var rect = new RECT(0, 0, (int)size.X, (int)size.Y);
            AdjustWindowRectEx(&rect, (uint)data->DwStyle, false, (uint)data->DwExStyle);
            SetWindowPos(
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

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void OnSetWindowFocus(ImGuiViewportPtr viewport)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;

            BringWindowToTop(data->Hwnd);
            SetForegroundWindow(data->Hwnd);
            SetFocus(data->Hwnd);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static byte OnGetWindowFocus(ImGuiViewportPtr viewport)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;
            return GetForegroundWindow() == data->Hwnd ? (byte)1 : (byte)0;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static byte OnGetWindowMinimized(ImGuiViewportPtr viewport)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;
            return IsIconic(data->Hwnd) ? (byte)1 : (byte)0;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void OnSetWindowTitle(ImGuiViewportPtr viewport, byte* title)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;
            fixed (char* pwszTitle = MemoryHelper.ReadStringNullTerminated((nint)title))
                SetWindowTextW(data->Hwnd, (ushort*)pwszTitle);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void OnSetWindowAlpha(ImGuiViewportPtr viewport, float alpha)
        {
            var data = (ImGuiViewportDataWin32*)viewport.PlatformUserData;
            var style = GetWindowLongW(data->Hwnd, GWL.GWL_EXSTYLE);

            alpha = Math.Clamp(alpha, 0f, 1f);
            if (alpha < 1.0f)
            {
                style |= WS.WS_EX_LAYERED;
                _ = SetWindowLongW(data->Hwnd, GWL.GWL_EXSTYLE, style);
            }
            else
            {
                style &= ~WS.WS_EX_LAYERED;
                _ = SetWindowLongW(data->Hwnd, GWL.GWL_EXSTYLE, style);
            }

            _ = SetLayeredWindowAttributes(data->Hwnd, 0, (byte)(255 * alpha), LWA.LWA_ALPHA);
        }

        // TODO: Decode why IME is miserable
        // private void OnSetImeInputPos(ImGuiViewportPtr viewport, Vector2 pos) {
        //     COMPOSITIONFORM cs = new COMPOSITIONFORM(
        //         0x20,
        //         new POINT(
        //             (int) (pos.X - viewport.Pos.X),
        //             (int) (pos.Y - viewport.Pos.Y)),
        //         new RECT(0, 0, 0, 0)
        //     );
        //     var hwnd = viewport.PlatformHandle;
        //     if (hwnd != nint.Zero) {
        //         var himc = ImmGetContext(hwnd);
        //         if (himc != nint.Zero) {
        //             ImmSetCompositionWindow(himc, ref cs);
        //             ImmReleaseContext(hwnd, himc);
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
