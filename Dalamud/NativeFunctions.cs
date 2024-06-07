using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud;

/// <summary>
/// Native user32 functions.
/// </summary>
internal static partial class NativeFunctions
{
    /// <summary>
    /// FLASHW_* from winuser.
    /// </summary>
    public enum FlashWindow : uint
    {
        /// <summary>
        /// Stop flashing. The system restores the window to its original state.
        /// </summary>
        Stop = 0,

        /// <summary>
        /// Flash the window caption.
        /// </summary>
        Caption = 1,

        /// <summary>
        /// Flash the taskbar button.
        /// </summary>
        Tray = 2,

        /// <summary>
        /// Flash both the window caption and taskbar button.
        /// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags.
        /// </summary>
        All = 3,

        /// <summary>
        /// Flash continuously, until the FLASHW_STOP flag is set.
        /// </summary>
        Timer = 4,

        /// <summary>
        /// Flash continuously until the window comes to the foreground.
        /// </summary>
        TimerNoFG = 12,
    }

    /// <summary>
    /// IDC_* from winuser.
    /// </summary>
    public enum CursorType
    {
        /// <summary>
        /// Standard arrow and small hourglass.
        /// </summary>
        AppStarting = 32650,

        /// <summary>
        /// Standard arrow.
        /// </summary>
        Arrow = 32512,

        /// <summary>
        /// Crosshair.
        /// </summary>
        Cross = 32515,

        /// <summary>
        /// Hand.
        /// </summary>
        Hand = 32649,

        /// <summary>
        /// Arrow and question mark.
        /// </summary>
        Help = 32651,

        /// <summary>
        /// I-beam.
        /// </summary>
        IBeam = 32513,

        /// <summary>
        /// Obsolete for applications marked version 4.0 or later.
        /// </summary>
        Icon = 32641,

        /// <summary>
        /// Slashed circle.
        /// </summary>
        No = 32648,

        /// <summary>
        /// Obsolete for applications marked version 4.0 or later.Use IDC_SIZEALL.
        /// </summary>
        Size = 32640,

        /// <summary>
        /// Four-pointed arrow pointing north, south, east, and west.
        /// </summary>
        SizeAll = 32646,

        /// <summary>
        /// Double-pointed arrow pointing northeast and southwest.
        /// </summary>
        SizeNeSw = 32643,

        /// <summary>
        /// Double-pointed arrow pointing north and south.
        /// </summary>
        SizeNS = 32645,

        /// <summary>
        /// Double-pointed arrow pointing northwest and southeast.
        /// </summary>
        SizeNwSe = 32642,

        /// <summary>
        /// Double-pointed arrow pointing west and east.
        /// </summary>
        SizeWE = 32644,

        /// <summary>
        /// Vertical arrow.
        /// </summary>
        UpArrow = 32516,

        /// <summary>
        /// Hourglass.
        /// </summary>
        Wait = 32514,
    }

    /// <summary>
    /// MB_* from winuser.
    /// </summary>
    [Flags]
    public enum MessageBoxType : uint
    {
        /// <summary>
        /// The default value for any of the various subtypes.
        /// </summary>
        DefaultValue = 0x0,

        // To indicate the buttons displayed in the message box, specify one of the following values.

        /// <summary>
        /// The message box contains three push buttons: Abort, Retry, and Ignore.
        /// </summary>
        AbortRetryIgnore = 0x2,

        /// <summary>
        /// The message box contains three push buttons: Cancel, Try Again, Continue. Use this message box type instead
        /// of MB_ABORTRETRYIGNORE.
        /// </summary>
        CancelTryContinue = 0x6,

        /// <summary>
        /// Adds a Help button to the message box. When the user clicks the Help button or presses F1, the system sends
        /// a WM_HELP message to the owner.
        /// </summary>
        Help = 0x4000,

        /// <summary>
        /// The message box contains one push button: OK. This is the default.
        /// </summary>
        Ok = DefaultValue,

        /// <summary>
        /// The message box contains two push buttons: OK and Cancel.
        /// </summary>
        OkCancel = 0x1,

        /// <summary>
        /// The message box contains two push buttons: Retry and Cancel.
        /// </summary>
        RetryCancel = 0x5,

        /// <summary>
        /// The message box contains two push buttons: Yes and No.
        /// </summary>
        YesNo = 0x4,

        /// <summary>
        /// The message box contains three push buttons: Yes, No, and Cancel.
        /// </summary>
        YesNoCancel = 0x3,

        // To display an icon in the message box, specify one of the following values.

        /// <summary>
        /// An exclamation-point icon appears in the message box.
        /// </summary>
        IconExclamation = 0x30,

        /// <summary>
        /// An exclamation-point icon appears in the message box.
        /// </summary>
        IconWarning = IconExclamation,

        /// <summary>
        /// An icon consisting of a lowercase letter i in a circle appears in the message box.
        /// </summary>
        IconInformation = 0x40,

        /// <summary>
        /// An icon consisting of a lowercase letter i in a circle appears in the message box.
        /// </summary>
        IconAsterisk = IconInformation,

        /// <summary>
        /// A question-mark icon appears in the message box.
        /// The question-mark message icon is no longer recommended because it does not clearly represent a specific type
        /// of message and because the phrasing of a message as a question could apply to any message type. In addition,
        /// users can confuse the message symbol question mark with Help information. Therefore, do not use this question
        /// mark message symbol in your message boxes. The system continues to support its inclusion only for backward
        /// compatibility.
        /// </summary>
        IconQuestion = 0x20,

        /// <summary>
        /// A stop-sign icon appears in the message box.
        /// </summary>
        IconStop = 0x10,

        /// <summary>
        /// A stop-sign icon appears in the message box.
        /// </summary>
        IconError = IconStop,

        /// <summary>
        /// A stop-sign icon appears in the message box.
        /// </summary>
        IconHand = IconStop,

        // To indicate the default button, specify one of the following values.

        /// <summary>
        /// The first button is the default button.
        /// MB_DEFBUTTON1 is the default unless MB_DEFBUTTON2, MB_DEFBUTTON3, or MB_DEFBUTTON4 is specified.
        /// </summary>
        DefButton1 = DefaultValue,

        /// <summary>
        /// The second button is the default button.
        /// </summary>
        DefButton2 = 0x100,

        /// <summary>
        /// The third button is the default button.
        /// </summary>
        DefButton3 = 0x200,

        /// <summary>
        /// The fourth button is the default button.
        /// </summary>
        DefButton4 = 0x300,

        // To indicate the modality of the dialog box, specify one of the following values.

        /// <summary>
        /// The user must respond to the message box before continuing work in the window identified by the hWnd parameter.
        /// However, the user can move to the windows of other threads and work in those windows. Depending on the hierarchy
        /// of windows in the application, the user may be able to move to other windows within the thread. All child windows
        /// of the parent of the message box are automatically disabled, but pop-up windows are not. MB_APPLMODAL is the
        /// default if neither MB_SYSTEMMODAL nor MB_TASKMODAL is specified.
        /// </summary>
        ApplModal = DefaultValue,

        /// <summary>
        /// Same as MB_APPLMODAL except that the message box has the WS_EX_TOPMOST style.
        /// Use system-modal message boxes to notify the user of serious, potentially damaging errors that require immediate
        /// attention (for example, running out of memory). This flag has no effect on the user's ability to interact with
        /// windows other than those associated with hWnd.
        /// </summary>
        SystemModal = 0x1000,

        /// <summary>
        /// Same as MB_APPLMODAL except that all the top-level windows belonging to the current thread are disabled if the
        /// hWnd parameter is NULL. Use this flag when the calling application or library does not have a window handle
        /// available but still needs to prevent input to other windows in the calling thread without suspending other threads.
        /// </summary>
        TaskModal = 0x2000,

        // To specify other options, use one or more of the following values.

        /// <summary>
        /// Same as desktop of the interactive window station. For more information, see Window Stations. If the current
        /// input desktop is not the default desktop, MessageBox does not return until the user switches to the default
        /// desktop.
        /// </summary>
        DefaultDesktopOnly = 0x20000,

        /// <summary>
        /// The text is right-justified.
        /// </summary>
        Right = 0x80000,

        /// <summary>
        /// Displays message and caption text using right-to-left reading order on Hebrew and Arabic systems.
        /// </summary>
        RtlReading = 0x100000,

        /// <summary>
        /// The message box becomes the foreground window. Internally, the system calls the SetForegroundWindow function
        /// for the message box.
        /// </summary>
        SetForeground = 0x10000,

        /// <summary>
        /// The message box is created with the WS_EX_TOPMOST window style.
        /// </summary>
        Topmost = 0x40000,

        /// <summary>
        /// The caller is a service notifying the user of an event. The function displays a message box on the current active
        /// desktop, even if there is no user logged on to the computer.
        /// </summary>
        ServiceNotification = 0x200000,
    }

    /// <summary>
    /// GWL_* from winuser.
    /// </summary>
    public enum WindowLongType
    {
        /// <summary>
        /// Sets a new extended window style.
        /// </summary>
        ExStyle = -20,

        /// <summary>
        /// Sets a new application instance handle.
        /// </summary>
        HInstance = -6,

        /// <summary>
        /// Sets a new identifier of the child window.The window cannot be a top-level window.
        /// </summary>
        Id = -12,

        /// <summary>
        /// Sets a new window style.
        /// </summary>
        Style = -16,

        /// <summary>
        /// Sets the user data associated with the window. This data is intended for use by the application that created the window. Its value is initially zero.
        /// </summary>
        UserData = -21,

        /// <summary>
        /// Sets a new address for the window procedure.
        /// </summary>
        WndProc = -4,

        // The following values are also available when the hWnd parameter identifies a dialog box.

        // /// <summary>
        // /// Sets the new pointer to the dialog box procedure.
        // /// </summary>
        // DWLP_DLGPROC = DWLP_MSGRESULT + sizeof(LRESULT),

        /// <summary>
        /// Sets the return value of a message processed in the dialog box procedure.
        /// </summary>
        MsgResult = 0,

        // /// <summary>
        // /// Sets new extra information that is private to the application, such as handles or pointers.
        // /// </summary>
        // DWLP_USER = DWLP_DLGPROC + sizeof(DLGPROC),
    }

