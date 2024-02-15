using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Internal.ImGuiInternalStructs;

[SuppressMessage(
    "StyleCop.CSharp.DocumentationRules",
    "SA1600:Elements should be documented",
    Justification = "See ImGui source code")]
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ImGuiInputTextState
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

    private const int CImGuiStbTextCreateUndoOffset = 0xB57A0;
    private const int CImGuiStbTextUndoOffset = 0xB59C0;

    private static readonly delegate* unmanaged<ImGuiInputTextState*, StbTextEditState*, int, int, int, void>
        StbTextMakeUndoReplace;

    private static readonly delegate* unmanaged<ImGuiInputTextState*, StbTextEditState*, void> StbTextUndo;

    [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1003:Symbols should be spaced correctly", Justification = ".")]
    static ImGuiInputTextState()
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
