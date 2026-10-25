using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal;
using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Fonts.Internal.Layout;
using Dalamud.Interface.Fonts.Internal.Markup;
using Dalamud.Utility;

using Lumina.Text;
using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.Fonts;

/// <summary>Draws and lays out text in the current font.</summary>
/// <remarks>Main thread only, as ImGui is. Off the main thread, the methods throw
/// <see cref="InvalidOperationException"/>.</remarks>
[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101", Justification = "Extension receivers are not members")]
public static unsafe class ImGuiTextExtensions
{
    extension(ImGui)
    {
        /// <summary>Draws text of a markup, through <see cref="ImGui.Text(ImU8String)"/>.</summary>
        /// <param name="text">The text.</param>
        /// <param name="markup">The markup.</param>
        /// <param name="callback">The callback to push for this text only. Keep it between frames.</param>
        /// <param name="wrap">Whether to wrap the text at the end of the window.</param>
        /// <remarks>The text is laid out as one, however long. <c>##</c> in it is drawn.</remarks>
        public static void TextMarkup(
            ImU8String text, TextMarkup markup, ITextMarkupCallback? callback = null, bool wrap = false)
        {
            using var pop = ImGui.PushTextMarkup(markup, callback);
            PlainText(text.Span, wrap);
            text.Recycle();
        }

        /// <summary>Draws a SeString compiled from <paramref name="text"/>.</summary>
        /// <param name="text">Macro string to compile to a SeString.</param>
        /// <param name="callback">The callback to push for this text only. Keep it between frames.</param>
        /// <param name="wrap">Whether to wrap the text.</param>
        public static void TextMacroString(
            ImU8String text, ITextMarkupCallback? callback = null, bool wrap = false) =>
            ImGui.TextMarkup(text, Fonts.TextMarkup.MacroString, callback, wrap);

        /// <summary>Draws a SeString.</summary>
        /// <param name="text">Compiled SeString.</param>
        /// <param name="callback">The callback to push for this text only. Keep it between frames.</param>
        /// <param name="wrap">Whether to wrap the text.</param>
        public static void TextSeString(
            ReadOnlySpan<byte> text, ITextMarkupCallback? callback = null, bool wrap = false) =>
            ImGui.TextMarkup(text, Fonts.TextMarkup.SeString, callback, wrap);

        /// <inheritdoc cref="TextSeString(ReadOnlySpan{byte},ITextMarkupCallback?,bool)"/>
        public static void TextSeString(
            SeStringBuilder text, ITextMarkupCallback? callback = null, bool wrap = false) =>
            ImGui.TextSeString(text.GetViewAsSpan(), callback, wrap);

        /// <inheritdoc cref="TextSeString(ReadOnlySpan{byte},ITextMarkupCallback?,bool)"/>
        public static void TextSeString(
            RentedSeStringBuilder text, ITextMarkupCallback? callback = null, bool wrap = false) =>
            ImGui.TextSeString(text.Builder, callback, wrap);

        /// <summary>Draws a layout as an item, as <see cref="ImGui.Text(ImU8String)"/> draws text.</summary>
        /// <param name="layout">The layout, such as from
        /// <see cref="CreateTextLayout(ReadOnlySpan{byte},ReadOnlySpan{TextStyleRange},float,int,ImTextLayoutFlags)"/>.
        /// </param>
        /// <param name="color">The text color, or <see langword="null"/> for <see cref="ImGuiCol.Text"/>.</param>
        /// <remarks>The item is the size of the layout box, placed on the text baseline of the current line.</remarks>
        public static void TextLayout(ImTextLayoutPtr layout, ImTextColor? color = null)
        {
            ThreadSafety.AssertMainThread();
            if (layout.IsNull)
                return;
            var box = layout.GetBoundingBox(out _);
            if (TryAddTextItem(new(box.Z, box.W), out var pos))
                layout.Render(ImGui.GetWindowDrawList(), pos, color ?? ImGui.GetColorU32(ImGuiCol.Text));
        }

        /// <summary>Pushes a callback that decides how text with a markup is drawn.</summary>
        /// <param name="callback">The callback. It can be <see langword="null"/> if <paramref name="inherit"/> is
        /// <see langword="false"/>, to only cut off the earlier callbacks.</param>
        /// <param name="inherit">Whether the callbacks pushed earlier are still asked. Set it to <see langword="false"/>
        /// to keep an outer callback, such as one finding links, away from a widget.</param>
        /// <returns>A disposable object that pops the callback.</returns>
        /// <remarks>For text with <see cref="TextMarkup.Plain"/> as its <see cref="TextStyle.Markup"/>, only
        /// <see cref="ITextMarkupCallback.FindLinks"/> is asked, and the link members for the links it found.
        /// </remarks>
        /// <seealso cref="ImGuiTextStyleExtensions.extension(ImGui).PushTextMarkup"/>
        public static ImPopFontDisposable PushTextCallback(ITextMarkupCallback? callback, bool inherit = true)
        {
            ThreadSafety.AssertMainThread();
            TextCallbackStack.Push(callback, inherit);
            return new(textCallback: true);
        }

        /// <summary>Makes layouts made with a callback stale, so that its answers are asked again.</summary>
        /// <param name="callback">The callback.</param>
        public static void InvalidateTextCallback(ITextMarkupCallback callback) =>
            TextCallbackStack.Invalidate(callback ?? throw new ArgumentNullException(nameof(callback)));

        /// <summary>Draws a styled text.</summary>
        /// <param name="text">Text to draw.</param>
        /// <param name="style">Style to apply.</param>
        /// <param name="wrap">Whether to wrap the text.</param>
        public static void TextStyled(ImU8String text, scoped in TextStyle style, bool wrap = false)
        {
            using var pop = style.Push();
            PlainText(text, wrap);
        }

        /// <summary>Draws a text with ranges in other styles, laid out as one text, as <see cref="ImGui.Text"/>
        /// does.</summary>
        /// <seealso cref="ImGui.PushTextWrapPos(float)"/>
        /// <param name="text">UTF-8 text to draw.</param>
        /// <param name="ranges">The ranges, in units of <paramref name="text"/>, in any order. Ranges of different
        /// fields compose, and of the same field, the later one wins. Text outside them is in the current style.
        /// </param>
        /// <param name="wrap">Whether to wrap the text at the end of the window.</param>
        /// <example><code>
        /// var wavy = new TextLineDecoration(TextLinePosition.Under, TextLineStyle.Wavy, 0xFF0000FF);
        /// ImGui.TextStyledRanges(
        ///     "Hello, world"u8,
        ///     [
        ///         TextStyleRange.Lines(0, 5, wavy),
        ///         TextStyleRange.Color(7, 5, 0xFF40C040),
        ///         TextStyleRange.Bold(7, 5),
        ///     ]);
        /// </code></example>
        public static void TextStyledRanges(
            ReadOnlySpan<byte> text, ReadOnlySpan<TextStyleRange> ranges, bool wrap = false) =>
            TextStyledRanges(text, 1, ranges, wrap);

        /// <inheritdoc cref="TextStyledRanges(ReadOnlySpan{byte},ReadOnlySpan{TextStyleRange},bool)"/>
        /// <param name="text">Text to draw.</param>
        /// <param name="ranges">The ranges, in units of <paramref name="text"/>, in any order. Ranges of different
        /// fields compose, and of the same field, the later one wins. Text outside them is in the current style.
        /// </param>
        /// <param name="wrap">Whether to wrap the text at the end of the window.</param>
        public static void TextStyledRanges(
            ReadOnlySpan<char> text, ReadOnlySpan<TextStyleRange> ranges, bool wrap = false) =>
            TextStyledRanges(MemoryMarshal.AsBytes(text), 2, ranges, wrap);

        /// <summary>Lays out a text in the current font and size, with ranges in other styles, to draw and to query.
        /// </summary>
        /// <param name="text">UTF-8 text.</param>
        /// <param name="ranges">The ranges, in units of <paramref name="text"/>, in any order. For overlapping ranges,
        /// the later one wins. Text outside them is in the current style.</param>
        /// <param name="wrapWidth">The width to wrap at, or 0 for no wrapping.</param>
        /// <param name="tabSize">Spaces between tab stops, counted from the start of each line, or 0 for 4.</param>
        /// <param name="flags">Layout flags.</param>
        /// <returns>The layout.</returns>
        /// <example><code>
        /// using var layout = ImGui.CreateTextLayout(line, highlights, tabSize: 4);
        /// layout.Ptr.Render(ImGui.GetWindowDrawList(), pos, ImGui.GetColorU32(ImGuiCol.Text));
        /// var caret = layout.Ptr.HitTest(ImGui.GetMousePos() - pos);
        /// </code></example>
        public static ImTextLayoutRef CreateTextLayout(
            ReadOnlySpan<byte> text, ReadOnlySpan<TextStyleRange> ranges = default, float wrapWidth = 0,
            int tabSize = 0, ImTextLayoutFlags flags = ImTextLayoutFlags.None) =>
            CreateTextLayout(text, 1, ranges, wrapWidth, tabSize, flags);

        /// <inheritdoc cref="CreateTextLayout(ReadOnlySpan{byte},ReadOnlySpan{TextStyleRange},float,int,ImTextLayoutFlags)" path="/summary"/>
        /// <param name="text">Text to lay out.</param>
        /// <param name="ranges">The ranges, in chars of <paramref name="text"/>, as for
        /// <see cref="TextStyledRanges(ReadOnlySpan{char},ReadOnlySpan{TextStyleRange},bool)"/>.</param>
        /// <param name="wrapWidth">The width to wrap at, or 0 for no wrapping.</param>
        /// <param name="tabSize">Spaces between tab stops, counted from the start of each line, or 0 for 4.</param>
        /// <param name="flags">Layout flags.</param>
        /// <returns>The layout.</returns>
        /// <remarks>The offsets the layout reports and takes, such as those of <see cref="ImTextLine"/> and
        /// <see cref="ImTextCaret"/>, are in bytes of the text, 2 per char. Divide them by 2 to index the text. Only
        /// <see cref="TextStyleRange"/> is in chars.</remarks>
        public static ImTextLayoutRef CreateTextLayout(
            ReadOnlySpan<char> text, ReadOnlySpan<TextStyleRange> ranges = default, float wrapWidth = 0,
            int tabSize = 0, ImTextLayoutFlags flags = ImTextLayoutFlags.None) =>
            CreateTextLayout(MemoryMarshal.AsBytes(text), 2, ranges, wrapWidth, tabSize, flags);

        /// <inheritdoc cref="CreateTextLayout(ReadOnlySpan{byte},ReadOnlySpan{TextStyleRange},float,int,ImTextLayoutFlags)" path="/summary"/>
        /// <param name="text">The text.</param>
        /// <param name="markup">The markup.</param>
        /// <param name="wrapWidth">The width to wrap at, or 0 for no wrapping.</param>
        /// <param name="tabSize">Spaces between tab stops, counted from the start of each line, or 0 for 4.</param>
        /// <param name="flags">Layout flags.</param>
        /// <returns>The layout.</returns>
        public static ImTextLayoutRef CreateTextLayout(
            ReadOnlySpan<byte> text, TextMarkup markup, float wrapWidth = 0, int tabSize = 0,
            ImTextLayoutFlags flags = ImTextLayoutFlags.None)
        {
            ThreadSafety.AssertMainThread();
            if (FontImpl.FromNative(ImGui.Font.Handle) is not { } font)
                return default;
            font = font.WithMarkup(markup);
            return new(NativeTextLayouts.ToNative(
                           TextLayouter.CreateStyled(font, DescOf(font, wrapWidth, tabSize, flags), text)));
        }

        /// <summary>Lays out a SeString in the current font and size as ImGui draws one, to draw and to query.
        /// </summary>
        /// <param name="text">The SeString.</param>
        /// <param name="wrapWidth">The width to wrap at, or 0 for no wrapping.</param>
        /// <param name="tabSize">Spaces between tab stops, counted from the start of each line, or 0 for 4.</param>
        /// <param name="flags">Layout flags.</param>
        /// <returns>The layout.</returns>
        public static ImTextLayoutRef CreateTextLayout(
            ReadOnlySeStringSpan text, float wrapWidth = 0, int tabSize = 0,
            ImTextLayoutFlags flags = ImTextLayoutFlags.None) =>
            ImGui.CreateTextLayout(text.Data, Fonts.TextMarkup.SeString, wrapWidth, tabSize, flags);

        /// <summary>Finds where text wraps first in the current font and size.</summary>
        /// <param name="text">The text.</param>
        /// <param name="wrapWidth">The width to wrap at.</param>
        /// <returns>The char offset where the first line ends, before its line break and trailing blanks, or the
        /// length of the text if it is one line.</returns>
        public static int CalcWordWrapPosition(ReadOnlySpan<char> text, float wrapWidth)
        {
            using var layout = ImGui.CreateTextLayout(text, wrapWidth: wrapWidth);
            var lines = layout.IsNull ? default : layout.Ptr.GetLines();
            return lines.Length > 1 ? lines[0].TextEnd / 2 : text.Length;
        }

        /// <summary>Gets what happened to a link of the styled text drawn last in this frame, like
        /// <see cref="ImGui.IsItemClicked()"/> after an item.</summary>
        /// <param name="interaction">What happened.</param>
        /// <param name="flags">What to look for.</param>
        /// <returns>Whether any of <paramref name="flags"/> happened.</returns>
        /// <remarks>Links are of text in fonts with <see cref="TextStyle.Markup"/>, drawn into the current window.
        /// Markup text drawn later replaces what is reported, even without links. Plain text does not.</remarks>
        public static bool TryGetTextInteraction(
            out TextInteraction interaction, TextInteractionFlags flags = TextInteractionFlags.LeftClick)
        {
            ThreadSafety.AssertMainThread();
            if (TextInteractionState.TryGet(out interaction) && (interaction.Flags & flags) != 0)
                return true;

            interaction = default;
            return false;
        }

        /// <summary>Marks all glyphs used from now on to be rasterized before this frame gets presented.</summary>
        /// <returns>A disposable object that stops the effect.</returns>
        /// <seealso cref="ImTextRenderFlags.CompleteGlyphs"/>
        public static ImPopFontDisposable PushCompleteGlyphs() => GlyphRasterBudget.BeginSynchronous();
    }

    /// <param name="drawList">The draw list.</param>
    extension(ImDrawListPtr drawList)
    {
        /// <summary>Draws text in the current text style, fit into a box and centered in it.</summary>
        /// <param name="min">The top left of the box, in screen coordinates as for
        /// <see cref="ImDrawListPtr.AddText(Vector2,uint,ImU8String)"/>.</param>
        /// <param name="max">The bottom right of the box.</param>
        /// <param name="text">The text.</param>
        /// <param name="color">The text color.</param>
        /// <param name="upscale">Whether to scale text smaller than the box up to fit it.</param>
        /// <returns><see langword="true"/> if anything was drawn, or <see langword="false"/> if the text draws
        /// nothing.</returns>
        /// <remarks>What is fit is the ink box, which is what the glyphs cover. The text is laid out again at the
        /// scaled size, so that the glyphs stay sharp.</remarks>
        public bool AddTextFitted(Vector2 min, Vector2 max, ImU8String text, ImTextColor color, bool upscale = false)
        {
            ThreadSafety.AssertMainThread();
            var drawn = FontImpl.FromNative(ImGui.Font.Handle) is { } font &&
                        AddTextFitted(drawList, font, min, max, text.Span, color, upscale);
            text.Recycle();
            return drawn;
        }
    }

    // Lays out text in a font and draws it fit into the box. Returns whether anything was drawn.
    private static bool AddTextFitted(
        ImDrawListPtr drawList, FontImpl font, Vector2 min, Vector2 max, ReadOnlySpan<byte> text, ImTextColor color,
        bool upscale)
    {
        var avail = Vector2.Max(max - min, Vector2.Zero);
        var layout = TextLayouter.CreateStyled(font, DescOf(font, 0, 0, ImTextLayoutFlags.None, drawList), text);
        if (ScaleToFit(layout.Ink, avail, upscale) is not { } scale)
            return false;
        if (scale != 1)
        {
            using (ImGui.PushFontScale(scale))
            {
                if (FontImpl.FromNative(ImGui.Font.Handle) is { } scaled)
                {
                    layout = TextLayouter.CreateStyled(
                        scaled, DescOf(scaled, 0, 0, ImTextLayoutFlags.None, drawList), text);
                }
            }
        }

        // Hinting and rounding can leave the new layout a little off the scaled size. A transform fixes what is left.
        var ink = layout.Ink;
        if (ScaleToFit(ink, avail, false) is not { } residual)
            return false;
        var inkMin = new Vector2(ink.X, ink.Y);
        var inkSize = new Vector2(ink.Z, ink.W) - inkMin;
        var pos = min + ((avail - (inkSize * residual)) / 2) - (inkMin * residual);
        if (residual < 0.999f)
            layout.Render(drawList, pos, color, transform: new ImTextTransform(new(residual, 0), new(0, residual)));
        else
            layout.Render(drawList, pos, color);
        return true;
    }

    // Gets the scale that fits an ink box into a size, or null if the ink box is empty.
    private static float? ScaleToFit(Vector4 ink, Vector2 avail, bool upscale)
    {
        var (w, h) = (ink.Z - ink.X, ink.W - ink.Y);
        if (!(w > 0 && h > 0))
            return null;
        var scale = MathF.Min(avail.X / w, avail.Y / h);
        return upscale ? scale : MathF.Min(scale, 1);
    }

    // Adds an item for text at the cursor, on the text baseline of the current line. Returns whether it is visible,
    // with where to draw the text.
    private static bool TryAddTextItem(Vector2 size, out Vector2 pos)
    {
        var window = ImGuiP.GetCurrentWindow();
        if (window.SkipItems)
        {
            pos = default;
            return false;
        }

        ref var dc = ref window.DC;
        pos = dc.CursorPos + new Vector2(0, dc.CurrLineTextBaseOffset);
        ImGuiP.ItemSize(size, 0);
        return ImGuiP.ItemAdd(new ImRect(pos, pos + size), 0);
    }

    private static void TextStyledRanges(
        ReadOnlySpan<byte> text, int codeUnitSize, ReadOnlySpan<TextStyleRange> ranges, bool wrap)
    {
        ThreadSafety.AssertMainThread();
        var window = ImGuiP.GetCurrentWindow();
        if (window.SkipItems)
            return;

        if (FontImpl.FromNative(ImGui.Font.Handle) is not { } font)
        {
            if (codeUnitSize == 1)
                PlainText(text, wrap);
            else
                PlainText(MemoryMarshal.Cast<byte, char>(text), wrap);
            return;
        }

        var wrapWidth = WrapWidthOf(window, wrap);
        var layout = CreateLayout(font, text, codeUnitSize, ranges, wrapWidth, 0, ImTextLayoutFlags.None);
        if (TryAddTextItem(layout.Size, out var pos))
            layout.Render(ImGui.GetWindowDrawList(), pos, ImGui.GetColorU32(ImGuiCol.Text));
    }

    // Draws text with ImGui, wrapping at the end of the window if asked. ImGui.Begin resets a wrap position that an
    // exception leaves.
    private static void PlainText(ImU8String text, bool wrap)
    {
        if (wrap)
            ImGui.PushTextWrapPos(0);
        ImGui.Text(text);
        if (wrap)
            ImGui.PopTextWrapPos();
    }

    // Gets the width ImGui.Text wraps text at, or 0 for none.
    private static float WrapWidthOf(ImGuiWindowPtr window, bool wrap)
    {
        ref var dc = ref window.DC;
        var wrapPos = wrap ? 0f : dc.TextWrapPos;
        return wrapPos >= 0 ? ImGuiP.CalcWrapWidthForPos(dc.CursorPos, wrapPos) : 0f;
    }

    // Gets the layout parameters at the current font size, as ImGui lays out text. Without a draw list, the pixel
    // density is that of the current context.
    private static TextLayoutDesc DescOf(
        FontImpl font, float wrapWidth, int tabSize, ImTextLayoutFlags flags, ImDrawListPtr drawList = default)
    {
        ImGuiP.GetTextDesc(out var desc, wrapWidth, flags, drawList);
        return TextLayoutDesc.FromNative(desc, font) with { TabSize = tabSize };
    }

    // Lays out text in the current font and size. Runs are split at every boundary of the ranges, and each takes the
    // ranges over it in order.
    private static TextLayout CreateLayout(
        FontImpl font, ReadOnlySpan<byte> text, int codeUnitSize, ReadOnlySpan<TextStyleRange> ranges,
        float wrapWidth, int tabSize, ImTextLayoutFlags flags)
    {
        var n = ranges.Length;
        var pooledBounds = PooledSpan<int>.Rent(n * 5);
        var bounds = pooledBounds.Span[..(n * 2)];
        var starts = pooledBounds.Span[(n * 2)..(n * 3)];
        var order = pooledBounds.Span[(n * 3)..(n * 4)];
        var active = pooledBounds.Span[(n * 4)..];
        for (var i = 0; i < n; i++)
        {
            (bounds[2 * i], bounds[(2 * i) + 1]) = (ranges[i].Offset, ranges[i].Offset + ranges[i].Length);
            (starts[i], order[i]) = (ranges[i].Offset, i);
        }

        bounds.Sort();
        starts.Sort(order);
        var pooledRuns = PooledSpan<TextRun>.Rent(Math.Max(bounds.Length - 1, 0));
        var runs = pooledRuns.Span;
        var (count, activeCount, nextStart) = (0, 0, 0);
        for (var i = 0; i + 1 < bounds.Length; i++)
        {
            var (begin, end) = (bounds[i], bounds[i + 1]);
            if (begin >= end)
                continue;
            for (; nextStart < n && starts[nextStart] <= begin; nextStart++)
            {
                var k = activeCount++;
                for (; k > 0 && active[k - 1] > order[nextStart]; k--)
                    active[k] = active[k - 1];
                active[k] = order[nextStart];
            }

            var run = font;
            var kept = 0;
            for (var j = 0; j < activeCount; j++)
            {
                ref readonly var r = ref ranges[active[j]];
                if (begin >= r.Offset + r.Length)
                    continue;
                active[kept++] = active[j];
                run = run.With(r);
            }

            activeCount = kept;
            if (run != font)
                runs[count++] = new(begin * codeUnitSize, end * codeUnitSize, run);
        }

        var res = TextLayouter.Create(
            font,
            DescOf(font, wrapWidth, tabSize, flags),
            text,
            codeUnitSize,
            runs[..count]);
        pooledRuns.Recycle();
        pooledBounds.Recycle();
        return res;
    }

    private static ImTextLayoutRef CreateTextLayout(
        ReadOnlySpan<byte> text, int codeUnitSize, ReadOnlySpan<TextStyleRange> ranges, float wrapWidth,
        int tabSize, ImTextLayoutFlags flags)
    {
        ThreadSafety.AssertMainThread();
        if (FontImpl.FromNative(ImGui.Font.Handle) is not { } font)
            return default;
        var layout = CreateLayout(font, text, codeUnitSize, ranges, wrapWidth, tabSize, flags);
        return new(NativeTextLayouts.ToNative(layout));
    }
}