    /// <summary>
    /// WM_* from winuser.
    /// These are spread throughout multiple files, find the documentation manually if you need it.
    /// https://gist.github.com/amgine/2395987.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = "No documentation available.")]
    public enum WindowsMessage
    {
        WM_NULL = 0x0000,
        WM_CREATE = 0x0001,
        WM_DESTROY = 0x0002,
        WM_MOVE = 0x0003,
        WM_SIZE = 0x0005,
        WM_ACTIVATE = 0x0006,
        WM_SETFOCUS = 0x0007,
        WM_KILLFOCUS = 0x0008,
        WM_ENABLE = 0x000A,
        WM_SETREDRAW = 0x000B,
        WM_SETTEXT = 0x000C,
        WM_GETTEXT = 0x000D,
        WM_GETTEXTLENGTH = 0x000E,
        WM_PAINT = 0x000F,
        WM_CLOSE = 0x0010,
        WM_QUERYENDSESSION = 0x0011,
        WM_QUERYOPEN = 0x0013,
        WM_ENDSESSION = 0x0016,
        WM_QUIT = 0x0012,
        WM_ERASEBKGND = 0x0014,
        WM_SYSCOLORCHANGE = 0x0015,
        WM_SHOWWINDOW = 0x0018,
        WM_WININICHANGE = 0x001A,
        WM_SETTINGCHANGE = WM_WININICHANGE,
        WM_DEVMODECHANGE = 0x001B,
        WM_ACTIVATEAPP = 0x001C,
        WM_FONTCHANGE = 0x001D,
        WM_TIMECHANGE = 0x001E,
        WM_CANCELMODE = 0x001F,
        WM_SETCURSOR = 0x0020,
        WM_MOUSEACTIVATE = 0x0021,
        WM_CHILDACTIVATE = 0x0022,
        WM_QUEUESYNC = 0x0023,
        WM_GETMINMAXINFO = 0x0024,
        WM_PAINTICON = 0x0026,
        WM_ICONERASEBKGND = 0x0027,
        WM_NEXTDLGCTL = 0x0028,
        WM_SPOOLERSTATUS = 0x002A,
        WM_DRAWITEM = 0x002B,
        WM_MEASUREITEM = 0x002C,
        WM_DELETEITEM = 0x002D,
        WM_VKEYTOITEM = 0x002E,
        WM_CHARTOITEM = 0x002F,
        WM_SETFONT = 0x0030,
        WM_GETFONT = 0x0031,
        WM_SETHOTKEY = 0x0032,
        WM_GETHOTKEY = 0x0033,
        WM_QUERYDRAGICON = 0x0037,
        WM_COMPAREITEM = 0x0039,
        WM_GETOBJECT = 0x003D,
        WM_COMPACTING = 0x0041,
        WM_COMMNOTIFY = 0x0044,
        WM_WINDOWPOSCHANGING = 0x0046,
        WM_WINDOWPOSCHANGED = 0x0047,
        WM_POWER = 0x0048,
        WM_COPYDATA = 0x004A,
        WM_CANCELJOURNAL = 0x004B,
        WM_NOTIFY = 0x004E,
        WM_INPUTLANGCHANGEREQUEST = 0x0050,
        WM_INPUTLANGCHANGE = 0x0051,
        WM_TCARD = 0x0052,
        WM_HELP = 0x0053,
        WM_USERCHANGED = 0x0054,
        WM_NOTIFYFORMAT = 0x0055,
        WM_CONTEXTMENU = 0x007B,
        WM_STYLECHANGING = 0x007C,
        WM_STYLECHANGED = 0x007D,
        WM_DISPLAYCHANGE = 0x007E,
        WM_GETICON = 0x007F,
        WM_SETICON = 0x0080,
        WM_NCCREATE = 0x0081,
        WM_NCDESTROY = 0x0082,
        WM_NCCALCSIZE = 0x0083,
        WM_NCHITTEST = 0x0084,
        WM_NCPAINT = 0x0085,
        WM_NCACTIVATE = 0x0086,
        WM_GETDLGCODE = 0x0087,
        WM_SYNCPAINT = 0x0088,

        WM_NCMOUSEMOVE = 0x00A0,
        WM_NCLBUTTONDOWN = 0x00A1,
        WM_NCLBUTTONUP = 0x00A2,
        WM_NCLBUTTONDBLCLK = 0x00A3,
        WM_NCRBUTTONDOWN = 0x00A4,
        WM_NCRBUTTONUP = 0x00A5,
        WM_NCRBUTTONDBLCLK = 0x00A6,
        WM_NCMBUTTONDOWN = 0x00A7,
        WM_NCMBUTTONUP = 0x00A8,
        WM_NCMBUTTONDBLCLK = 0x00A9,
        WM_NCXBUTTONDOWN = 0x00AB,
        WM_NCXBUTTONUP = 0x00AC,
        WM_NCXBUTTONDBLCLK = 0x00AD,

        WM_INPUT_DEVICE_CHANGE = 0x00FE,
        WM_INPUT = 0x00FF,

        WM_KEYFIRST = 0x0100,
        WM_KEYDOWN = WM_KEYFIRST,
        WM_KEYUP = 0x0101,
        WM_CHAR = 0x0102,
        WM_DEADCHAR = 0x0103,
        WM_SYSKEYDOWN = 0x0104,
        WM_SYSKEYUP = 0x0105,
        WM_SYSCHAR = 0x0106,
        WM_SYSDEADCHAR = 0x0107,
        WM_UNICHAR = 0x0109,
        WM_KEYLAST = WM_UNICHAR,

        WM_IME_STARTCOMPOSITION = 0x010D,
        WM_IME_ENDCOMPOSITION = 0x010E,
        WM_IME_COMPOSITION = 0x010F,
        WM_IME_KEYLAST = WM_IME_COMPOSITION,

        WM_INITDIALOG = 0x0110,
        WM_COMMAND = 0x0111,
        WM_SYSCOMMAND = 0x0112,
        WM_TIMER = 0x0113,
        WM_HSCROLL = 0x0114,
        WM_VSCROLL = 0x0115,
        WM_INITMENU = 0x0116,
        WM_INITMENUPOPUP = 0x0117,
        WM_MENUSELECT = 0x011F,
        WM_MENUCHAR = 0x0120,
        WM_ENTERIDLE = 0x0121,
        WM_MENURBUTTONUP = 0x0122,
        WM_MENUDRAG = 0x0123,
        WM_MENUGETOBJECT = 0x0124,
        WM_UNINITMENUPOPUP = 0x0125,
        WM_MENUCOMMAND = 0x0126,

        WM_CHANGEUISTATE = 0x0127,
        WM_UPDATEUISTATE = 0x0128,
        WM_QUERYUISTATE = 0x0129,

        WM_CTLCOLORMSGBOX = 0x0132,
        WM_CTLCOLOREDIT = 0x0133,
        WM_CTLCOLORLISTBOX = 0x0134,
        WM_CTLCOLORBTN = 0x0135,
        WM_CTLCOLORDLG = 0x0136,
        WM_CTLCOLORSCROLLBAR = 0x0137,
        WM_CTLCOLORSTATIC = 0x0138,
        MN_GETHMENU = 0x01E1,

        WM_MOUSEFIRST = 0x0200,
        WM_MOUSEMOVE = WM_MOUSEFIRST,
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_LBUTTONDBLCLK = 0x0203,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205,
        WM_RBUTTONDBLCLK = 0x0206,
        WM_MBUTTONDOWN = 0x0207,
        WM_MBUTTONUP = 0x0208,
        WM_MBUTTONDBLCLK = 0x0209,
        WM_MOUSEWHEEL = 0x020A,
        WM_XBUTTONDOWN = 0x020B,
        WM_XBUTTONUP = 0x020C,
        WM_XBUTTONDBLCLK = 0x020D,
        WM_MOUSEHWHEEL = 0x020E,

        WM_PARENTNOTIFY = 0x0210,
        WM_ENTERMENULOOP = 0x0211,
        WM_EXITMENULOOP = 0x0212,

        WM_NEXTMENU = 0x0213,
        WM_SIZING = 0x0214,
        WM_CAPTURECHANGED = 0x0215,
        WM_MOVING = 0x0216,

        WM_POWERBROADCAST = 0x0218,

        WM_DEVICECHANGE = 0x0219,

        WM_MDICREATE = 0x0220,
        WM_MDIDESTROY = 0x0221,
        WM_MDIACTIVATE = 0x0222,
        WM_MDIRESTORE = 0x0223,
        WM_MDINEXT = 0x0224,
        WM_MDIMAXIMIZE = 0x0225,
        WM_MDITILE = 0x0226,
        WM_MDICASCADE = 0x0227,
        WM_MDIICONARRANGE = 0x0228,
        WM_MDIGETACTIVE = 0x0229,

        WM_MDISETMENU = 0x0230,
        WM_ENTERSIZEMOVE = 0x0231,
        WM_EXITSIZEMOVE = 0x0232,
        WM_DROPFILES = 0x0233,
        WM_MDIREFRESHMENU = 0x0234,

        WM_IME_SETCONTEXT = 0x0281,
        WM_IME_NOTIFY = 0x0282,
        WM_IME_CONTROL = 0x0283,
        WM_IME_COMPOSITIONFULL = 0x0284,
        WM_IME_SELECT = 0x0285,
        WM_IME_CHAR = 0x0286,
        WM_IME_REQUEST = 0x0288,
        WM_IME_KEYDOWN = 0x0290,
        WM_IME_KEYUP = 0x0291,

        WM_MOUSEHOVER = 0x02A1,
        WM_MOUSELEAVE = 0x02A3,
        WM_NCMOUSEHOVER = 0x02A0,
        WM_NCMOUSELEAVE = 0x02A2,

        WM_WTSSESSION_CHANGE = 0x02B1,

        WM_TABLET_FIRST = 0x02c0,
        WM_TABLET_LAST = 0x02df,

        WM_CUT = 0x0300,
        WM_COPY = 0x0301,
        WM_PASTE = 0x0302,
        WM_CLEAR = 0x0303,
        WM_UNDO = 0x0304,
        WM_RENDERFORMAT = 0x0305,
        WM_RENDERALLFORMATS = 0x0306,
        WM_DESTROYCLIPBOARD = 0x0307,
        WM_DRAWCLIPBOARD = 0x0308,
        WM_PAINTCLIPBOARD = 0x0309,
        WM_VSCROLLCLIPBOARD = 0x030A,
        WM_SIZECLIPBOARD = 0x030B,
        WM_ASKCBFORMATNAME = 0x030C,
        WM_CHANGECBCHAIN = 0x030D,
        WM_HSCROLLCLIPBOARD = 0x030E,
        WM_QUERYNEWPALETTE = 0x030F,
        WM_PALETTEISCHANGING = 0x0310,
        WM_PALETTECHANGED = 0x0311,
        WM_HOTKEY = 0x0312,

