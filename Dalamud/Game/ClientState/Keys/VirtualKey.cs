namespace Dalamud.Game.ClientState.Keys;

/// <summary>
/// Virtual-key codes.
/// </summary>
/// <remarks>
/// Defined in winuser.h from Windows SDK v6.1.
/// </remarks>
public enum VirtualKey : ushort
{
    /// <summary>
    /// This is an addendum to use on functions in which you have to pass a zero value to represent no key code.
    /// </summary>
    [VirtualKey("No key")]
    NO_KEY = 0,

    /// <summary>
    /// Left mouse button.
    /// </summary>
    [VirtualKey("Left mouse button")]
    LBUTTON = 1,

    /// <summary>
    /// Right mouse button.
    /// </summary>
    [VirtualKey("Right mouse button")]
    RBUTTON = 2,

    /// <summary>
    /// Control-break processing.
    /// </summary>
    [VirtualKey("CANCEL")]
    CANCEL = 3,

    /// <summary>
    /// Middle mouse button (three-button mouse).
    /// </summary>
    /// <remarks>
    /// NOT contiguous with L and R buttons.
    /// </remarks>
    [VirtualKey("Mouse 3")]
    MBUTTON = 4,

    /// <summary>
    /// X1 mouse button.
    /// </summary>

    /// <remarks>
    /// NOT contiguous with L and R buttons.
    /// </remarks>
    [VirtualKey("Mouse 4")]
    XBUTTON1 = 5,

    /// <summary>
    /// X2 mouse button.
    /// </summary>

    /// <remarks>
    /// NOT contiguous with L and R buttons.
    /// </remarks>
    [VirtualKey("Mouse 5")]
    XBUTTON2 = 6,

    /// <summary>
    /// BACKSPACE key.
    /// </summary>
    [VirtualKey("Backspace")]
    BACK = 8,

    /// <summary>
    /// TAB key.
    /// </summary>
    [VirtualKey("Tab")]
    TAB = 9,

    /// <summary>
    /// CLEAR key.
    /// </summary>
    [VirtualKey("Clear")]
    CLEAR = 12,

    /// <summary>
    /// RETURN key.
    /// </summary>
    [VirtualKey("Return/Enter")]
    RETURN = 13,

    /// <summary>
    /// SHIFT key.
    /// </summary>
    [VirtualKey("Shift")]
    SHIFT = 16,

    /// <summary>
    /// CONTROL key.
    /// </summary>
    [VirtualKey("Control")]
    CONTROL = 17,

    /// <summary>
    /// ALT key.
    /// </summary>
    [VirtualKey("Alt")]
    MENU = 18,

    /// <summary>
    /// PAUSE key.
    /// </summary>
    [VirtualKey("Pause")]
    PAUSE = 19,

    /// <summary>
    /// CAPS LOCK key.
    /// </summary>
    [VirtualKey("Caps Lock")]
    CAPITAL = 20,

    /// <summary>
    /// IME Kana mode.
    /// </summary>
    [VirtualKey("Kana Key")]
    KANA = 21,

    /// <summary>
    /// IME Hangeul mode (maintained for compatibility; use User32.VirtualKey.HANGUL).
    /// </summary>
    [VirtualKey("Hangul Key")]
    HANGEUL = KANA,

    /// <summary>
    /// IME Hangul mode.
    /// </summary>
    [VirtualKey("Hangul Key 2")]
    HANGUL = KANA,

    /// <summary>
    /// IME Junja mode.
    /// </summary>
    [VirtualKey("Junja Key")]
    JUNJA = 23,

    /// <summary>
    /// IME final mode.
    /// </summary>
    [VirtualKey("Final Key")]
    FINAL = 24,

    /// <summary>
    /// IME Hanja mode.
    /// </summary>
    [VirtualKey("Hanja Key")]
    HANJA = 25,

    /// <summary>
    /// IME Kanji mode.
    /// </summary>
    [VirtualKey("Kanji Key")]
    KANJI = HANJA,

    /// <summary>
    /// ESC key.
    /// </summary>
    [VirtualKey("Escape")]
    ESCAPE = 27,

    /// <summary>
    /// IME convert.
    /// </summary>
    [VirtualKey("Convert Key")]
    CONVERT = 28,

    /// <summary>
    /// IME nonconvert.
    /// </summary>
    [VirtualKey("Non-Convert Key")]
    NONCONVERT = 29,

