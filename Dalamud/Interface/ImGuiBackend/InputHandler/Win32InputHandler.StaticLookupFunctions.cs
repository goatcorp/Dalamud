using System.Runtime.CompilerServices;

using ImGuiNET;

using TerraFX.Interop.Windows;

namespace Dalamud.Interface.ImGuiBackend.InputHandler;

/// <summary>
/// An implementation of <see cref="IImGuiInputHandler"/>, using Win32 APIs.
/// </summary>
internal sealed partial class Win32InputHandler
{
    /// <summary>
    /// Maps a <see cref="VK"/> to <see cref="ImGuiKey"/>.
    /// </summary>
    /// <param name="key">The virtual key.</param>
    /// <returns>The corresponding <see cref="ImGuiKey"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImGuiKey VirtualKeyToImGuiKey(int key) => key switch
    {
        VK.VK_TAB => ImGuiKey.Tab,
        VK.VK_LEFT => ImGuiKey.LeftArrow,
        VK.VK_RIGHT => ImGuiKey.RightArrow,
        VK.VK_UP => ImGuiKey.UpArrow,
        VK.VK_DOWN => ImGuiKey.DownArrow,
        VK.VK_PRIOR => ImGuiKey.PageUp,
        VK.VK_NEXT => ImGuiKey.PageDown,
        VK.VK_HOME => ImGuiKey.Home,
        VK.VK_END => ImGuiKey.End,
        VK.VK_INSERT => ImGuiKey.Insert,
        VK.VK_DELETE => ImGuiKey.Delete,
        VK.VK_BACK => ImGuiKey.Backspace,
        VK.VK_SPACE => ImGuiKey.Space,
        VK.VK_RETURN => ImGuiKey.Enter,
        VK.VK_ESCAPE => ImGuiKey.Escape,
        VK.VK_OEM_7 => ImGuiKey.Apostrophe,
        VK.VK_OEM_COMMA => ImGuiKey.Comma,
        VK.VK_OEM_MINUS => ImGuiKey.Minus,
        VK.VK_OEM_PERIOD => ImGuiKey.Period,
        VK.VK_OEM_2 => ImGuiKey.Slash,
        VK.VK_OEM_1 => ImGuiKey.Semicolon,
        VK.VK_OEM_PLUS => ImGuiKey.Equal,
        VK.VK_OEM_4 => ImGuiKey.LeftBracket,
        VK.VK_OEM_5 => ImGuiKey.Backslash,
        VK.VK_OEM_6 => ImGuiKey.RightBracket,
        VK.VK_OEM_3 => ImGuiKey.GraveAccent,
        VK.VK_CAPITAL => ImGuiKey.CapsLock,
        VK.VK_SCROLL => ImGuiKey.ScrollLock,
        VK.VK_NUMLOCK => ImGuiKey.NumLock,
        VK.VK_SNAPSHOT => ImGuiKey.PrintScreen,
        VK.VK_PAUSE => ImGuiKey.Pause,
        VK.VK_NUMPAD0 => ImGuiKey.Keypad0,
        VK.VK_NUMPAD1 => ImGuiKey.Keypad1,
        VK.VK_NUMPAD2 => ImGuiKey.Keypad2,
        VK.VK_NUMPAD3 => ImGuiKey.Keypad3,
        VK.VK_NUMPAD4 => ImGuiKey.Keypad4,
        VK.VK_NUMPAD5 => ImGuiKey.Keypad5,
        VK.VK_NUMPAD6 => ImGuiKey.Keypad6,
        VK.VK_NUMPAD7 => ImGuiKey.Keypad7,
        VK.VK_NUMPAD8 => ImGuiKey.Keypad8,
        VK.VK_NUMPAD9 => ImGuiKey.Keypad9,
        VK.VK_DECIMAL => ImGuiKey.KeypadDecimal,
        VK.VK_DIVIDE => ImGuiKey.KeypadDivide,
        VK.VK_MULTIPLY => ImGuiKey.KeypadMultiply,
        VK.VK_SUBTRACT => ImGuiKey.KeypadSubtract,
        VK.VK_ADD => ImGuiKey.KeypadAdd,
        VK.VK_RETURN + 256 => ImGuiKey.KeypadEnter,
        VK.VK_LSHIFT => ImGuiKey.LeftShift,
        VK.VK_LCONTROL => ImGuiKey.LeftCtrl,
        VK.VK_LMENU => ImGuiKey.LeftAlt,
        VK.VK_LWIN => ImGuiKey.LeftSuper,
        VK.VK_RSHIFT => ImGuiKey.RightShift,
        VK.VK_RCONTROL => ImGuiKey.RightCtrl,
        VK.VK_RMENU => ImGuiKey.RightAlt,
        VK.VK_RWIN => ImGuiKey.RightSuper,
        VK.VK_APPS => ImGuiKey.Menu,
        '0' => ImGuiKey._0,
        '1' => ImGuiKey._1,
        '2' => ImGuiKey._2,
        '3' => ImGuiKey._3,
        '4' => ImGuiKey._4,
        '5' => ImGuiKey._5,
        '6' => ImGuiKey._6,
        '7' => ImGuiKey._7,
        '8' => ImGuiKey._8,
        '9' => ImGuiKey._9,
        'A' => ImGuiKey.A,
        'B' => ImGuiKey.B,
        'C' => ImGuiKey.C,
        'D' => ImGuiKey.D,
        'E' => ImGuiKey.E,
        'F' => ImGuiKey.F,
        'G' => ImGuiKey.G,
        'H' => ImGuiKey.H,
        'I' => ImGuiKey.I,
        'J' => ImGuiKey.J,
        'K' => ImGuiKey.K,
        'L' => ImGuiKey.L,
        'M' => ImGuiKey.M,
        'N' => ImGuiKey.N,
        'O' => ImGuiKey.O,
        'P' => ImGuiKey.P,
        'Q' => ImGuiKey.Q,
        'R' => ImGuiKey.R,
        'S' => ImGuiKey.S,
        'T' => ImGuiKey.T,
        'U' => ImGuiKey.U,
        'V' => ImGuiKey.V,
        'W' => ImGuiKey.W,
        'X' => ImGuiKey.X,
        'Y' => ImGuiKey.Y,
        'Z' => ImGuiKey.Z,
        VK.VK_F1 => ImGuiKey.F1,
        VK.VK_F2 => ImGuiKey.F2,
        VK.VK_F3 => ImGuiKey.F3,
        VK.VK_F4 => ImGuiKey.F4,
        VK.VK_F5 => ImGuiKey.F5,
        VK.VK_F6 => ImGuiKey.F6,
        VK.VK_F7 => ImGuiKey.F7,
        VK.VK_F8 => ImGuiKey.F8,
        VK.VK_F9 => ImGuiKey.F9,
        VK.VK_F10 => ImGuiKey.F10,
        VK.VK_F11 => ImGuiKey.F11,
        VK.VK_F12 => ImGuiKey.F12,
        _ => ImGuiKey.None,
    };