        WM_PRINT = 0x0317,
        WM_PRINTCLIENT = 0x0318,

        WM_APPCOMMAND = 0x0319,

        WM_THEMECHANGED = 0x031A,

        WM_CLIPBOARDUPDATE = 0x031D,

        WM_DWMCOMPOSITIONCHANGED = 0x031E,
        WM_DWMNCRENDERINGCHANGED = 0x031F,
        WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320,
        WM_DWMWINDOWMAXIMIZEDCHANGE = 0x0321,

        WM_GETTITLEBARINFOEX = 0x033F,

        WM_HANDHELDFIRST = 0x0358,
        WM_HANDHELDLAST = 0x035F,

        WM_AFXFIRST = 0x0360,
        WM_AFXLAST = 0x037F,

        WM_PENWINFIRST = 0x0380,
        WM_PENWINLAST = 0x038F,

        WM_APP = 0x8000,

        WM_USER = 0x0400,

        WM_REFLECT = WM_USER + 0x1C00,
    }

    /// <summary>
    /// Returns true if the current application has focus, false otherwise.
    /// </summary>
    /// <returns>
    /// If the current application is focused.
    /// </returns>
    public static bool ApplicationIsActivated()
    {
        var activatedHandle = GetForegroundWindow();
        if (activatedHandle == IntPtr.Zero)
            return false; // No window is currently activated

        _ = GetWindowThreadProcessId(activatedHandle, out var activeProcId);
        if (Marshal.GetLastWin32Error() != 0)
            return false;

        return activeProcId == Environment.ProcessId;
    }

    /// <summary>
    /// Passes message information to the specified window procedure.
    /// </summary>
    /// <param name="lpPrevWndFunc">
    /// The previous window procedure. If this value is obtained by calling the GetWindowLong function with the nIndex parameter set to
    /// GWL_WNDPROC or DWL_DLGPROC, it is actually either the address of a window or dialog box procedure, or a special internal value
    /// meaningful only to CallWindowProc.
    /// </param>
    /// <param name="hWnd">
    /// A handle to the window procedure to receive the message.
    /// </param>
    /// <param name="msg">
    /// The message.
    /// </param>
    /// <param name="wParam">
    /// Additional message-specific information. The contents of this parameter depend on the value of the Msg parameter.
    /// </param>
    /// <param name="lParam">
    /// More additional message-specific information. The contents of this parameter depend on the value of the Msg parameter.
    /// </param>
    /// <returns>
    /// Use the CallWindowProc function for window subclassing. Usually, all windows with the same class share one window procedure. A
    /// subclass is a window or set of windows with the same class whose messages are intercepted and processed by another window procedure
    /// (or procedures) before being passed to the window procedure of the class.
    /// The SetWindowLong function creates the subclass by changing the window procedure associated with a particular window, causing the
    /// system to call the new window procedure instead of the previous one.An application must pass any messages not processed by the new
    /// window procedure to the previous window procedure by calling CallWindowProc.This allows the application to create a chain of window
    /// procedures.
    /// </returns>
    [DllImport("user32.dll")]
    public static extern long CallWindowProcW(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, ulong wParam, long lParam);

    /// <summary>
    /// See https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-flashwindowex.
    /// Flashes the specified window. It does not change the active state of the window.
    /// </summary>
    /// <param name="pwfi">
    /// A pointer to a FLASHWINFO structure.
    /// </param>
    /// <returns>
    /// The return value specifies the window's state before the call to the FlashWindowEx function. If the window caption
    /// was drawn as active before the call, the return value is nonzero. Otherwise, the return value is zero.
    /// </returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FlashWindowEx(ref FlashWindowInfo pwfi);

    /// <summary>
    /// Retrieves a handle to the foreground window (the window with which the user is currently working). The system assigns
    /// a slightly higher priority to the thread that creates the foreground window than it does to other threads.
    /// </summary>
    /// <returns>
    /// The return value is a handle to the foreground window. The foreground window can be NULL in certain circumstances,
    /// such as when a window is losing activation.
    /// </returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    public static extern IntPtr GetForegroundWindow();

    /// <summary>
    /// Retrieves the identifier of the thread that created the specified window and, optionally, the identifier of the
    /// process that created the window.
    /// </summary>
    /// <param name="handle">
    /// A handle to the window.
    /// </param>
    /// <param name="processId">
    /// A pointer to a variable that receives the process identifier. If this parameter is not NULL, GetWindowThreadProcessId
    /// copies the identifier of the process to the variable; otherwise, it does not.
    /// </param>
    /// <returns>
    /// The return value is the identifier of the thread that created the window.
    /// </returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

    /// <summary>
    /// Displays a modal dialog box that contains a system icon, a set of buttons, and a brief application-specific message,
    /// such as status or error information. The message box returns an integer value that indicates which button the user
    /// clicked.
    /// </summary>
    /// <param name="hWnd">
    /// A handle to the owner window of the message box to be created. If this parameter is NULL, the message box has no
    /// owner window.
    /// </param>
    /// <param name="text">
    /// The message to be displayed. If the string consists of more than one line, you can separate the lines using a carriage
    /// return and/or linefeed character between each line.
    /// </param>
    /// <param name="caption">
    /// The dialog box title. If this parameter is NULL, the default title is Error.</param>
    /// <param name="type">
    /// The contents and behavior of the dialog box. This parameter can be a combination of flags from the following groups
    /// of flags.
    /// </param>
    /// <returns>
    /// If a message box has a Cancel button, the function returns the IDCANCEL value if either the ESC key is pressed or
    /// the Cancel button is selected. If the message box has no Cancel button, pressing ESC will no effect - unless an
    /// MB_OK button is present. If an MB_OK button is displayed and the user presses ESC, the return value will be IDOK.
    /// If the function fails, the return value is zero.To get extended error information, call GetLastError. If the function
    /// succeeds, the return value is one of the ID* enum values.
    /// </returns>
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int MessageBoxW(IntPtr hWnd, string text, string caption, MessageBoxType type);

    /// <summary>
    /// Changes an attribute of the specified window. The function also sets a value at the specified offset in the extra window memory.
    /// </summary>
    /// <param name="hWnd">
    /// A handle to the window and, indirectly, the class to which the window belongs. The SetWindowLongPtr function fails if the
    /// process that owns the window specified by the hWnd parameter is at a higher process privilege in the UIPI hierarchy than the
    /// process the calling thread resides in.
    /// </param>
    /// <param name="nIndex">
    /// The zero-based offset to the value to be set. Valid values are in the range zero through the number of bytes of extra window
    /// memory, minus the size of a LONG_PTR. To set any other value, specify one of the values.
    /// </param>
    /// <param name="dwNewLong">
    /// The replacement value.
    /// </param>
    /// <returns>
    /// If the function succeeds, the return value is the previous value of the specified offset. If the function fails, the return
    /// value is zero.To get extended error information, call GetLastError. If the previous value is zero and the function succeeds,
    /// the return value is zero, but the function does not clear the last error information. To determine success or failure, clear
    /// the last error information by calling SetLastError with 0, then call SetWindowLongPtr.Function failure will be indicated by
    /// a return value of zero and a GetLastError result that is nonzero.
    /// </returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowLongPtrW(IntPtr hWnd, WindowLongType nIndex, IntPtr dwNewLong);

    /// <summary>
    /// See https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-flashwinfo.
    /// Contains the flash status for a window and the number of times the system should flash the window.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FlashWindowInfo
    {
        /// <summary>
        /// The size of the structure, in bytes.
        /// </summary>
        public uint Size;

        /// <summary>
        /// A handle to the window to be flashed. The window can be either opened or minimized.
        /// </summary>
        public IntPtr Hwnd;

        /// <summary>
        /// The flash status. This parameter can be one or more of the FlashWindow enum values.
        /// </summary>
        public FlashWindow Flags;

        /// <summary>
        /// The number of times to flash the window.
        /// </summary>
        public uint Count;

        /// <summary>
        /// The rate at which the window is to be flashed, in milliseconds. If dwTimeout is zero, the function uses the
        /// default cursor blink rate.
        /// </summary>
        public uint Timeout;
    }

    /// <summary>
    /// Parameters for use with SystemParametersInfo.
    /// </summary>
    public enum AccessibilityParameter
    {
#pragma warning disable SA1602
        SPI_GETCLIENTAREAANIMATION = 0x1042,
#pragma warning restore SA1602
    }
    
    /// <summary>
    /// Retrieves or sets the value of one of the system-wide parameters. This function can also update the user profile while setting a parameter.
    /// </summary>
    /// <param name="uiAction">The system-wide parameter to be retrieved or set.</param>
    /// <param name="uiParam">A parameter whose usage and format depends on the system parameter being queried or set.</param>
    /// <param name="pvParam">A parameter whose usage and format depends on the system parameter being queried or set. If not otherwise indicated, you must specify zero for this parameter.</param>
    /// <param name="fWinIni">If a system parameter is being set, specifies whether the user profile is to be updated, and if so, whether the WM_SETTINGCHANGE message is to be broadcast to all top-level windows to notify them of the change.</param>
    /// <returns>If the function succeeds, the return value is a nonzero value.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref int pvParam, uint fWinIni);
}

/// <summary>
/// Native imm32 functions.
/// </summary>
internal static partial class NativeFunctions
{
    /// <summary>
    /// GCS_* from imm32.
    /// These values are used with ImmGetCompositionString and WM_IME_COMPOSITION.
    /// </summary>
    [Flags]
    public enum IMEComposition
    {
        /// <summary>
        /// Retrieve or update the attribute of the composition string.
        /// </summary>
        CompAttr = 0x0010,

        /// <summary>
        /// Retrieve or update clause information of the composition string.
        /// </summary>
        CompClause = 0x0020,

        /// <summary>
        /// Retrieve or update the attributes of the reading string of the current composition.
        /// </summary>
        CompReadAttr = 0x0002,

        /// <summary>
        /// Retrieve or update the clause information of the reading string of the composition string.
        /// </summary>
        CompReadClause = 0x0004,

        /// <summary>
        /// Retrieve or update the reading string of the current composition.
        /// </summary>
        CompReadStr = 0x0001,

