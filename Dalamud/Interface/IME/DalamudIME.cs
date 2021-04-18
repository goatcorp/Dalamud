using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;
using Dalamud.Interface.IME.Win32_Utils;
using static Dalamud.Interface.IME.Win32_Utils.Imm;

namespace Dalamud.Interface.IME
{
    class DalamudIME :IDisposable
    {
        private Dalamud dalamud;
        internal List<string> ImmCand = new List<string>();
        internal string ImmComp = String.Empty;
        internal bool uiVisible = false;
        internal bool UIVisible
        {
            get => this.uiVisible;
            set
            {
                this.uiVisible = value;
                this.dalamud.DalamudUi.OpenIMEPanel(value);
            }
        }
        private IntPtr _hWnd;
        delegate long WndProcDelegate(IntPtr hWnd, uint msg, ulong wParam, long lParam);
        private WndProcDelegate _wndProcDelegate;
        private IntPtr _wndProcPtr;
        private IntPtr _oldWndProcPtr;
        internal DalamudIMEWindow IMEWindow;
        public DalamudIME(Dalamud dalamud)
        {
            this.dalamud = dalamud;
            this._hWnd = dalamud.InterfaceManager.WindowHandlePtr;
            InitializeWndProc();
        }

        public void Dispose()
        {
            if (_oldWndProcPtr != IntPtr.Zero)
            {
                Win32.SetWindowLongPtr(_hWnd, WindowLongType.GWL_WNDPROC, _oldWndProcPtr);
                _oldWndProcPtr = IntPtr.Zero;
            }
        }

        #region WndProc
        void InitializeWndProc()
        {
            _wndProcDelegate = WndProcDetour;
            _wndProcPtr = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
            _oldWndProcPtr = Win32.SetWindowLongPtr(_hWnd, WindowLongType.GWL_WNDPROC, _wndProcPtr);
        }

        private long WndProcDetour(IntPtr hWnd, uint msg, ulong wParam, long lParam)
        {
            if (hWnd == _hWnd && ImGui.GetCurrentContext() != IntPtr.Zero && ImGui.GetIO().WantTextInput)
            {
                var io = ImGui.GetIO();
                var wmsg = (WindowsMessage)msg;

                switch (wmsg)
                {
                    case WindowsMessage.WM_IME_NOTIFY:
                        switch ((IMECommand)wParam)
                        {
                            case IMECommand.IMN_CHANGECANDIDATE:
                                this.UIVisible = true;
                                if (hWnd == IntPtr.Zero)
                                    return 0;
                                var hIMC = Imm.ImmGetContext(hWnd);
                                if (hIMC == IntPtr.Zero)
                                    return 0;
                                var size = Imm.ImmGetCandidateList(hIMC, 0, IntPtr.Zero, 0);
                                if (size > 0)
                                {
                                    IntPtr candlistPtr = Marshal.AllocHGlobal((int)size);
                                    size = Imm.ImmGetCandidateList(hIMC, 0, candlistPtr, (uint)size);
                                    CandidateList candlist = Marshal.PtrToStructure<CandidateList>(candlistPtr);
                                    var pageSize = candlist.dwPageSize;
                                    int candCount = candlist.dwCount;
                                    if (pageSize > 0 && candCount > 1)
                                    {
                                        int[] dwOffsets = new int[candCount];
                                        for (int i = 0; i < candCount; i++)
                                            dwOffsets[i] = Marshal.ReadInt32(candlistPtr + (i + 6) * sizeof(int));

                                        int pageStart = candlist.dwPageStart;
                                        int pageEnd = pageStart + pageSize;

                                        string[] cand = new string[pageSize];
                                        this.ImmCand.Clear();
                                        for (int i = 0; i < pageSize; i++)
                                        {
                                            var offStart = dwOffsets[i + pageStart];
                                            var offEnd = i + pageStart + 1 < candCount ? dwOffsets[i + pageStart + 1] : size;
                                            IntPtr pStrStart = candlistPtr + (int)offStart;
                                            IntPtr pStrEnd = candlistPtr + (int)offEnd;
                                            int len = (int)(pStrEnd.ToInt64() - pStrStart.ToInt64());
                                            if (len > 0)
                                            {
                                                var candBytes = new byte[len];
                                                Marshal.Copy(pStrStart, candBytes, 0, len);
                                                string candStr = Encoding.Unicode.GetString(candBytes);
                                                cand[i] = candStr;
                                                this.ImmCand.Add(candStr);
                                            }
                                        }
                                    }
                                    Marshal.FreeHGlobal(candlistPtr);
                                }
                                break;
                            case IMECommand.IMN_OPENCANDIDATE:
                                this.UIVisible = true;
                                this.ImmCand.Clear();
                                break;
                            case IMECommand.IMN_CLOSECANDIDATE:
                                this.UIVisible = false;
                                this.ImmCand.Clear();
                                break;
                            default:
                                break;
                        }
                        break;
                    case WindowsMessage.WM_IME_COMPOSITION:
                        if ((lParam & (long)IMEComposition.GCS_RESULTSTR) > 0)
                        {
                            var hIMC = Imm.ImmGetContext(hWnd);
                            if (hIMC == IntPtr.Zero)
                                return 0;
                            var dwSize = Imm.ImmGetCompositionString(hIMC, (uint)IMEComposition.GCS_RESULTSTR, IntPtr.Zero, 0);
                            IntPtr unmanagedPointer = Marshal.AllocHGlobal((int)dwSize);
                            Imm.ImmGetCompositionString(hIMC, (uint)IMEComposition.GCS_RESULTSTR, unmanagedPointer, (uint)dwSize);
                            byte[] bytes = new byte[dwSize];
                            Marshal.Copy(unmanagedPointer, bytes, 0, (int)dwSize);
                            Marshal.FreeHGlobal(unmanagedPointer);
                            string lpstr = Encoding.Unicode.GetString(bytes);
                            io.AddInputCharactersUTF8(lpstr);
                            this.ImmComp = string.Empty;
                            this.ImmCand.Clear();
                            this.UIVisible = false;
                        }
                        if (((long)(IMEComposition.GCS_COMPSTR | IMEComposition.GCS_COMPATTR | IMEComposition.GCS_COMPCLAUSE |
                            IMEComposition.GCS_COMPREADATTR | IMEComposition.GCS_COMPREADCLAUSE | IMEComposition.GCS_COMPREADSTR) & lParam) > 0)
                        {
                            var hIMC = Imm.ImmGetContext(hWnd);
                            if (hIMC == IntPtr.Zero)
                                return 0;
                            var dwSize = Imm.ImmGetCompositionString(hIMC, (uint)IMEComposition.GCS_COMPSTR, IntPtr.Zero, 0);
                            IntPtr unmanagedPointer = Marshal.AllocHGlobal((int)dwSize);
                            Imm.ImmGetCompositionString(hIMC, (uint)IMEComposition.GCS_COMPSTR, unmanagedPointer, (uint)dwSize);
                            byte[] bytes = new byte[dwSize];
                            Marshal.Copy(unmanagedPointer, bytes, 0, (int)dwSize);
                            Marshal.FreeHGlobal(unmanagedPointer);
                            string lpstr = Encoding.Unicode.GetString(bytes);
                            this.ImmComp = lpstr;
                            if (lpstr == string.Empty)
                                this.UIVisible = false;
                        }
                        break;

                    default:
                        break;
                }
            }

            return Win32.CallWindowProc(_oldWndProcPtr, hWnd, msg, wParam, lParam);
        }
        #endregion
    }
}