    /// <summary>
    /// IME accept.
    /// </summary>
    [VirtualKey("IME Accept Key")]
    ACCEPT = 30,

    /// <summary>
    /// IME mode change request.
    /// </summary>
    [VirtualKey("IME Mode-Change Key")]
    MODECHANGE = 31,

    /// <summary>
    /// SPACEBAR.
    /// </summary>
    [VirtualKey("Spacebar")]
    SPACE = 32,

    /// <summary>
    /// PAGE UP key.
    /// </summary>
    [VirtualKey("Page Up")]
    PRIOR = 33,

    /// <summary>
    /// PAGE DOWN key.
    /// </summary>
    [VirtualKey("Page Down")]
    NEXT = 34,

    /// <summary>
    /// END key.
    /// </summary>
    [VirtualKey("End")]
    END = 35,

    /// <summary>
    /// HOME key.
    /// </summary>
    [VirtualKey("Home")]
    HOME = 36,

    /// <summary>
    /// LEFT ARROW key.
    /// </summary>
    [VirtualKey("Left Arrow")]
    LEFT = 37,

    /// <summary>
    /// UP ARROW key.
    /// </summary>
    [VirtualKey("Up Arrow")]
    UP = 38,

    /// <summary>
    /// RIGHT ARROW key.
    /// </summary>
    [VirtualKey("Right Arrow")]
    RIGHT = 39,

    /// <summary>
    /// DOWN ARROW key.
    /// </summary>
    [VirtualKey("Down Arrow")]
    DOWN = 40,

    /// <summary>
    /// SELECT key.
    /// </summary>
    [VirtualKey("Select")]
    SELECT = 41,

    /// <summary>
    /// PRINT key.
    /// </summary>
    [VirtualKey("Print")]
    PRINT = 42,

    /// <summary>
    /// EXECUTE key.
    /// </summary>
    [VirtualKey("Execute")]
    EXECUTE = 43,

    /// <summary>
    /// PRINT SCREEN key.
    /// </summary>
    [VirtualKey("Print Screen")]
    SNAPSHOT = 44,

    /// <summary>
    /// INS key.
    /// </summary>
    [VirtualKey("Insert")]
    INSERT = 45,

    /// <summary>
    /// DEL key.
    /// </summary>
    [VirtualKey("Delete")]
    DELETE = 46,

    /// <summary>
    /// HELP key.
    /// </summary>
    [VirtualKey("Help")]
    HELP = 47,

    /// <summary>
    /// 0 key.
    /// </summary>
    [VirtualKey("Number-Row 0")]
    KEY_0 = 48,

    /// <summary>
    /// 1 key.
    /// </summary>
    [VirtualKey("Number-Row 1")]
    KEY_1 = 49,

    /// <summary>
    /// 2 key.
    /// </summary>
    [VirtualKey("Number-Row 2")]
    KEY_2 = 50,

    /// <summary>
    /// 3 key.
    /// </summary>
    [VirtualKey("Number-Row 3")]
    KEY_3 = 51,

    /// <summary>
    /// 4 key.
    /// </summary>
    [VirtualKey("Number-Row 4")]
    KEY_4 = 52,

    /// <summary>
    /// 5 key.
    /// </summary>
    [VirtualKey("Number-Row 5")]
    KEY_5 = 53,

    /// <summary>
    /// 6 key.
    /// </summary>
    [VirtualKey("Number-Row 6")]
    KEY_6 = 54,

    /// <summary>
    /// 7 key.
    /// </summary>
    [VirtualKey("Number-Row 7")]
    KEY_7 = 55,

    /// <summary>
    /// 8 key.
    /// </summary>
    [VirtualKey("Number-Row 8")]
    KEY_8 = 56,

    /// <summary>
    /// 9 key.
    /// </summary>
    [VirtualKey("Number-Row 9")]
    KEY_9 = 57,

    /// <summary>
    /// A key.
    /// </summary>
    [VirtualKey("A")]
    A = 65,

    /// <summary>
    /// B key.
    /// </summary>
    [VirtualKey("B")]
    B = 66,

    /// <summary>
    /// C key.
    /// </summary>
    [VirtualKey("C")]
    C = 67,

    /// <summary>
    /// D key.
    /// </summary>
    [VirtualKey("D")]
    D = 68,

    /// <summary>
    /// E key.
    /// </summary>
    [VirtualKey("E")]
    E = 69,

