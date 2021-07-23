namespace Dalamud.Game.ClientState.Keys
{
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
        VK_NO_KEY = 0,

        /// <summary>
        /// Left mouse button.
        /// </summary>
        VK_LBUTTON = 1,

        /// <summary>
        /// Right mouse button.
        /// </summary>
        VK_RBUTTON = 2,

        /// <summary>
        /// Control-break processing.
        /// </summary>
        VK_CANCEL = 3,

        /// <summary>
        /// Middle mouse button (three-button mouse).
        /// </summary>
        /// <remarks>
        /// NOT contiguous with L and R buttons.
        /// </remarks>
        VK_MBUTTON = 4,

        /// <summary>
        /// X1 mouse button.
        /// </summary>
        /// <remarks>
        /// NOT contiguous with L and R buttons.
        /// </remarks>
        VK_XBUTTON1 = 5,

        /// <summary>
        /// X2 mouse button.
        /// </summary>
        /// <remarks>
        /// NOT contiguous with L and R buttons.
        /// </remarks>
        VK_XBUTTON2 = 6,

        /// <summary>
        /// BACKSPACE key.
        /// </summary>
        VK_BACK = 8,

        /// <summary>
        /// TAB key.
        /// </summary>
        VK_TAB = 9,

        /// <summary>
        /// CLEAR key.
        /// </summary>
        VK_CLEAR = 12,

        /// <summary>
        /// RETURN key.
        /// </summary>
        VK_RETURN = 13,

        /// <summary>
        /// SHIFT key.
        /// </summary>
        VK_SHIFT = 16,

        /// <summary>
        /// CONTROL key.
        /// </summary>
        VK_CONTROL = 17,

        /// <summary>
        /// ALT key.
        /// </summary>
        VK_MENU = 18,

        /// <summary>
        /// PAUSE key.
        /// </summary>
        VK_PAUSE = 19,

        /// <summary>
        /// CAPS LOCK key.
        /// </summary>
        VK_CAPITAL = 20,

        /// <summary>
        /// IME Kana mode.
        /// </summary>
        VK_KANA = 21,

        /// <summary>
        /// IME Hanguel mode (maintained for compatibility; use User32.VirtualKey.VK_HANGUL).
        /// </summary>
        VK_HANGEUL = VK_KANA,

        /// <summary>
        /// IME Hangul mode.
        /// </summary>
        VK_HANGUL = VK_KANA,

        /// <summary>
        /// IME Junja mode.
        /// </summary>
        VK_JUNJA = 23,

        /// <summary>
        /// IME final mode.
        /// </summary>
        VK_FINAL = 24,

        /// <summary>
        /// IME Hanja mode.
        /// </summary>
        VK_HANJA = 25,

        /// <summary>
        /// IME Kanji mode.
        /// </summary>
        VK_KANJI = VK_HANJA,

        /// <summary>
        /// ESC key.
        /// </summary>
        VK_ESCAPE = 27,

        /// <summary>
        /// IME convert.
        /// </summary>
        VK_CONVERT = 28,

        /// <summary>
        /// IME nonconvert.
        /// </summary>
        VK_NONCONVERT = 29,

        /// <summary>
        /// IME accept.
        /// </summary>
        VK_ACCEPT = 30,

        /// <summary>
        /// IME mode change request.
        /// </summary>
        VK_MODECHANGE = 31,

        /// <summary>
        /// SPACEBAR.
        /// </summary>
        VK_SPACE = 32,

        /// <summary>
        /// PAGE UP key.
        /// </summary>
        VK_PRIOR = 33,

        /// <summary>
        /// PAGE DOWN key.
        /// </summary>
        VK_NEXT = 34,

        /// <summary>
        /// END key.
        /// </summary>
        VK_END = 35,

        /// <summary>
        /// HOME key.
        /// </summary>
        VK_HOME = 36,

        /// <summary>
        /// LEFT ARROW key.
        /// </summary>
        VK_LEFT = 37,

        /// <summary>
        /// UP ARROW key.
        /// </summary>
        VK_UP = 38,

        /// <summary>
        /// RIGHT ARROW key.
        /// </summary>
        VK_RIGHT = 39,

        /// <summary>
        /// DOWN ARROW key.
        /// </summary>
        VK_DOWN = 40,

        /// <summary>
        /// SELECT key.
        /// </summary>
        VK_SELECT = 41,

        /// <summary>
        /// PRINT key.
        /// </summary>
        VK_PRINT = 42,

        /// <summary>
        /// EXECUTE key.
        /// </summary>
        VK_EXECUTE = 43,

        /// <summary>
        /// PRINT SCREEN key.
        /// </summary>
        VK_SNAPSHOT = 44,

        /// <summary>
        /// INS key.
        /// </summary>
        VK_INSERT = 45,

        /// <summary>
        /// DEL key.
        /// </summary>
        VK_DELETE = 46,

        /// <summary>
        /// HELP key.
        /// </summary>
        VK_HELP = 47,

        /// <summary>
        /// 0 key.
        /// </summary>
        VK_KEY_0 = 48,

        /// <summary>
        /// 1 key.
        /// </summary>
        VK_KEY_1 = 49,

        /// <summary>
        /// 2 key.
        /// </summary>
        VK_KEY_2 = 50,

        /// <summary>
        /// 3 key.
        /// </summary>
        VK_KEY_3 = 51,

        /// <summary>
        /// 4 key.
        /// </summary>
        VK_KEY_4 = 52,

        /// <summary>
        /// 5 key.
        /// </summary>
        VK_KEY_5 = 53,

        /// <summary>
        /// 6 key.
        /// </summary>
        VK_KEY_6 = 54,

        /// <summary>
        /// 7 key.
        /// </summary>
        VK_KEY_7 = 55,

        /// <summary>
        /// 8 key.
        /// </summary>
        VK_KEY_8 = 56,

        /// <summary>
        /// 9 key.
        /// </summary>
        VK_KEY_9 = 57,

        /// <summary>
        /// A key.
        /// </summary>
        VK_A = 65,

        /// <summary>
        /// B key.
        /// </summary>
        VK_B = 66,

        /// <summary>
        /// C key.
        /// </summary>
        VK_C = 67,

        /// <summary>
        /// D key.
        /// </summary>
        VK_D = 68,

        /// <summary>
        /// E key.
        /// </summary>
        VK_E = 69,

        /// <summary>
        /// F key.
        /// </summary>
        VK_F = 70,

        /// <summary>
        /// G key.
        /// </summary>
        VK_G = 71,

        /// <summary>
        /// H key.
        /// </summary>
        VK_H = 72,

        /// <summary>
        /// I key.
        /// </summary>
        VK_I = 73,

        /// <summary>
        /// J key.
        /// </summary>
        VK_J = 74,

        /// <summary>
        /// K key.
        /// </summary>
        VK_K = 75,

        /// <summary>
        /// L key.
        /// </summary>
        VK_L = 76,

        /// <summary>
        /// M key.
        /// </summary>
        VK_M = 77,

        /// <summary>
        /// N key.
        /// </summary>
        VK_N = 78,

        /// <summary>
        /// O key.
        /// </summary>
        VK_O = 79,

        /// <summary>
        /// P key.
        /// </summary>
        VK_P = 80,

        /// <summary>
        /// Q key.
        /// </summary>
        VK_Q = 81,

        /// <summary>
        /// R key.
        /// </summary>
        VK_R = 82,

        /// <summary>
        /// S key.
        /// </summary>
        VK_S = 83,

        /// <summary>
        /// T key.
        /// </summary>
        VK_T = 84,

        /// <summary>
        /// U key.
        /// </summary>
        VK_U = 85,

        /// <summary>
        /// V key.
        /// </summary>
        VK_V = 86,

        /// <summary>
        /// W key.
        /// </summary>
        VK_W = 87,

        /// <summary>
        /// X key.
        /// </summary>
        VK_X = 88,

        /// <summary>
        /// Y key.
        /// </summary>
        VK_Y = 89,

        /// <summary>
        /// Z key.
        /// </summary>
        VK_Z = 90,

        /// <summary>
        /// Left Windows key (Natural keyboard).
        /// </summary>
        VK_LWIN = 91,

        /// <summary>
        /// Right Windows key (Natural keyboard).
        /// </summary>
        VK_RWIN = 92,

        /// <summary>
        /// Applications key (Natural keyboard).
        /// </summary>
        VK_APPS = 93,

        /// <summary>
        /// Computer Sleep key.
        /// </summary>
        VK_SLEEP = 95,

        /// <summary>
        /// Numeric keypad 0 key.
        /// </summary>
        VK_NUMPAD0 = 96,

        /// <summary>
        /// Numeric keypad 1 key.
        /// </summary>
        VK_NUMPAD1 = 97,

        /// <summary>
        /// Numeric keypad 2 key.
        /// </summary>
        VK_NUMPAD2 = 98,

        /// <summary>
        /// Numeric keypad 3 key.
        /// </summary>
        VK_NUMPAD3 = 99,

        /// <summary>
        /// Numeric keypad 4 key.
        /// </summary>
        VK_NUMPAD4 = 100,

        /// <summary>
        /// Numeric keypad 5 key.
        /// </summary>
        VK_NUMPAD5 = 101,

        /// <summary>
        /// Numeric keypad 6 key.
        /// </summary>
        VK_NUMPAD6 = 102,

        /// <summary>
        /// Numeric keypad 7 key.
        /// </summary>
        VK_NUMPAD7 = 103,

        /// <summary>
        /// Numeric keypad 8 key.
        /// </summary>
        VK_NUMPAD8 = 104,

        /// <summary>
        /// Numeric keypad 9 key.
        /// </summary>
        VK_NUMPAD9 = 105,

        /// <summary>
        /// Multiply key.
        /// </summary>
        VK_MULTIPLY = 106,

        /// <summary>
        /// Add key.
        /// </summary>
        VK_ADD = 107,

        /// <summary>
        /// Separator key.
        /// </summary>
        VK_SEPARATOR = 108,

        /// <summary>
        /// Subtract key.
        /// </summary>
        VK_SUBTRACT = 109,

        /// <summary>
        /// Decimal key.
        /// </summary>
        VK_DECIMAL = 110,

        /// <summary>
        /// Divide key.
        /// </summary>
        VK_DIVIDE = 111,

        /// <summary>
        /// F1 Key.
        /// </summary>
        VK_F1 = 112,

        /// <summary>
        /// F2 Key.
        /// </summary>
        VK_F2 = 113,

        /// <summary>
        /// F3 Key.
        /// </summary>
        VK_F3 = 114,

        /// <summary>
        /// F4 Key.
        /// </summary>
        VK_F4 = 115,

        /// <summary>
        /// F5 Key.
        /// </summary>
        VK_F5 = 116,

        /// <summary>
        /// F6 Key.
        /// </summary>
        VK_F6 = 117,

        /// <summary>
        /// F7 Key.
        /// </summary>
        VK_F7 = 118,

        /// <summary>
        /// F8 Key.
        /// </summary>
        VK_F8 = 119,

        /// <summary>
        /// F9 Key.
        /// </summary>
        VK_F9 = 120,

        /// <summary>
        /// F10 Key.
        /// </summary>
        VK_F10 = 121,

        /// <summary>
        /// F11 Key.
        /// </summary>
        VK_F11 = 122,

        /// <summary>
        /// F12 Key.
        /// </summary>
        VK_F12 = 123,

        /// <summary>
        /// F13 Key.
        /// </summary>
        VK_F13 = 124,

        /// <summary>
        /// F14 Key.
        /// </summary>
        VK_F14 = 125,

        /// <summary>
        /// F15 Key.
        /// </summary>
        VK_F15 = 126,

        /// <summary>
        /// F16 Key.
        /// </summary>
        VK_F16 = 127,

        /// <summary>
        /// F17 Key.
        /// </summary>
        VK_F17 = 128,

        /// <summary>
        /// F18 Key.
        /// </summary>
        VK_F18 = 129,

        /// <summary>
        /// F19 Key.
        /// </summary>
        VK_F19 = 130,

        /// <summary>
        /// F20 Key.
        /// </summary>
        VK_F20 = 131,

        /// <summary>
        /// F21 Key.
        /// </summary>
        VK_F21 = 132,

        /// <summary>
        /// F22 Key.
        /// </summary>
        VK_F22 = 133,

        /// <summary>
        /// F23 Key.
        /// </summary>
        VK_F23 = 134,

        /// <summary>
        /// F24 Key.
        /// </summary>
        VK_F24 = 135,

        /// <summary>
        /// NUM LOCK key.
        /// </summary>
        VK_NUMLOCK = 144,

        /// <summary>
        /// SCROLL LOCK key.
        /// </summary>
        VK_SCROLL = 145,

        /// <summary>
        /// '=' key on numpad (NEC PC-9800 kbd definitions).
        /// </summary>
        VK_OEM_NEC_EQUAL = 146,

        /// <summary>
        /// 'Dictionary' key (Fujitsu/OASYS kbd definitions).
        /// </summary>
        VK_OEM_FJ_JISHO = VK_OEM_NEC_EQUAL,

        /// <summary>
        /// 'Unregister word' key (Fujitsu/OASYS kbd definitions).
        /// </summary>
        VK_OEM_FJ_MASSHOU = 147,

        /// <summary>
        /// 'Register word' key (Fujitsu/OASYS kbd definitions).
        /// </summary>
        VK_OEM_FJ_TOUROKU = 148,

        /// <summary>
        /// 'Left OYAYUBI' key (Fujitsu/OASYS kbd definitions).
        /// </summary>
        VK_OEM_FJ_LOYA = 149,

        /// <summary>
        /// 'Right OYAYUBI' key (Fujitsu/OASYS kbd definitions).
        /// </summary>
        VK_OEM_FJ_ROYA = 150,

        /// <summary>
        /// Left SHIFT key.
        /// </summary>
        /// <remarks>
        /// Used only as parameters to User32.GetAsyncKeyState and User32.GetKeyState. No other API or message will distinguish left and right keys in this way.
        /// </remarks>
        VK_LSHIFT = 160,

        /// <summary>
        /// Right SHIFT key.
        /// </summary>
        VK_RSHIFT = 161,

        /// <summary>
        /// Left CONTROL key.
        /// </summary>
        VK_LCONTROL = 162,

        /// <summary>
        /// Right CONTROL key.
        /// </summary>
        VK_RCONTROL = 163,

        /// <summary>
        /// Left MENU key.
        /// </summary>
        VK_LMENU = 164,

        /// <summary>
        /// Right MENU key.
        /// </summary>
        VK_RMENU = 165,

        /// <summary>
        /// Browser Back key.
        /// </summary>
        VK_BROWSER_BACK = 166,

        /// <summary>
        /// Browser Forward key.
        /// </summary>
        VK_BROWSER_FORWARD = 167,

        /// <summary>
        /// Browser Refresh key.
        /// </summary>
        VK_BROWSER_REFRESH = 168,

        /// <summary>
        /// Browser Stop key.
        /// </summary>
        VK_BROWSER_STOP = 169,

        /// <summary>
        /// Browser Search key.
        /// </summary>
        VK_BROWSER_SEARCH = 170,

        /// <summary>
        /// Browser Favorites key.
        /// </summary>
        VK_BROWSER_FAVORITES = 171,

        /// <summary>
        /// Browser Start and Home key.
        /// </summary>
        VK_BROWSER_HOME = 172,

        /// <summary>
        /// Volume Mute key.
        /// </summary>
        VK_VOLUME_MUTE = 173,

        /// <summary>
        /// Volume Down key.
        /// </summary>
        VK_VOLUME_DOWN = 174,

        /// <summary>
        /// Volume Up key.
        /// </summary>
        VK_VOLUME_UP = 175,

        /// <summary>
        /// Next Track key.
        /// </summary>
        VK_MEDIA_NEXT_TRACK = 176,

        /// <summary>
        /// Previous Track key.
        /// </summary>
        VK_MEDIA_PREV_TRACK = 177,

        /// <summary>
        /// Stop Media key.
        /// </summary>
        VK_MEDIA_STOP = 178,

        /// <summary>
        /// Play/Pause Media key.
        /// </summary>
        VK_MEDIA_PLAY_PAUSE = 179,

        /// <summary>
        /// Start Mail key.
        /// </summary>
        VK_LAUNCH_MAIL = 180,

        /// <summary>
        /// Select Media key.
        /// </summary>
        VK_LAUNCH_MEDIA_SELECT = 181,

        /// <summary>
        /// Start Application 1 key.
        /// </summary>
        VK_LAUNCH_APP1 = 182,

        /// <summary>
        /// Start Application 2 key.
        /// </summary>
        VK_LAUNCH_APP2 = 183,

        /// <summary>
        /// Used for miscellaneous characters; it can vary by keyboard..
        /// </summary>
        /// <remarks>
        /// For the US standard keyboard, the ';:' key.
        /// </remarks>
        VK_OEM_1 = 186,

        /// <summary>
        /// For any country/region, the '+' key.
        /// </summary>
        VK_OEM_PLUS = 187,

        /// <summary>
        /// For any country/region, the ',' key.
        /// </summary>
        VK_OEM_COMMA = 188,

        /// <summary>
        /// For any country/region, the '-' key.
        /// </summary>
        VK_OEM_MINUS = 189,

        /// <summary>
        /// For any country/region, the '.' key.
        /// </summary>
        VK_OEM_PERIOD = 190,

        /// <summary>
        /// Used for miscellaneous characters; it can vary by keyboard..
        /// </summary>
        /// <remarks>
        /// For the US standard keyboard, the '/?' key.
        /// </remarks>
        VK_OEM_2 = 191,

        /// <summary>
        /// Used for miscellaneous characters; it can vary by keyboard..
        /// </summary>
        /// <remarks>
        /// For the US standard keyboard, the '`~' key.
        /// </remarks>
        VK_OEM_3 = 192,

        /// <summary>
        /// Used for miscellaneous characters; it can vary by keyboard..
        /// </summary>
        /// <remarks>
        /// For the US standard keyboard, the '[{' key.
        /// </remarks>
        VK_OEM_4 = 219,

        /// <summary>
        /// Used for miscellaneous characters; it can vary by keyboard..
        /// </summary>
        /// <remarks>
        /// For the US standard keyboard, the '\|' key.
        /// </remarks>
        VK_OEM_5 = 220,

        /// <summary>
        /// Used for miscellaneous characters; it can vary by keyboard..
        /// </summary>
        /// <remarks>
        /// For the US standard keyboard, the ']}' key.
        /// </remarks>
        VK_OEM_6 = 221,

        /// <summary>
        /// Used for miscellaneous characters; it can vary by keyboard..
        /// </summary>
        /// <remarks>
        /// For the US standard keyboard, the 'single-quote/double-quote' (''"') key.
        /// </remarks>
        VK_OEM_7 = 222,

        /// <summary>
        /// Used for miscellaneous characters; it can vary by keyboard..
        /// </summary>
        VK_OEM_8 = 223,

        /// <summary>
        /// OEM specific.
        /// </summary>
        /// <remarks>
        /// 'AX' key on Japanese AX kbd.
        /// </remarks>
        VK_OEM_AX = 225,

        /// <summary>
        /// Either the angle bracket ("&lt;&gt;") key or the backslash ("\|") key on the RT 102-key keyboard.
        /// </summary>
        VK_OEM_102 = 226,

        /// <summary>
        /// OEM specific.
        /// </summary>
        /// <remarks>
        /// Help key on ICO.
        /// </remarks>
        VK_ICO_HELP = 227,

        /// <summary>
        /// OEM specific.
        /// </summary>
        /// <remarks>
        /// 00 key on ICO.
        /// </remarks>
        VK_ICO_00 = 228,

        /// <summary>
        /// IME PROCESS key.
        /// </summary>
        VK_PROCESSKEY = 229,

        /// <summary>
        /// OEM specific.
        /// </summary>
        /// <remarks>
        /// Clear key on ICO.
        /// </remarks>
        VK_ICO_CLEAR = 230,

        /// <summary>
        /// Used to pass Unicode characters as if they were keystrokes. The VK_PACKET key is the low word of a 32-bit Virtual Key value used for non-keyboard input methods..
        /// </summary>
        /// <remarks>
        /// For more information, see Remark in User32.KEYBDINPUT, User32.SendInput, User32.WindowMessage.WM_KEYDOWN, and User32.WindowMessage.WM_KEYUP.
        /// </remarks>
        VK_PACKET = 231,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        VK_OEM_RESET = 233,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        VK_OEM_JUMP = 234,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        VK_OEM_PA1 = 235,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        VK_OEM_PA2 = 236,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        VK_OEM_PA3 = 237,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        VK_OEM_WSCTRL = 238,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        VK_OEM_CUSEL = 239,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        VK_OEM_ATTN = 240,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        VK_OEM_FINISH = 241,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        VK_OEM_COPY = 242,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        VK_OEM_AUTO = 243,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        VK_OEM_ENLW = 244,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        VK_OEM_BACKTAB = 245,

        /// <summary>
        /// Attn key.
        /// </summary>
        VK_ATTN = 246,

        /// <summary>
        /// CrSel key.
        /// </summary>
        VK_CRSEL = 247,

        /// <summary>
        /// ExSel key.
        /// </summary>
        VK_EXSEL = 248,

        /// <summary>
        /// Erase EOF key.
        /// </summary>
        VK_EREOF = 249,

        /// <summary>
        /// Play key.
        /// </summary>
        VK_PLAY = 250,

        /// <summary>
        /// Zoom key.
        /// </summary>
        VK_ZOOM = 251,

        /// <summary>
        /// Reserved constant by Windows headers definition.
        /// </summary>
        VK_NONAME = 252,

        /// <summary>
        /// PA1 key.
        /// </summary>
        VK_PA1 = 253,

        /// <summary>
        /// Clear key.
        /// </summary>
        VK_OEM_CLEAR = 254,
    }
}