        /// <summary>
        /// Retrieve or update the current composition string.
        /// </summary>
        CompStr = 0x0008,

        /// <summary>
        /// Retrieve or update the cursor position in composition string.
        /// </summary>
        CursorPos = 0x0080,

        /// <summary>
        /// Retrieve or update the starting position of any changes in composition string.
        /// </summary>
        DeltaStart = 0x0100,

        /// <summary>
        /// Retrieve or update clause information of the result string.
        /// </summary>
        ResultClause = 0x1000,

        /// <summary>
        /// Retrieve or update clause information of the reading string.
        /// </summary>
        ResultReadClause = 0x0400,

        /// <summary>
        /// Retrieve or update the reading string.
        /// </summary>
        ResultReadStr = 0x0200,

        /// <summary>
        /// Retrieve or update the string of the composition result.
        /// </summary>
        ResultStr = 0x0800,
    }

    /// <summary>
    /// IMN_* from imm32.
    /// Input Method Manager Commands, this enum is not exhaustive.
    /// </summary>
    public enum IMECommand
    {
        /// <summary>
        /// Notifies the application when an IME is about to change the content of the candidate window.
        /// </summary>
        ChangeCandidate = 0x0003,

        /// <summary>
        /// Notifies an application when an IME is about to close the candidates window.
        /// </summary>
        CloseCandidate = 0x0004,

        /// <summary>
        /// Notifies an application when an IME is about to open the candidate window.
        /// </summary>
        OpenCandidate = 0x0005,

        /// <summary>
        /// Notifies an application when the conversion mode of the input context is updated.
        /// </summary>
        SetConversionMode = 0x0006,
    }

    /// <summary>
    /// Returns the input context associated with the specified window.
    /// </summary>
    /// <param name="hWnd">Unnamed parameter 1.</param>
    /// <returns>
    /// Returns the handle to the input context.
    /// </returns>
    [DllImport("imm32.dll")]
    public static extern IntPtr ImmGetContext(IntPtr hWnd);

    /// <summary>
    /// Retrieves information about the composition string.
    /// </summary>
    /// <param name="hImc">
    /// Unnamed parameter 1.
    /// </param>
    /// <param name="arg2">
    /// Unnamed parameter 2.
    /// </param>
    /// <param name="lpBuf">
    /// Pointer to a buffer in which the function retrieves the composition string information.
    /// </param>
    /// <param name="dwBufLen">
    /// Size, in bytes, of the output buffer, even if the output is a Unicode string. The application sets this parameter to 0
    /// if the function is to return the size of the required output buffer.
    /// </param>
    /// <returns>
    /// Returns the number of bytes copied to the output buffer. If dwBufLen is set to 0, the function returns the buffer size,
    /// in bytes, required to receive all requested information, excluding the terminating null character. The return value is
    /// always the size, in bytes, even if the requested data is a Unicode string.
    /// This function returns one of the following negative error codes if it does not succeed:
    /// - IMM_ERROR_NODATA.Composition data is not ready in the input context.
    /// - IMM_ERROR_GENERAL.General error detected by IME.
    /// </returns>
    [DllImport("imm32.dll")]
    public static extern long ImmGetCompositionStringW(IntPtr hImc, IMEComposition arg2, IntPtr lpBuf, uint dwBufLen);

    /// <summary>
    /// Retrieves a candidate list.
    /// </summary>
    /// <param name="hImc">
    /// Unnamed parameter 1.
    /// </param>
    /// <param name="deIndex">
    /// Zero-based index of the candidate list.
    /// </param>
    /// <param name="lpCandList">
    /// Pointer to a CANDIDATELIST structure in which the function retrieves the candidate list.
    /// </param>
    /// <param name="dwBufLen">
    /// Size, in bytes, of the buffer to receive the candidate list. The application can specify 0 for this parameter if the
    /// function is to return the required size of the output buffer only.
    /// </param>
    /// <returns>
    /// Returns the number of bytes copied to the candidate list buffer if successful. If the application has supplied 0 for
    /// the dwBufLen parameter, the function returns the size required for the candidate list buffer. The function returns 0
    /// if it does not succeed.
    /// </returns>
    [DllImport("imm32.dll")]
    public static extern long ImmGetCandidateListW(IntPtr hImc, uint deIndex, IntPtr lpCandList, uint dwBufLen);

    /// <summary>
    /// Sets the position of the composition window.
    /// </summary>
    /// <param name="hImc">
    /// Unnamed parameter 1.
    /// </param>
    /// <param name="frm">
    /// Pointer to a COMPOSITIONFORM structure that contains the new position and other related information about
    /// the composition window.
    /// </param>
    /// <returns>
    /// Returns a nonzero value if successful, or 0 otherwise.
    /// </returns>
    [DllImport("imm32.dll", CharSet = CharSet.Auto)]
    public static extern bool ImmSetCompositionWindow(IntPtr hImc, ref CompositionForm frm);

    /// <summary>
    /// Releases the input context and unlocks the memory associated in the input context. An application must call this
    /// function for each call to the ImmGetContext function.
    /// </summary>
    /// <param name="hwnd">
    /// Unnamed parameter 1.
    /// </param>
    /// <param name="hImc">
    /// Unnamed parameter 2.
    /// </param>
    /// <returns>
    /// Returns a nonzero value if successful, or 0 otherwise.
    /// </returns>
    [DllImport("imm32.dll", CharSet = CharSet.Auto)]
    public static extern bool ImmReleaseContext(IntPtr hwnd, IntPtr hImc);

    /// <summary>
    /// Contains information about a candidate list.
    /// </summary>
    public struct CandidateList
    {
        /// <summary>
        /// Size, in bytes, of the structure, the offset array, and all candidate strings.
        /// </summary>
        public int Size;

        /// <summary>
        /// Candidate style values. This member can have one or more of the IME_CAND_* values.
        /// </summary>
        public int Style;

        /// <summary>
        /// Number of candidate strings.
        /// </summary>
        public int Count;

        /// <summary>
        /// Index of the selected candidate string.
        /// </summary>
        public int Selection;

        /// <summary>
        /// Index of the first candidate string in the candidate window. This varies as the user presses the PAGE UP and PAGE DOWN keys.
        /// </summary>
        public int PageStart;

        /// <summary>
        /// Number of candidate strings to be shown in one page in the candidate window. The user can move to the next page by pressing IME-defined keys, such as the PAGE UP or PAGE DOWN key. If this number is 0, an application can define a proper value by itself.
        /// </summary>
        public int PageSize;

        // /// <summary>
        // /// Offset to the start of the first candidate string, relative to the start of this structure. The offsets
        // /// for subsequent strings immediately follow this member, forming an array of 32-bit offsets.
        // /// </summary>
        // public IntPtr Offset; // manually handle
    }

    /// <summary>
    /// Contains style and position information for a composition window.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CompositionForm
    {
        /// <summary>
        /// Position style. This member can be one of the CFS_* values.
        /// </summary>
        public int Style;

        /// <summary>
        /// A POINT structure containing the coordinates of the upper left corner of the composition window.
        /// </summary>
        public Point CurrentPos;

        /// <summary>
        /// A RECT structure containing the coordinates of the upper left and lower right corners of the composition window.
        /// </summary>
        public Rect Area;
    }

    /// <summary>
    /// Contains coordinates for a point.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        /// <summary>
        /// The X position.
        /// </summary>
        public int X;

        /// <summary>
        /// The Y position.
        /// </summary>
        public int Y;
    }

    /// <summary>
    /// Contains dimensions for a rectangle.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        /// <summary>
        /// The left position.
        /// </summary>
        public int Left;

        /// <summary>
        /// The top position.
        /// </summary>
        public int Top;

        /// <summary>
        /// The right position.
        /// </summary>
        public int Right;

        /// <summary>
        /// The bottom position.
        /// </summary>
        public int Bottom;
    }
}

/// <summary>
/// Native kernel32 functions.
/// </summary>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1629:Documentation text should end with a period", Justification = "Stupid rule")]
internal static partial class NativeFunctions
{
    /// <summary>
    /// MEM_* from memoryapi.
    /// </summary>
    [Flags]
    public enum AllocationType
    {
        /// <summary>
        /// To coalesce two adjacent placeholders, specify MEM_RELEASE | MEM_COALESCE_PLACEHOLDERS. When you coalesce
        /// placeholders, lpAddress and dwSize must exactly match those of the placeholder.
        /// </summary>
        CoalescePlaceholders = 0x1,

        /// <summary>
        /// Frees an allocation back to a placeholder (after you've replaced a placeholder with a private allocation using
        /// VirtualAlloc2 or Virtual2AllocFromApp). To split a placeholder into two placeholders, specify
        /// MEM_RELEASE | MEM_PRESERVE_PLACEHOLDER.
        /// </summary>
        PreservePlaceholder = 0x2,

        /// <summary>
        /// Allocates memory charges (from the overall size of memory and the paging files on disk) for the specified reserved
        /// memory pages. The function also guarantees that when the caller later initially accesses the memory, the contents
        /// will be zero. Actual physical pages are not allocated unless/until the virtual addresses are actually accessed.
        /// To reserve and commit pages in one step, call VirtualAllocEx with MEM_COMMIT | MEM_RESERVE. Attempting to commit
        /// a specific address range by specifying MEM_COMMIT without MEM_RESERVE and a non-NULL lpAddress fails unless the
        /// entire range has already been reserved. The resulting error code is ERROR_INVALID_ADDRESS. An attempt to commit
        /// a page that is already committed does not cause the function to fail. This means that you can commit pages without
        /// first determining the current commitment state of each page. If lpAddress specifies an address within an enclave,
        /// flAllocationType must be MEM_COMMIT.
        /// </summary>
        Commit = 0x1000,

        /// <summary>
        /// Reserves a range of the process's virtual address space without allocating any actual physical storage in memory
        /// or in the paging file on disk. You commit reserved pages by calling VirtualAllocEx again with MEM_COMMIT. To
        /// reserve and commit pages in one step, call VirtualAllocEx with MEM_COMMIT | MEM_RESERVE. Other memory allocation
        /// functions, such as malloc and LocalAlloc, cannot use reserved memory until it has been released.
        /// </summary>
        Reserve = 0x2000,

        /// <summary>
        /// Decommits the specified region of committed pages. After the operation, the pages are in the reserved state.
        /// The function does not fail if you attempt to decommit an uncommitted page. This means that you can decommit
        /// a range of pages without first determining the current commitment state. The MEM_DECOMMIT value is not supported
        /// when the lpAddress parameter provides the base address for an enclave.
        /// </summary>
        Decommit = 0x4000,