    /// <summary>
    /// F key.
    /// </summary>
    [VirtualKey("F")]
    F = 70,

    /// <summary>
    /// G key.
    /// </summary>
    [VirtualKey("G")]
    G = 71,

    /// <summary>
    /// H key.
    /// </summary>
    [VirtualKey("H")]
    H = 72,

    /// <summary>
    /// I key.
    /// </summary>
    [VirtualKey("I")]
    I = 73,

    /// <summary>
    /// J key.
    /// </summary>
    [VirtualKey("J")]
    J = 74,

    /// <summary>
    /// K key.
    /// </summary>
    [VirtualKey("K")]
    K = 75,

    /// <summary>
    /// L key.
    /// </summary>
    [VirtualKey("L")]
    L = 76,

    /// <summary>
    /// M key.
    /// </summary>
    [VirtualKey("M")]
    M = 77,

    /// <summary>
    /// N key.
    /// </summary>
    [VirtualKey("N")]
    N = 78,

    /// <summary>
    /// O key.
    /// </summary>
    [VirtualKey("O")]
    O = 79,

    /// <summary>
    /// P key.
    /// </summary>
    [VirtualKey("P")]
    P = 80,

    /// <summary>
    /// Q key.
    /// </summary>
    [VirtualKey("Q")]
    Q = 81,

    /// <summary>
    /// R key.
    /// </summary>
    [VirtualKey("R")]
    R = 82,

    /// <summary>
    /// S key.
    /// </summary>
    [VirtualKey("S")]
    S = 83,

    /// <summary>
    /// T key.
    /// </summary>
    [VirtualKey("T")]
    T = 84,

    /// <summary>
    /// U key.
    /// </summary>
    [VirtualKey("U")]
    U = 85,

    /// <summary>
    /// V key.
    /// </summary>
    [VirtualKey("V")]
    V = 86,

    /// <summary>
    /// W key.
    /// </summary>
    [VirtualKey("W")]
    W = 87,

    /// <summary>
    /// X key.
    /// </summary>
    [VirtualKey("X")]
    X = 88,

    /// <summary>
    /// Y key.
    /// </summary>
    [VirtualKey("Y")]
    Y = 89,

    /// <summary>
    /// Z key.
    /// </summary>
    [VirtualKey("Z")]
    Z = 90,

    /// <summary>
    /// Left Windows key (Natural keyboard).
    /// </summary>
    [VirtualKey("Left Windows")]
    LWIN = 91,

    /// <summary>
    /// Right Windows key (Natural keyboard).
    /// </summary>
    [VirtualKey("Right Windows")]
    RWIN = 92,

    /// <summary>
    /// Applications key (Natural keyboard).
    /// </summary>
    [VirtualKey("Applications")]
    APPS = 93,

    /// <summary>
    /// Computer Sleep key.
    /// </summary>
    [VirtualKey("Sleep")]
    SLEEP = 95,

    /// <summary>
    /// Numeric keypad 0 key.
    /// </summary>
    [VirtualKey("Numpad 0")]
    NUMPAD0 = 96,

    /// <summary>
    /// Numeric keypad 1 key.
    /// </summary>
    [VirtualKey("Numpad 1")]
    NUMPAD1 = 97,

    /// <summary>
    /// Numeric keypad 2 key.
    /// </summary>
    [VirtualKey("Numpad 2")]
    NUMPAD2 = 98,

    /// <summary>
    /// Numeric keypad 3 key.
    /// </summary>
    [VirtualKey("Numpad 3")]
    NUMPAD3 = 99,

    /// <summary>
    /// Numeric keypad 4 key.
    /// </summary>
    [VirtualKey("Numpad 4")]
    NUMPAD4 = 100,

    /// <summary>
    /// Numeric keypad 5 key.
    /// </summary>
    [VirtualKey("Numpad 5")]
    NUMPAD5 = 101,

    /// <summary>
    /// Numeric keypad 6 key.
    /// </summary>
    [VirtualKey("Numpad 6")]
    NUMPAD6 = 102,

    /// <summary>
    /// Numeric keypad 7 key.
    /// </summary>
    [VirtualKey("Numpad 7")]
    NUMPAD7 = 103,

    /// <summary>
    /// Numeric keypad 8 key.
    /// </summary>
    [VirtualKey("Numpad 8")]
    NUMPAD8 = 104,

