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
        s = Math.Clamp(s, 0, self.TextLen);
        e = Math.Clamp(e, 0, self.TextLen);
        c = Math.Clamp(c, 0, self.TextLen);
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

    public static bool ReplaceSelectionAndPushUndo(
        this ImGuiInputTextStatePtr self, ReadOnlySpan<char> newText, out int insertedLength)
    {
        insertedLength = 0;
        var off = self.Stb.SelectStart;
        var len = self.Stb.SelectEnd - self.Stb.SelectStart;
        var newTextLen = Encoding.UTF8.GetByteCount(newText);
        var rented = newTextLen <= 128 ? stackalloc byte[newTextLen] : new byte[newTextLen];
        Encoding.UTF8.GetBytes(newText, rented);

        // The undo record must match what gets inserted. Truncate like STB_TEXTEDIT_INSERTCHARS does.
        if ((self.Flags & ImGuiInputTextFlags.CallbackResize) == 0)
            rented = rented[..TruncateUtf8(rented, Math.Max(0, self.BufCapacity - 1 - self.TextLen + len))];

        if (!self.MakeUndoReplace(off, len, rented.Length) || !self.ReplaceChars(off, len, rented))
            return false;

        insertedLength = rented.Length;
        return true;
    }

    public static bool ReplaceChars(this ImGuiInputTextStatePtr self, int pos, int len, ReadOnlySpan<byte> newText)
    {
        self.DeleteChars(pos, len);
        return self.InsertChars(pos, newText);
    }

    // See imgui_widgets.cpp: STB_TEXTEDIT_DELETECHARS
    public static void DeleteChars(this ImGuiInputTextStatePtr self, int pos, int n)
    {
        if (n == 0)
            return;

        var text = self.TextA.AsSpan();
        text.Slice(pos + n, self.TextLen - n - pos + 1).CopyTo(text[pos..]);
        self.EditedBefore = self.EditedThisFrame = true;
        self.TextLen -= n;
    }

    // See imgui_widgets.cpp: STB_TEXTEDIT_INSERTCHARS
    public static bool InsertChars(this ImGuiInputTextStatePtr self, int pos, ReadOnlySpan<byte> newText)
    {
        if (newText.Length == 0)
            return true;

        var isResizable = (self.Flags & ImGuiInputTextFlags.CallbackResize) != 0;
        var textLen = self.TextLen;
        Debug.Assert(pos <= textLen, "pos <= text_len");

        if (!isResizable && newText.Length > self.BufCapacity - 1 - textLen)
            return false;

        // Grow internal buffer if needed
        if (textLen + newText.Length + 1 > self.TextA.Size)
        {
            if (!isResizable)
                return false;

            self.TextA.Resize(textLen + Math.Clamp(newText.Length, 32, Math.Max(256, newText.Length)) + 1);
            self.Handle->TextSrc = self.TextA.Data;
        }

        var text = self.TextA.AsSpan();
        if (pos != textLen)
            text[pos..textLen].CopyTo(text[(pos + newText.Length)..]);
        newText.CopyTo(text[pos..]);

        self.EditedBefore = self.EditedThisFrame = true;
        self.TextLen += newText.Length;
        text[self.TextLen] = 0;

        return true;
    }

    // Follows ImTextFindValidUtf8CodepointEnd of imgui.cpp.
    private static int TruncateUtf8(ReadOnlySpan<byte> text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text.Length;

        // Back off from continuation bytes to the start of the codepoint that does not fit.
        var len = maxLength;
        while (len > 0 && (text[len] & 0xC0) == 0x80)
            len--;
        return len;
    }
}
