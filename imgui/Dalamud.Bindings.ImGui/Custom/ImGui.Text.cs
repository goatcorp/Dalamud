using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    /// <summary>Gets or sets what gives the font to push for a font, and the size to push it at.</summary>
    /// <remarks>A size of 0 keeps the current size.</remarks>
    internal static Func<ImFontPtr, (ImFontPtr Font, float Size)>? InherentFont { get; set; }

    /// <summary>Pushes a font at its own size, or at the current size if it has none.</summary>
    /// <param name="font">The font.</param>
    public static void PushFont(ImFontPtr font)
    {
        var (resolved, size) = font.IsNull ? (font, 0) : InherentFont?.Invoke(font) ?? (font, 0);
        PushFont(resolved, size);
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
                ImGuiSizeOf(font, fontSize),
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
        var size = ImGuiSizeOf(font, fontSize);
        fixed (byte* textPtr = text)
            ImGuiNative.AddText(self.Handle, font, size, pos, col, textPtr, textPtr + text.Length, wrapWidth, null);
        text.Recycle();
    }

    /// <summary>Draws text using a text descriptor.</summary>
    /// <param name="self">The draw list.</param>
    /// <param name="font">The font. Null for the draw list's current font.</param>
    /// <param name="desc">The text descriptor. A null reference uses the current style.</param>
    /// <param name="pos">Top-left position.</param>
    /// <param name="col">Color.</param>
    /// <param name="text">The text.</param>
    /// <param name="clipRect">Clip rectangle (x0, y0, x1, y1). Default for the draw list's.</param>
    /// <param name="renderFlags">Rendering-only flags.</param>
    /// <param name="transform">Transform around <paramref name="pos"/>. Default for none.</param>
    public static void AddTextEx(
        ImDrawListPtr self, ImFontPtr font, scoped in ImTextDesc desc, Vector2 pos, uint col, ImU8String text,
        scoped in Vector4 clipRect = default, ImTextRenderFlags renderFlags = ImTextRenderFlags.None,
        scoped in ImTextTransform transform = default)
    {
        var hasClip = !Unsafe.IsNullRef(in clipRect) && clipRect != default;
        var hasTransform = !Unsafe.IsNullRef(in transform) &&
                           (transform.AxisX != default || transform.AxisY != default);
        fixed (ImTextDesc* descPtr = &desc)
        fixed (Vector4* clipRectPtr = &clipRect)
        fixed (ImTextTransform* transformPtr = &transform)
        fixed (byte* textPtr = text)
        {
            ImGuiNative.AddTextEx(
                self,
                font,
                descPtr,
                pos,
                col,
                textPtr,
                textPtr + text.Length,
                hasClip ? clipRectPtr : null,
                renderFlags,
                hasTransform ? transformPtr : null);
        }

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
        if (window->SkipItems != 0)
        {
            text.Recycle();
            return;
        }

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
        {
            text.Recycle();
            return;
        }

        // Render
        var textCol = GetColorU32(ImGuiCol.Text);
        ImGuiP.RenderBullet(
            window->DrawList,
            bb.Min + new Vector2(style.FramePadding.X + g.FontSize * 0.5f, g.FontSize * 0.5f),
            textCol);
        ImGuiP.RenderText(bb.Min + new Vector2(g.FontSize + style.FramePadding.X * 2, 0.0f), text.Span, false);
        text.Recycle();
    }

    public static Vector2 CalcTextSize(
        ImU8String text, bool hideTextAfterDoubleHash = false, float wrapWidth = -1.0f)
    {
        Vector2 @out;
        fixed (byte* textPtr = text)
            @out = ImGuiNative.CalcTextSize(
                textPtr,
                textPtr + text.Length,
                hideTextAfterDoubleHash ? (byte)1 : (byte)0,
                wrapWidth);
        text.Recycle();
        return @out;
    }

    /// <summary>Measures text in a font at a size, like the removed <c>ImFont::CalcTextSizeA</c>.</summary>
    /// <param name="font">The font.</param>
    /// <param name="size">Size in pixels (after global scaling).</param>
    /// <param name="maxWidth">Stop measuring at this width.</param>
    /// <param name="wrapWidth">Wrap width. 0 or less disables wrapping.</param>
    /// <param name="text">The text.</param>
    /// <param name="remaining">Bytes measured.</param>
    /// <returns>The size.</returns>
    public static Vector2 CalcTextSizeA(
        ImFontPtr font, float size, float maxWidth, float wrapWidth, ImU8String text, out int remaining)
    {
        ImGuiP.GetTextDesc(out var desc, wrapWidth);
        desc.Size = size;
        desc.MaxWidth = maxWidth;
        Vector2 @out;
        fixed (byte* textPtr = text)
        {
            byte* remainingPtr = null;
            @out = ImGuiPNative.CalcTextSizeEx(
                font.Handle,
                &desc,
                textPtr,
                textPtr + text.Length,
                textPtr + text.Length,
                &remainingPtr,
                null);
            remaining = (int)(remainingPtr - textPtr);
        }

        text.Recycle();
        return @out;
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

        var valueSize = CalcTextSize(text.Span);
        var labelSize = CalcTextSize(label.Span, true);

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
            append(&g.Handle->LogBuffer, text.Span);
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

    /// <summary>Draws text in a font at a size, like the removed <c>ImFont::RenderText</c>.</summary>
    /// <param name="font">The font.</param>
    /// <param name="drawList">The draw list.</param>
    /// <param name="size">Size in pixels (after global scaling).</param>
    /// <param name="pos">Top-left position.</param>
    /// <param name="col">Color.</param>
    /// <param name="clipRect">Clip rectangle.</param>
    /// <param name="text">The text.</param>
    /// <param name="wrapWidth">Wrap width. 0 or less disables wrapping.</param>
    /// <param name="cpuFineClip">Clip glyph quads on the CPU.</param>
    public static void RenderText(
        ImFontPtr font, ImDrawListPtr drawList, float size, Vector2 pos, uint col, Vector4 clipRect,
        ImU8String text, float wrapWidth = 0.0f, bool cpuFineClip = false)
    {
        ImGuiP.GetTextDesc(out var desc, wrapWidth, drawList: drawList);
        desc.Size = size;
        fixed (byte* textPtr = text)
        {
            ImGuiNative.AddTextEx(
                drawList,
                font,
                &desc,
                pos,
                col,
                textPtr,
                textPtr + text.Length,
                &clipRect,
                cpuFineClip ? ImTextRenderFlags.CpuFineClip : ImTextRenderFlags.None,
                null);
        }

        text.Recycle();
    }

    /// <summary>Sets a text tooltip if the last item is hovered.</summary>
    /// <param name="text">The text. It is not used as a format string.</param>
    public static void SetItemTooltip(ImU8String text)
    {
        if (IsItemHovered(ImGuiHoveredFlags.ForTooltip))
            SetTooltip(text.Span);
        text.Recycle();
    }

    public static void SetTooltip(ImU8String text)
    {
        ImGuiP.BeginTooltipEx(ImGuiTooltipFlags.OverridePrevious, ImGuiWindowFlags.None);
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

    private static float InherentSizeOf(ImFontPtr font) => font.IsNull ? 0 : InherentFont?.Invoke(font).Size ?? 0;

    private static float ImGuiSizeOf(ImFontPtr font, float fontSize) =>
        fontSize == 0 ? InherentSizeOf(font) * GetStyle().FontScaleMain : fontSize;
}
