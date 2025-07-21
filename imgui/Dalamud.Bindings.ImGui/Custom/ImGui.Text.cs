using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    public static void AddText(ImFontGlyphRangesBuilderPtr self, AutoUtf8Buffer text)
    {
        fixed (byte* textPtr = text.Span)
            ImGuiNative.AddText(self.Handle, textPtr, textPtr + text.Length);
        text.Dispose();
    }

    public static void AddText(ImDrawListPtr self, Vector2 pos, uint col, AutoUtf8Buffer text)
    {
        fixed (byte* textPtr = text.Span)
            ImGuiNative.AddText(self.Handle, pos, col, textPtr, textPtr + text.Length);
        text.Dispose();
    }

    public static void AddText(
        ImDrawListPtr self, ImFontPtr font, float fontSize, Vector2 pos, uint col, AutoUtf8Buffer text, float wrapWidth,
        scoped in Vector4 cpuFineClipRect)
    {
        fixed (byte* textPtr = text.Span)
        fixed (Vector4* cpuFineClipRectPtr = &cpuFineClipRect)
            ImGuiNative.AddText(
                self.Handle,
                font,
                fontSize,
                pos,
                col,
                textPtr,
                textPtr + text.Length,
                wrapWidth,
                cpuFineClipRectPtr);
        text.Dispose();
    }

    public static void AddText(
        ImDrawListPtr self, ImFontPtr font, float fontSize, Vector2 pos, uint col, AutoUtf8Buffer text,
        float wrapWidth = 0f)
    {
        fixed (byte* textPtr = text.Span)
            ImGuiNative.AddText(
                self.Handle,
                font,
                fontSize,
                pos,
                col,
                textPtr,
                textPtr + text.Length,
                wrapWidth,
                null);
        text.Dispose();
    }

    public static void append(ImGuiTextBufferPtr self, AutoUtf8Buffer str)
    {
        fixed (byte* strPtr = str.Span)
            ImGuiNative.append(self.Handle, strPtr, strPtr + str.Length);
        str.Dispose();
    }

    public static void BulletText(AutoUtf8Buffer text)
    {
        ImGuiWindow* window = ImGuiP.GetCurrentWindow();
        if (window->SkipItems != 0)
            return;

        scoped ref var g = ref *GetCurrentContext().Handle;
        scoped ref readonly var style = ref g.Style;

        var labelSize = CalcTextSize(text.Span);
        var totalSize = new Vector2(
            g.FontSize + (labelSize.X > 0.0f ? (labelSize.X + style.FramePadding.X * 2) : 0.0f),
            labelSize.Y); // Empty text doesn't add padding
        var pos = window->DC.CursorPos;
        pos.Y += window->DC.CurrLineTextBaseOffset;
        ImGuiP.ItemSize(totalSize, 0.0f);
        var bb = new ImRect(pos, pos + totalSize);
        if (!ImGuiP.ItemAdd(bb, 0))
            return;

        // Render
        var textCol = GetColorU32(ImGuiCol.Text);
        ImGuiP.RenderBullet(
            window->DrawList,
            bb.Min + new Vector2(style.FramePadding.X + g.FontSize * 0.5f, g.FontSize * 0.5f),
            textCol);
        ImGuiP.RenderText(bb.Min + new Vector2(g.FontSize + style.FramePadding.X * 2, 0.0f), text.Span, false);
    }

    public static Vector2 CalcTextSize(
        AutoUtf8Buffer text, bool hideTextAfterDoubleHash = false, float wrapWidth = -1.0f)
    {
        var @out = Vector2.Zero;
        fixed (byte* textPtr = text.Span)
            ImGuiNative.CalcTextSize(
                &@out,
                textPtr,
                textPtr + text.Length,
                hideTextAfterDoubleHash ? (byte)1 : (byte)0,
                wrapWidth);
        text.Dispose();
        return @out;
    }

    public static Vector2 CalcTextSizeA(
        ImFontPtr self, float size, float maxWidth, float wrapWidth, AutoUtf8Buffer text, out int remaining)
    {
        var @out = Vector2.Zero;
        fixed (byte* textPtr = text.Span)
        {
            byte* remainingPtr = null;
            ImGuiNative.CalcTextSizeA(
                &@out,
                self.Handle,
                size,
                maxWidth,
                wrapWidth,
                textPtr,
                textPtr + text.Length,
                &remainingPtr);
            remaining = (int)(remainingPtr - textPtr);
        }

        text.Dispose();
        return @out;
    }

    public static int CalcWordWrapPositionA(ImFontPtr font, float scale, AutoUtf8Buffer text, float wrapWidth)
    {
        fixed (byte* ptr = text.Span)
        {
            var r =
                (int)(ImGuiNative.CalcWordWrapPositionA(font.Handle, scale, ptr, ptr + text.Length, wrapWidth) - ptr);
            text.Dispose();
            return r;
        }
    }

    public static void InsertChars(ImGuiInputTextCallbackDataPtr self, int pos, AutoUtf8Buffer text)
    {
        fixed (byte* ptr = text.Span)
            ImGuiNative.InsertChars(self.Handle, pos, ptr, ptr + text.Length);
        text.Dispose();
    }

    public static void LabelText(AutoUtf8Buffer label, AutoUtf8Buffer text)
    {
        var window = ImGuiP.GetCurrentWindow().Handle;
        if (window->SkipItems != 0)
        {
            label.Dispose();
            text.Dispose();
            return;
        }

        scoped ref var g = ref *GetCurrentContext().Handle;
        scoped ref readonly var style = ref g.Style;
        var w = CalcItemWidth();

        var valueSize = CalcTextSize(text);
        var labelSize = CalcTextSize(label, true);

        var pos = window->DC.CursorPos;
        var valueBb = new ImRect(pos, pos + new Vector2(w, valueSize.Y + style.FramePadding.Y * 2));
        var totalBb = new ImRect(
            pos,
            pos + new Vector2(
                w + (labelSize.X > 0.0f ? style.ItemInnerSpacing.X + labelSize.X : 0.0f),
                Math.Max(valueSize.Y, labelSize.Y) + style.FramePadding.Y * 2));
        ImGuiP.ItemSize(totalBb, style.FramePadding.Y);
        if (!ImGuiP.ItemAdd(totalBb, 0))
        {
            label.Dispose();
            text.Dispose();
            return;
        }

        // Render
        ImGuiP.RenderTextClipped(valueBb.Min + style.FramePadding, valueBb.Max, text.Span, valueSize, new(0.0f, 0.0f));
        if (labelSize.X > 0.0f)
        {
            ImGuiP.RenderText(
                new(valueBb.Max.X + style.ItemInnerSpacing.X, valueBb.Min.Y + style.FramePadding.Y),
                label.Span);
        }

        label.Dispose();
        text.Dispose();
    }

    public static void LogText(AutoUtf8Buffer text)
    {
        var g = GetCurrentContext();
        if (!g.LogFile.IsNull)
        {
            g.LogBuffer.Buf.Resize(0);
            append(&g.Handle->LogBuffer, text.Span);
            fixed (byte* textPtr = text.Span)
                ImGuiPNative.ImFileWrite(textPtr, 1, (ulong)text.Length, g.LogFile);
        }
        else
        {
            append(&g.Handle->LogBuffer, text);
        }

        text.Dispose();
    }

    public static void PassFilter(ImGuiTextFilterPtr self, AutoUtf8Buffer text)
    {
        fixed (byte* textPtr = text.Span)
            ImGuiNative.PassFilter(self.Handle, textPtr, textPtr + text.Length);
        text.Dispose();
    }

    public static void RenderText(
        ImFontPtr self, ImDrawListPtr drawList, float size, Vector2 pos, uint col, Vector4 clipRect,
        AutoUtf8Buffer text, float wrapWidth = 0.0f, bool cpuFineClip = false)
    {
        fixed (byte* textPtr = text.Span)
            ImGuiNative.RenderText(
                self,
                drawList,
                size,
                pos,
                col,
                clipRect,
                textPtr,
                textPtr + text.Length,
                wrapWidth,
                cpuFineClip ? (byte)1 : (byte)0);
        text.Dispose();
    }

    public static void SetTooltip(AutoUtf8Buffer text)
    {
        ImGuiP.BeginTooltipEx(ImGuiTooltipFlags.OverridePreviousTooltip, ImGuiWindowFlags.None);
        Text(text.Span);
        EndTooltip();
        text.Dispose();
    }

    public static void Text(AutoUtf8Buffer text)
    {
        fixed (byte* ptr = text.Span)
            ImGuiNative.TextUnformatted(ptr, ptr + text.Length);
        text.Dispose();
    }

    public static void TextColored(uint col, AutoUtf8Buffer text)
    {
        PushStyleColor(ImGuiCol.Text, col);
        Text(text.Span);
        PopStyleColor();
        text.Dispose();
    }

    public static void TextColored(scoped in Vector4 col, AutoUtf8Buffer text)
    {
        PushStyleColor(ImGuiCol.Text, col);
        Text(text.Span);
        PopStyleColor();
        text.Dispose();
    }

    public static void TextDisabled(AutoUtf8Buffer text)
    {
        TextColored(*GetStyleColorVec4(ImGuiCol.TextDisabled), text.Span);
        text.Dispose();
    }

    public static void TextUnformatted(AutoUtf8Buffer text)
    {
        Text(text.Span);
        text.Dispose();
    }

    public static void TextWrapped(AutoUtf8Buffer text)
    {
        scoped ref var g = ref *GetCurrentContext().Handle;
        var needBackup = g.CurrentWindow->DC.TextWrapPos < 0.0f; // Keep existing wrap position if one is already set
        if (needBackup)
            PushTextWrapPos(0.0f);
        Text(text.Span);
        if (needBackup)
            PopTextWrapPos();
        text.Dispose();
    }

    public static bool TreeNode(AutoUtf8Buffer label)
    {
        var window = ImGuiP.GetCurrentWindow();
        if (window.SkipItems)
        {
            label.Dispose();
            return false;
        }

        fixed (byte* labelPtr = label.Span)
        {
            var res = ImGuiP.TreeNodeBehavior(
                window.Handle->GetID(label.Span),
                ImGuiTreeNodeFlags.None,
                labelPtr,
                labelPtr + ImGuiP.FindRenderedTextEnd(label.Span, out _, out _));
            label.Dispose();
            return res;
        }
    }

    public static bool TreeNodeEx(
        AutoUtf8Buffer id, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None, AutoUtf8Buffer label = default)
    {
        var window = ImGuiP.GetCurrentWindow();
        bool res;
        if (window.SkipItems)
        {
            res = false;
        }
        else if (label.IsEmpty)
        {
            fixed (byte* ptr = id.Span)
            {
                res = ImGuiP.TreeNodeBehavior(
                    window.Handle->GetID(id.Span),
                    flags,
                    ptr,
                    ptr + ImGuiP.FindRenderedTextEnd(id.Span, out _, out _));
            }
        }
        else
        {
            fixed (byte* ptr = label.Span)
                res = ImGuiP.TreeNodeBehavior(window.Handle->GetID(id.Span), flags, ptr, ptr + label.Length);
        }

        id.Dispose();
        label.Dispose();
        return res;
    }
}
