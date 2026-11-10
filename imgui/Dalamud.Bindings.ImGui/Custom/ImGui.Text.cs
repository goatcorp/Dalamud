using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    public static void AddText(ImFontGlyphRangesBuilderPtr self, ImU8String text)
    {
        fixed (byte* textPtr = text)
            ImGuiNative.AddText(self.Handle, textPtr, textPtr + text.Length);
        text.Recycle();
    }

    public static void AddText(ImDrawListPtr self, Vector2 pos, uint col, ImU8String text)
    {
        fixed (byte* textPtr = text)
            ImGuiNative.AddText(self.Handle, pos, col, textPtr, textPtr + text.Length);
        text.Recycle();
    }

    public static void AddText(
        ImDrawListPtr self, ImFontPtr font, float fontSize, Vector2 pos, uint col, ImU8String text, float wrapWidth,
        scoped in Vector4 cpuFineClipRect)
    {
        fixed (byte* textPtr = text)
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
        text.Recycle();
    }

    public static void AddText(
        ImDrawListPtr self, ImFontPtr font, float fontSize, Vector2 pos, uint col, ImU8String text,
        float wrapWidth = 0f)
    {
        fixed (byte* textPtr = text)
            ImGuiNative.AddText(self.Handle, font, fontSize, pos, col, textPtr, textPtr + text.Length, wrapWidth, null);
        text.Recycle();
    }

    public static void append(this ImGuiTextBufferPtr self, ImU8String str)
    {
        fixed (byte* strPtr = str)
            ImGuiNative.append(self.Handle, strPtr, strPtr + str.Length);
        str.Recycle();
    }

    public static void BulletText(ImU8String text)
    {
        ImGuiWindow* window = ImGuiP.GetCurrentWindow();
        if (window->SkipItems != 0) return;
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
        ImU8String text, bool hideTextAfterDoubleHash = false, float wrapWidth = -1.0f)
    {
        var @out = Vector2.Zero;
        fixed (byte* textPtr = text)
            ImGuiNative.CalcTextSize(
                &@out,
                textPtr,
                textPtr + text.Length,
                hideTextAfterDoubleHash ? (byte)1 : (byte)0,
                wrapWidth);
        text.Recycle();
        return @out;
    }

    public static Vector2 CalcTextSizeA(
        ImFontPtr self, float size, float maxWidth, float wrapWidth, ImU8String text, out int remaining)
    {
        var @out = Vector2.Zero;
        fixed (byte* textPtr = text)
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

        text.Recycle();
        return @out;
    }

    public static int CalcWordWrapPositionA(
        ImFontPtr font, float scale, ImU8String text, float wrapWidth)
    {
        fixed (byte* ptr = text)
        {
            var r =
                (int)(ImGuiNative.CalcWordWrapPositionA(font.Handle, scale, ptr, ptr + text.Length, wrapWidth) - ptr);
            text.Recycle();
            return r;
        }
    }

    public static void InsertChars(
        ImGuiInputTextCallbackDataPtr self, int pos, ImU8String text)
    {
        fixed (byte* ptr = text)
            ImGuiNative.InsertChars(self.Handle, pos, ptr, ptr + text.Length);
        text.Recycle();
    }

    public static void LabelText(
        ImU8String label,
        ImU8String text)
    {
        var window = ImGuiP.GetCurrentWindow().Handle;
        if (window->SkipItems != 0)
        {
            label.Recycle();
            text.Recycle();
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
            label.Recycle();
            text.Recycle();
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

        label.Recycle();
        text.Recycle();
    }

    public static void LogText(ImU8String text)
    {
        var g = GetCurrentContext();
        if (!g.LogFile.IsNull)
        {
            g.LogBuffer.Buf.Resize(0);
            append(&g.Handle->LogBuffer, text.Span);
            fixed (byte* textPtr = text)
                ImGuiPNative.ImFileWrite(textPtr, 1, (ulong)text.Length, g.LogFile);
        }
        else
        {
            append(&g.Handle->LogBuffer, text);
        }

        text.Recycle();
    }

    public static bool PassFilter(ImGuiTextFilterPtr self, ImU8String text)
    {
        fixed (byte* textPtr = text)
        {
            var r = ImGuiNative.PassFilter(self.Handle, textPtr, textPtr + text.Length) != 0;
            text.Recycle();
            return r;
        }
    }

    public static void RenderText(
        ImFontPtr self, ImDrawListPtr drawList, float size, Vector2 pos, uint col, Vector4 clipRect,
        ImU8String text, float wrapWidth = 0.0f, bool cpuFineClip = false)
    {
        fixed (byte* textPtr = text)
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
        text.Recycle();
    }

    public static void SetTooltip(ImU8String text)
    {
        ImGuiP.BeginTooltipEx(ImGuiTooltipFlags.OverridePreviousTooltip, ImGuiWindowFlags.None);
        Text(text.Span);
        EndTooltip();
        text.Recycle();
    }

    public static void Text(ImU8String text)
    {
        fixed (byte* ptr = text)
            ImGuiNative.TextUnformatted(ptr, ptr + text.Length);
        text.Recycle();
    }

    public static void TextColored(uint col, ImU8String text)
    {
        PushStyleColor(ImGuiCol.Text, col);
        Text(text.Span);
        PopStyleColor();
        text.Recycle();
    }

    public static void TextColored(scoped in Vector4 col, ImU8String text)
    {
        PushStyleColor(ImGuiCol.Text, col);
        Text(text.Span);
        PopStyleColor();
        text.Recycle();
    }

    public static void TextDisabled(ImU8String text)
    {
        TextColored(*GetStyleColorVec4(ImGuiCol.TextDisabled), text.Span);
        text.Recycle();
    }

    public static void TextUnformatted(ImU8String text)
    {
        Text(text.Span);
        text.Recycle();
    }

    public static void TextWrapped(ImU8String text)
    {
        scoped ref var g = ref *GetCurrentContext().Handle;
        var needBackup = g.CurrentWindow->DC.TextWrapPos < 0.0f; // Keep existing wrap position if one is already set
        if (needBackup)
            PushTextWrapPos(0.0f);
        Text(text.Span);
        if (needBackup)
            PopTextWrapPos();
        text.Recycle();
    }

    public static void TextColoredWrapped(uint col, ImU8String text)
    {
        PushStyleColor(ImGuiCol.Text, col);
        TextWrapped(text.Span);
        PopStyleColor();
        text.Recycle();
    }

    public static void TextColoredWrapped(scoped in Vector4 col, ImU8String text)
    {
        PushStyleColor(ImGuiCol.Text, col);
        TextWrapped(text.Span);
        PopStyleColor();
        text.Recycle();
    }

    public static bool TreeNode(ImU8String label)
    {
        var window = ImGuiP.GetCurrentWindow();
        if (window.SkipItems)
        {
            label.Recycle();
            return false;
        }

        var res = ImGuiP.TreeNodeBehavior(
            window.Handle->GetID(label.Span),
            ImGuiTreeNodeFlags.None,
            label.Span[..ImGuiP.FindRenderedTextEnd(label.Span, out _, out _)]);
        label.Recycle();
        return res;
    }

    public static bool TreeNodeEx(
        ImU8String id, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None,
        ImU8String label = default)
    {
        var window = ImGuiP.GetCurrentWindow();
        bool res;
        if (window.SkipItems)
        {
            res = false;
        }
        else if (label.IsNull)
        {
            res = ImGuiP.TreeNodeBehavior(
                window.Handle->GetID(id.Span),
                flags,
                id.Span[..ImGuiP.FindRenderedTextEnd(id.Span, out _, out _)]);
        }
        else
        {
            res = ImGuiP.TreeNodeBehavior(window.Handle->GetID(id.Span), flags, label.Span[..label.Length]);
        }

        id.Recycle();
        label.Recycle();
        return res;
    }
}
