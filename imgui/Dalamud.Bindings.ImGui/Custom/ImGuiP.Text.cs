using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Dalamud.Bindings.ImGui;

public static unsafe partial class ImGuiP
{
    public static void DebugLog(ImU8String text)
    {
        var g = ImGui.GetCurrentContext().Handle;
        ImGui.append(&g->DebugLogBuf, $"[{g->FrameCount:00000}] ");
        ImGui.append(&g->DebugLogBuf, text.Span);
        if ((g->DebugLogFlags & ImGuiDebugLogFlags.OutputToTty) != ImGuiDebugLogFlags.None)
            Debug.Write(text.ToString());
        text.Recycle();
    }

    /// <summary>Gets the layout parameters of text in the current font, as ImGui lays it out.</summary>
    /// <param name="desc">Receives the layout parameters.</param>
    /// <param name="wrapWidth">Wrap width. 0 or less disables wrapping.</param>
    /// <param name="flags">Layout flags.</param>
    /// <param name="drawList">The draw list to take the font size and pixel density from, or null for the current
    /// context's.</param>
    public static void GetTextDesc(
        out ImTextDesc desc, float wrapWidth = 0.0f, ImTextLayoutFlags flags = ImTextLayoutFlags.None,
        ImDrawListPtr drawList = default)
    {
        Unsafe.SkipInit(out desc);
        fixed (ImTextDesc* p = &desc)
            ImGuiPNative.GetTextDesc(p, wrapWidth, flags, drawList);
    }

    public static int FindRenderedTextEnd(
        ReadOnlySpan<byte> text, out ReadOnlySpan<byte> before, out ReadOnlySpan<byte> after)
    {
        if (text.IsEmpty)
        {
            before = after = text;
            return 0;
        }

        // Native reads past the end to look for a label suffix, so it gets a NUL-terminated copy.
        var terminated = new ImU8String(text, true);
        int r;
        fixed (byte* ptr = terminated)
            r = (int)(ImGuiPNative.FindRenderedTextEnd(ptr, ptr + text.Length) - ptr);
        terminated.Recycle();
        before = text[..r];
        after = text[r..];
        return r;
    }

    public static int FindRenderedTextEnd(
        ReadOnlySpan<char> text, out ReadOnlySpan<char> before, out ReadOnlySpan<char> after)
    {
        var textBuf = new ImU8String(text);
        FindRenderedTextEnd(textBuf.Span, out var beforeBytes, out var afterBytes);
        before = text[..Encoding.UTF8.GetCharCount(beforeBytes)];
        after = text[before.Length..];
        textBuf.Recycle();
        return before.Length;
    }

    public static uint GetID(ImGuiWindowPtr self, ImU8String str)
    {
        fixed (byte* strPtr = str)
        {
            var seed = *self.IDStack.Back;
            var id = ImGuiPNative.ImHashStr(strPtr, (nuint)str.Length, seed);
            var g = ImGui.GetCurrentContext();
            if (g.DebugHookIdInfoId == id)
                DebugHookIdInfo(id, ImGuiDataType.String, strPtr, strPtr + str.Length);
            str.Recycle();
            return id;
        }
    }

    public static uint GetID(ImGuiWindowPtr self, void* ptr) => ImGuiPNative.GetID(self.Handle, ptr);
    public static uint GetID(ImGuiWindowPtr self, int n) => ImGuiPNative.GetID(self.Handle, n);

    public static uint ImHashData(ReadOnlySpan<byte> data, uint seed = 0)
    {
        fixed (byte* ptr = data) return ImGuiPNative.ImHashData(ptr, (nuint)data.Length, seed);
    }

    public static uint ImHashStr(ImU8String data, uint seed = 0)
    {
        fixed (byte* ptr = data)
        {
            var res = ImGuiPNative.ImHashStr(ptr, (nuint)data.Length, seed);
            data.Recycle();
            return res;
        }
    }

    public static void ImParseFormatSanitizeForPrinting(ReadOnlySpan<byte> fmtIn, Span<byte> fmtOut)
    {
        fixed (byte* fmtInPtr = fmtIn)
        fixed (byte* fmtOutPtr = fmtOut)
            ImGuiPNative.ImParseFormatSanitizeForPrinting(fmtInPtr, fmtOutPtr, (nuint)fmtOut.Length);
    }

    public static void ImParseFormatSanitizeForScanning(ReadOnlySpan<byte> fmtIn, Span<byte> fmtOut)
    {
        fixed (byte* fmtInPtr = fmtIn)
        fixed (byte* fmtOutPtr = fmtOut)
            ImGuiPNative.ImParseFormatSanitizeForScanning(fmtInPtr, fmtOutPtr, (nuint)fmtOut.Length);
    }