        /// <summary>
        /// Releases the specified region of pages, or placeholder (for a placeholder, the address space is released and
        /// available for other allocations). After this operation, the pages are in the free state. If you specify this
        /// value, dwSize must be 0 (zero), and lpAddress must point to the base address returned by the VirtualAlloc function
        /// when the region is reserved. The function fails if either of these conditions is not met. If any pages in the
        /// region are committed currently, the function first decommits, and then releases them. The function does not
        /// fail if you attempt to release pages that are in different states, some reserved and some committed. This means
        /// that you can release a range of pages without first determining the current commitment state.
        /// </summary>
        Release = 0x8000,

        /// <summary>
        /// Indicates that data in the memory range specified by lpAddress and dwSize is no longer of interest. The pages
        /// should not be read from or written to the paging file. However, the memory block will be used again later, so
        /// it should not be decommitted. This value cannot be used with any other value. Using this value does not guarantee
        /// that the range operated on with MEM_RESET will contain zeros. If you want the range to contain zeros, decommit
        /// the memory and then recommit it. When you use MEM_RESET, the VirtualAllocEx function ignores the value of fProtect.
        /// However, you must still set fProtect to a valid protection value, such as PAGE_NOACCESS. VirtualAllocEx returns
        /// an error if you use MEM_RESET and the range of memory is mapped to a file. A shared view is only acceptable
        /// if it is mapped to a paging file.
        /// </summary>
        Reset = 0x80000,

        /// <summary>
        /// MEM_RESET_UNDO should only be called on an address range to which MEM_RESET was successfully applied earlier.
        /// It indicates that the data in the specified memory range specified by lpAddress and dwSize is of interest to
        /// the caller and attempts to reverse the effects of MEM_RESET. If the function succeeds, that means all data in
        /// the specified address range is intact. If the function fails, at least some of the data in the address range
        /// has been replaced with zeroes. This value cannot be used with any other value. If MEM_RESET_UNDO is called on
        /// an address range which was not MEM_RESET earlier, the behavior is undefined. When you specify MEM_RESET, the
        /// VirtualAllocEx function ignores the value of flProtect. However, you must still set flProtect to a valid
        /// protection value, such as PAGE_NOACCESS.
        /// </summary>
        ResetUndo = 0x1000000,

        /// <summary>
        /// Reserves an address range that can be used to map Address Windowing Extensions (AWE) pages. This value must
        /// be used with MEM_RESERVE and no other values.
        /// </summary>
        Physical = 0x400000,

        /// <summary>
        /// Allocates memory at the highest possible address. This can be slower than regular allocations, especially when
        /// there are many allocations.
        /// </summary>
        TopDown = 0x100000,

        /// <summary>
        /// Causes the system to track pages that are written to in the allocated region. If you specify this value, you
        /// must also specify MEM_RESERVE. To retrieve the addresses of the pages that have been written to since the region
        /// was allocated or the write-tracking state was reset, call the GetWriteWatch function. To reset the write-tracking
        /// state, call GetWriteWatch or ResetWriteWatch. The write-tracking feature remains enabled for the memory region
        /// until the region is freed.
        /// </summary>
        WriteWatch = 0x200000,

        /// <summary>
        /// Allocates memory using large page support. The size and alignment must be a multiple of the large-page minimum.
        /// To obtain this value, use the GetLargePageMinimum function. If you specify this value, you must also specify
        /// MEM_RESERVE and MEM_COMMIT.
        /// </summary>
        LargePages = 0x20000000,
    }

    /// <summary>
    /// SEM_* from errhandlingapi.
    /// </summary>
    [Flags]
    public enum ErrorModes : uint
    {
        /// <summary>
        /// Use the system default, which is to display all error dialog boxes.
        /// </summary>
        SystemDefault = 0x0,

        /// <summary>
        /// The system does not display the critical-error-handler message box. Instead, the system sends the error to the
        /// calling process. Best practice is that all applications call the process-wide SetErrorMode function with a parameter
        /// of SEM_FAILCRITICALERRORS at startup. This is to prevent error mode dialogs from hanging the application.
        /// </summary>
        FailCriticalErrors = 0x0001,

        /// <summary>
        /// The system automatically fixes memory alignment faults and makes them invisible to the application. It does
        /// this for the calling process and any descendant processes. This feature is only supported by certain processor
        /// architectures. For more information, see the Remarks section. After this value is set for a process, subsequent
        /// attempts to clear the value are ignored.
        /// </summary>
        NoAlignmentFaultExcept = 0x0004,

        /// <summary>
        /// The system does not display the Windows Error Reporting dialog.
        /// </summary>
        NoGpFaultErrorBox = 0x0002,

        /// <summary>
        /// The OpenFile function does not display a message box when it fails to find a file. Instead, the error is returned
        /// to the caller. This error mode overrides the OF_PROMPT flag.
        /// </summary>
        NoOpenFileErrorBox = 0x8000,
    }

    /// <summary>
    /// PAGE_* from memoryapi.
    /// </summary>
    [Flags]
    public enum MemoryProtection
    {
        /// <summary>
        /// Enables execute access to the committed region of pages. An attempt to write to the committed region results
        /// in an access violation. This flag is not supported by the CreateFileMapping function.
        /// </summary>
        Execute = 0x10,

        /// <summary>
        /// Enables execute or read-only access to the committed region of pages. An attempt to write to the committed region
        /// results in an access violation.
        /// </summary>
        ExecuteRead = 0x20,

        /// <summary>
        /// Enables execute, read-only, or read/write access to the committed region of pages.
        /// </summary>
        ExecuteReadWrite = 0x40,

        /// <summary>
        /// Enables execute, read-only, or copy-on-write access to a mapped view of a file mapping object. An attempt to
        /// write to a committed copy-on-write page results in a private copy of the page being made for the process. The
        /// private page is marked as PAGE_EXECUTE_READWRITE, and the change is written to the new page. This flag is not
        /// supported by the VirtualAlloc or VirtualAllocEx functions.
        /// </summary>
        ExecuteWriteCopy = 0x80,

        /// <summary>
        /// Disables all access to the committed region of pages. An attempt to read from, write to, or execute the committed
        /// region results in an access violation. This flag is not supported by the CreateFileMapping function.
        /// </summary>
        NoAccess = 0x01,

        /// <summary>
        /// Enables read-only access to the committed region of pages. An attempt to write to the committed region results
        /// in an access violation. If Data Execution Prevention is enabled, an attempt to execute code in the committed
        /// region results in an access violation.
        /// </summary>
        ReadOnly = 0x02,

        /// <summary>
        /// Enables read-only or read/write access to the committed region of pages. If Data Execution Prevention is enabled,
        /// attempting to execute code in the committed region results in an access violation.
        /// </summary>
        ReadWrite = 0x04,

        /// <summary>
        /// Enables read-only or copy-on-write access to a mapped view of a file mapping object. An attempt to write to
        /// a committed copy-on-write page results in a private copy of the page being made for the process. The private
        /// page is marked as PAGE_READWRITE, and the change is written to the new page. If Data Execution Prevention is
        /// enabled, attempting to execute code in the committed region results in an access violation. This flag is not
        /// supported by the VirtualAlloc or VirtualAllocEx functions.
        /// </summary>
        WriteCopy = 0x08,

        /// <summary>
        /// Sets all locations in the pages as invalid targets for CFG. Used along with any execute page protection like
        /// PAGE_EXECUTE, PAGE_EXECUTE_READ, PAGE_EXECUTE_READWRITE and PAGE_EXECUTE_WRITECOPY. Any indirect call to locations
        /// in those pages will fail CFG checks and the process will be terminated. The default behavior for executable
        /// pages allocated is to be marked valid call targets for CFG. This flag is not supported by the VirtualProtect
        /// or CreateFileMapping functions.
        /// </summary>
        TargetsInvalid = 0x40000000,

        /// <summary>
        /// Pages in the region will not have their CFG information updated while the protection changes for VirtualProtect.
        /// For example, if the pages in the region was allocated using PAGE_TARGETS_INVALID, then the invalid information
        /// will be maintained while the page protection changes. This flag is only valid when the protection changes to
        /// an executable type like PAGE_EXECUTE, PAGE_EXECUTE_READ, PAGE_EXECUTE_READWRITE and PAGE_EXECUTE_WRITECOPY.
        /// The default behavior for VirtualProtect protection change to executable is to mark all locations as valid call
        /// targets for CFG.
        /// </summary>
        TargetsNoUpdate = TargetsInvalid,

        /// <summary>
        /// Pages in the region become guard pages. Any attempt to access a guard page causes the system to raise a
        /// STATUS_GUARD_PAGE_VIOLATION exception and turn off the guard page status. Guard pages thus act as a one-time
        /// access alarm. For more information, see Creating Guard Pages. When an access attempt leads the system to turn
        /// off guard page status, the underlying page protection takes over. If a guard page exception occurs during a
        /// system service, the service typically returns a failure status indicator. This value cannot be used with
        /// PAGE_NOACCESS. This flag is not supported by the CreateFileMapping function.
        /// </summary>
        Guard = 0x100,

        /// <summary>
        /// Sets all pages to be non-cachable. Applications should not use this attribute except when explicitly required
        /// for a device. Using the interlocked functions with memory that is mapped with SEC_NOCACHE can result in an
        /// EXCEPTION_ILLEGAL_INSTRUCTION exception. The PAGE_NOCACHE flag cannot be used with the PAGE_GUARD, PAGE_NOACCESS,
        /// or PAGE_WRITECOMBINE flags. The PAGE_NOCACHE flag can be used only when allocating private memory with the
        /// VirtualAlloc, VirtualAllocEx, or VirtualAllocExNuma functions. To enable non-cached memory access for shared
        /// memory, specify the SEC_NOCACHE flag when calling the CreateFileMapping function.
        /// </summary>
        NoCache = 0x200,

        /// <summary>
        /// Sets all pages to be write-combined. Applications should not use this attribute except when explicitly required
        /// for a device. Using the interlocked functions with memory that is mapped as write-combined can result in an
        /// EXCEPTION_ILLEGAL_INSTRUCTION exception. The PAGE_WRITECOMBINE flag cannot be specified with the PAGE_NOACCESS,
        /// PAGE_GUARD, and PAGE_NOCACHE flags. The PAGE_WRITECOMBINE flag can be used only when allocating private memory
        /// with the VirtualAlloc, VirtualAllocEx, or VirtualAllocExNuma functions. To enable write-combined memory access
        /// for shared memory, specify the SEC_WRITECOMBINE flag when calling the CreateFileMapping function.
        /// </summary>
        WriteCombine = 0x400,
    }

