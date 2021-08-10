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
        NO_KEY = 0,

        /// <summary>
        /// Left mouse button.
        /// </summary>
        LBUTTON = 1,

        /// <summary>
        /// Right mouse button.
        /// </summary>
        RBUTTON = 2,

        /// <summary>
        /// Control-break processing.
        /// </summary>
        CANCEL = 3,

        /// <summary>
        /// Middle mouse button (three-button mouse).
        /// </summary>
        /// <remarks>
        /// NOT contiguous with L and R buttons.
        /// </remarks>
        MBUTTON = 4,

        /// <summary>
        /// X1 mouse button.
        /// </summary>
        /// <remarks>
        /// NOT contiguous with L and R buttons.
        /// </remarks>
        XBUTTON1 = 5,

        /// <summary>
        /// X2 mouse button.
        /// </summary>
        /// <remarks>
        /// NOT contiguous with L and R buttons.
        /// </remarks>
        XBUTTON2 = 6,

        /// <summary>
        /// BACKSPACE key.
        /// </summary>
        BACK = 8,

        /// <summary>
        /// TAB key.
        /// </summary>
        TAB = 9,

        /// <summary>
        /// CLEAR key.
        /// </summary>
        CLEAR = 12,

        /// <summary>
        /// RETURN key.
        /// </summary>
        RETURN = 13,

        /// <summary>
        /// SHIFT key.
        /// </summary>
        SHIFT = 16,

        /// <summary>
        /// CONTROL key.
        /// </summary>
        CONTROL = 17,

        /// <summary>
        /// ALT key.
        /// </summary>
        MENU = 18,

        /// <summary>
        /// PAUSE key.
        /// </summary>
        PAUSE = 19,

        /// <summary>
        /// CAPS LOCK key.
        /// </summary>
        CAPITAL = 20,

        /// <summary>
        /// IME Kana mode.
        /// </summary>
        KANA = 21,

        /// <summary>
        /// IME Hanguel mode (maintained for compatibility; use User32.VirtualKey.HANGUL).
        /// </summary>
        HANGEUL = KANA,

        /// <summary>
        /// IME Hangul mode.
        /// </summary>
        HANGUL = KANA,

        /// <summary>
        /// IME Junja mode.
        /// </summary>
        JUNJA = 23,

        /// <summary>
        /// IME final mode.
        /// </summary>
        FINAL = 24,

        /// <summary>
        /// IME Hanja mode.
        /// </summary>
        HANJA = 25,

        /// <summary>
        /// IME Kanji mode.
        /// </summary>
        KANJI = HANJA,

        /// <summary>
        /// ESC key.
        /// </summary>
        ESCAPE = 27,

        /// <summary>
        /// IME convert.
        /// </summary>
        CONVERT = 28,

        /// <summary>
        /// IME nonconvert.
        /// </summary>
        NONCONVERT = 29,

        /// <summary>
        /// IME accept.
        /// </summary>
        ACCEPT = 30,

        /// <summary>
        /// IME mode change request.
        /// </summary>
        MODECHANGE = 31,

        /// <summary>
        /// SPACEBAR.
        /// </summary>
        SPACE = 32,

        /// <summary>
        /// PAGE UP key.
        /// </summary>
        PRIOR = 33,

        /// <summary>
        /// PAGE DOWN key.
        /// </summary>
        NEXT = 34,

        /// <summary>
        /// END key.
        /// </summary>
        END = 35,

        /// <summary>
        /// HOME key.
        /// </summary>
        HOME = 36,

        /// <summary>
        /// LEFT ARROW key.
        /// </summary>
        LEFT = 37,

        /// <summary>
        /// UP ARROW key.
        /// </summary>
        UP = 38,

        /// <summary>
        /// RIGHT ARROW key.
        /// </summary>
        RIGHT = 39,

        /// <summary>
        /// DOWN ARROW key.
        /// </summary>
        DOWN = 40,

        /// <summary>
        /// SELECT key.
        /// </summary>
        SELECT = 41,

        /// <summary>
        /// PRINT key.
        /// </summary>
        PRINT = 42,

        /// <summary>
        /// EXECUTE key.
        /// </summary>
        EXECUTE = 43,

        /// <summary>
        /// PRINT SCREEN key.
        /// </summary>
        SNAPSHOT = 44,

        /// <summary>
        /// INS key.
        /// </summary>
        INSERT = 45,

        /// <summary>
        /// DEL key.
        /// </summary>
        DELETE = 46,

        /// <summary>
        /// HELP key.
        /// </summary>
        HELP = 47,

        /// <summary>
        /// 0 key.
        /// </summary>
        KEY_0 = 48,

        /// <summary>
        /// 1 key.
        /// </summary>
        KEY_1 = 49,

        /// <summary>
        /// 2 key.
        /// </summary>
        KEY_2 = 50,

        /// <summary>
        /// 3 key.
        /// </summary>
        KEY_3 = 51,

        /// <summary>
        /// 4 key.
        /// </summary>
        KEY_4 = 52,

        /// <summary>
        /// 5 key.
        /// </summary>
        KEY_5 = 53,

        /// <summary>
        /// 6 key.
        /// </summary>
        KEY_6 = 54,

        /// <summary>
        /// 7 key.
        /// </summary>
        KEY_7 = 55,

        /// <summary>
        /// 8 key.
        /// </summary>
        KEY_8 = 56,

        /// <summary>
        /// 9 key.
        /// </summary>
        KEY_9 = 57,

        /// <summary>
        /// A key.
        /// </summary>
        A = 65,

        /// <summary>
        /// B key.
        /// </summary>
        B = 66,

        /// <summary>
        /// C key.
        /// </summary>
        C = 67,

        /// <summary>
        /// D key.
        /// </summary>
        D = 68,

        /// <summary>
        /// E key.
        /// </summary>
        E = 69,

        /// <summary>
        /// F key.
        /// </summary>
        F = 70,

        /// <summary>
        /// G key.
        /// </summary>
        G = 71,

        /// <summary>
        /// H key.
        /// </summary>
        H = 72,

        /// <summary>
        /// I key.
        /// </summary>
        I = 73,

        /// <summary>
        /// J key.
        /// </summary>
        J = 74,

        /// <summary>
        /// K key.
        /// </summary>
        K = 75,

        /// <summary>
        /// L key.
        /// </summary>
        L = 76,

        /// <summary>
        /// M key.
        /// </summary>
        M = 77,

        /// <summary>
        /// N key.
        /// </summary>
        N = 78,

        /// <summary>
        /// O key.
        /// </summary>
        O = 79,

        /// <summary>
        /// P key.
        /// </summary>
        P = 80,

        /// <summary>
        /// Q key.
        /// </summary>
        Q = 81,

        /// <summary>
        /// R key.
        /// </summary>
        R = 82,

        /// <summary>
        /// S key.
        /// </summary>
        S = 83,

        /// <summary>
        /// T key.
        /// </summary>
        T = 84,

        /// <summary>
        /// U key.
        /// </summary>
        U = 85,

        /// <summary>
        /// V key.
        /// </summary>
        V = 86,

        /// <summary>
        /// W key.
        /// </summary>
        W = 87,

        /// <summary>
        /// X key.
        /// </summary>
        X = 88,

        /// <summary>
        /// Y key.
        /// </summary>
        Y = 89,

        /// <summary>
        /// Z key.
        /// </summary>
        Z = 90,

        /// <summary>
        /// Left Windows key (Natural keyboard).
        /// </summary>
        LWIN = 91,

        /// <summary>
        /// Right Windows key (Natural keyboard).
        /// </summary>
        RWIN = 92,

        /// <summary>
        /// Applications key (Natural keyboard).
        /// </summary>
        APPS = 93,

        /// <summary>
        /// Computer Sleep key.
        /// </summary>
        SLEEP = 95,

        /// <summary>
        /// Numeric keypad 0 key.
        /// </summary>
        NUMPAD0 = 96,

        /// <summary>
        /// Numeric keypad 1 key.
        /// </summary>
        NUMPAD1 = 97,

        /// <summary>
        /// Numeric keypad 2 key.
        /// </summary>
        NUMPAD2 = 98,

        /// <summary>
        /// Numeric keypad 3 key.
        /// </summary>
        NUMPAD3 = 99,

        /// <summary>
        /// Numeric keypad 4 key.
        /// </summary>
        NUMPAD4 = 100,

        /// <summary>
        /// Numeric keypad 5 key.
        /// </summary>
        NUMPAD5 = 101,

        /// <summary>
        /// Numeric keypad 6 key.
        /// </summary>
        NUMPAD6 = 102,

        /// <summary>
        /// Numeric keypad 7 key.
        /// </summary>
        NUMPAD7 = 103,

        /// <summary>
        /// Numeric keypad 8 key.
        /// </summary>
        NUMPAD8 = 104,

        /// <summary>
        /// Numeric keypad 9 key.
        /// </summary>
        NUMPAD9 = 105,

        /// <summary>
        /// Multiply key.
        /// </summary>
        MULTIPLY = 106,

        /// <summary>
        /// Add key.
        /// </summary>
        ADD = 107,

        /// <summary>
        /// Separator key.
        /// </summary>
        SEPARATOR = 108,

        /// <summary>
        /// Subtract key.
        /// </summary>
        SUBTRACT = 109,

        /// <summary>
        /// Decimal key.
        /// </summary>
        DECIMAL = 110,

        /// <summary>
        /// Divide key.
        /// </summary>
        DIVIDE = 111,

        /// <summary>
        /// F1 Key.
        /// </summary>
        F1 = 112,

        /// <summary>
        /// F2 Key.
        /// </summary>
        F2 = 113,

        /// <summary>
        /// F3 Key.
        /// </summary>
        F3 = 114,

        /// <summary>
        /// F4 Key.
        /// </summary>
        F4 = 115,

        /// <summary>
        /// F5 Key.
        /// </summary>
        F5 = 116,

        /// <summary>
        /// F6 Key.
        /// </summary>
        F6 = 117,

        /// <summary>
        /// F7 Key.
        /// </summary>
        F7 = 118,

        /// <summary>
        /// F8 Key.
        /// </summary>
        F8 = 119,

        /// <summary>
        /// F9 Key.
        /// </summary>
        F9 = 120,

        /// <summary>
        /// F10 Key.
        /// </summary>
        F10 = 121,

        /// <summary>
        /// F11 Key.
        /// </summary>
        F11 = 122,

        /// <summary>
        /// F12 Key.
        /// </summary>
        F12 = 123,

        /// <summary>
        /// F13 Key.
        /// </summary>
        F13 = 124,

        /// <summary>
        /// F14 Key.
        /// </summary>
        F14 = 125,

        /// <summary>
        /// F15 Key.
        /// </summary>
        F15 = 126,

        /// <summary>
        /// F16 Key.
        /// </summary>
        F16 = 127,

        /// <summary>
        /// F17 Key.
        /// </summary>
        F17 = 128,

        /// <summary>
        /// F18 Key.
        /// </summary>
        F18 = 129,

        /// <summary>
        /// F19 Key.
        /// </summary>
        F19 = 130,

        /// <summary>
        /// F20 Key.
        /// </summary>
        F20 = 131,

        /// <summary>
        /// F21 Key.
        /// </summary>
        F21 = 132,

        /// <summary>
        /// F22 Key.
        /// </summary>
        F22 = 133,

        /// <summary>
        /// F23 Key.
        /// </summary>
        F23 = 134,

        /// <summary>
        /// F24 Key.
        /// </summary>
        F24 = 135,

        /// <summary>
        /// NUM LOCK key.
        /// </summary>
        NUMLOCK = 144,

        /// <summary>
        /// SCROLL LOCK key.
        /// </summary>
        SCROLL = 145,

        /// <summary>
        /// '=' key on numpad (NEC PC-9800 kbd definitions).
        /// </summary>
        OEM_NEC_EQUAL = 146,

        /// <summary>
        /// 'Dictionary' key (Fujitsu/OASYS kbd definitions).
        /// </summary>
        OEM_FJ_JISHO = OEM_NEC_EQUAL,

        /// <summary>
        /// 'Unregister word' key (Fujitsu/OASYS kbd definitions).
        /// </summary>
        OEM_FJ_MASSHOU = 147,

        /// <summary>
        /// 'Register word' key (Fujitsu/OASYS kbd definitions).
        /// </summary>
        OEM_FJ_TOUROKU = 148,

        /// <summary>
        /// 'Left OYAYUBI' key (Fujitsu/OASYS kbd definitions).
        /// </summary>
        OEM_FJ_LOYA = 149,

        /// <summary>
        /// 'Right OYAYUBI' key (Fujitsu/OASYS kbd definitions).
        /// </summary>
        OEM_FJ_ROYA = 150,

        /// <summary>
        /// Left SHIFT key.
        /// </summary>
        /// <remarks>
        /// Used only as parameters to User32.GetAsyncKeyState and User32.GetKeyState. No other API or message will distinguish left and right keys in this way.
        /// </remarks>
        LSHIFT = 160,

        /// <summary>
        /// Right SHIFT key.
        /// </summary>
        RSHIFT = 161,

        /// <summary>
        /// Left CONTROL key.
        /// </summary>
        LCONTROL = 162,

        /// <summary>
        /// Right CONTROL key.
        /// </summary>
        RCONTROL = 163,

        /// <summary>
        /// Left MENU key.
        /// </summary>
        LMENU = 164,

        /// <summary>
        /// Right MENU key.
        /// </summary>
        RMENU = 165,

        /// <summary>
        /// Browser Back key.
        /// </summary>
        BROWSER_BACK = 166,

        /// <summary>
        /// Browser Forward key.
        /// </summary>
        BROWSER_FORWARD = 167,

        /// <summary>
        /// Browser Refresh key.
        /// </summary>
        BROWSER_REFRESH = 168,

        /// <summary>
        /// Browser Stop key.
        /// </summary>
        BROWSER_STOP = 169,

        /// <summary>
        /// Browser Search key.
        /// </summary>
        BROWSER_SEARCH = 170,

        /// <summary>
        /// Browser Favorites key.
        /// </summary>
        BROWSER_FAVORITES = 171,

        /// <summary>
        /// Browser Start and Home key.
        /// </summary>
        BROWSER_HOME = 172,

        /// <summary>
        /// Volume Mute key.
        /// </summary>
        VOLUME_MUTE = 173,

        /// <summary>
        /// Volume Down key.
        /// </summary>
        VOLUME_DOWN = 174,

        /// <summary>
        /// Volume Up key.
        /// </summary>
        VOLUME_UP = 175,

        /// <summary>
        /// Next Track key.
        /// </summary>
        MEDIA_NEXT_TRACK = 176,

        /// <summary>
        /// Previous Track key.
        /// </summary>
        MEDIA_PREV_TRACK = 177,

        /// <summary>
        /// Stop Media key.
        /// </summary>
        MEDIA_STOP = 178,

        /// <summary>
        /// Play/Pause Media key.
        /// </summary>
        MEDIA_PLAY_PAUSE = 179,

        /// <summary>
        /// Start Mail key.
        /// </summary>
        LAUNCH_MAIL = 180,

        /// <summary>
        /// Select Media key.
        /// </summary>
        LAUNCH_MEDIA_SELECT = 181,

        /// <summary>
        /// Start Application 1 key.
        /// </summary>
        LAUNCH_APP1 = 182,

        /// <summary>
        /// Start Application 2 key.
        /// </summary>
        LAUNCH_APP2 = 183,

        /// <summary>
        /// Used for miscellaneous characters; it can vary by keyboard..
        /// </summary>
        /// <remarks>
        /// For the US standard keyboard, the ';:' key.
        /// </remarks>
        OEM_1 = 186,

        /// <summary>
        /// For any country/region, the '+' key.
        /// </summary>
        OEM_PLUS = 187,

        /// <summary>
        /// For any country/region, the ',' key.
        /// </summary>
        OEM_COMMA = 188,

        /// <summary>
        /// For any country/region, the '-' key.
        /// </summary>
        OEM_MINUS = 189,

        /// <summary>
        /// For any country/region, the '.' key.
        /// </summary>
        OEM_PERIOD = 190,

        /// <summary>
        /// Used for miscellaneous characters; it can vary by keyboard..
        /// </summary>
        /// <remarks>
        /// For the US standard keyboard, the '/?' key.
        /// </remarks>
        OEM_2 = 191,

        /// <summary>
        /// Used for miscellaneous characters; it can vary by keyboard..
        /// </summary>
        /// <remarks>
        /// For the US standard keyboard, the '`~' key.
        /// </remarks>
        OEM_3 = 192,

        /// <summary>
        /// Used for miscellaneous characters; it can vary by keyboard..
        /// </summary>
        /// <remarks>
        /// For the US standard keyboard, the '[{' key.
        /// </remarks>
        OEM_4 = 219,

        /// <summary>
        /// Used for miscellaneous characters; it can vary by keyboard..
        /// </summary>
        /// <remarks>
        /// For the US standard keyboard, the '\|' key.
        /// </remarks>
        OEM_5 = 220,

        /// <summary>
        /// Used for miscellaneous characters; it can vary by keyboard..
        /// </summary>
        /// <remarks>
        /// For the US standard keyboard, the ']}' key.
        /// </remarks>
        OEM_6 = 221,

        /// <summary>
        /// Used for miscellaneous characters; it can vary by keyboard..
        /// </summary>
        /// <remarks>
        /// For the US standard keyboard, the 'single-quote/double-quote' (''"') key.
        /// </remarks>
        OEM_7 = 222,

        /// <summary>
        /// Used for miscellaneous characters; it can vary by keyboard..
        /// </summary>
        OEM_8 = 223,

        /// <summary>
        /// OEM specific.
        /// </summary>
        /// <remarks>
        /// 'AX' key on Japanese AX kbd.
        /// </remarks>
        OEM_AX = 225,

        /// <summary>
        /// Either the angle bracket ("&lt;&gt;") key or the backslash ("\|") key on the RT 102-key keyboard.
        /// </summary>
        OEM_102 = 226,

        /// <summary>
        /// OEM specific.
        /// </summary>
        /// <remarks>
        /// Help key on ICO.
        /// </remarks>
        ICO_HELP = 227,

        /// <summary>
        /// OEM specific.
        /// </summary>
        /// <remarks>
        /// 00 key on ICO.
        /// </remarks>
        ICO_00 = 228,

        /// <summary>
        /// IME PROCESS key.
        /// </summary>
        PROCESSKEY = 229,

        /// <summary>
        /// OEM specific.
        /// </summary>
        /// <remarks>
        /// Clear key on ICO.
        /// </remarks>
        ICO_CLEAR = 230,

        /// <summary>
        /// Used to pass Unicode characters as if they were keystrokes. The PACKET key is the low word of a 32-bit Virtual Key value used for non-keyboard input methods..
        /// </summary>
        /// <remarks>
        /// For more information, see Remark in User32.KEYBDINPUT, User32.SendInput, User32.WindowMessage.WM_KEYDOWN, and User32.WindowMessage.WM_KEYUP.
        /// </remarks>
        PACKET = 231,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        OEM_RESET = 233,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        OEM_JUMP = 234,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        OEM_PA1 = 235,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        OEM_PA2 = 236,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        OEM_PA3 = 237,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        OEM_WSCTRL = 238,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        OEM_CUSEL = 239,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        OEM_ATTN = 240,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        OEM_FINISH = 241,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        OEM_COPY = 242,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        OEM_AUTO = 243,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        OEM_ENLW = 244,

        /// <summary>
        /// Nokia/Ericsson definition.
        /// </summary>
        OEM_BACKTAB = 245,

        /// <summary>
        /// Attn key.
        /// </summary>
        ATTN = 246,

        /// <summary>
        /// CrSel key.
        /// </summary>
        CRSEL = 247,

        /// <summary>
        /// ExSel key.
        /// </summary>
        EXSEL = 248,

        /// <summary>
        /// Erase EOF key.
        /// </summary>
        EREOF = 249,

        /// <summary>
        /// Play key.
        /// </summary>
        PLAY = 250,

        /// <summary>
        /// Zoom key.
        /// </summary>
        ZOOM = 251,

        /// <summary>
        /// Reserved constant by Windows headers definition.
        /// </summary>
        NONAME = 252,

        /// <summary>
        /// PA1 key.
        /// </summary>
        PA1 = 253,

        /// <summary>
        /// Clear key.
        /// </summary>
        OEM_CLEAR = 254,
    }
}
