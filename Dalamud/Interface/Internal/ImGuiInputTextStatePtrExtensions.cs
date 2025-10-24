using System.Diagnostics;
using System.Text;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Internal;

#pragma warning disable SA1600
internal static unsafe class ImGuiInputTextStatePtrExtensions
{
    public static (int Start, int End, int Cursor) GetSelectionTuple(this ImGuiInputTextStatePtr self) =>
        (self.Stb.SelectStart, self.Stb.SelectEnd, self.Stb.Cursor);

    public static void SetSelectionTuple(this ImGuiInputTextStatePtr self, (int Start, int End, int Cursor) value) =>
        (self.Stb.SelectStart, self.Stb.SelectEnd, self.Stb.Cursor) = value;

    public static void SetSelectionRange(this ImGuiInputTextStatePtr self, int offset, int length, int relativeCursorOffset)
    {
        self.Stb.SelectStart = offset;
        self.Stb.SelectEnd = offset + length;
        if (relativeCursorOffset >= 0)
            self.Stb.Cursor = self.Stb.SelectStart + relativeCursorOffset;
        else
            self.Stb.Cursor = self.Stb.SelectEnd + 1 + relativeCursorOffset;
        self.SanitizeSelectionRange();
    }

    public static void SanitizeSelectionRange(this ImGuiInputTextStatePtr self)
    {
        ref var s = ref self.Stb.SelectStart;
        ref var e = ref self.Stb.SelectEnd;
        ref var c = ref self.Stb.Cursor;
        s = Math.Clamp(s, 0, self.CurLenW);
        e = Math.Clamp(e, 0, self.CurLenW);
        c = Math.Clamp(c, 0, self.CurLenW);
        if (s == e)
            s = e = c;
        if (s > e)
            (s, e) = (e, s);
    }

    public static void Undo(this ImGuiInputTextStatePtr self) => ImGuiP.Custom_StbTextUndo(self);

    public static bool MakeUndoReplace(this ImGuiInputTextStatePtr self, int offset, int oldLength, int newLength)
    {
        if (oldLength == 0 && newLength == 0)
            return false;

        ImGuiP.Custom_StbTextMakeUndoReplace(self, offset, oldLength, newLength);
        return true;
    }

    public static bool ReplaceSelectionAndPushUndo(this ImGuiInputTextStatePtr self, ReadOnlySpan<char> newText)
    {
        var off = self.Stb.SelectStart;
        var len = self.Stb.SelectEnd - self.Stb.SelectStart;
        return self.MakeUndoReplace(off, len, newText.Length) && self.ReplaceChars(off, len, newText);
    }

    public static bool ReplaceChars(this ImGuiInputTextStatePtr self, int pos, int len, ReadOnlySpan<char> newText)
    {
        self.DeleteChars(pos, len);
        return self.InsertChars(pos, newText);
    }

    // See imgui_widgets.cpp: STB_TEXTEDIT_DELETECHARS
    public static void DeleteChars(this ImGuiInputTextStatePtr self, int pos, int n)
    {
        if (n == 0)
            return;

        var dst = (char*)self.TextW.Data + pos;

        // We maintain our buffer length in both UTF-8 and wchar formats
        self.Edited = true;
        self.CurLenA -= Encoding.UTF8.GetByteCount(dst, n);
        self.CurLenW -= n;

        // Offset remaining text (FIXME-OPT: Use memmove)
        var src = (char*)self.TextW.Data + pos + n;
        int i;
        for (i = 0; src[i] != 0; i++)
            dst[i] = src[i];
        dst[i] = '\0';
    }

    // See imgui_widgets.cpp: STB_TEXTEDIT_INSERTCHARS
    public static bool InsertChars(this ImGuiInputTextStatePtr self, int pos, ReadOnlySpan<char> newText)
    {
        if (newText.Length == 0)
            return true;

        var isResizable = (self.Flags & ImGuiInputTextFlags.CallbackResize) != 0;
        var textLen = self.CurLenW;
        Debug.Assert(pos <= textLen, "pos <= text_len");

        var newTextLenUtf8 = Encoding.UTF8.GetByteCount(newText);
        if (!isResizable && newTextLenUtf8 + self.CurLenA + 1 > self.BufCapacityA)
            return false;

        // Grow internal buffer if needed
        if (newText.Length + textLen + 1 > self.TextW.Size)
        {
            if (!isResizable)
                return false;

            Debug.Assert(textLen < self.TextW.Size, "text_len < self.TextW.Length");
            self.TextW.Resize(textLen + Math.Clamp(newText.Length * 4, 32, Math.Max(256, newText.Length)) + 1);
        }

        var text = new Span<char>(self.TextW.Data, self.TextW.Size);
        if (pos != textLen)
            text[pos..textLen].CopyTo(text[(pos + newText.Length)..]);
        newText.CopyTo(text[pos..]);

        self.Edited = true;
        self.CurLenW += newText.Length;
        self.CurLenA += newTextLenUtf8;
        self.TextW[self.CurLenW] = '\0';

        return true;
    }
}