    /// <summary>
    /// HEAP_* from heapapi.
    /// </summary>
    [Flags]
    public enum HeapOptions
    {
        /// <summary>
        /// Serialized access is not used when the heap functions access this heap. This option applies to all
        /// subsequent heap function calls. Alternatively, you can specify this option on individual heap function
        /// calls. The low-fragmentation heap (LFH) cannot be enabled for a heap created with this option. A heap
        /// created with this option cannot be locked.
        /// </summary>
        NoSerialize = 0x00000001,
        
        /// <summary>
        /// The system raises an exception to indicate failure (for example, an out-of-memory condition) for calls to
        /// HeapAlloc and HeapReAlloc instead of returning NULL.
        /// </summary>
        GenerateExceptions = 0x00000004,
        
        /// <summary>
        /// The allocated memory will be initialized to zero. Otherwise, the memory is not initialized to zero.
        /// </summary>
        ZeroMemory = 0x00000008,
    
        /// <summary>
        /// All memory blocks that are allocated from this heap allow code execution, if the hardware enforces data
        /// execution prevention. Use this flag heap in applications that run code from the heap. If
        /// HEAP_CREATE_ENABLE_EXECUTE is not specified and an application attempts to run code from a protected page,
        /// the application receives an exception with the status code STATUS_ACCESS_VIOLATION.
        /// </summary>
        CreateEnableExecute = 0x00040000,
    }

    /// <summary>
    /// See https://docs.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-setevent
    /// Sets the specified event object to the signaled state.
    /// </summary>
    /// <param name="hEvent">A handle to the event object. The CreateEvent or OpenEvent function returns this handle.</param>
    /// <returns>
    /// If the function succeeds, the return value is nonzero.
    /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
    /// </returns>
    [DllImport("kernel32.dll")]
    public static extern bool SetEvent(IntPtr hEvent);

    /// <summary>
    /// See https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-freelibrary.
    /// Frees the loaded dynamic-link library (DLL) module and, if necessary, decrements its reference count. When the reference
    /// count reaches zero, the module is unloaded from the address space of the calling process and the handle is no longer
    /// valid.
    /// </summary>
    /// <param name="hModule">
    /// A handle to the loaded library module. The LoadLibrary, LoadLibraryEx, GetModuleHandle, or GetModuleHandleEx function
    /// returns this handle.
    /// </param>
    /// <returns>
    /// If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended
    /// error information, call the GetLastError function.
    /// </returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FreeLibrary(IntPtr hModule);

    /// <summary>
    /// See https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-getmodulefilenamew.
    /// Retrieves the fully qualified path for the file that contains the specified module. The module must have been loaded
    /// by the current process. To locate the file for a module that was loaded by another process, use the GetModuleFileNameEx
    /// function.
    /// </summary>
    /// <param name="hModule">
    /// A handle to the loaded module whose path is being requested. If this parameter is NULL, GetModuleFileName retrieves
    /// the path of the executable file of the current process. The GetModuleFileName function does not retrieve the path
    /// for modules that were loaded using the LOAD_LIBRARY_AS_DATAFILE flag. For more information, see LoadLibraryEx.
    /// </param>
    /// <param name="lpFilename">
    /// A pointer to a buffer that receives the fully qualified path of the module. If the length of the path is less than
    /// the size that the nSize parameter specifies, the function succeeds and the path is returned as a null-terminated
    /// string. If the length of the path exceeds the size that the nSize parameter specifies, the function succeeds and
    /// the string is truncated to nSize characters including the terminating null character.
    /// </param>
    /// <param name="nSize">
    /// The size of the lpFilename buffer, in TCHARs.
    /// </param>
    /// <returns>
    /// If the function succeeds, the return value is the length of the string that is copied to the buffer, in characters,
    /// not including the terminating null character. If the buffer is too small to hold the module name, the string is
    /// truncated to nSize characters including the terminating null character, the function returns nSize, and the function
    /// sets the last error to ERROR_INSUFFICIENT_BUFFER. If nSize is zero, the return value is zero and the last error
    /// code is ERROR_SUCCESS. If the function fails, the return value is 0 (zero). To get extended error information, call
    /// GetLastError.
    /// </returns>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [PreserveSig]
    public static extern uint GetModuleFileNameW(
        [In] IntPtr hModule,
        [Out] StringBuilder lpFilename,
        [In][MarshalAs(UnmanagedType.U4)] int nSize);

    /// <summary>
    /// See https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-getmodulehandlew.
    /// Retrieves a module handle for the specified module. The module must have been loaded by the calling process. To
    /// avoid the race conditions described in the Remarks section, use the GetModuleHandleEx function.
    /// </summary>
    /// <param name="lpModuleName">
    /// The name of the loaded module (either a .dll or .exe file). If the file name extension is omitted, the default
    /// library extension .dll is appended. The file name string can include a trailing point character (.) to indicate
    /// that the module name has no extension. The string does not have to specify a path. When specifying a path, be sure
    /// to use backslashes (\), not forward slashes (/). The name is compared (case independently) to the names of modules
    /// currently mapped into the address space of the calling process. If this parameter is NULL, GetModuleHandle returns
    /// a handle to the file used to create the calling process (.exe file). The GetModuleHandle function does not retrieve
    /// handles for modules that were loaded using the LOAD_LIBRARY_AS_DATAFILE flag.For more information, see LoadLibraryEx.
    /// </param>
    /// <returns>
    /// If the function succeeds, the return value is a handle to the specified module. If the function fails, the return
    /// value is NULL.To get extended error information, call GetLastError.
    /// </returns>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    public static extern IntPtr GetModuleHandleW(string lpModuleName);

    /// <summary>
    /// Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).
    /// </summary>
    /// <param name="hModule">
    /// A handle to the DLL module that contains the function or variable. The LoadLibrary, LoadLibraryEx, LoadPackagedLibrary,
    /// or GetModuleHandle function returns this handle. The GetProcAddress function does not retrieve addresses from modules
    /// that were loaded using the LOAD_LIBRARY_AS_DATAFILE flag.For more information, see LoadLibraryEx.
    /// </param>
    /// <param name="procName">
    /// The function or variable name, or the function's ordinal value. If this parameter is an ordinal value, it must be
    /// in the low-order word; the high-order word must be zero.
    /// </param>
    /// <returns>
    /// If the function succeeds, the return value is the address of the exported function or variable. If the function
    /// fails, the return value is NULL.To get extended error information, call GetLastError.
    /// </returns>
    [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    [SuppressMessage("Globalization", "CA2101:Specify marshaling for P/Invoke string arguments", Justification = "Ansi only")]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    /// <summary>
    /// See https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryw.
    /// Loads the specified module into the address space of the calling process. The specified module may cause other modules
    /// to be loaded. For additional load options, use the LoadLibraryEx function.
    /// </summary>
    /// <param name="lpFileName">
    /// The name of the module. This can be either a library module (a .dll file) or an executable module (an .exe file).
    /// The name specified is the file name of the module and is not related to the name stored in the library module itself,
    /// as specified by the LIBRARY keyword in the module-definition (.def) file. If the string specifies a full path, the
    /// function searches only that path for the module. If the string specifies a relative path or a module name without
    /// a path, the function uses a standard search strategy to find the module; for more information, see the Remarks.
    /// If the function cannot find the module, the function fails.When specifying a path, be sure to use backslashes (\),
    /// not forward slashes(/). For more information about paths, see Naming a File or Directory. If the string specifies
    /// a module name without a path and the file name extension is omitted, the function appends the default library extension
    /// .dll to the module name. To prevent the function from appending .dll to the module name, include a trailing point
    /// character (.) in the module name string.
    /// </param>
    /// <returns>
    /// If the function succeeds, the return value is a handle to the module. If the function fails, the return value is
    /// NULL.To get extended error information, call GetLastError.
    /// </returns>
    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

    /// <summary>
    /// ReadProcessMemory copies the data in the specified address range from the address space of the specified process
    /// into the specified buffer of the current process. Any process that has a handle with PROCESS_VM_READ access can
    /// call the function. The entire area to be read must be accessible, and if it is not accessible, the function fails.
    /// </summary>
    /// <param name="hProcess">
    /// A handle to the process with memory that is being read. The handle must have PROCESS_VM_READ access to the process.
    /// </param>
    /// <param name="lpBaseAddress">
    /// A pointer to the base address in the specified process from which to read. Before any data transfer occurs, the
    /// system verifies that all data in the base address and memory of the specified size is accessible for read access,
    /// and if it is not accessible the function fails.
    /// </param>
    /// <param name="lpBuffer">
    /// A pointer to a buffer that receives the contents from the address space of the specified process.
    /// </param>
    /// <param name="dwSize">
    /// The number of bytes to be read from the specified process.
    /// </param>
    /// <param name="lpNumberOfBytesRead">
    /// A pointer to a variable that receives the number of bytes transferred into the specified buffer. If lpNumberOfBytesRead
    /// is NULL, the parameter is ignored.
    /// </param>
    /// <returns>
    /// If the function succeeds, the return value is nonzero. If the function fails, the return value is 0 (zero). To get
    /// extended error information, call GetLastError. The function fails if the requested read operation crosses into an
    /// area of the process that is inaccessible.
    /// </returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        IntPtr lpBuffer,
        int dwSize,
        out IntPtr lpNumberOfBytesRead);

