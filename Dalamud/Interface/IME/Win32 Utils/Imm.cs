using System;
using System.Runtime.InteropServices;

namespace Dalamud.Interface.IME.Win32_Utils
{
    class Imm
    {

        [DllImport("imm32.dll")]
        public static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("imm32.dll", EntryPoint = "ImmGetCompositionStringW")]
        public static extern long ImmGetCompositionString(IntPtr hImc, uint arg2, IntPtr lpBuf, uint dwBufLen);

        [DllImport("imm32.dll", EntryPoint = "ImmGetCandidateListW")]
        public static extern long ImmGetCandidateList(IntPtr hImc, uint arg2, IntPtr lpCandList, uint dwBufLen);

        [DllImport("imm32.dll", CharSet = CharSet.Auto)]
        public static extern bool ImmSetCompositionWindow(IntPtr hImc, ref CompositionForm frm);

        [DllImport("imm32.dll", CharSet = CharSet.Auto)]
        public static extern bool ImmReleaseContext(IntPtr hwnd, IntPtr hImc);

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct CompositionForm
        {
            public int Style;
            public Point CurrentPos;
            public Rect Area;
        }
        public struct CandidateList
        {
            public int dwSize;
            public int dwStyle;
            public int dwCount;
            public int dwSelection;
            public int dwPageStart;
            public int dwPageSize;
            // public IntPtr dwOffset; // manually handle
        }


    }
}
