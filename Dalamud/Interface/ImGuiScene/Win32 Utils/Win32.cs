using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ImGuiScene
{
    // Even though we are importing PInvoke stuff from Nuget, we still need this class
    // for some APIs that do not seem to be exposed in any way through those packages.
    // In the future, we may be able to use https://github.com/microsoft/cswin32
    internal class Win32
    {
        public enum ImeCommand
        {
            IMN_CLOSESTATUSWINDOW = 0x0001,
            IMN_OPENSTATUSWINDOW = 0x0002,
            IMN_CHANGECANDIDATE = 0x0003,
            IMN_CLOSECANDIDATE = 0x0004,
            IMN_OPENCANDIDATE = 0x0005,
            IMN_SETCONVERSIONMODE = 0x0006,
            IMN_SETSENTENCEMODE = 0x0007,
            IMN_SETOPENSTATUS = 0x0008,
            IMN_SETCANDIDATEPOS = 0x0009,
            IMN_SETCOMPOSITIONFONT = 0x000A,
            IMN_SETCOMPOSITIONWINDOW = 0x000B,
            IMN_SETSTATUSWINDOWPOS = 0x000C,
            IMN_GUIDELINE = 0x000D,
            IMN_PRIVATE = 0x000E
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int X, int Y)
            {
                this.X = X;
                this.Y = Y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct COMPOSITIONFORM
        {
            public uint dwStyle;
            public POINT ptCurrentPos;
            public RECT rcArea;

            public COMPOSITIONFORM(uint dwStyle, POINT ptCurrentPos, RECT rcArea)
            {
                this.dwStyle = dwStyle;
                this.ptCurrentPos = ptCurrentPos;
                this.rcArea = rcArea;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort HIWORD(ulong val)
        {
            // #define HIWORD(l)  ((WORD)((((DWORD_PTR)(l)) >> 16) & 0xffff))
            return (ushort)((val >> 16) & 0xFFFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort LOWORD(ulong val)
        {
            // #define LOWORD(l)  ((WORD)(((DWORD_PTR)(l)) & 0xffff))
            return (ushort)(val & 0xFFFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GET_XBUTTON_WPARAM(ulong val)
        {
            // #define GET_XBUTTON_WPARAM(wParam)  (HIWORD(wParam))
            return HIWORD(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short GET_WHEEL_DELTA_WPARAM(ulong val)
        {
            // #define GET_WHEEL_DELTA_WPARAM(wParam)  ((short)HIWORD(wParam))
            return (short)HIWORD(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GET_X_LPARAM(ulong val)
        {
            // #define GET_X_LPARAM(lp)  ((int)(short)LOWORD(lp))
            return (int)(short)LOWORD(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GET_Y_LPARAM(ulong val)
        {
            // #define GET_Y_LPARAM(lp)  ((int)(short)HIWORD(lp))
            return (int)(short)HIWORD(val);
        }

        [DllImport("dwmapi.dll")]
        public static extern int DwmIsCompositionEnabled(out bool enabled);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool BringWindowToTop(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetFocus(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
        // [DllImport("Imm32.dll", SetLastError=true)]
        // public static extern IntPtr ImmGetContext(IntPtr hWnd);
        // [DllImport("Imm32.dll", SetLastError=true)]
        // public static extern bool ImmSetCompositionWindow(IntPtr hImc, ref COMPOSITIONFORM lpCompForm);
        // [DllImport("Imm32.dll", SetLastError=true)]
        // public static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hImc);


        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);
        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
        [DllImport("user32.dll")]
        public static extern IntPtr GetCapture();
        [DllImport("user32.dll")]
        public static extern IntPtr SetCapture(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern short GetKeyState(VirtualKey nVirtKey);
        [DllImport("user32.dll")]
        public static extern IntPtr GetCursor();
        [DllImport("user32.dll")]
        public static extern IntPtr SetCursor(IntPtr handle);
        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursor(IntPtr hInstance, Cursor lpCursorName);
        [DllImport("user32.dll")]
        public static extern int ShowCursor(bool bShow);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, WindowLongType nIndex, IntPtr dwNewLong);
        [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
        public static extern long CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, ulong wParam, long lParam);

        [DllImport("user32.dll", EntryPoint = "GetCursorInfo")]
        private static extern bool GetCursorInfo_Internal(ref CURSORINFO pci);

        public static bool GetCursorInfo(out CURSORINFO pci)
        {
            pci = new CURSORINFO
            {
                cbSize = Marshal.SizeOf(typeof(CURSORINFO))
            };

            return GetCursorInfo_Internal(ref pci);
        }
    }
}