    /// <summary>
    /// Numeric keypad 9 key.
    /// </summary>
    [VirtualKey("Numpad 9")]
    NUMPAD9 = 105,

    /// <summary>
    /// Multiply key.
    /// </summary>
    [VirtualKey("Numpad Multiply")]
    MULTIPLY = 106,

    /// <summary>
    /// Add key.
    /// </summary>
    [VirtualKey("Numpad Add")]
    ADD = 107,

    /// <summary>
    /// Separator key.
    /// </summary>
    [VirtualKey("Numpad Separator")]
    SEPARATOR = 108,

    /// <summary>
    /// Subtract key.
    /// </summary>
    [VirtualKey("Numpad Subtract")]
    SUBTRACT = 109,

    /// <summary>
    /// Decimal key.
    /// </summary>
    [VirtualKey("Numpad Decimal")]
    DECIMAL = 110,

    /// <summary>
    /// Divide key.
    /// </summary>
    [VirtualKey("Numpad Divide")]
    DIVIDE = 111,

    /// <summary>
    /// F1 Key.
    /// </summary>
    [VirtualKey("F1")]
    F1 = 112,

    /// <summary>
    /// F2 Key.
    /// </summary>
    [VirtualKey("F2")]
    F2 = 113,

    /// <summary>
    /// F3 Key.
    /// </summary>
    [VirtualKey("F3")]
    F3 = 114,

    /// <summary>
    /// F4 Key.
    /// </summary>
    [VirtualKey("F4")]
    F4 = 115,

    /// <summary>
    /// F5 Key.
    /// </summary>
    [VirtualKey("F5")]
    F5 = 116,

    /// <summary>
    /// F6 Key.
    /// </summary>
    [VirtualKey("F6")]
    F6 = 117,

    /// <summary>
    /// F7 Key.
    /// </summary>
    [VirtualKey("F7")]
    F7 = 118,

    /// <summary>
    /// F8 Key.
    /// </summary>
    [VirtualKey("F8")]
    F8 = 119,

    /// <summary>
    /// F9 Key.
    /// </summary>
    [VirtualKey("F9")]
    F9 = 120,

    /// <summary>
    /// F10 Key.
    /// </summary>
    [VirtualKey("F10")]
    F10 = 121,

    /// <summary>
    /// F11 Key.
    /// </summary>
    [VirtualKey("F11")]
    F11 = 122,

    /// <summary>
    /// F12 Key.
    /// </summary>
    [VirtualKey("F12")]
    F12 = 123,

    /// <summary>
    /// F13 Key.
    /// </summary>
    [VirtualKey("F13")]
    F13 = 124,

    /// <summary>
    /// F14 Key.
    /// </summary>
    [VirtualKey("F14")]
    F14 = 125,

    /// <summary>
    /// F15 Key.
    /// </summary>
    [VirtualKey("F15")]
    F15 = 126,

    /// <summary>
    /// F16 Key.
    /// </summary>
    [VirtualKey("F16")]
    F16 = 127,

    /// <summary>
    /// F17 Key.
    /// </summary>
    [VirtualKey("F17")]
    F17 = 128,

    /// <summary>
    /// F18 Key.
    /// </summary>
    [VirtualKey("F18")]
    F18 = 129,

    /// <summary>
    /// F19 Key.
    /// </summary>
    [VirtualKey("F19")]
    F19 = 130,

    /// <summary>
    /// F20 Key.
    /// </summary>
    [VirtualKey("F20")]
    F20 = 131,

    /// <summary>
    /// F21 Key.
    /// </summary>
    [VirtualKey("F21")]
    F21 = 132,

    /// <summary>
    /// F22 Key.
    /// </summary>
    [VirtualKey("F22")]
    F22 = 133,

    /// <summary>
    /// F23 Key.
    /// </summary>
    [VirtualKey("F23")]
    F23 = 134,

    /// <summary>
    /// F24 Key.
    /// </summary>
    [VirtualKey("F24")]
    F24 = 135,

    /// <summary>
    /// NUM LOCK key.
    /// </summary>
    [VirtualKey("Num-Lock")]
    NUMLOCK = 144,

    /// <summary>
    /// SCROLL LOCK key.
    /// </summary>
    [VirtualKey("Scroll-Lock")]
    SCROLL = 145,

    /// <summary>
    /// '=' key on numpad (NEC PC-9800 kbd definitions).
    /// </summary>
    [VirtualKey("Numpad Equals")]
    OEM_NEC_EQUAL = 146,