    public static int ImStrchrRange<T>(ReadOnlySpan<T> str, T c, out ReadOnlySpan<T> before, out ReadOnlySpan<T> after)
        where T : unmanaged, IEquatable<T>
    {
        var i = str.IndexOf(c);
        if (i < 0)
        {
            before = after = default;
            return -1;
        }

        before = str[..i];
        after = str[i..];
        return i;
    }

    public static int ImStreolRange(
        ReadOnlySpan<byte> str, byte c, out ReadOnlySpan<byte> before, out ReadOnlySpan<byte> after)
    {
        var i = str.IndexOf((byte)'\n');
        if (i < 0)
        {
            before = after = default;
            return -1;
        }

        before = str[..i];
        after = str[i..];
        return i;
    }

    public static int ImStreolRange(
        ReadOnlySpan<char> str, char c, out ReadOnlySpan<char> before, out ReadOnlySpan<char> after)
    {
        var i = str.IndexOf('\n');
        if (i < 0)
        {
            before = after = default;
            return -1;
        }

        before = str[..i];
        after = str[i..];
        return i;
    }

    public static void LogRenderedText(scoped in Vector2 refPos, ImU8String text)
    {
        fixed (Vector2* refPosPtr = &refPos)
        fixed (byte* textPtr = text)
            ImGuiPNative.LogRenderedText(refPosPtr, textPtr, textPtr + text.Length);
        text.Recycle();
    }

    public static void RenderText(Vector2 pos, ImU8String text, bool hideTextAfterHash = true)
    {
        fixed (byte* textPtr = text)
            ImGuiPNative.RenderText(pos, textPtr, textPtr + text.Length, hideTextAfterHash ? (byte)1 : (byte)0);
        text.Recycle();
    }

    public static void RenderTextWrapped(
        Vector2 pos, ImU8String text, float wrapWidth)
    {
        fixed (byte* textPtr = text)
            ImGuiPNative.RenderTextWrapped(pos, textPtr, textPtr + text.Length, wrapWidth);
        text.Recycle();
    }

    public static void RenderTextClipped(
        scoped in Vector2 posMin, scoped in Vector2 posMax, ImU8String text,
        scoped in Vector2? textSizeIfKnown = null,
        scoped in Vector2 align = default, scoped in ImRect? clipRect = null)
    {
        var textSizeIfKnownOrDefault = textSizeIfKnown ?? default;
        var clipRectOrDefault = clipRect ?? default;
        fixed (byte* textPtr = text)
            ImGuiPNative.RenderTextClipped(
                posMin,
                posMax,
                textPtr,
                textPtr + text.Length,
                textSizeIfKnown.HasValue ? &textSizeIfKnownOrDefault : null,
                align,
                clipRect.HasValue ? &clipRectOrDefault : null);
        text.Recycle();
    }

    public static void RenderTextClippedEx(
        ImDrawListPtr drawList, scoped in Vector2 posMin, scoped in Vector2 posMax,
        ImU8String text,
        scoped in Vector2? textSizeIfKnown = null, scoped in Vector2 align = default, scoped in ImRect? clipRect = null)
    {
        var textSizeIfKnownOrDefault = textSizeIfKnown ?? default;
        var clipRectOrDefault = clipRect ?? default;
        fixed (byte* textPtr = text)
            ImGuiPNative.RenderTextClippedEx(
                drawList.Handle,
                posMin,
                posMax,
                textPtr,
                textPtr + text.Length,
                textSizeIfKnown.HasValue ? &textSizeIfKnownOrDefault : null,
                align,
                clipRect.HasValue ? &clipRectOrDefault : null);
        text.Recycle();
    }

    public static void RenderTextEllipsis(
        ImDrawListPtr drawList, scoped in Vector2 posMin, scoped in Vector2 posMax, float ellipsisMaxX,
        ImU8String text, scoped in Vector2? textSizeIfKnown = null)
    {
        var textSizeIfKnownOrDefault = textSizeIfKnown ?? default;
        fixed (byte* textPtr = text)
            ImGuiPNative.RenderTextEllipsis(
                drawList.Handle,
                posMin,
                posMax,
                ellipsisMaxX,
                textPtr,
                textPtr + text.Length,
                textSizeIfKnown.HasValue ? &textSizeIfKnownOrDefault : null);
        text.Recycle();
    }

    public static void TextEx(ReadOnlySpan<byte> text, ImGuiTextFlags flags)
    {
        fixed (byte* textPtr = text)
            ImGuiPNative.TextEx(textPtr, textPtr + text.Length, flags);
    }
}
