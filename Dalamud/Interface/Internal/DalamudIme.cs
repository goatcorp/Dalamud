// #define IMEDEBUG

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

using Dalamud.Game.Text;
using Dalamud.Hooking.WndProcHook;
using Dalamud.Interface.Colors;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal.ManagedAsserts;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Utility;

using ImGuiNET;

#if IMEDEBUG
using Serilog;
#endif

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Internal;

/// <summary>
/// This class handles CJK IME.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class DalamudIme : IInternalDisposableService
{
    private const int CImGuiStbTextCreateUndoOffset = 0xB57A0;
    private const int CImGuiStbTextUndoOffset = 0xB59C0;

    private const int ImePageSize = 9;

    private static readonly Dictionary<int, string> WmNames =
        typeof(WM).GetFields(BindingFlags.Public | BindingFlags.Static)
                  .Where(x => x.IsLiteral && !x.IsInitOnly && x.FieldType == typeof(int))
                  .Select(x => ((int)x.GetRawConstantValue()!, x.Name))
                  .DistinctBy(x => x.Item1)
                  .ToDictionary(x => x.Item1, x => x.Name);

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

    [ServiceManager.ServiceDependency]
    private readonly WndProcHookManager wndProcHookManager = Service<WndProcHookManager>.Get();

    private readonly InterfaceManager interfaceManager;

    private readonly ImGuiSetPlatformImeDataDelegate setPlatformImeDataDelegate;

    /// <summary>The candidates.</summary>
    private readonly List<(string String, bool Supported)> candidateStrings = new();

    /// <summary>The selected imm component.</summary>
    private string compositionString = string.Empty;

    /// <summary>The cursor position in screen coordinates.</summary>
    private Vector2 cursorScreenPos;

    /// <summary>The associated viewport.</summary>
    private ImGuiViewportPtr associatedViewport;

    /// <summary>The index of the first imm candidate in relation to the full list.</summary>
    private CANDIDATELIST immCandNative;

    /// <summary>The partial conversion from-range.</summary>
    private int partialConversionFrom;

    /// <summary>The partial conversion to-range.</summary>
    private int partialConversionTo;

    /// <summary>The cursor offset in the composition string.</summary>
    private int compositionCursorOffset;

    /// <summary>The input mode icon from <see cref="SeIconChar"/>.</summary>
    private char inputModeIcon;

    /// <summary>Undo range for modifying the buffer while composition is in progress.</summary>
    private (int Start, int End, int Cursor)? temporaryUndoSelection;

    private bool hadWantTextInput;
    private bool updateInputLanguage = true;
    private bool updateImeStatusAgain;

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
    private DalamudIme(InterfaceManager.InterfaceManagerWithScene imws)
    {
        Debug.Assert(ImGuiHelpers.IsImGuiInitialized, "IMWS initialized but IsImGuiInitialized is false?");

        this.interfaceManager = imws.Manager;
        this.setPlatformImeDataDelegate = this.ImGuiSetPlatformImeData;

        ImGui.GetIO().SetPlatformImeDataFn = Marshal.GetFunctionPointerForDelegate(this.setPlatformImeDataDelegate);
        this.interfaceManager.Draw += this.Draw;
        this.wndProcHookManager.PreWndProc += this.WndProcHookManagerOnPreWndProc;
    }

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
    private static bool ShowCursorInInputText
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

    private static ImGuiInputTextState* TextState =>
        (ImGuiInputTextState*)(ImGui.GetCurrentContext() + ImGuiContextOffsets.TextStateOffset);

    /// <summary>Gets a value indicating whether to display partial conversion status.</summary>
    private bool ShowPartialConversion => this.partialConversionFrom != 0 ||
                                          this.partialConversionTo != this.compositionString.Length;

    /// <summary>Gets a value indicating whether to draw.</summary>
    private bool ShouldDraw =>
        this.candidateStrings.Count != 0 || this.ShowPartialConversion || this.inputModeIcon != default;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.interfaceManager.Draw -= this.Draw;
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
            if (!this.EncounteredHan)
            {
                if (HanRange.Any(x => x.FirstCodePoint <= chr && chr < x.FirstCodePoint + x.Length))
                {
                    if (Service<FontAtlasFactory>.Get()
                                                 ?.GetFdtReader(GameFontFamilyAndSize.Axis12)
                                                 .FindGlyph(chr) is null)
                    {
                        this.EncounteredHan = true;
                        Service<InterfaceManager>.Get().RebuildFonts();
                    }
                }
            }

            if (!this.EncounteredHangul)
            {
                if (HangulRange.Any(x => x.FirstCodePoint <= chr && chr < x.FirstCodePoint + x.Length))
                {
                    this.EncounteredHangul = true;
                    Service<InterfaceManager>.Get().RebuildFonts();
                }
            }
        }
    }

    private static (string String, bool Supported) ToUcs2(char* data, int nc = -1)
    {
        if (nc == -1)
        {
            nc = 0;
            while (data[nc] != 0)
                nc++;
        }

        var supported = true;
        var sb = new StringBuilder();
        sb.EnsureCapacity(nc);
        for (var i = 0; i < nc; i++)
        {
            if (char.IsHighSurrogate(data[i]) && i + 1 < nc && char.IsLowSurrogate(data[i + 1]))
            {
                // Surrogate pair is found, but only UCS-2 characters are supported. Skip the next low surrogate.
                sb.Append('\xFFFD');
                supported = false;
                i++;
            }
            else if (char.IsSurrogate(data[i]) || !Rune.IsValid(data[i]))
            {
                // Lone surrogate pair, or an invalid codepoint.
                sb.Append('\xFFFD');
                supported = false;
            }
            else
            {
                sb.Append(data[i]);
            }
        }

        return (sb.ToString(), supported);
    }

    private static string ImmGetCompositionString(HIMC hImc, uint comp)
    {
        var numBytes = ImmGetCompositionStringW(hImc, comp, null, 0);
        if (numBytes == 0)
            return string.Empty;

        var data = stackalloc char[numBytes / 2];
        _ = ImmGetCompositionStringW(hImc, comp, data, (uint)numBytes);

        return ToUcs2(data, numBytes / 2).String;
    }

    private void ReleaseUnmanagedResources()
    {
        if (ImGuiHelpers.IsImGuiInitialized)
            ImGui.GetIO().SetPlatformImeDataFn = nint.Zero;
    }

    private void WndProcHookManagerOnPreWndProc(WndProcEventArgs args)
    {
        if (!ImGuiHelpers.IsImGuiInitialized)
        {
            this.updateInputLanguage = true;
            this.temporaryUndoSelection = null;
            return;
        }

        // Are we not the target of text input?
        if (!ImGui.GetIO().WantTextInput)
        {
            if (this.hadWantTextInput)
            {
                // Force the cancellation of whatever was being input.
                var hImc2 = ImmGetContext(args.Hwnd);
                if (hImc2 != 0)
                {
                    ImmNotifyIME(hImc2, NI.NI_COMPOSITIONSTR, CPS_CANCEL, 0);
                    ImmReleaseContext(args.Hwnd, hImc2);
                }
            }

            this.hadWantTextInput = false;
            this.updateInputLanguage = true;
            this.temporaryUndoSelection = null;
            return;
        }

        this.hadWantTextInput = true;

        var hImc = ImmGetContext(args.Hwnd);
        if (hImc == nint.Zero)
        {
            this.updateInputLanguage = true;
            this.temporaryUndoSelection = null;
            return;
        }

        try
        {
            var invalidTarget = TextState->Id == 0 || (TextState->Flags & ImGuiInputTextFlags.ReadOnly) != 0;

#if IMEDEBUG
            switch (args.Message)
            {
                case WM.WM_IME_NOTIFY:
                    Log.Verbose($"{nameof(WM.WM_IME_NOTIFY)}({ImeDebug.ImnName((int)args.WParam)}, 0x{args.LParam:X})");
                    break;
                case WM.WM_IME_CONTROL:
                    Log.Verbose(
                        $"{nameof(WM.WM_IME_CONTROL)}({ImeDebug.ImcName((int)args.WParam)}, 0x{args.LParam:X})");
                    break;
                case WM.WM_IME_REQUEST:
                    Log.Verbose(
                        $"{nameof(WM.WM_IME_REQUEST)}({ImeDebug.ImrName((int)args.WParam)}, 0x{args.LParam:X})");
                    break;
                case WM.WM_IME_SELECT:
                    Log.Verbose($"{nameof(WM.WM_IME_SELECT)}({(int)args.WParam != 0}, 0x{args.LParam:X})");
                    break;
                case WM.WM_IME_STARTCOMPOSITION:
                    Log.Verbose($"{nameof(WM.WM_IME_STARTCOMPOSITION)}()");
                    break;
                case WM.WM_IME_COMPOSITION:
                    Log.Verbose(
                        $"{nameof(WM.WM_IME_COMPOSITION)}({(char)args.WParam}, {ImeDebug.GcsName((int)args.LParam)})");
                    break;
                case WM.WM_IME_COMPOSITIONFULL:
                    Log.Verbose($"{nameof(WM.WM_IME_COMPOSITIONFULL)}()");
                    break;
                case WM.WM_IME_ENDCOMPOSITION:
                    Log.Verbose($"{nameof(WM.WM_IME_ENDCOMPOSITION)}()");
                    break;
                case WM.WM_IME_CHAR:
                    Log.Verbose($"{nameof(WM.WM_IME_CHAR)}({(char)args.WParam}, 0x{args.LParam:X})");
                    break;
                case WM.WM_IME_KEYDOWN:
                    Log.Verbose($"{nameof(WM.WM_IME_KEYDOWN)}({(char)args.WParam}, 0x{args.LParam:X})");
                    break;
                case WM.WM_IME_KEYUP:
                    Log.Verbose($"{nameof(WM.WM_IME_KEYUP)}({(char)args.WParam}, 0x{args.LParam:X})");
                    break;
                case WM.WM_IME_SETCONTEXT:
                    Log.Verbose($"{nameof(WM.WM_IME_SETCONTEXT)}({(int)args.WParam != 0}, 0x{args.LParam:X})");
                    break;
            }
#endif
            if (this.updateInputLanguage
                || (args.Message == WM.WM_IME_NOTIFY
                    && (int)args.WParam
                    is IMN.IMN_SETCONVERSIONMODE
                    or IMN.IMN_OPENSTATUSWINDOW
                    or IMN.IMN_CLOSESTATUSWINDOW))
            {
                this.UpdateInputLanguage(hImc);
                this.updateInputLanguage = false;
            }

            // Microsoft Korean IME and Google Japanese IME drop notifying us of a candidate list change.
            // As the candidate list update is already there on the next WndProc call, update the candidate list again
            // here.
            if (this.updateImeStatusAgain)
            {
                this.UpdateCandidates(hImc);
                this.updateImeStatusAgain = false;
            }

            switch (args.Message)
            {
                case WM.WM_IME_NOTIFY
                    when (nint)args.WParam is IMN.IMN_OPENCANDIDATE or IMN.IMN_CLOSECANDIDATE
                         or IMN.IMN_CHANGECANDIDATE:
                    this.UpdateCandidates(hImc);
                    this.updateImeStatusAgain = true;
                    args.SuppressWithValue(0);
                    break;

                case WM.WM_IME_STARTCOMPOSITION:
                    this.updateImeStatusAgain = true;
                    args.SuppressWithValue(0);
                    break;

                case WM.WM_IME_COMPOSITION:
                    if (invalidTarget)
                        ImmNotifyIME(hImc, NI.NI_COMPOSITIONSTR, CPS_CANCEL, 0);
                    else
                        this.ReplaceCompositionString(hImc, ((int)args.LParam & GCS.GCS_RESULTSTR) != 0);

                    this.updateImeStatusAgain = true;
                    args.SuppressWithValue(0);
                    break;

                case WM.WM_IME_ENDCOMPOSITION:
                    this.ClearState(hImc, false);
                    this.updateImeStatusAgain = true;
                    args.SuppressWithValue(0);
                    break;

                case WM.WM_IME_CHAR:
                case WM.WM_IME_KEYDOWN:
                case WM.WM_IME_KEYUP:
                case WM.WM_IME_CONTROL:
                case WM.WM_IME_REQUEST:
                    this.updateImeStatusAgain = true;
                    args.SuppressWithValue(0);
                    break;

                case WM.WM_IME_SETCONTEXT:
                    // Hide candidate and composition windows.
                    args.LParam = (LPARAM)((nint)args.LParam & ~(ISC_SHOWUICOMPOSITIONWINDOW | 0xF));

                    this.updateImeStatusAgain = true;
                    args.SuppressWithDefault();
                    break;

                case WM.WM_IME_NOTIFY:
                case WM.WM_IME_COMPOSITIONFULL:
                case WM.WM_IME_SELECT:
                    this.updateImeStatusAgain = true;
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
                    // If key inputs that usually result in focus change, cancel the input process.
                    if (!string.IsNullOrEmpty(ImmGetCompositionString(hImc, GCS.GCS_COMPSTR)))
                    {
                        this.ClearState(hImc);
                        args.WParam = VK.VK_PROCESSKEY;
                    }

                    this.UpdateCandidates(hImc);
                    break;

                case WM.WM_KEYDOWN when (int)args.WParam is VK.VK_ESCAPE && this.candidateStrings.Count != 0:
                    this.ClearState(hImc);
                    args.SuppressWithDefault();
                    break;

                case WM.WM_LBUTTONDOWN:
                case WM.WM_RBUTTONDOWN:
                case WM.WM_MBUTTONDOWN:
                case WM.WM_XBUTTONDOWN:
                    // If mouse click happened while IME composition was in progress, force complete the input process.
                    if (!string.IsNullOrEmpty(ImmGetCompositionString(hImc, GCS.GCS_COMPSTR)))
                    {
                        ImmNotifyIME(hImc, NI.NI_COMPOSITIONSTR, CPS_COMPLETE, 0);
                        
                        // Disable further handling of mouse button down event, or something would lock up the cursor.
                        args.SuppressWithValue(1);
                    }

                    break;
            }
        }
        finally
        {
            ImmReleaseContext(args.Hwnd, hImc);
        }
    }

    private void UpdateInputLanguage(HIMC hImc)
    {
        uint conv, sent;
        ImmGetConversionStatus(hImc, &conv, &sent);
        var lang = GetKeyboardLayout(0);
        var open = ImmGetOpenStatus(hImc) != false;

        var native = (conv & 1) != 0;
        var katakana = (conv & 2) != 0;
        var fullwidth = (conv & 8) != 0;
        switch (lang & 0x3F)
        {
            case LANG.LANG_KOREAN:
                if (native)
                    this.inputModeIcon = (char)SeIconChar.ImeKoreanHangul;
                else if (fullwidth)
                    this.inputModeIcon = (char)SeIconChar.ImeAlphanumeric;
                else
                    this.inputModeIcon = (char)SeIconChar.ImeAlphanumericHalfWidth;
                break;

            case LANG.LANG_JAPANESE:
                // wtf
                // see the function called from: 48 8b 0d ?? ?? ?? ?? e8 ?? ?? ?? ?? 8b d8 e9 ?? 00 00 0
                if (open && native && katakana && fullwidth)
                    this.inputModeIcon = (char)SeIconChar.ImeKatakana;
                else if (open && native && katakana)
                    this.inputModeIcon = (char)SeIconChar.ImeKatakanaHalfWidth;
                else if (open && native)
                    this.inputModeIcon = (char)SeIconChar.ImeHiragana;
                else if (open && fullwidth)
                    this.inputModeIcon = (char)SeIconChar.ImeAlphanumeric;
                else
                    this.inputModeIcon = (char)SeIconChar.ImeAlphanumericHalfWidth;
                break;

            case LANG.LANG_CHINESE:
                if (native)
                    this.inputModeIcon = (char)SeIconChar.ImeChineseHan;
                else
                    this.inputModeIcon = (char)SeIconChar.ImeChineseLatin;
                break;

            default:
                this.inputModeIcon = default;
                break;
        }
    }

    private void ReplaceCompositionString(HIMC hImc, bool finalCommit)
    {
        var newString = finalCommit
                            ? ImmGetCompositionString(hImc, GCS.GCS_RESULTSTR)
                            : ImmGetCompositionString(hImc, GCS.GCS_COMPSTR);

#if IMEDEBUG
        Log.Verbose($"{nameof(this.ReplaceCompositionString)}({newString})");
#endif

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
            this.ClearState(hImc, false);
            newString = string.Empty;
        }

        this.compositionString = newString;
        this.compositionCursorOffset = ImmGetCompositionStringW(hImc, GCS.GCS_CURSORPOS, null, 0);

        var attrLength = ImmGetCompositionStringW(hImc, GCS.GCS_COMPATTR, null, 0);
        if (attrLength > 0)
        {
            var attrPtr = stackalloc byte[attrLength];
            var attr = new Span<byte>(attrPtr, Math.Min(this.compositionString.Length, attrLength));
            _ = ImmGetCompositionStringW(hImc, GCS.GCS_COMPATTR, attrPtr, (uint)attrLength);
            var l = 0;
            while (l < attr.Length && attr[l] is not ATTR_TARGET_CONVERTED and not ATTR_TARGET_NOTCONVERTED)
                l++;

            var r = l;
            while (r < attr.Length && attr[r] is ATTR_TARGET_CONVERTED or ATTR_TARGET_NOTCONVERTED)
                r++;

            if (r == 0 || l == this.compositionString.Length)
                (l, r) = (0, this.compositionString.Length);

            (this.partialConversionFrom, this.partialConversionTo) = (l, r);
        }
        else
        {
            this.partialConversionFrom = 0;
            this.partialConversionTo = this.compositionString.Length;
        }

        this.UpdateCandidates(hImc);
    }

    private void ClearState(HIMC hImc, bool invokeCancel = true)
    {
        this.compositionString = string.Empty;
        this.partialConversionFrom = this.partialConversionTo = 0;
        this.compositionCursorOffset = 0;
        this.temporaryUndoSelection = null;
        TextState->Stb.SelectStart = TextState->Stb.Cursor = TextState->Stb.SelectEnd;
        this.candidateStrings.Clear();
        this.immCandNative = default;
        if (invokeCancel)
            ImmNotifyIME(hImc, NI.NI_COMPOSITIONSTR, CPS_CANCEL, 0);

#if IMEDEBUG
        Log.Information($"{nameof(this.ClearState)}({invokeCancel})");
#endif
    }

    private void UpdateCandidates(HIMC hImc)
    {
        this.candidateStrings.Clear();
        this.immCandNative = default;

        if (hImc == default)
            return;

        var size = (int)ImmGetCandidateListW(hImc, 0, null, 0);
        if (size == 0)
            return;

        var pStorage = stackalloc byte[size];
        if (size != ImmGetCandidateListW(hImc, 0, (CANDIDATELIST*)pStorage, (uint)size))
            return;

        ref var candlist = ref *(CANDIDATELIST*)pStorage;
        this.immCandNative = candlist;

        if (candlist.dwPageSize == 0 || candlist.dwCount == 0)
            return;

        foreach (var i in Enumerable.Range(
                     (int)candlist.dwPageStart,
                     (int)Math.Min(candlist.dwCount - candlist.dwPageStart, candlist.dwPageSize)))
        {
            this.candidateStrings.Add(ToUcs2((char*)(pStorage + candlist.dwOffset[i])));
            this.ReflectCharacterEncounters(this.candidateStrings[^1].String);
        }
    }

    private void ImGuiSetPlatformImeData(ImGuiViewportPtr viewport, ImGuiPlatformImeDataPtr data)
    {
        this.cursorScreenPos = data.InputPos;
        this.associatedViewport = data.WantVisible ? viewport : default;
    }

    private void Draw()
    {
        if (!this.ShouldDraw)
            return;

        if (Service<DalamudIme>.GetNullable() is not { } ime)
            return;

        var viewport = ime.associatedViewport;
        if (viewport.NativePtr is null)
            return;

        var drawCand = ime.candidateStrings.Count != 0;
        var drawConv = drawCand || ime.ShowPartialConversion;
        var drawIme = ime.inputModeIcon != 0;
        var imeIconFont = InterfaceManager.DefaultFont;

        var pad = ImGui.GetStyle().WindowPadding;
        var candTextSize = ImGui.CalcTextSize(ime.compositionString == string.Empty ? " " : ime.compositionString);

        var native = ime.immCandNative;
        var totalIndex = native.dwSelection + 1;
        var totalSize = native.dwCount;

        var pageStart = native.dwPageStart;
        var pageIndex = (pageStart / ImePageSize) + 1;
        var pageCount = (totalSize / ImePageSize) + 1;
        var pageInfo = $"{totalIndex}/{totalSize} ({pageIndex}/{pageCount})";

        // Calc the window size.
        var maxTextWidth = 0f;
        for (var i = 0; i < ime.candidateStrings.Count; i++)
        {
            var textSize = ImGui.CalcTextSize($"{i + 1}. {ime.candidateStrings[i]}");
            maxTextWidth = maxTextWidth > textSize.X ? maxTextWidth : textSize.X;
        }

        maxTextWidth = maxTextWidth > ImGui.CalcTextSize(pageInfo).X ? maxTextWidth : ImGui.CalcTextSize(pageInfo).X;
        maxTextWidth = maxTextWidth > ImGui.CalcTextSize(ime.compositionString).X
                           ? maxTextWidth
                           : ImGui.CalcTextSize(ime.compositionString).X;

        var numEntries = (drawCand ? ime.candidateStrings.Count + 1 : 0) + 1 + (drawIme ? 1 : 0);
        var spaceY = ImGui.GetStyle().ItemSpacing.Y;
        var imeWindowHeight = (spaceY * (numEntries - 1)) + (candTextSize.Y * numEntries);
        var windowSize = new Vector2(maxTextWidth, imeWindowHeight) + (pad * 2);

        // 1. Figure out the expanding direction.
        var expandUpward = ime.cursorScreenPos.Y + windowSize.Y > viewport.WorkPos.Y + viewport.WorkSize.Y;
        var windowPos = ime.cursorScreenPos - pad;
        if (expandUpward)
        {
            windowPos.Y -= windowSize.Y - candTextSize.Y - (pad.Y * 2);
            if (drawIme)
                windowPos.Y += candTextSize.Y + spaceY;
        }
        else
        {
            if (drawIme)
                windowPos.Y -= candTextSize.Y + spaceY;
        }

        // 2. Contain within the viewport. Do not use clamp, as the target window might be too small.
        if (windowPos.X < viewport.WorkPos.X)
            windowPos.X = viewport.WorkPos.X;
        else if (windowPos.X + windowSize.X > viewport.WorkPos.X + viewport.WorkSize.X)
            windowPos.X = (viewport.WorkPos.X + viewport.WorkSize.X) - windowSize.X;
        if (windowPos.Y < viewport.WorkPos.Y)
            windowPos.Y = viewport.WorkPos.Y;
        else if (windowPos.Y + windowSize.Y > viewport.WorkPos.Y + viewport.WorkSize.Y)
            windowPos.Y = (viewport.WorkPos.Y + viewport.WorkSize.Y) - windowSize.Y;

        var cursor = windowPos + pad;

        // Draw the ime window.
        var drawList = ImGui.GetForegroundDrawList(viewport);

        // Draw the background rect for candidates.
        if (drawCand)
        {
            Vector2 candRectLt, candRectRb;
            if (!expandUpward)
            {
                candRectLt = windowPos + candTextSize with { X = 0 } + pad with { X = 0 };
                candRectRb = windowPos + windowSize;
                if (drawIme)
                    candRectLt.Y += spaceY + candTextSize.Y;
            }
            else
            {
                candRectLt = windowPos;
                candRectRb = windowPos + (windowSize - candTextSize with { X = 0 } - pad with { X = 0 });
                if (drawIme)
                    candRectRb.Y -= spaceY + candTextSize.Y;
            }

            drawList.AddRectFilled(
                candRectLt,
                candRectRb,
                ImGui.GetColorU32(ImGuiCol.WindowBg),
                ImGui.GetStyle().WindowRounding);
        }

        if (!expandUpward && drawIme)
        {
            for (var dx = -2; dx <= 2; dx++)
            {
                for (var dy = -2; dy <= 2; dy++)
                {
                    if (dx != 0 || dy != 0)
                    {
                        imeIconFont.RenderChar(
                            drawList,
                            imeIconFont.FontSize,
                            cursor + new Vector2(dx, dy),
                            ImGui.GetColorU32(ImGuiCol.WindowBg),
                            ime.inputModeIcon);
                    }
                }
            }

            imeIconFont.RenderChar(
                drawList,
                imeIconFont.FontSize,
                cursor,
                ImGui.GetColorU32(ImGuiCol.Text),
                ime.inputModeIcon);
            cursor.Y += candTextSize.Y + spaceY;
        }

        if (!expandUpward && drawConv)
        {
            DrawTextBeingConverted();
            cursor.Y += candTextSize.Y + spaceY;

            // Add a separator.
            drawList.AddLine(cursor, cursor + new Vector2(maxTextWidth, 0), ImGui.GetColorU32(ImGuiCol.Separator));
        }

        if (drawCand)
        {
            // Add the candidate words.
            for (var i = 0; i < ime.candidateStrings.Count; i++)
            {
                var selected = i == (native.dwSelection % ImePageSize);
                var color = ImGui.GetColorU32(ImGuiCol.Text);
                if (selected)
                    color = ImGui.GetColorU32(ImGuiCol.NavHighlight);

                var s = $"{i + 1}. {ime.candidateStrings[i].String}";
                drawList.AddText(cursor, color, s);
                if (!ime.candidateStrings[i].Supported)
                {
                    var pos = cursor + ImGui.CalcTextSize(s) with { Y = 0 } +
                              new Vector2(4 * ImGuiHelpers.GlobalScale, 0);
                    drawList.AddText(pos, ImGui.GetColorU32(ImGuiColors.DalamudRed), " (x)");
                }

                cursor.Y += candTextSize.Y + spaceY;
            }

            // Add a separator
            drawList.AddLine(cursor, cursor + new Vector2(maxTextWidth, 0), ImGui.GetColorU32(ImGuiCol.Separator));

            // Add the pages infomation.
            drawList.AddText(cursor, ImGui.GetColorU32(ImGuiCol.Text), pageInfo);
            cursor.Y += candTextSize.Y + spaceY;
        }

        if (expandUpward && drawConv)
        {
            // Add a separator.
            drawList.AddLine(cursor, cursor + new Vector2(maxTextWidth, 0), ImGui.GetColorU32(ImGuiCol.Separator));

            DrawTextBeingConverted();
            cursor.Y += candTextSize.Y + spaceY;
        }

        if (expandUpward && drawIme)
        {
            for (var dx = -2; dx <= 2; dx++)
            {
                for (var dy = -2; dy <= 2; dy++)
                {
                    if (dx != 0 || dy != 0)
                    {
                        imeIconFont.RenderChar(
                            drawList,
                            imeIconFont.FontSize,
                            cursor + new Vector2(dx, dy),
                            ImGui.GetColorU32(ImGuiCol.WindowBg),
                            ime.inputModeIcon);
                    }
                }
            }

            imeIconFont.RenderChar(
                drawList,
                imeIconFont.FontSize,
                cursor,
                ImGui.GetColorU32(ImGuiCol.Text),
                ime.inputModeIcon);
        }

        return;

        void DrawTextBeingConverted()
        {
            // Draw the text background.
            drawList.AddRectFilled(
                cursor - (pad / 2),
                cursor + candTextSize + (pad / 2),
                ImGui.GetColorU32(ImGuiCol.WindowBg));

            // If only a part of the full text is marked for conversion, then draw background for the part being edited.
            if (ime.partialConversionFrom != 0 || ime.partialConversionTo != ime.compositionString.Length)
            {
                var part1 = ime.compositionString[..ime.partialConversionFrom];
                var part2 = ime.compositionString[..ime.partialConversionTo];
                var size1 = ImGui.CalcTextSize(part1);
                var size2 = ImGui.CalcTextSize(part2);
                drawList.AddRectFilled(
                    cursor + size1 with { Y = 0 },
                    cursor + size2,
                    ImGui.GetColorU32(ImGuiCol.TextSelectedBg));
            }

            // Add the text being converted.
            drawList.AddText(cursor, ImGui.GetColorU32(ImGuiCol.Text), ime.compositionString);

            // Draw the caret inside the composition string.
            if (DalamudIme.ShowCursorInInputText)
            {
                var partBeforeCaret = ime.compositionString[..ime.compositionCursorOffset];
                var sizeBeforeCaret = ImGui.CalcTextSize(partBeforeCaret);
                drawList.AddLine(
                    cursor + sizeBeforeCaret with { Y = 0 },
                    cursor + sizeBeforeCaret,
                    ImGui.GetColorU32(ImGuiCol.Text));
            }
        }
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

#if IMEDEBUG
    private static class ImeDebug
    {
        private static readonly (int Value, string Name)[] GcsFields =
        {
            (GCS.GCS_COMPREADSTR, nameof(GCS.GCS_COMPREADSTR)),
            (GCS.GCS_COMPREADATTR, nameof(GCS.GCS_COMPREADATTR)),
            (GCS.GCS_COMPREADCLAUSE, nameof(GCS.GCS_COMPREADCLAUSE)),
            (GCS.GCS_COMPSTR, nameof(GCS.GCS_COMPSTR)),
            (GCS.GCS_COMPATTR, nameof(GCS.GCS_COMPATTR)),
            (GCS.GCS_COMPCLAUSE, nameof(GCS.GCS_COMPCLAUSE)),
            (GCS.GCS_CURSORPOS, nameof(GCS.GCS_CURSORPOS)),
            (GCS.GCS_DELTASTART, nameof(GCS.GCS_DELTASTART)),
            (GCS.GCS_RESULTREADSTR, nameof(GCS.GCS_RESULTREADSTR)),
            (GCS.GCS_RESULTREADCLAUSE, nameof(GCS.GCS_RESULTREADCLAUSE)),
            (GCS.GCS_RESULTSTR, nameof(GCS.GCS_RESULTSTR)),
            (GCS.GCS_RESULTCLAUSE, nameof(GCS.GCS_RESULTCLAUSE)),
        };

        private static readonly IReadOnlyDictionary<int, string> ImnFields =
            typeof(IMN)
                .GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(x => x.IsLiteral)
                .ToDictionary(x => (int)x.GetRawConstantValue()!, x => x.Name);

        public static string GcsName(int val)
        {
            var sb = new StringBuilder();
            foreach (var (value, name) in GcsFields)
            {
                if ((val & value) != 0)
                {
                    if (sb.Length != 0)
                        sb.Append(" | ");
                    sb.Append(name);
                    val &= ~value;
                }
            }

            if (val != 0)
            {
                if (sb.Length != 0)
                    sb.Append(" | ");
                sb.Append($"0x{val:X}");
            }

            return sb.ToString();
        }

        public static string ImcName(int val) => ImnFields.TryGetValue(val, out var name) ? name : $"0x{val:X}";

        public static string ImnName(int val) => ImnFields.TryGetValue(val, out var name) ? name : $"0x{val:X}";

        public static string ImrName(int val) => val switch
        {
            IMR_CANDIDATEWINDOW => nameof(IMR_CANDIDATEWINDOW),
            IMR_COMPOSITIONFONT => nameof(IMR_COMPOSITIONFONT),
            IMR_COMPOSITIONWINDOW => nameof(IMR_COMPOSITIONWINDOW),
            IMR_CONFIRMRECONVERTSTRING => nameof(IMR_CONFIRMRECONVERTSTRING),
            IMR_DOCUMENTFEED => nameof(IMR_DOCUMENTFEED),
            IMR_QUERYCHARPOSITION => nameof(IMR_QUERYCHARPOSITION),
            IMR_RECONVERTSTRING => nameof(IMR_RECONVERTSTRING),
            _ => $"0x{val:X}",
        };
    }
#endif
}