    /// <summary>
    /// ReadProcessMemory copies the data in the specified address range from the address space of the specified process
    /// into the specified buffer of the current process. Any process that has a handle with PROCESS_VM_READ access can
    /// call the function. The entire area to be read must be accessible, and if it is not accessible, the function fails.
    /// </summary>
    /// <param name="hProcess">
    /// A handle to the process with memory that is being read. The handle must have PROCESS_VM_READ access to the process.
    /// </param>
    /// <param name="lpBaseAddress">
    /// A pointer to the base address in the specified process from which to read. Before any data transfer occurs, the
    /// system verifies that all data in the base address and memory of the specified size is accessible for read access,
    /// and if it is not accessible the function fails.
    /// </param>
    /// <param name="lpBuffer">
    /// A pointer to a buffer that receives the contents from the address space of the specified process.
    /// </param>
    /// <param name="dwSize">
    /// The number of bytes to be read from the specified process.
    /// </param>
    /// <param name="lpNumberOfBytesRead">
    /// A pointer to a variable that receives the number of bytes transferred into the specified buffer. If lpNumberOfBytesRead
    /// is NULL, the parameter is ignored.
    /// </param>
    /// <returns>
    /// If the function succeeds, the return value is nonzero. If the function fails, the return value is 0 (zero). To get
    /// extended error information, call GetLastError. The function fails if the requested read operation crosses into an
    /// area of the process that is inaccessible.
    /// </returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        byte[] lpBuffer,
        int dwSize,
        out IntPtr lpNumberOfBytesRead);

    /// <summary>
    /// See https://docs.microsoft.com/en-us/windows/win32/api/errhandlingapi/nf-errhandlingapi-seterrormode.
    /// Controls whether the system will handle the specified types of serious errors or whether the process will handle
    /// them.
    /// </summary>
    /// <param name="uMode">
    /// The process error mode. This parameter can be one or more of the ErrorMode enum values.
    /// </param>
    /// <returns>
    /// The return value is the previous state of the error-mode bit flags.
    /// </returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern ErrorModes SetErrorMode(ErrorModes uMode);

    /// <summary>
    /// See https://docs.microsoft.com/en-us/windows/win32/api/errhandlingapi/nf-errhandlingapi-setunhandledexceptionfilter.
    /// Enables an application to supersede the top-level exception handler of each thread of a process. After calling this
    /// function, if an exception occurs in a process that is not being debugged, and the exception makes it to the unhandled
    /// exception filter, that filter will call the exception filter function specified by the lpTopLevelExceptionFilter
    /// parameter.
    /// </summary>
    /// <param name="lpTopLevelExceptionFilter">
    /// A pointer to a top-level exception filter function that will be called whenever the UnhandledExceptionFilter function
    /// gets control, and the process is not being debugged. A value of NULL for this parameter specifies default handling
    /// within UnhandledExceptionFilter. The filter function has syntax similar to that of UnhandledExceptionFilter: It
    /// takes a single parameter of type LPEXCEPTION_POINTERS, has a WINAPI calling convention, and returns a value of type
    /// LONG. The filter function should return one of the EXCEPTION_* enum values.
    /// </param>
    /// <returns>
    /// The SetUnhandledExceptionFilter function returns the address of the previous exception filter established with the
    /// function. A NULL return value means that there is no current top-level exception handler.
    /// </returns>
    [DllImport("kernel32.dll")]
    public static extern IntPtr SetUnhandledExceptionFilter(IntPtr lpTopLevelExceptionFilter);

    /// <summary>
    /// HeapCreate - Creates a private heap object that can be used by the calling process.
    /// The function reserves space in the virtual address space of the process and allocates
    /// physical storage for a specified initial portion of this block.
    /// Ref: https://learn.microsoft.com/en-us/windows/win32/api/heapapi/nf-heapapi-heapcreate
    /// </summary>
    /// <param name="flOptions">
    /// The heap allocation options.
    /// These options affect subsequent access to the new heap through calls to the heap functions.
    /// </param>
    /// <param name="dwInitialSize">
    /// The initial size of the heap, in bytes.
    ///
    /// This value determines the initial amount of memory that is committed for the heap.
    /// The value is rounded up to a multiple of the system page size. The value must be smaller than dwMaximumSize.
    /// 
    /// If this parameter is 0, the function commits one page. To determine the size of a page on the host computer,
    /// use the GetSystemInfo function.
    /// </param>
    /// <param name="dwMaximumSize">
    /// The maximum size of the heap, in bytes. The HeapCreate function rounds dwMaximumSize up to a multiple of the
    /// system page size and then reserves a block of that size in the process's virtual address space for the heap.
    /// If allocation requests made by the HeapAlloc or HeapReAlloc functions exceed the size specified by
    /// dwInitialSize, the system commits additional pages of memory for the heap, up to the heap's maximum size.
    /// 
    /// If dwMaximumSize is not zero, the heap size is fixed and cannot grow beyond the maximum size. Also, the largest
    /// memory block that can be allocated from the heap is slightly less than 512 KB for a 32-bit process and slightly
    /// less than 1,024 KB for a 64-bit process. Requests to allocate larger blocks fail, even if the maximum size of
    /// the heap is large enough to contain the block.
    /// 
    /// If dwMaximumSize is 0, the heap can grow in size. The heap's size is limited only by the available memory.
    /// Requests to allocate memory blocks larger than the limit for a fixed-size heap do not automatically fail;
    /// instead, the system calls the VirtualAlloc function to obtain the memory that is needed for large blocks.
    /// Applications that need to allocate large memory blocks should set dwMaximumSize to 0.
    /// </param>
    /// <returns>
    /// If the function succeeds, the return value is a handle to the newly created heap.
    /// 
    /// If the function fails, the return value is NULL. To get extended error information, call GetLastError.
    /// </returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint HeapCreate(HeapOptions flOptions, nuint dwInitialSize, nuint dwMaximumSize);
    
    /// <summary>
    /// Allocates a block of memory from a heap. The allocated memory is not movable.
    /// </summary>
    /// <param name="hHeap">
    /// A handle to the heap from which the memory will be allocated. This handle is returned by the HeapCreate or
    /// GetProcessHeap function.
    /// </param>
    /// <param name="dwFlags">
    /// The heap allocation options. Specifying any of these values will override the corresponding value specified when
    /// the heap was created with HeapCreate. </param>
    /// <param name="dwBytes">
    /// The number of bytes to be allocated.
    ///
    /// If the heap specified by the hHeap parameter is a "non-growable" heap, dwBytes must be less than 0x7FFF8.
    /// You create a non-growable heap by calling the HeapCreate function with a nonzero value.
    /// </param>
    /// <returns>
    /// If the function succeeds, the return value is a pointer to the allocated memory block.
    ///
    /// If the function fails and you have not specified HEAP_GENERATE_EXCEPTIONS, the return value is NULL.
    ///
    /// If the function fails and you have specified HEAP_GENERATE_EXCEPTIONS, the function may generate either of the
    /// exceptions listed in the following table. The particular exception depends upon the nature of the heap
    /// corruption. For more information, see GetExceptionCode.
    /// </returns>
    [DllImport("kernel32.dll", SetLastError=false)]
    public static extern nint HeapAlloc(nint hHeap, HeapOptions dwFlags, nuint dwBytes);
    
    /// <summary>
    /// See https://docs.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualalloc.
    /// Reserves, commits, or changes the state of a region of pages in the virtual address space of the calling process.
    /// Memory allocated by this function is automatically initialized to zero. To allocate memory in the address space
    /// of another process, use the VirtualAllocEx function.
    /// </summary>
    /// <param name="lpAddress">
    /// The starting address of the region to allocate. If the memory is being reserved, the specified address is rounded
    /// down to the nearest multiple of the allocation granularity. If the memory is already reserved and is being committed,
    /// the address is rounded down to the next page boundary. To determine the size of a page and the allocation granularity
    /// on the host computer, use the GetSystemInfo function. If this parameter is NULL, the system determines where to
    /// allocate the region. If this address is within an enclave that you have not initialized by calling InitializeEnclave,
    /// VirtualAlloc allocates a page of zeros for the enclave at that address. The page must be previously uncommitted,
    /// and will not be measured with the EEXTEND instruction of the Intel Software Guard Extensions programming model.
    /// If the address in within an enclave that you initialized, then the allocation operation fails with the
    /// ERROR_INVALID_ADDRESS error.
    /// </param>
    /// <param name="dwSize">
    /// The size of the region, in bytes. If the lpAddress parameter is NULL, this value is rounded up to the next page
    /// boundary. Otherwise, the allocated pages include all pages containing one or more bytes in the range from lpAddress
    /// to lpAddress+dwSize. This means that a 2-byte range straddling a page boundary causes both pages to be included
    /// in the allocated region.
    /// </param>
    /// <param name="flAllocationType">
    /// The type of memory allocation. This parameter must contain one of the MEM_* enum values.
    /// </param>
    /// <param name="flProtect">
    /// The memory protection for the region of pages to be allocated. If the pages are being committed, you can specify
    /// any one of the memory protection constants.
    /// </param>
    /// <returns>
    /// If the function succeeds, the return value is the base address of the allocated region of pages. If the function
    /// fails, the return value is NULL.To get extended error information, call GetLastError.
    /// </returns>
    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    public static extern IntPtr VirtualAlloc(
        IntPtr lpAddress,
        UIntPtr dwSize,
        AllocationType flAllocationType,
        MemoryProtection flProtect);

    /// <inheritdoc cref="VirtualAlloc(IntPtr, UIntPtr, AllocationType, MemoryProtection)"/>
    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    public static extern IntPtr VirtualAlloc(
        IntPtr lpAddress,
        UIntPtr dwSize,
        AllocationType flAllocationType,
        Memory.MemoryProtection flProtect);

    /// <summary>
    /// See https://docs.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualfree.
    /// Releases, decommits, or releases and decommits a region of pages within the virtual address space of the calling
    /// process.
    /// process.
    /// </summary>
    /// <param name="lpAddress">
    /// A pointer to the base address of the region of pages to be freed. If the dwFreeType parameter is MEM_RELEASE, this
    /// parameter must be the base address returned by the VirtualAlloc function when the region of pages is reserved.
    /// </param>
    /// <param name="dwSize">
    /// The size of the region of memory to be freed, in bytes. If the dwFreeType parameter is MEM_RELEASE, this parameter
    /// must be 0 (zero). The function frees the entire region that is reserved in the initial allocation call to VirtualAlloc.
    /// If the dwFreeType parameter is MEM_DECOMMIT, the function decommits all memory pages that contain one or more bytes
    /// in the range from the lpAddress parameter to (lpAddress+dwSize). This means, for example, that a 2-byte region of
    /// memory that straddles a page boundary causes both pages to be decommitted.If lpAddress is the base address returned
    /// by VirtualAlloc and dwSize is 0 (zero), the function decommits the entire region that is allocated by VirtualAlloc.
    /// After that, the entire region is in the reserved state.
    /// </param>
    /// <param name="dwFreeType">
    /// The type of free operation. This parameter must be one of the MEM_* enum values.
    /// </param>
    /// <returns>
    /// If the function succeeds, the return value is a nonzero value. If the function fails, the return value is 0 (zero).
    /// To get extended error information, call GetLastError.
    /// </returns>
    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    public static extern bool VirtualFree(
        IntPtr lpAddress,
        UIntPtr dwSize,
        AllocationType dwFreeType);

    /// <summary>
    /// See https://docs.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualprotect.
    /// Changes the protection on a region of committed pages in the virtual address space of the calling process.
    /// </summary>
    /// <param name="lpAddress">
    /// The address of the starting page of the region of pages whose access protection attributes are to be changed. All
    /// pages in the specified region must be within the same reserved region allocated when calling the VirtualAlloc or
    /// VirtualAllocEx function using MEM_RESERVE. The pages cannot span adjacent reserved regions that were allocated by
    /// separate calls to VirtualAlloc or VirtualAllocEx using MEM_RESERVE.
    /// </param>
    /// <param name="dwSize">
    /// The size of the region whose access protection attributes are to be changed, in bytes. The region of affected pages
    /// includes all pages containing one or more bytes in the range from the lpAddress parameter to (lpAddress+dwSize).
    /// This means that a 2-byte range straddling a page boundary causes the protection attributes of both pages to be changed.
    /// </param>
    /// <param name="flNewProtection">
    /// The memory protection option. This parameter can be one of the memory protection constants. For mapped views, this
    /// value must be compatible with the access protection specified when the view was mapped (see MapViewOfFile,
    /// MapViewOfFileEx, and MapViewOfFileExNuma).
    /// </param>
    /// <param name="lpflOldProtect">
    /// A pointer to a variable that receives the previous access protection value of the first page in the specified region
    /// of pages. If this parameter is NULL or does not point to a valid variable, the function fails.
    /// </param>
    /// <returns>
    /// If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.
    /// To get extended error information, call GetLastError.
    /// </returns>
    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    public static extern bool VirtualProtect(
        IntPtr lpAddress,
        UIntPtr dwSize,
        MemoryProtection flNewProtection,
        out MemoryProtection lpflOldProtect);

    /// <inheritdoc cref="VirtualAlloc(IntPtr, UIntPtr, AllocationType, MemoryProtection)"/>
    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    public static extern bool VirtualProtect(
        IntPtr lpAddress,
        UIntPtr dwSize,
        Memory.MemoryProtection flNewProtection,
        out Memory.MemoryProtection lpflOldProtect);

    /// <summary>
    /// Writes data to an area of memory in a specified process. The entire area to be written to must be accessible or
    /// the operation fails.
    /// </summary>
    /// <param name="hProcess">
    /// A handle to the process memory to be modified. The handle must have PROCESS_VM_WRITE and PROCESS_VM_OPERATION access
    /// to the process.
    /// </param>
    /// <param name="lpBaseAddress">
    /// A pointer to the base address in the specified process to which data is written. Before data transfer occurs, the
    /// system verifies that all data in the base address and memory of the specified size is accessible for write access,
    /// and if it is not accessible, the function fails.
    /// </param>
    /// <param name="lpBuffer">
    /// A pointer to the buffer that contains data to be written in the address space of the specified process.
    /// </param>
    /// <param name="dwSize">
    /// The number of bytes to be written to the specified process.
    /// </param>
    /// <param name="lpNumberOfBytesWritten">
    /// A pointer to a variable that receives the number of bytes transferred into the specified process. This parameter
    /// is optional. If lpNumberOfBytesWritten is NULL, the parameter is ignored.
    /// </param>
    /// <returns>
    /// If the function succeeds, the return value is nonzero. If the function fails, the return value is 0 (zero). To get
    /// extended error information, call GetLastError.The function fails if the requested write operation crosses into an
    /// area of the process that is inaccessible.
    /// </returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        byte[] lpBuffer,
        int dwSize,
        out IntPtr lpNumberOfBytesWritten);

    /// <summary>
    /// Get a handle to the current process.
    /// </summary>
    /// <returns>Handle to the process.</returns>
    [DllImport("kernel32.dll")]
    public static extern IntPtr GetCurrentProcess();

    /// <summary>
    /// Get the current process ID.
    /// </summary>
    /// <returns>The process ID.</returns>
    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentProcessId();

    /// <summary>
    /// Get the current thread ID.
    /// </summary>
    /// <returns>The thread ID.</returns>
    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();
}

