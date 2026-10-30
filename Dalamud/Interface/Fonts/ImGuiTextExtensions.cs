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
        /// <summary>Draws text parsed according to <paramref name="markup"/>.</summary>
        /// <param name="text">The markup bytes.</param>
        /// <param name="markup">The markup format.</param>
        /// <param name="callback">Optional callback pushed for this call only. Reuse the same instance across frames so
        /// layouts stay cached.</param>
        /// <param name="wrap">Whether to wrap at the end of the window.</param>
        /// <remarks>Laid out as a single text block regardless of length. <c>##</c> is rendered literally.</remarks>
        public static void TextMarkup(
            ImU8String text, TextMarkup markup, ITextMarkupCallback? callback = null, bool wrap = false)
        {
            ThreadSafety.AssertMainThread();
            TextInteractionState.Reset();
            using var pop = ImGui.PushTextMarkup(markup, callback);
            PlainText(text, wrap);
        }

        /// <summary>Draws a SeString compiled from <paramref name="text"/>.</summary>
        /// <param name="text">Macro string to compile to a SeString.</param>
        /// <param name="callback">Optional callback pushed for this call only. Reuse the same instance across frames so
        /// layouts stay cached.</param>
        /// <param name="wrap">Whether to wrap the text.</param>
        public static void TextMacroString(
            ImU8String text, ITextMarkupCallback? callback = null, bool wrap = false) =>
            ImGui.TextMarkup(text, Fonts.TextMarkup.MacroString, callback, wrap);

        /// <summary>Draws a SeString.</summary>
        /// <param name="text">Compiled SeString.</param>
        /// <param name="callback">Optional callback pushed for this call only. Reuse the same instance across frames so
        /// layouts stay cached.</param>
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
        /// <see cref="CreateTextLayout(ReadOnlySpan{byte},ReadOnlySpan{TextStyleRange},TextLayoutOptions)"/>.
        /// </param>
        /// <param name="color">The text color, or <see langword="null"/> for <see cref="ImGuiCol.Text"/>.</param>
        /// <remarks>The item is the size of the layout box, placed on the text baseline of the current line.</remarks>
        public static void TextLayout(ImTextLayoutPtr layout, ImTextColor? color = null)
        {
            ThreadSafety.AssertMainThread();
            TextInteractionState.Reset();
            if (layout.IsNull)
                return;
            var box = layout.GetBoundingBox(out _);
            if (TryAddTextItem(new(box.Z, box.W), out var pos))
                layout.Render(ImGui.GetWindowDrawList(), pos, color ?? ImGui.GetColorU32(ImGuiCol.Text));
        }

        /// <summary>Pushes a callback that customizes markup entities and links.</summary>
        /// <param name="callback">The callback, or <see langword="null"/> when <paramref name="inherit"/> is
        /// <see langword="false"/> to suppress outer callbacks.</param>
        /// <param name="inherit">Whether callbacks pushed earlier remain active. Set to <see langword="false"/> to isolate
        /// inner widgets from outer callbacks such as URL detection.</param>
        /// <returns>A disposable scope that pops the callback.</returns>
        /// <remarks>For <see cref="TextMarkup.Plain"/> text, only <see cref="ITextMarkupCallback.FindLinks"/> and link
        /// styling and activation methods are invoked.</remarks>
        /// <seealso cref="ImGuiTextStyleExtensions.extension(ImGui).PushTextMarkup"/>
        public static ImPopFontDisposable PushTextCallback(ITextMarkupCallback? callback, bool inherit = true)
        {
            ThreadSafety.AssertMainThread();
            TextCallbackStack.Push(callback, inherit);
            return new(textCallback: true);
        }

        /// <summary>Invalidates cached text layouts built with <paramref name="callback"/> so they are rebuilt on next use.</summary>
        /// <param name="callback">The callback.</param>
        public static void InvalidateTextCallback(ITextMarkupCallback callback) =>
            TextCallbackStack.Invalidate(callback ?? throw new ArgumentNullException(nameof(callback)));

        /// <summary>Draws text in the given style.</summary>
        /// <param name="text">Text to draw.</param>
        /// <param name="style">Style to apply.</param>
        /// <param name="wrap">Whether to wrap the text.</param>
        public static void TextStyled(ImU8String text, scoped in TextStyle style, bool wrap = false)
        {
            using var pop = style.Push();
            PlainText(text, wrap);
        }

        /// <summary>Draws text with per-range style overrides, laid out as a single text block like
        /// <see cref="ImGui.Text"/>.</summary>
        /// <seealso cref="ImGui.PushTextWrapPos(float)"/>
        /// <param name="text">UTF-8 text to draw.</param>
        /// <param name="ranges">Style ranges in code units of <paramref name="text"/>, in any order. Ranges affecting
        /// different fields compose, and for the same field, the later range wins. Uncovered text uses the current
        /// style.</param>
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
        /// <param name="ranges">Style ranges in code units of <paramref name="text"/>, in any order. Ranges affecting
        /// different fields compose, and for the same field, the later range wins. Uncovered text uses the current
        /// style.</param>
        /// <param name="wrap">Whether to wrap the text at the end of the window.</param>
        public static void TextStyledRanges(
            ReadOnlySpan<char> text, ReadOnlySpan<TextStyleRange> ranges, bool wrap = false) =>
            TextStyledRanges(MemoryMarshal.AsBytes(text), 2, ranges, wrap);

        /// <summary>Lays out text in the current font and size with per-range style overrides for rendering and
        /// hit-testing.</summary>
        /// <param name="text">UTF-8 text.</param>
        /// <param name="ranges">Style ranges in code units of <paramref name="text"/>, in any order. Ranges affecting
        /// different fields compose, and for the same field, the later range wins. Uncovered text uses the current
        /// style.</param>
        /// <param name="options">How to lay out the text, such as the wrap width.</param>
        /// <returns>The layout.</returns>
        /// <example><code>
        /// using var layout = ImGui.CreateTextLayout(line, highlights, new() { TabSize = 8 });
        /// layout.Ptr.Render(ImGui.GetWindowDrawList(), pos, ImGui.GetColorU32(ImGuiCol.Text));
        /// var caret = layout.Ptr.HitTest(ImGui.GetMousePos() - pos);
        /// </code></example>
        public static ImTextLayoutRef CreateTextLayout(
            ReadOnlySpan<byte> text, ReadOnlySpan<TextStyleRange> ranges = default,
            TextLayoutOptions options = default) =>
            CreateTextLayout(text, 1, ranges, options);

        /// <inheritdoc cref="CreateTextLayout(ReadOnlySpan{byte},ReadOnlySpan{TextStyleRange},TextLayoutOptions)" path="/summary"/>
        /// <param name="text">Text to lay out.</param>
        /// <param name="ranges">The ranges, in chars of <paramref name="text"/>, as for
        /// <see cref="TextStyledRanges(ReadOnlySpan{char},ReadOnlySpan{TextStyleRange},bool)"/>.</param>
        /// <param name="options">How to lay out the text, such as the wrap width.</param>
        /// <returns>The layout.</returns>
        /// <remarks>The offsets the layout reports and takes, such as those of <see cref="ImTextLine"/> and
        /// <see cref="ImTextCaret"/>, are in chars of the text, as are those of <see cref="TextStyleRange"/>.</remarks>
        public static ImTextLayoutRef CreateTextLayout(
            ReadOnlySpan<char> text, ReadOnlySpan<TextStyleRange> ranges = default,
            TextLayoutOptions options = default) =>
            CreateTextLayout(MemoryMarshal.AsBytes(text), 2, ranges, options);

        /// <inheritdoc cref="CreateTextLayout(ReadOnlySpan{byte},ReadOnlySpan{TextStyleRange},TextLayoutOptions)" path="/summary"/>
        /// <param name="text">The text.</param>
        /// <param name="markup">The markup.</param>
        /// <param name="options">How to lay out the text, such as the wrap width.</param>
        /// <returns>The layout.</returns>
        public static ImTextLayoutRef CreateTextLayout(
            ReadOnlySpan<byte> text, TextMarkup markup, TextLayoutOptions options = default)
        {
            ThreadSafety.AssertMainThread();
            if (FontImpl.Current is not { } font)
                return default;
            font = font.WithMarkup(markup);
            return new(NativeTextLayouts.ToNative(TextLayouter.CreateStyled(font, DescOf(font, options), text)));
        }

        /// <summary>Lays out a SeString in the current font and size as ImGui draws one, to draw and to query.
        /// </summary>
        /// <param name="text">The SeString.</param>
        /// <param name="options">How to lay out the text, such as the wrap width.</param>
        /// <returns>The layout.</returns>
        public static ImTextLayoutRef CreateTextLayout(ReadOnlySeStringSpan text, TextLayoutOptions options = default) =>
            ImGui.CreateTextLayout(text.Data, Fonts.TextMarkup.SeString, options);

        /// <summary>Finds where the first line of text wraps in the current font and size.</summary>
        /// <param name="text">The text.</param>
        /// <param name="wrapWidth">The width to wrap at.</param>
        /// <returns>The char offset where the first line ends, before its line break and trailing blanks, or the
        /// length of the text if it is one line.</returns>
        public static int CalcWordWrapPosition(ReadOnlySpan<char> text, float wrapWidth)
        {
            using var layout = ImGui.CreateTextLayout(text, options: new() { WrapWidth = wrapWidth });
            var lines = layout.IsNull ? default : layout.Ptr.GetLines();
            return lines.Length > 1 ? lines[0].TextEnd : text.Length;
        }

        /// <summary>Checks whether a link in the most recently drawn styled text this frame was interacted with, similar to
        /// <see cref="ImGui.IsItemClicked()"/>.</summary>
        /// <param name="interaction">The link interaction details when returning <see langword="true"/>.</param>
        /// <param name="flags">Interaction flags to match.</param>
        /// <returns><see langword="true"/> if any flag in <paramref name="flags"/> occurred, or <see langword="false"/>
        /// otherwise.</returns>
        /// <remarks><ul>
        /// <li>Links are of text in fonts with <see cref="TextStyle.Markup"/>, drawn into the current window.</li>
        /// <li>Markup text drawn later for another item replaces what is reported, even without links, and so does any
        /// item submitted later, even a clipped one. Markup text drawn later for the same item, such as the label of
        /// <see cref="ImGui.LabelText(ImU8String, ImU8String)"/>, replaces it only if a link of it is hovered or held.
        /// Plain text replaces it only while callbacks are pushed. Text that is only measured does not.</li>
        /// <li>Links in the label of a widget that takes the mouse are never hovered or clicked, as the widget is
        /// hovered first. Such widgets are
        /// <see cref="ImGui.Selectable(ImU8String, bool, ImGuiSelectableFlags, Vector2)"/>,
        /// <see cref="ImGui.TreeNodeEx(ImU8String, ImGuiTreeNodeFlags, ImU8String)"/> and
        /// <see cref="ImGui.Button(ImU8String, Vector2)"/>.</li>
        /// </ul></remarks>
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
        /// <remarks>Fits the full ink bounds, covering the layout box plus glyph overhangs, edges, and shadows. Re-lays
        /// out at the scaled size so glyphs stay sharp.</remarks>
        public bool AddTextFitted(Vector2 min, Vector2 max, ImU8String text, ImTextColor color, bool upscale = false)
        {
            ThreadSafety.AssertMainThread();
            var drawn = FontImpl.Current is { } font &&
                        AddTextFitted(drawList, font, min, max, text.Span, color, upscale);
            text.Recycle();
            return drawn;
        }
    }

    private static bool AddTextFitted(
        ImDrawListPtr drawList, FontImpl font, Vector2 min, Vector2 max, ReadOnlySpan<byte> text, ImTextColor color,
        bool upscale)
    {
        var avail = max - min;
        if (!(avail.X > 0 && avail.Y > 0))
            return false;
        var layout = TextLayouter.CreateStyled(font, DescOf(font, default, drawList), text);
        if (!layout.HasDrawnContent || ScaleToFit(layout.Ink, avail, upscale) is not { } scale)
            return false;
        if (scale != 1)
        {
            using (ImGui.PushFontScale(scale))
            {
                var scaled = FontImpl.Current!;
                layout = TextLayouter.CreateStyled(scaled, DescOf(scaled, default, drawList), text);
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

    private static float? ScaleToFit(Vector4 ink, Vector2 avail, bool upscale)
    {
        var (w, h) = (ink.Z - ink.X, ink.W - ink.Y);
        if (!(w > 0 && h > 0))
            return null;
        var scale = MathF.Min(avail.X / w, avail.Y / h);
        return upscale ? scale : MathF.Min(scale, 1);
    }

    // Places the item on the current line's text baseline.
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
        return ImGuiP.ItemAdd(new(pos, pos + size), 0);
    }

    private static void TextStyledRanges(
        ReadOnlySpan<byte> text, int codeUnitSize, ReadOnlySpan<TextStyleRange> ranges, bool wrap)
    {
        ThreadSafety.AssertMainThread();
        var window = ImGuiP.GetCurrentWindow();
        if (window.SkipItems)
            return;

        if (FontImpl.Current is not { } font)
        {
            if (codeUnitSize == 1)
                PlainText(text, wrap);
            else
                PlainText(MemoryMarshal.Cast<byte, char>(text), wrap);
            return;
        }

        var wrapWidth = WrapWidthOf(window, wrap);
        var layout = CreateLayout(font, text, codeUnitSize, ranges, new() { WrapWidth = wrapWidth });
        if (TryAddTextItem(layout.Size, out var pos))
            layout.Render(ImGui.GetWindowDrawList(), pos, ImGui.GetColorU32(ImGuiCol.Text));
    }

    // No try/finally is needed because ImGui.Begin resets any leaked wrap position.
    private static void PlainText(ImU8String text, bool wrap)
    {
        if (wrap)
            ImGui.PushTextWrapPos(0);
        ImGui.Text(text);
        if (wrap)
            ImGui.PopTextWrapPos();
    }

    private static float WrapWidthOf(ImGuiWindowPtr window, bool wrap)
    {
        ref var dc = ref window.DC;
        var wrapPos = wrap ? 0f : dc.TextWrapPos;
        return wrapPos >= 0 ? ImGuiP.CalcWrapWidthForPos(dc.CursorPos, wrapPos) : 0f;
    }

    // Gets the layout parameters at the current font size, as ImGui lays out text. Without a draw list, the pixel
    // density is that of the current context.
    private static TextLayoutDesc DescOf(FontImpl font, in TextLayoutOptions options, ImDrawListPtr drawList = default)
    {
        ImGuiP.GetTextDesc(out var desc, options.WrapWidth, options.Flags, drawList);
        desc.MaxLines = options.MaxLines;
        desc.Align = options.Align;
        return TextLayoutDesc.FromNative(desc, font) with { TabSize = options.TabSize };
    }

    // Lays out text in the current font and size. Runs are split at every boundary of the ranges, and each takes the
    // ranges over it in order.
    private static TextLayout CreateLayout(
        FontImpl font, ReadOnlySpan<byte> text, int codeUnitSize, ReadOnlySpan<TextStyleRange> ranges,
        in TextLayoutOptions options)
    {
        var n = ranges.Length;
        var pooledBounds = PooledSpan<int>.Rent(n * 5);
        var bounds = pooledBounds.Span[..(n * 2)];
        var starts = pooledBounds.Span[(n * 2)..(n * 3)];
        var order = pooledBounds.Span[(n * 3)..(n * 4)];
        var active = pooledBounds.Span[(n * 4)..];

        // Clamped to the text, so that a length such as int.MaxValue does not overflow.
        var textLength = text.Length / codeUnitSize;
        for (var i = 0; i < n; i++)
        {
            var begin = Math.Clamp(ranges[i].Offset, 0, textLength);
            var end = (int)Math.Clamp((long)ranges[i].Offset + ranges[i].Length, begin, textLength);
            (bounds[2 * i], bounds[(2 * i) + 1]) = (begin, end);
            (starts[i], order[i]) = (begin, i);
        }

        bounds.Sort();
        starts.Sort(order);
        var pooledRuns = PooledSpan<TextRun>.Rent(Math.Max(bounds.Length - 1, 0));
        var runs = pooledRuns.Span;
        // Sweep sorted range boundaries, keeping active indices ordered by input index so later overrides win.
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
                if (begin >= (long)r.Offset + r.Length)
                    continue;
                active[kept++] = active[j];
                run = run.With(r);
            }

            activeCount = kept;
            if (run != font)
                runs[count++] = new(begin * codeUnitSize, end * codeUnitSize, run);
        }

        var res = TextLayouter.Create(font, DescOf(font, options), text, codeUnitSize, runs[..count]);
        pooledRuns.Recycle();
        pooledBounds.Recycle();
        return res;
    }

    private static ImTextLayoutRef CreateTextLayout(
        ReadOnlySpan<byte> text, int codeUnitSize, ReadOnlySpan<TextStyleRange> ranges, TextLayoutOptions options)
    {
        ThreadSafety.AssertMainThread();
        if (FontImpl.Current is not { } font)
            return default;
        var layout = CreateLayout(font, text, codeUnitSize, ranges, options);
        return new(NativeTextLayouts.ToNative(layout));
    }
}