    /// <summary>
    /// Maps a <see cref="ImGuiKey"/> to <see cref="VK"/>.
    /// </summary>
    /// <param name="key">The ImGui key.</param>
    /// <returns>The corresponding <see cref="VK"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ImGuiKeyToVirtualKey(ImGuiKey key) => key switch
    {
        ImGuiKey.Tab => VK.VK_TAB,
        ImGuiKey.LeftArrow => VK.VK_LEFT,
        ImGuiKey.RightArrow => VK.VK_RIGHT,
        ImGuiKey.UpArrow => VK.VK_UP,
        ImGuiKey.DownArrow => VK.VK_DOWN,
        ImGuiKey.PageUp => VK.VK_PRIOR,
        ImGuiKey.PageDown => VK.VK_NEXT,
        ImGuiKey.Home => VK.VK_HOME,
        ImGuiKey.End => VK.VK_END,
        ImGuiKey.Insert => VK.VK_INSERT,
        ImGuiKey.Delete => VK.VK_DELETE,
        ImGuiKey.Backspace => VK.VK_BACK,
        ImGuiKey.Space => VK.VK_SPACE,
        ImGuiKey.Enter => VK.VK_RETURN,
        ImGuiKey.Escape => VK.VK_ESCAPE,
        ImGuiKey.Apostrophe => VK.VK_OEM_7,
        ImGuiKey.Comma => VK.VK_OEM_COMMA,
        ImGuiKey.Minus => VK.VK_OEM_MINUS,
        ImGuiKey.Period => VK.VK_OEM_PERIOD,
        ImGuiKey.Slash => VK.VK_OEM_2,
        ImGuiKey.Semicolon => VK.VK_OEM_1,
        ImGuiKey.Equal => VK.VK_OEM_PLUS,
        ImGuiKey.LeftBracket => VK.VK_OEM_4,
        ImGuiKey.Backslash => VK.VK_OEM_5,
        ImGuiKey.RightBracket => VK.VK_OEM_6,
        ImGuiKey.GraveAccent => VK.VK_OEM_3,
        ImGuiKey.CapsLock => VK.VK_CAPITAL,
        ImGuiKey.ScrollLock => VK.VK_SCROLL,
        ImGuiKey.NumLock => VK.VK_NUMLOCK,
        ImGuiKey.PrintScreen => VK.VK_SNAPSHOT,
        ImGuiKey.Pause => VK.VK_PAUSE,
        ImGuiKey.Keypad0 => VK.VK_NUMPAD0,
        ImGuiKey.Keypad1 => VK.VK_NUMPAD1,
        ImGuiKey.Keypad2 => VK.VK_NUMPAD2,
        ImGuiKey.Keypad3 => VK.VK_NUMPAD3,
        ImGuiKey.Keypad4 => VK.VK_NUMPAD4,
        ImGuiKey.Keypad5 => VK.VK_NUMPAD5,
        ImGuiKey.Keypad6 => VK.VK_NUMPAD6,
        ImGuiKey.Keypad7 => VK.VK_NUMPAD7,
        ImGuiKey.Keypad8 => VK.VK_NUMPAD8,
        ImGuiKey.Keypad9 => VK.VK_NUMPAD9,
        ImGuiKey.KeypadDecimal => VK.VK_DECIMAL,
        ImGuiKey.KeypadDivide => VK.VK_DIVIDE,
        ImGuiKey.KeypadMultiply => VK.VK_MULTIPLY,
        ImGuiKey.KeypadSubtract => VK.VK_SUBTRACT,
        ImGuiKey.KeypadAdd => VK.VK_ADD,
        ImGuiKey.KeypadEnter => VK.VK_RETURN + 256,
        ImGuiKey.LeftShift => VK.VK_LSHIFT,
        ImGuiKey.LeftCtrl => VK.VK_LCONTROL,
        ImGuiKey.LeftAlt => VK.VK_LMENU,
        ImGuiKey.LeftSuper => VK.VK_LWIN,
        ImGuiKey.RightShift => VK.VK_RSHIFT,
        ImGuiKey.RightCtrl => VK.VK_RCONTROL,
        ImGuiKey.RightAlt => VK.VK_RMENU,
        ImGuiKey.RightSuper => VK.VK_RWIN,
        ImGuiKey.Menu => VK.VK_APPS,
        ImGuiKey._0 => '0',
        ImGuiKey._1 => '1',
        ImGuiKey._2 => '2',
        ImGuiKey._3 => '3',
        ImGuiKey._4 => '4',
        ImGuiKey._5 => '5',
        ImGuiKey._6 => '6',
        ImGuiKey._7 => '7',
        ImGuiKey._8 => '8',
        ImGuiKey._9 => '9',
        ImGuiKey.A => 'A',
        ImGuiKey.B => 'B',
        ImGuiKey.C => 'C',
        ImGuiKey.D => 'D',
        ImGuiKey.E => 'E',
        ImGuiKey.F => 'F',
        ImGuiKey.G => 'G',
        ImGuiKey.H => 'H',
        ImGuiKey.I => 'I',
        ImGuiKey.J => 'J',
        ImGuiKey.K => 'K',
        ImGuiKey.L => 'L',
        ImGuiKey.M => 'M',
        ImGuiKey.N => 'N',
        ImGuiKey.O => 'O',
        ImGuiKey.P => 'P',
        ImGuiKey.Q => 'Q',
        ImGuiKey.R => 'R',
        ImGuiKey.S => 'S',
        ImGuiKey.T => 'T',
        ImGuiKey.U => 'U',
        ImGuiKey.V => 'V',
        ImGuiKey.W => 'W',
        ImGuiKey.X => 'X',
        ImGuiKey.Y => 'Y',
        ImGuiKey.Z => 'Z',
        ImGuiKey.F1 => VK.VK_F1,
        ImGuiKey.F2 => VK.VK_F2,
        ImGuiKey.F3 => VK.VK_F3,
        ImGuiKey.F4 => VK.VK_F4,
        ImGuiKey.F5 => VK.VK_F5,
        ImGuiKey.F6 => VK.VK_F6,
        ImGuiKey.F7 => VK.VK_F7,
        ImGuiKey.F8 => VK.VK_F8,
        ImGuiKey.F9 => VK.VK_F9,
        ImGuiKey.F10 => VK.VK_F10,
        ImGuiKey.F11 => VK.VK_F11,
        ImGuiKey.F12 => VK.VK_F12,
        _ => 0,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsGamepadKey(ImGuiKey key) => (int)key is >= 617 and <= 640;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsModKey(ImGuiKey key) =>
        key is ImGuiKey.LeftShift
            or ImGuiKey.RightShift
            or ImGuiKey.ModShift
            or ImGuiKey.LeftCtrl
            or ImGuiKey.ModCtrl
            or ImGuiKey.LeftAlt
            or ImGuiKey.RightAlt
            or ImGuiKey.ModAlt;

    private static void AddKeyEvent(ImGuiKey key, bool down, int nativeKeycode, int nativeScancode = -1)
    {
        var io = ImGui.GetIO();
        io.AddKeyEvent(key, down);
        io.SetKeyEventNativeData(key, nativeKeycode, nativeScancode);
    }

    private static void UpdateKeyModifiers()
    {
        var io = ImGui.GetIO();
        io.AddKeyEvent(ImGuiKey.ModCtrl, IsVkDown(VK.VK_CONTROL));
        io.AddKeyEvent(ImGuiKey.ModShift, IsVkDown(VK.VK_SHIFT));
        io.AddKeyEvent(ImGuiKey.ModAlt, IsVkDown(VK.VK_MENU));
        io.AddKeyEvent(ImGuiKey.ModSuper, IsVkDown(VK.VK_APPS));
    }

    private static void UpAllKeys()
    {
        var io = ImGui.GetIO();
        for (var i = (int)ImGuiKey.NamedKey_BEGIN; i < (int)ImGuiKey.NamedKey_END; i++)
            io.AddKeyEvent((ImGuiKey)i, false);
    }

    private static void UpAllMouseButton()
    {
        var io = ImGui.GetIO();
        for (var i = 0; i < io.MouseDown.Count; i++)
            io.MouseDown[i] = false;
    }

    private static bool IsVkDown(int key) => (TerraFX.Interop.Windows.Windows.GetKeyState(key) & 0x8000) != 0;

    private static int GetButton(uint msg, WPARAM wParam) => msg switch
    {
        WM.WM_LBUTTONUP or WM.WM_LBUTTONDOWN or WM.WM_LBUTTONDBLCLK => 0,
        WM.WM_RBUTTONUP or WM.WM_RBUTTONDOWN or WM.WM_RBUTTONDBLCLK => 1,
        WM.WM_MBUTTONUP or WM.WM_MBUTTONDOWN or WM.WM_MBUTTONDBLCLK => 2,
        WM.WM_XBUTTONUP or WM.WM_XBUTTONDOWN or WM.WM_XBUTTONDBLCLK =>
            TerraFX.Interop.Windows.Windows.GET_XBUTTON_WPARAM(wParam) == TerraFX.Interop.Windows.Windows.XBUTTON1 ? 3 : 4,
        _ => 0,
    };

    private static void ViewportFlagsToWin32Styles(ImGuiViewportFlags flags, out int style, out int exStyle)
    {
        style = (int)(flags.HasFlag(ImGuiViewportFlags.NoDecoration) ? WS.WS_POPUP : WS.WS_OVERLAPPEDWINDOW);
        exStyle =
            (int)(flags.HasFlag(ImGuiViewportFlags.NoTaskBarIcon) ? WS.WS_EX_TOOLWINDOW : (uint)WS.WS_EX_APPWINDOW);
        exStyle |= WS.WS_EX_NOREDIRECTIONBITMAP;
        if (flags.HasFlag(ImGuiViewportFlags.TopMost))
            exStyle |= WS.WS_EX_TOPMOST;
    }
}
