using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Interface.Internal;
using Dalamud.Logging.Internal;
using ImGuiNET;

using static Dalamud.NativeFunctions;

namespace Dalamud.Game.Gui.Internal
{
    /// <summary>
    /// This class handles IME for non-English users.
    /// </summary>
    internal class DalamudIME : IDisposable
    {
        private static readonly ModuleLog Log = new("IME");

        private IntPtr interfaceHandle;
        private IntPtr wndProcPtr;
        private IntPtr oldWndProcPtr;
        private WndProcDelegate wndProcDelegate;

        /// <summary>
        /// Initializes a new instance of the <see cref="DalamudIME"/> class.
        /// </summary>
        internal DalamudIME()
        {
        }

        private delegate long WndProcDelegate(IntPtr hWnd, uint msg, ulong wParam, long lParam);

        /// <summary>
        /// Gets a value indicating whether the module is enabled.
        /// </summary>
        internal bool IsEnabled { get; private set; }

        /// <summary>
        /// Gets the imm candidates.
        /// </summary>
        internal List<string> ImmCand { get; private set; } = new();

        /// <summary>
        /// Gets the imm component.
        /// </summary>
        internal string ImmComp { get; private set; } = string.Empty;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.oldWndProcPtr != IntPtr.Zero)
            {
                SetWindowLongPtrW(this.interfaceHandle, WindowLongType.WndProc, this.oldWndProcPtr);
                this.oldWndProcPtr = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Enables the IME module.
        /// </summary>
        internal void Enable()
        {
            try
            {
                this.wndProcDelegate = this.WndProcDetour;
                this.interfaceHandle = Service<InterfaceManager>.Get().WindowHandlePtr;
                this.wndProcPtr = Marshal.GetFunctionPointerForDelegate(this.wndProcDelegate);
                this.oldWndProcPtr = SetWindowLongPtrW(this.interfaceHandle, WindowLongType.WndProc, this.wndProcPtr);
                this.IsEnabled = true;
                Log.Information("Enabled!");
            }
            catch (Exception ex)
            {
                Log.Information(ex, "Enable failed");
            }
        }

        private void ToggleWindow(bool visible)
        {
            if (visible)
                Service<DalamudInterface>.Get().OpenIMEWindow();
            else
                Service<DalamudInterface>.Get().CloseIMEWindow();
        }

        private long WndProcDetour(IntPtr hWnd, uint msg, ulong wParam, long lParam)
        {
            try
            {
                if (hWnd == this.interfaceHandle && ImGui.GetCurrentContext() != IntPtr.Zero && ImGui.GetIO().WantTextInput)
                {
                    var io = ImGui.GetIO();
                    var wmsg = (WindowsMessage)msg;

                    switch (wmsg)
                    {
                        case WindowsMessage.WM_IME_NOTIFY:
                            switch ((IMECommand)wParam)
                            {
                                case IMECommand.ChangeCandidate:
                                    this.ToggleWindow(true);

                                    if (hWnd == IntPtr.Zero)
                                        return 0;

                                    var hIMC = ImmGetContext(hWnd);
                                    if (hIMC == IntPtr.Zero)
                                        return 0;

                                    var size = ImmGetCandidateListW(hIMC, 0, IntPtr.Zero, 0);
                                    if (size > 0)
                                    {
                                        var candlistPtr = Marshal.AllocHGlobal((int)size);
                                        size = ImmGetCandidateListW(hIMC, 0, candlistPtr, (uint)size);

                                        var candlist = Marshal.PtrToStructure<CandidateList>(candlistPtr);
                                        var pageSize = candlist.PageSize;
                                        var candCount = candlist.Count;
                                        if (pageSize > 0 && candCount > 1)
                                        {
                                            var dwOffsets = new int[candCount];
                                            for (var i = 0; i < candCount; i++)
                                                dwOffsets[i] = Marshal.ReadInt32(candlistPtr + ((i + 6) * sizeof(int)));

                                            var pageStart = candlist.PageStart;
                                            // var pageEnd = pageStart + pageSize;

                                            var cand = new string[pageSize];
                                            this.ImmCand.Clear();

                                            for (var i = 0; i < pageSize; i++)
                                            {
                                                var offStart = dwOffsets[i + pageStart];
                                                var offEnd = i + pageStart + 1 < candCount ? dwOffsets[i + pageStart + 1] : size;

                                                var pStrStart = candlistPtr + (int)offStart;
                                                var pStrEnd = candlistPtr + (int)offEnd;

                                                var len = (int)(pStrEnd.ToInt64() - pStrStart.ToInt64());
                                                if (len > 0)
                                                {
                                                    var candBytes = new byte[len];
                                                    Marshal.Copy(pStrStart, candBytes, 0, len);

                                                    var candStr = Encoding.Unicode.GetString(candBytes);
                                                    cand[i] = candStr;

                                                    this.ImmCand.Add(candStr);
                                                }
                                            }
                                        }

                                        Marshal.FreeHGlobal(candlistPtr);
                                    }

                                    break;
                                case IMECommand.OpenCandidate:
                                    this.ToggleWindow(true);
                                    this.ImmCand.Clear();
                                    break;
                                case IMECommand.CloseCandidate:
                                    this.ToggleWindow(false);
                                    this.ImmCand.Clear();
                                    break;
                                default:
                                    break;
                            }

                            break;
                        case WindowsMessage.WM_IME_COMPOSITION:
                            if ((lParam & (long)IMEComposition.ResultStr) > 0)
                            {
                                var hIMC = ImmGetContext(hWnd);
                                if (hIMC == IntPtr.Zero)
                                    return 0;

                                var dwSize = ImmGetCompositionStringW(hIMC, IMEComposition.ResultStr, IntPtr.Zero, 0);
                                var unmanagedPointer = Marshal.AllocHGlobal((int)dwSize);
                                ImmGetCompositionStringW(hIMC, IMEComposition.ResultStr, unmanagedPointer, (uint)dwSize);

                                var bytes = new byte[dwSize];
                                Marshal.Copy(unmanagedPointer, bytes, 0, (int)dwSize);
                                Marshal.FreeHGlobal(unmanagedPointer);

                                var lpstr = Encoding.Unicode.GetString(bytes);
                                io.AddInputCharactersUTF8(lpstr);

                                this.ImmComp = string.Empty;
                                this.ImmCand.Clear();
                                this.ToggleWindow(false);
                            }

                            if (((long)(IMEComposition.CompStr | IMEComposition.CompAttr | IMEComposition.CompClause |
                                IMEComposition.CompReadAttr | IMEComposition.CompReadClause | IMEComposition.CompReadStr) & lParam) > 0)
                            {
                                var hIMC = ImmGetContext(hWnd);
                                if (hIMC == IntPtr.Zero)
                                    return 0;

                                var dwSize = ImmGetCompositionStringW(hIMC, IMEComposition.CompStr, IntPtr.Zero, 0);
                                var unmanagedPointer = Marshal.AllocHGlobal((int)dwSize);
                                ImmGetCompositionStringW(hIMC, IMEComposition.CompStr, unmanagedPointer, (uint)dwSize);

                                var bytes = new byte[dwSize];
                                Marshal.Copy(unmanagedPointer, bytes, 0, (int)dwSize);
                                Marshal.FreeHGlobal(unmanagedPointer);

                                var lpstr = Encoding.Unicode.GetString(bytes);
                                this.ImmComp = lpstr;
                                if (lpstr == string.Empty)
                                    this.ToggleWindow(false);
                            }

                            break;

                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Prevented a crash in an IME hook");
            }

            return CallWindowProcW(this.oldWndProcPtr, hWnd, msg, wParam, lParam);
        }
    }
}
