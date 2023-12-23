using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

using Dalamud.Game.Text;
using Dalamud.Hooking.WndProcHook;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Utility;

using ImGuiNET;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Internal;

/// <summary>
/// This class handles CJK IME.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class DalamudIme : IDisposable, IServiceType
{
    private const int ImGuiContextTextStateOffset = 0x4588;
    private const int CImGuiStbTextCreateUndoOffset = 0xB57A0;
    private const int CImGuiStbTextUndoOffset = 0xB59C0;

    private static readonly UnicodeRange[] HanRange =
    {
        UnicodeRanges.CjkRadicalsSupplement,
        UnicodeRanges.CjkSymbolsandPunctuation,
        UnicodeRanges.CjkUnifiedIdeographsExtensionA,
        UnicodeRanges.CjkUnifiedIdeographs,
        UnicodeRanges.CjkCompatibilityIdeographs,
        UnicodeRanges.CjkCompatibilityForms,
        // No more; Extension B~ are outside BMP range
    };

    private static readonly UnicodeRange[] HangulRange =
    {
        UnicodeRanges.HangulJamo,
        UnicodeRanges.HangulSyllables,
        UnicodeRanges.HangulCompatibilityJamo,
        UnicodeRanges.HangulJamoExtendedA,
        UnicodeRanges.HangulJamoExtendedB,
    };

    private static readonly delegate* unmanaged<ImGuiInputTextState*, StbTextEditState*, int, int, int, void>
        StbTextMakeUndoReplace;

    private static readonly delegate* unmanaged<ImGuiInputTextState*, StbTextEditState*, void> StbTextUndo;

    private readonly ImGuiSetPlatformImeDataDelegate setPlatformImeDataDelegate;

    private (int Start, int End, int Cursor)? temporaryUndoSelection;

    [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1003:Symbols should be spaced correctly", Justification = ".")]
    static DalamudIme()
    {
        nint cimgui;
        try
        {
            _ = ImGui.GetCurrentContext();

            cimgui = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
                            .First(x => x.ModuleName == "cimgui.dll")
                            .BaseAddress;
        }
        catch
        {
            return;
        }

        StbTextMakeUndoReplace =
            (delegate* unmanaged<ImGuiInputTextState*, StbTextEditState*, int, int, int, void>)
            (cimgui + CImGuiStbTextCreateUndoOffset);
        StbTextUndo =
            (delegate* unmanaged<ImGuiInputTextState*, StbTextEditState*, void>)
            (cimgui + CImGuiStbTextUndoOffset);
    }

    [ServiceManager.ServiceConstructor]
    private DalamudIme() => this.setPlatformImeDataDelegate = this.ImGuiSetPlatformImeData;

    /// <summary>
    /// Finalizes an instance of the <see cref="DalamudIme"/> class.
    /// </summary>
    ~DalamudIme() => this.ReleaseUnmanagedResources();

    private delegate void ImGuiSetPlatformImeDataDelegate(ImGuiViewportPtr viewport, ImGuiPlatformImeDataPtr data);

    /// <summary>
    /// Gets a value indicating whether Han(Chinese) input has been detected.
    /// </summary>
    public bool EncounteredHan { get; private set; }

    /// <summary>
    /// Gets a value indicating whether Hangul(Korean) input has been detected.
    /// </summary>
    public bool EncounteredHangul { get; private set; }

    /// <summary>
    /// Gets a value indicating whether to display the cursor in input text. This also deals with blinking.
    /// </summary>
    internal static bool ShowCursorInInputText
    {
        get
        {
            if (!ImGuiHelpers.IsImGuiInitialized)
                return true;
            if (!ImGui.GetIO().ConfigInputTextCursorBlink)
                return true;
            var textState = TextState;
            if (textState->Id == 0 || (textState->Flags & ImGuiInputTextFlags.ReadOnly) != 0)
                return true;
            if (textState->CursorAnim <= 0)
                return true;
            return textState->CursorAnim % 1.2f <= 0.8f;
        }
    }

    /// <summary>
    /// Gets the cursor position, in screen coordinates.
    /// </summary>
    internal Vector2 CursorPos { get; private set; }

    /// <summary>
    /// Gets the associated viewport.
    /// </summary>
    internal ImGuiViewportPtr AssociatedViewport { get; private set; }

    /// <summary>
    /// Gets the index of the first imm candidate in relation to the full list.
    /// </summary>
    internal CANDIDATELIST ImmCandNative { get; private set; }

    /// <summary>
    /// Gets the imm candidates.
    /// </summary>
    internal List<string> ImmCand { get; private set; } = new();

    /// <summary>
    /// Gets the selected imm component.
    /// </summary>
    internal string ImmComp { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the partial conversion from-range.
    /// </summary>
    internal int PartialConversionFrom { get; private set; }

    /// <summary>
    /// Gets the partial conversion to-range.
    /// </summary>
    internal int PartialConversionTo { get; private set; }

    /// <summary>
    /// Gets the cursor offset in the composition string.
    /// </summary>
    internal int CompositionCursorOffset { get; private set; }

    /// <summary>
    /// Gets a value indicating whether to display partial conversion status.
    /// </summary>
    internal bool ShowPartialConversion => this.PartialConversionFrom != 0 ||
                                           this.PartialConversionTo != this.ImmComp.Length;

    /// <summary>
    /// Gets the input mode icon from <see cref="SeIconChar"/>.
    /// </summary>
    internal char InputModeIcon { get; private set; }

    private static ImGuiInputTextState* TextState =>
        (ImGuiInputTextState*)(ImGui.GetCurrentContext() + ImGuiContextTextStateOffset);

    /// <inheritdoc/>
    public void Dispose()
    {
        this.ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Looks for the characters inside <paramref name="str"/> and enables fonts accordingly.
    /// </summary>
    /// <param name="str">The string.</param>
    public void ReflectCharacterEncounters(string str)
    {
        foreach (var chr in str)
        {
            if (HanRange.Any(x => x.FirstCodePoint <= chr && chr < x.FirstCodePoint + x.Length))
            {
                if (Service<GameFontManager>.Get()
                                            .GetFdtReader(GameFontFamilyAndSize.Axis12)
                                            ?.FindGlyph(chr) is null)
                {
                    if (!this.EncounteredHan)
                    {
                        this.EncounteredHan = true;
                        Service<InterfaceManager>.Get().RebuildFonts();
                    }
                }
            }

            if (HangulRange.Any(x => x.FirstCodePoint <= chr && chr < x.FirstCodePoint + x.Length))
            {
                if (!this.EncounteredHangul)
                {
                    this.EncounteredHangul = true;
                    Service<InterfaceManager>.Get().RebuildFonts();
                }
            }
        }
    }

    /// <summary>
    /// Processes window messages.
    /// </summary>
    /// <param name="args">The arguments.</param>
    public void ProcessImeMessage(WndProcEventArgs args)
    {
        if (!ImGuiHelpers.IsImGuiInitialized)
            return;

        // Are we not the target of text input?
        if (!ImGui.GetIO().WantTextInput)
            return;

        var hImc = ImmGetContext(args.Hwnd);
        if (hImc == nint.Zero)
            return;

        try
        {
            var invalidTarget = TextState->Id == 0 || (TextState->Flags & ImGuiInputTextFlags.ReadOnly) != 0;

            switch (args.Message)
            {
                case WM.WM_IME_NOTIFY
                    when (nint)args.WParam is IMN.IMN_OPENCANDIDATE or IMN.IMN_CLOSECANDIDATE
                         or IMN.IMN_CHANGECANDIDATE:
                    this.UpdateImeWindowStatus(hImc);
                    args.SuppressWithValue(0);
                    break;

                case WM.WM_IME_STARTCOMPOSITION:
                    args.SuppressWithValue(0);
                    break;

                case WM.WM_IME_COMPOSITION:
                    if (invalidTarget)
                        ImmNotifyIME(hImc, NI.NI_COMPOSITIONSTR, CPS_CANCEL, 0);
                    else
                        this.ReplaceCompositionString(hImc, (uint)args.LParam);

                    // Log.Verbose($"{nameof(WM.WM_IME_COMPOSITION)}({(nint)args.LParam:X}): {this.ImmComp}");
                    args.SuppressWithValue(0);
                    break;

                case WM.WM_IME_ENDCOMPOSITION:
                    // Log.Verbose($"{nameof(WM.WM_IME_ENDCOMPOSITION)}({(nint)args.WParam:X}, {(nint)args.LParam:X}): {this.ImmComp}");
                    args.SuppressWithValue(0);
                    break;

                case WM.WM_IME_CONTROL:
                    // Log.Verbose($"{nameof(WM.WM_IME_CONTROL)}({(nint)args.WParam:X}, {(nint)args.LParam:X}): {this.ImmComp}");
                    args.SuppressWithValue(0);
                    break;

                case WM.WM_IME_REQUEST:
                    // Log.Verbose($"{nameof(WM.WM_IME_REQUEST)}({(nint)args.WParam:X}, {(nint)args.LParam:X}): {this.ImmComp}");
                    args.SuppressWithValue(0);
                    break;

                case WM.WM_IME_SETCONTEXT:
                    // Hide candidate and composition windows.
                    args.LParam = (LPARAM)((nint)args.LParam & ~(ISC_SHOWUICOMPOSITIONWINDOW | 0xF));

                    // Log.Verbose($"{nameof(WM.WM_IME_SETCONTEXT)}({(nint)args.WParam:X}, {(nint)args.LParam:X}): {this.ImmComp}");
                    args.SuppressWithDefault();
                    break;

                case WM.WM_IME_NOTIFY:
                    // Log.Verbose($"{nameof(WM.WM_IME_NOTIFY)}({(nint)args.WParam:X}): {this.ImmComp}");
                    break;

                case WM.WM_KEYDOWN when (int)args.WParam is
                                        VK.VK_TAB
                                        or VK.VK_PRIOR
                                        or VK.VK_NEXT
                                        or VK.VK_END
                                        or VK.VK_HOME
                                        or VK.VK_LEFT
                                        or VK.VK_UP
                                        or VK.VK_RIGHT
                                        or VK.VK_DOWN
                                        or VK.VK_RETURN:
                    if (this.ImmCand.Count != 0)
                    {
                        this.ClearState(hImc);
                        args.WParam = VK.VK_PROCESSKEY;
                    }

                    break;

                case WM.WM_LBUTTONDOWN:
                case WM.WM_RBUTTONDOWN:
                case WM.WM_MBUTTONDOWN:
                case WM.WM_XBUTTONDOWN:
                    ImmNotifyIME(hImc, NI.NI_COMPOSITIONSTR, CPS_COMPLETE, 0);
                    break;
            }

            this.UpdateInputLanguage(hImc);
        }
        finally
        {
            ImmReleaseContext(args.Hwnd, hImc);
        }
    }

    private static string ImmGetCompositionString(HIMC hImc, uint comp)
    {
        var numBytes = ImmGetCompositionStringW(hImc, comp, null, 0);
        if (numBytes == 0)
            return string.Empty;

        var data = stackalloc char[numBytes / 2];
        _ = ImmGetCompositionStringW(hImc, comp, data, (uint)numBytes);
        return new(data, 0, numBytes / 2);
    }

    private void ReleaseUnmanagedResources()
    {
        if (ImGuiHelpers.IsImGuiInitialized)
            ImGui.GetIO().SetPlatformImeDataFn = nint.Zero;
    }

    private void UpdateInputLanguage(HIMC hImc)
    {
        uint conv, sent;
        ImmGetConversionStatus(hImc, &conv, &sent);
        var lang = GetKeyboardLayout(0);
        var open = ImmGetOpenStatus(hImc) != false;

        // Log.Verbose($"{nameof(this.UpdateInputLanguage)}: conv={conv:X} sent={sent:X} open={open} lang={lang:X}");

        var native = (conv & 1) != 0;
        var katakana = (conv & 2) != 0;
        var fullwidth = (conv & 8) != 0;
        switch (lang & 0x3F)
        {
            case LANG.LANG_KOREAN:
                if (native)
                    this.InputModeIcon = (char)SeIconChar.ImeKoreanHangul;
                else if (fullwidth)
                    this.InputModeIcon = (char)SeIconChar.ImeAlphanumeric;
                else
                    this.InputModeIcon = (char)SeIconChar.ImeAlphanumericHalfWidth;
                break;

            case LANG.LANG_JAPANESE:
                // wtf
                // see the function called from: 48 8b 0d ?? ?? ?? ?? e8 ?? ?? ?? ?? 8b d8 e9 ?? 00 00 0
                if (open && native && katakana && fullwidth)
                    this.InputModeIcon = (char)SeIconChar.ImeKatakana;
                else if (open && native && katakana)
                    this.InputModeIcon = (char)SeIconChar.ImeKatakanaHalfWidth;
                else if (open && native)
                    this.InputModeIcon = (char)SeIconChar.ImeHiragana;
                else if (open && fullwidth)
                    this.InputModeIcon = (char)SeIconChar.ImeAlphanumeric;
                else
                    this.InputModeIcon = (char)SeIconChar.ImeAlphanumericHalfWidth;
                break;

            case LANG.LANG_CHINESE:
                if (native)
                    this.InputModeIcon = (char)SeIconChar.ImeChineseHan;
                else
                    this.InputModeIcon = (char)SeIconChar.ImeChineseLatin;
                break;

            default:
                this.InputModeIcon = default;
                break;
        }

        this.UpdateImeWindowStatus(hImc);
    }

    private void ReplaceCompositionString(HIMC hImc, uint comp)
    {
        var finalCommit = (comp & GCS.GCS_RESULTSTR) != 0;
        var newString = finalCommit
                            ? ImmGetCompositionString(hImc, GCS.GCS_RESULTSTR)
                            : ImmGetCompositionString(hImc, GCS.GCS_COMPSTR);

        this.ReflectCharacterEncounters(newString);

        if (this.temporaryUndoSelection is not null)
        {
            TextState->Undo();
            TextState->SelectionTuple = this.temporaryUndoSelection.Value;
            this.temporaryUndoSelection = null;
        }

        TextState->SanitizeSelectionRange();
        if (TextState->ReplaceSelectionAndPushUndo(newString))
            this.temporaryUndoSelection = TextState->SelectionTuple;

        // Put the cursor at the beginning, so that the candidate window appears aligned with the text.
        TextState->SetSelectionRange(TextState->SelectionTuple.Start, newString.Length, 0);

        if (finalCommit)
        {
            this.ClearState(hImc);
            return;
        }

        this.ImmComp = newString;
        this.CompositionCursorOffset = ImmGetCompositionStringW(hImc, GCS.GCS_CURSORPOS, null, 0);

        if ((comp & GCS.GCS_COMPATTR) != 0)
        {
            var attrLength = ImmGetCompositionStringW(hImc, GCS.GCS_COMPATTR, null, 0);
            var attrPtr = stackalloc byte[attrLength];
            var attr = new Span<byte>(attrPtr, Math.Min(this.ImmComp.Length, attrLength));
            _ = ImmGetCompositionStringW(hImc, GCS.GCS_COMPATTR, attrPtr, (uint)attrLength);
            var l = 0;
            while (l < attr.Length && attr[l] is not ATTR_TARGET_CONVERTED and not ATTR_TARGET_NOTCONVERTED)
                l++;

            var r = l;
            while (r < attr.Length && attr[r] is ATTR_TARGET_CONVERTED or ATTR_TARGET_NOTCONVERTED)
                r++;

            if (r == 0 || l == this.ImmComp.Length)
                (l, r) = (0, this.ImmComp.Length);

            (this.PartialConversionFrom, this.PartialConversionTo) = (l, r);
        }
        else
        {
            this.PartialConversionFrom = 0;
            this.PartialConversionTo = this.ImmComp.Length;
        }

        this.UpdateImeWindowStatus(hImc);
    }

    private void ClearState(HIMC hImc)
    {
        this.ImmComp = string.Empty;
        this.PartialConversionFrom = this.PartialConversionTo = 0;
        this.CompositionCursorOffset = 0;
        this.temporaryUndoSelection = null;
        TextState->Stb.SelectStart = TextState->Stb.Cursor = TextState->Stb.SelectEnd;
        ImmNotifyIME(hImc, NI.NI_COMPOSITIONSTR, CPS_CANCEL, 0);
        this.UpdateImeWindowStatus(default);

        // Log.Information($"{nameof(this.ClearState)}");
    }

    private void LoadCand(HIMC hImc)
    {
        this.ImmCand.Clear();
        this.ImmCandNative = default;

        if (hImc == default)
            return;

        var size = (int)ImmGetCandidateListW(hImc, 0, null, 0);
        if (size == 0)
            return;

        var pStorage = stackalloc byte[size];
        if (size != ImmGetCandidateListW(hImc, 0, (CANDIDATELIST*)pStorage, (uint)size))
            return;

        ref var candlist = ref *(CANDIDATELIST*)pStorage;
        this.ImmCandNative = candlist;

        if (candlist.dwPageSize == 0 || candlist.dwCount == 0)
            return;

        foreach (var i in Enumerable.Range(
                     (int)candlist.dwPageStart,
                     (int)Math.Min(candlist.dwCount - candlist.dwPageStart, candlist.dwPageSize)))
        {
            this.ImmCand.Add(new((char*)(pStorage + candlist.dwOffset[i])));
            this.ReflectCharacterEncounters(this.ImmCand[^1]);
        }
    }

    private void UpdateImeWindowStatus(HIMC hImc)
    {
        if (Service<DalamudInterface>.GetNullable() is not { } di)
            return;

        this.LoadCand(hImc);
        if (this.ImmCand.Count != 0 || this.ShowPartialConversion || this.InputModeIcon != default)
            di.OpenImeWindow();
        else
            di.CloseImeWindow();
    }

    private void ImGuiSetPlatformImeData(ImGuiViewportPtr viewport, ImGuiPlatformImeDataPtr data)
    {
        this.CursorPos = data.InputPos;
        this.AssociatedViewport = data.WantVisible ? viewport : default;
    }

    [ServiceManager.CallWhenServicesReady("Effectively waiting for cimgui context initialization.")]
    private void ContinueConstruction(InterfaceManager.InterfaceManagerWithScene interfaceManagerWithScene)
    {
        if (!ImGuiHelpers.IsImGuiInitialized)
        {
            throw new InvalidOperationException(
                $"Expected {nameof(InterfaceManager.InterfaceManagerWithScene)} to have initialized ImGui.");
        }

        ImGui.GetIO().SetPlatformImeDataFn = Marshal.GetFunctionPointerForDelegate(this.setPlatformImeDataDelegate);
    }

    /// <summary>
    /// Ported from imstb_textedit.h.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 0xE2C)]
    private struct StbTextEditState
    {
        /// <summary>
        /// Position of the text cursor within the string.
        /// </summary>
        public int Cursor;

        /// <summary>
        /// Selection start point.
        /// </summary>
        public int SelectStart;

        /// <summary>
        /// selection start and end point in characters; if equal, no selection.
        /// </summary>
        /// <remarks>
        /// Note that start may be less than or greater than end (e.g. when dragging the mouse,
        /// start is where the initial click was, and you can drag in either direction.)
        /// </remarks>
        public int SelectEnd;

        /// <summary>
        /// Each text field keeps its own insert mode state.
        /// To keep an app-wide insert mode, copy this value in/out of the app state.
        /// </summary>
        public byte InsertMode;

        /// <summary>
        /// Page size in number of row.
        /// This value MUST be set to >0 for pageup or pagedown in multilines documents.
        /// </summary>
        public int RowCountPerPage;

        // Remainder is stb-private data.
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ImGuiInputTextState
    {
        public uint Id;
        public int CurLenW;
        public int CurLenA;
        public ImVector<char> TextWRaw;
        public ImVector<byte> TextARaw;
        public ImVector<byte> InitialTextARaw;
        public bool TextAIsValid;
        public int BufCapacityA;
        public float ScrollX;
        public StbTextEditState Stb;
        public float CursorAnim;
        public bool CursorFollow;
        public bool SelectedAllMouseLock;
        public bool Edited;
        public ImGuiInputTextFlags Flags;

        public ImVectorWrapper<char> TextW => new((ImVector*)&this.ThisPtr->TextWRaw);

        public (int Start, int End, int Cursor) SelectionTuple
        {
            get => (this.Stb.SelectStart, this.Stb.SelectEnd, this.Stb.Cursor);
            set => (this.Stb.SelectStart, this.Stb.SelectEnd, this.Stb.Cursor) = value;
        }

        private ImGuiInputTextState* ThisPtr => (ImGuiInputTextState*)Unsafe.AsPointer(ref this);

        public void SetSelectionRange(int offset, int length, int relativeCursorOffset)
        {
            this.Stb.SelectStart = offset;
            this.Stb.SelectEnd = offset + length;
            if (relativeCursorOffset >= 0)
                this.Stb.Cursor = this.Stb.SelectStart + relativeCursorOffset;
            else
                this.Stb.Cursor = this.Stb.SelectEnd + 1 + relativeCursorOffset;
            this.SanitizeSelectionRange();
        }

        public void SanitizeSelectionRange()
        {
            ref var s = ref this.Stb.SelectStart;
            ref var e = ref this.Stb.SelectEnd;
            ref var c = ref this.Stb.Cursor;
            s = Math.Clamp(s, 0, this.CurLenW);
            e = Math.Clamp(e, 0, this.CurLenW);
            c = Math.Clamp(c, 0, this.CurLenW);
            if (s == e)
                s = e = c;
            if (s > e)
                (s, e) = (e, s);
        }

        public void Undo() => StbTextUndo(this.ThisPtr, &this.ThisPtr->Stb);

        public bool MakeUndoReplace(int offset, int oldLength, int newLength)
        {
            if (oldLength == 0 && newLength == 0)
                return false;

            StbTextMakeUndoReplace(this.ThisPtr, &this.ThisPtr->Stb, offset, oldLength, newLength);
            return true;
        }

        public bool ReplaceSelectionAndPushUndo(ReadOnlySpan<char> newText)
        {
            var off = this.Stb.SelectStart;
            var len = this.Stb.SelectEnd - this.Stb.SelectStart;
            return this.MakeUndoReplace(off, len, newText.Length) && this.ReplaceChars(off, len, newText);
        }

        public bool ReplaceChars(int pos, int len, ReadOnlySpan<char> newText)
        {
            this.DeleteChars(pos, len);
            return this.InsertChars(pos, newText);
        }

        // See imgui_widgets.cpp: STB_TEXTEDIT_DELETECHARS
        public void DeleteChars(int pos, int n)
        {
            if (n == 0)
                return;

            var dst = this.TextW.Data + pos;

            // We maintain our buffer length in both UTF-8 and wchar formats
            this.Edited = true;
            this.CurLenA -= Encoding.UTF8.GetByteCount(dst, n);
            this.CurLenW -= n;

            // Offset remaining text (FIXME-OPT: Use memmove)
            var src = this.TextW.Data + pos + n;
            int i;
            for (i = 0; src[i] != 0; i++)
                dst[i] = src[i];
            dst[i] = '\0';
        }

        // See imgui_widgets.cpp: STB_TEXTEDIT_INSERTCHARS
        public bool InsertChars(int pos, ReadOnlySpan<char> newText)
        {
            if (newText.Length == 0)
                return true;

            var isResizable = (this.Flags & ImGuiInputTextFlags.CallbackResize) != 0;
            var textLen = this.CurLenW;
            Debug.Assert(pos <= textLen, "pos <= text_len");

            var newTextLenUtf8 = Encoding.UTF8.GetByteCount(newText);
            if (!isResizable && newTextLenUtf8 + this.CurLenA + 1 > this.BufCapacityA)
                return false;

            // Grow internal buffer if needed
            if (newText.Length + textLen + 1 > this.TextW.Length)
            {
                if (!isResizable)
                    return false;

                Debug.Assert(textLen < this.TextW.Length, "text_len < this.TextW.Length");
                this.TextW.Resize(textLen + Math.Clamp(newText.Length * 4, 32, Math.Max(256, newText.Length)) + 1);
            }

            var text = this.TextW.DataSpan;
            if (pos != textLen)
                text.Slice(pos, textLen - pos).CopyTo(text[(pos + newText.Length)..]);
            newText.CopyTo(text[pos..]);

            this.Edited = true;
            this.CurLenW += newText.Length;
            this.CurLenA += newTextLenUtf8;
            this.TextW[this.CurLenW] = '\0';

            return true;
        }
    }
}