/// <summary>
/// Native dbghelp functions.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Native funcs")]
internal static partial class NativeFunctions
{
    /// <summary>
    /// Type of minidump to create.
    /// </summary>
    public enum MiniDumpType : int
    {
        /// <summary>
        /// Normal minidump.
        /// </summary>
        MiniDumpNormal,

        /// <summary>
        /// Minidump with data segments.
        /// </summary>
        MiniDumpWithDataSegs,

        /// <summary>
        /// Minidump with full memory.
        /// </summary>
        MiniDumpWithFullMemory,
    }

    /// <summary>
    /// Initializes the symbol handler for a process.
    /// </summary>
    /// <param name="hProcess">
    /// A handle that identifies the caller.
    /// This value should be unique and nonzero, but need not be a process handle.
    /// However, if you do use a process handle, be sure to use the correct handle.
    /// If the application is a debugger, use the process handle for the process being debugged.
    /// Do not use the handle returned by GetCurrentProcess when debugging another process, because calling functions like SymLoadModuleEx can have unexpected results.
    /// This parameter cannot be NULL.</param>
    /// <param name="userSearchPath">
    /// The path, or series of paths separated by a semicolon (;), that is used to search for symbol files.
    /// If this parameter is NULL, the library attempts to form a symbol path from the following sources:
    ///    - The current working directory of the application
    ///    - The _NT_SYMBOL_PATH environment variable
    ///    - The _NT_ALTERNATE_SYMBOL_PATH environment variable
    /// Note that the search path can also be set using the SymSetSearchPath function.
    /// </param>
    /// <param name="fInvadeProcess">
    /// If this value is <see langword="true"/>, enumerates the loaded modules for the process and effectively calls the SymLoadModule64 function for each module.
    /// </param>
    /// <returns>Whether or not the function succeeded.</returns>
    [DllImport("dbghelp.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool SymInitialize(IntPtr hProcess, string userSearchPath, bool fInvadeProcess);

    /// <summary>
    /// Deallocates all resources associated with the process handle.
    /// </summary>
    /// <param name="hProcess">A handle to the process that was originally passed to the <seealso cref="SymInitialize"/> function.</param>
    /// <returns>Whether or not the function succeeded.</returns>
    [DllImport("dbghelp.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool SymCleanup(IntPtr hProcess);

    /// <summary>
    /// Creates a minidump.
    /// </summary>
    /// <param name="hProcess">Target process handle.</param>
    /// <param name="processId">Target process ID.</param>
    /// <param name="hFile">Output file handle.</param>
    /// <param name="dumpType">Type of dump to take.</param>
    /// <param name="exceptionInfo">Exception information.</param>
    /// <param name="userStreamParam">User information.</param>
    /// <param name="callback">Callback.</param>
    /// <returns>Whether or not the minidump succeeded.</returns>
    [DllImport("dbghelp.dll")]
    public static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, IntPtr hFile, int dumpType, ref MinidumpExceptionInformation exceptionInfo, IntPtr userStreamParam, IntPtr callback);

    /// <summary>
    /// Structure describing minidump exception information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MinidumpExceptionInformation
    {
        /// <summary>
        /// ID of the thread that caused the exception.
        /// </summary>
        public uint ThreadId;

        /// <summary>
        /// Pointer to the exception record.
        /// </summary>
        public IntPtr ExceptionPointers;

        /// <summary>
        /// ClientPointers field.
        /// </summary>
        public int ClientPointers;
    }

    /// <summary>
    /// Finds window according to conditions.
    /// </summary>
    /// <param name="parentHandle">Handle to parent window.</param>
    /// <param name="childAfter">Window to search after.</param>
    /// <param name="className">Name of class.</param>
    /// <param name="windowTitle">Name of window.</param>
    /// <returns>Found window, or null.</returns>
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindowEx(
        IntPtr parentHandle,
        IntPtr childAfter,
        string className,
        IntPtr windowTitle);
}

/// <summary>
/// Native ws2_32 functions.
/// </summary>
internal static partial class NativeFunctions
{
    /// <summary>
    /// See https://docs.microsoft.com/en-us/windows/win32/api/winsock/nf-winsock-setsockopt.
    /// The setsockopt function sets a socket option.
    /// </summary>
    /// <param name="socket">
    /// A descriptor that identifies a socket.
    /// </param>
    /// <param name="level">
    /// The level at which the option is defined (for example, SOL_SOCKET).
    /// </param>
    /// <param name="optName">
    /// The socket option for which the value is to be set (for example, SO_BROADCAST). The optname parameter must be a
    /// socket option defined within the specified level, or behavior is undefined.
    /// </param>
    /// <param name="optVal">
    /// A pointer to the buffer in which the value for the requested option is specified.
    /// </param>
    /// <param name="optLen">
    /// The size, in bytes, of the buffer pointed to by the optval parameter.
    /// </param>
    /// <returns>
    /// If no error occurs, setsockopt returns zero. Otherwise, a value of SOCKET_ERROR is returned, and a specific error
    /// code can be retrieved by calling WSAGetLastError.
    /// </returns>
    [DllImport("ws2_32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "setsockopt")]
    public static extern int SetSockOpt(IntPtr socket, SocketOptionLevel level, SocketOptionName optName, ref IntPtr optVal, int optLen);
}

/// <summary>
/// Native dwmapi functions.
/// </summary>
internal static partial class NativeFunctions
{
    /// <summary>
    /// Attributes for use with DwmSetWindowAttribute.
    /// </summary>
    public enum DWMWINDOWATTRIBUTE : int
    {
        /// <summary>
        /// Allows the window frame for this window to be drawn in dark mode colors when the dark mode system setting is enabled.
        /// </summary>
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
    }

    /// <summary>
    /// Sets the value of Desktop Window Manager (DWM) non-client rendering attributes for a window.
    /// </summary>
    /// <param name="hwnd">The handle to the window for which the attribute value is to be set.</param>
    /// <param name="attr">The attribute to be set.</param>
    /// <param name="attrValue">The value of the attribute.</param>
    /// <param name="attrSize">The size of the attribute.</param>
    /// <returns>HRESULT.</returns>
    [DllImport("dwmapi.dll", PreserveSig = true)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attr, ref int attrValue, int attrSize);
}