    /// <summary>
    /// 'Dictionary' key (Fujitsu/OASYS kbd definitions).
    /// </summary>
    [VirtualKey("Dictionary (Fujitsu)")]
    OEM_FJ_JISHO = OEM_NEC_EQUAL,

    /// <summary>
    /// 'Unregister word' key (Fujitsu/OASYS kbd definitions).
    /// </summary>
    [VirtualKey("Unregister word (Fujitsu)")]
    OEM_FJ_MASSHOU = 147,

    /// <summary>
    /// 'Register word' key (Fujitsu/OASYS kbd definitions).
    /// </summary>
    [VirtualKey("Register word (Fujitsu)")]
    OEM_FJ_TOUROKU = 148,

    /// <summary>
    /// 'Left OYAYUBI' key (Fujitsu/OASYS kbd definitions).
    /// </summary>
    [VirtualKey("Left Oyayubi (Fujitsu)")]
    OEM_FJ_LOYA = 149,

    /// <summary>
    /// 'Right OYAYUBI' key (Fujitsu/OASYS kbd definitions).
    /// </summary>
    [VirtualKey("Right Oyayubi (Fujitsu)")]
    OEM_FJ_ROYA = 150,

    /// <summary>
    /// Left SHIFT key.
    /// </summary>
    /// <remarks>
    /// Used only as parameters to User32.GetAsyncKeyState and User32.GetKeyState. No other API or message will distinguish
    /// left and right keys in this way.
    /// </remarks>
    [VirtualKey("Left Shift")]
    LSHIFT = 160,

    /// <summary>
    /// Right SHIFT key.
    /// </summary>
    [VirtualKey("Right Shift")]
    RSHIFT = 161,

    /// <summary>
    /// Left CONTROL key.
    /// </summary>
    [VirtualKey("Left Control")]
    LCONTROL = 162,

    /// <summary>
    /// Right CONTROL key.
    /// </summary>
    [VirtualKey("Right Control")]
    RCONTROL = 163,

    /// <summary>
    /// Left MENU key.
    /// </summary>
    [VirtualKey("Left Menu")]
    LMENU = 164,

    /// <summary>
    /// Right MENU key.
    /// </summary>
    [VirtualKey("Right Menu")]
    RMENU = 165,

    /// <summary>
    /// Browser Back key.
    /// </summary>
    [VirtualKey("Browser Back")]
    BROWSER_BACK = 166,

    /// <summary>
    /// Browser Forward key.
    /// </summary>
    [VirtualKey("Browser Forward")]
    BROWSER_FORWARD = 167,

    /// <summary>
    /// Browser Refresh key.
    /// </summary>
    [VirtualKey("Browser Refresh")]
    BROWSER_REFRESH = 168,

    /// <summary>
    /// Browser Stop key.
    /// </summary>
    [VirtualKey("Browser Stop")]
    BROWSER_STOP = 169,

    /// <summary>
    /// Browser Search key.
    /// </summary>
    [VirtualKey("Browser Search")]
    BROWSER_SEARCH = 170,

    /// <summary>
    /// Browser Favorites key.
    /// </summary>
    [VirtualKey("Browser Favorites")]
    BROWSER_FAVORITES = 171,

    /// <summary>
    /// Browser Start and Home key.
    /// </summary>
    [VirtualKey("Browser Home")]
    BROWSER_HOME = 172,

    /// <summary>
    /// Volume Mute key.
    /// </summary>
    [VirtualKey("Mute Volume")]
    VOLUME_MUTE = 173,

    /// <summary>
    /// Volume Down key.
    /// </summary>
    [VirtualKey("Volume Down")]
    VOLUME_DOWN = 174,

    /// <summary>
    /// Volume Up key.
    /// </summary>
    [VirtualKey("Volume Up")]
    VOLUME_UP = 175,

    /// <summary>
    /// Next Track key.
    /// </summary>
    [VirtualKey("Next Track")]
    MEDIA_NEXT_TRACK = 176,

    /// <summary>
    /// Previous Track key.
    /// </summary>
    [VirtualKey("Previous Track")]
    MEDIA_PREV_TRACK = 177,

    /// <summary>
    /// Stop Media key.
    /// </summary>
    [VirtualKey("Stop Media")]
    MEDIA_STOP = 178,

    /// <summary>
    /// Play/Pause Media key.
    /// </summary>
    [VirtualKey("Play/Pause Media")]
    MEDIA_PLAY_PAUSE = 179,

    /// <summary>
    /// Start Mail key.
    /// </summary>
    [VirtualKey("Launch Mail")]
    LAUNCH_MAIL = 180,

    /// <summary>
    /// Select Media key.
    /// </summary>
    [VirtualKey("Launch Media Player")]
    LAUNCH_MEDIA_SELECT = 181,

    /// <summary>
    /// Start Application 1 key.
    /// </summary>
    [VirtualKey("Launch Application 1")]
    LAUNCH_APP1 = 182,

    /// <summary>
    /// Start Application 2 key.
    /// </summary>
    [VirtualKey("Launch Application 2")]
    LAUNCH_APP2 = 183,

    /// <summary>
    /// Used for miscellaneous characters; it can vary by keyboard..
    /// </summary>
    /// <remarks>
    /// For the US standard keyboard, the ';:' key.
    /// </remarks>
    [VirtualKey("Semicolon")]
    OEM_1 = 186,

    /// <summary>
    /// For any country/region, the '+' key.
    /// </summary>
    [VirtualKey("Plus")]
    OEM_PLUS = 187,

    /// <summary>
    /// For any country/region, the ',' key.
    /// </summary>
    [VirtualKey("Comma")]
    OEM_COMMA = 188,

    /// <summary>
    /// For any country/region, the '-' key.
    /// </summary>
    [VirtualKey("Minus")]
    OEM_MINUS = 189,

    /// <summary>
    /// For any country/region, the '.' key.
    /// </summary>
    [VirtualKey("Period")]
    OEM_PERIOD = 190,

    /// <summary>
    /// Used for miscellaneous characters; it can vary by keyboard..
    /// </summary>
    /// <remarks>
    /// For the US standard keyboard, the '/?' key.
    /// </remarks>
    [VirtualKey("Forward Slash/Question Mark")]
    OEM_2 = 191,

    /// <summary>
    /// Used for miscellaneous characters; it can vary by keyboard..
    /// </summary>
    /// <remarks>
    /// For the US standard keyboard, the '`~' key.
    /// </remarks>
    [VirtualKey("Tilde")]
    OEM_3 = 192,

    /// <summary>
    /// Used for miscellaneous characters; it can vary by keyboard..
    /// </summary>
    /// <remarks>
    /// For the US standard keyboard, the '[{' key.
    /// </remarks>
    [VirtualKey("Opening Bracket")]
    OEM_4 = 219,

    /// <summary>
    /// Used for miscellaneous characters; it can vary by keyboard..
    /// </summary>
    /// <remarks>
    /// For the US standard keyboard, the '\|' key.
    /// </remarks>
    [VirtualKey("Back Slash/Pipe")]
    OEM_5 = 220,

    /// <summary>
    /// Used for miscellaneous characters; it can vary by keyboard..
    /// </summary>
    /// <remarks>
    /// For the US standard keyboard, the ']}' key.
    /// </remarks>
    [VirtualKey("Closing Bracket")]
    OEM_6 = 221,

    /// <summary>
    /// Used for miscellaneous characters; it can vary by keyboard..
    /// </summary>
    /// <remarks>
    /// For the US standard keyboard, the 'single-quote/double-quote' (''"') key.
    /// </remarks>
    [VirtualKey("Single Quote/Double Quote")]
    OEM_7 = 222,

    /// <summary>
    /// Used for miscellaneous characters; it can vary by keyboard..
    /// </summary>
    [VirtualKey("OEM 8")]
    OEM_8 = 223,

    /// <summary>
    /// OEM specific.
    /// </summary>

    /// <remarks>
    /// 'AX' key on Japanese AX kbd.
    /// </remarks>
    [VirtualKey("OEM AX")]
    OEM_AX = 225,

    /// <summary>
    /// Either the angle bracket ("&lt;&gt;") key or the backslash ("\|") key on the RT 102-key keyboard.
    /// </summary>
    [VirtualKey("OEM 102")]
    OEM_102 = 226,

    /// <summary>
    /// OEM specific.
    /// </summary>
    /// <remarks>
    /// Help key on ICO.
    /// </remarks>
    [VirtualKey("Help (ICO)")]
    ICO_HELP = 227,

    /// <summary>
    /// OEM specific.
    /// </summary>
    /// <remarks>
    /// 00 key on ICO.
    /// </remarks>
    [VirtualKey("00 (ICO)")]
    ICO_00 = 228,

    /// <summary>
    /// IME PROCESS key.
    /// </summary>
    [VirtualKey("IME Process")]
    PROCESSKEY = 229,

    /// <summary>
    /// OEM specific.
    /// </summary>
    /// <remarks>
    /// Clear key on ICO.
    /// </remarks>
    [VirtualKey("Clear (ICO)")]
    ICO_CLEAR = 230,

    /// <summary>
    /// Used to pass Unicode characters as if they were keystrokes. The PACKET key is the low word of a 32-bit Virtual Key
    /// value used for non-keyboard input methods..
    /// </summary>
    /// <remarks>
    /// For more information, see Remark in User32.KEYBDINPUT, User32.SendInput, User32.WindowMessage.WM_KEYDOWN, and
    /// User32.WindowMessage.WM_KEYUP.
    /// </remarks>
    [VirtualKey("Packet")]
    PACKET = 231,

    /// <summary>
    /// Nokia/Ericsson definition.
    /// </summary>
    [VirtualKey("OEM Reset")]
    OEM_RESET = 233,

    /// <summary>
    /// Nokia/Ericsson definition.
    /// </summary>
    [VirtualKey("OEM Jump")]
    OEM_JUMP = 234,

    /// <summary>
    /// Nokia/Ericsson definition.
    /// </summary>
    [VirtualKey("OEM PA1")]
    OEM_PA1 = 235,

    /// <summary>
    /// Nokia/Ericsson definition.
    /// </summary>
    [VirtualKey("OEM PA2")]
    OEM_PA2 = 236,

    /// <summary>
    /// Nokia/Ericsson definition.
    /// </summary>
    [VirtualKey("OEM PA3")]
    OEM_PA3 = 237,

    /// <summary>
    /// Nokia/Ericsson definition.
    /// </summary>
    [VirtualKey("OEM WSCTRL")]
    OEM_WSCTRL = 238,

    /// <summary>
    /// Nokia/Ericsson definition.
    /// </summary>
    [VirtualKey("OEM CUSEL")]
    OEM_CUSEL = 239,

    /// <summary>
    /// Nokia/Ericsson definition.
    /// </summary>
    [VirtualKey("OEM ATTN")]
    OEM_ATTN = 240,

    /// <summary>
    /// Nokia/Ericsson definition.
    /// </summary>
    [VirtualKey("OEM Finish")]
    OEM_FINISH = 241,

    /// <summary>
    /// Nokia/Ericsson definition.
    /// </summary>
    [VirtualKey("OEM Copy")]
    OEM_COPY = 242,

    /// <summary>
    /// Nokia/Ericsson definition.
    /// </summary>
    [VirtualKey("OEM Auto")]
    OEM_AUTO = 243,

    /// <summary>
    /// Nokia/Ericsson definition.
    /// </summary>
    [VirtualKey("OEM ENLW")]
    OEM_ENLW = 244,

    /// <summary>
    /// Nokia/Ericsson definition.
    /// </summary>
    [VirtualKey("OEM Backtab")]
    OEM_BACKTAB = 245,

    /// <summary>
    /// Attn key.
    /// </summary>
    [VirtualKey("ATTN")]
    ATTN = 246,

    /// <summary>
    /// CrSel key.
    /// </summary>
    [VirtualKey("CRSEL")]
    CRSEL = 247,

    /// <summary>
    /// ExSel key.
    /// </summary>
    [VirtualKey("EXSEL")]
    EXSEL = 248,

    /// <summary>
    /// Erase EOF key.
    /// </summary>
    [VirtualKey("Erase EOF")]
    EREOF = 249,

    /// <summary>
    /// Play key.
    /// </summary>
    [VirtualKey("Play")]
    PLAY = 250,

    /// <summary>
    /// Zoom key.
    /// </summary>
    [VirtualKey("Zoom")]
    ZOOM = 251,

    /// <summary>
    /// Reserved constant by Windows headers definition.
    /// </summary>
    [VirtualKey("Reserved")]
    NONAME = 252,

    /// <summary>
    /// PA1 key.
    /// </summary>
    [VirtualKey("PA1")]
    PA1 = 253,

    /// <summary>
    /// Clear key.
    /// </summary>
    [VirtualKey("Clear")]
    OEM_CLEAR = 254,
}
