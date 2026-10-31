using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal.Sources;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Converts between the sizes of fonts.</summary>
/// <remarks><ul>
/// <li>The glyph size is the size fonts are drawn at, before the global scale. It is the em for game fonts, and the
/// natural line box for others. <see cref="Font.Measure"/> converts between them.</li>
/// <li><see cref="TextStyle.Size"/> gives the em or the natural line box, as <see cref="TextStyle.SizeBasis"/> says.
/// </li>
/// <li>The push size is ImGui's font size before the global scale, which is the line box.
/// <see cref="ImGui.PushFont(ImFontPtr, float)"/> and
/// <see cref="ImGuiStyle.FontSizeBase"/> take it.</li>
/// <li>The ImGui size is the push size times the global scale. <see cref="ImGui.GetFontSize"/> gives it, and text
/// layouts and draw lists take it.</li>
/// </ul></remarks>
internal static unsafe class FontSizes
{
    [ThreadStatic]
    private static Level[]? cachedLevels;

    [ThreadStatic]
    private static (float DeviceScale, Sizes Root) cachedKey;

    /// <summary>Gets the global scale, or 1 without an ImGui context.</summary>
    public static float GlobalScale =>
        ImGui.GetCurrentContext().IsNull ? 1f : ImGui.GetCurrentContext().Handle->Style.FontScaleMain;

    /// <summary>Gets the device pixels per pixel before global scaling, or 1 without an ImGui context.</summary>
    public static float DeviceScale
    {
        get
        {
            var ctx = ImGui.GetCurrentContext().Handle;
            return ctx is null ? 1f : ctx->Style.FontScaleMain * (ctx->CurrentPixelDensity is > 0 and var d ? d : 1f);
        }
    }

    /// <summary>Gets the sizes of the current font.</summary>
    /// <param name="root">The sizes of the default font, under the font stack.</param>
    /// <returns>The sizes.</returns>
    /// <remarks><ul>
    /// <li>A font with a default line height takes that of the font under it in the font stack.</li>
    /// <li>The sizes of each level are kept, and reused while the levels under it and the device scale are the same.
    /// </li>
    /// </ul></remarks>
    public static Sizes Current(in Sizes root)
    {
        var ctx = ImGui.GetCurrentContext().Handle;
        var key = (DeviceScale, root);
        var valid = cachedKey == key;
        cachedKey = key;
        if (cachedLevels is null || cachedLevels.Length <= ctx->FontStack.Size)
            (cachedLevels, valid) = (new Level[ctx->FontStack.Size + 8], false);

        var current = root;
        for (var i = 0; i <= ctx->FontStack.Size; i++)
        {
            var (font, pushPx) = i < ctx->FontStack.Size
                                     ? (new ImFontPtr(ctx->FontStack[i].Font), ctx->FontStack[i].FontSizeBeforeScaling)
                                     : (new ImFontPtr(ctx->Font), ctx->FontSizeBase);
            var impl = FontImpl.FromNative(font.Handle);
            var target = impl?.Entry.Slot.LoadedTarget;
            ref var level = ref cachedLevels[i];
            if (valid && impl is not null && level.Impl == impl && level.PushPx == pushPx && level.Target == target)
            {
                current = level.Sizes;
                continue;
            }

            valid = false;
            if (impl is not null)
                current = Over(impl.Style, target, current);
            if (current.LineHeight.Unit != StyleUnit.Px)
                current = current.Scale(pushPx / current.PushPx);
            level = new(impl, pushPx, target, current);
        }

        return current;
    }

    /// <summary>Gets the sizes of a style pushed over a font.</summary>
    /// <param name="style">The style.</param>
    /// <param name="font">The font of its family, or null if not loaded.</param>
    /// <param name="under">The sizes of the font under it.</param>
    /// <returns>The sizes.</returns>
    public static Sizes Over(in TextStyle style, Font? font, in Sizes under)
    {
        var emBox = IsEmBox(style, font);
        var current = emBox ? under.EmPx : under.LinePx;
        var size = StyleLength.Resolve(style.Size, current, 1, current) is > 0 and < float.PositiveInfinity and var s
                       ? s
                       : current;
        var (glyph, em, line) = font?.Measure(size, emBox, DeviceScale) ?? (size, size, size);
        return new(
            glyph,
            em,
            line,
            emBox,
            style.LineHeight.Unit == StyleUnit.Default ? under.LineHeight : style.LineHeight);
    }

    /// <summary>Gets whether the size of a style measures the em, rather than the natural line box.</summary>
    /// <param name="style">The style.</param>
    /// <param name="font">The font of its family, or null if not loaded.</param>
    /// <returns>Whether it measures the em.</returns>
    public static bool IsEmBox(in TextStyle style, Font? font)
    {
        var sizeRatio = 1f;
        return style.SizeBasis == FontSizeBasis.EmBox
               || (style.SizeBasis == FontSizeBasis.Auto && font?.GetPrimary(ref sizeRatio).SizeIsEm is true);
    }

    /// <summary>Gets the font to push for a font, and its push size at its own size.</summary>
    /// <param name="font">The font.</param>
    /// <returns>The font of this frame, and the push size. A font not of this frame gives the current font. Without
    /// fonts, it is the font as given and 0.</returns>
    public static (ImFontPtr Font, float Size) InherentPush(ImFontPtr font) =>
        FontImpl.FromNative(font.Handle) is { } impl
            ? (new ImFontPtr(impl.Native),
               Over(impl.Style, impl.Entry.Slot.LoadedTarget, Current(impl.Resolver.Root)).PushPx)
            : (font, 0);

    /// <summary>The sizes of a font in the font stack.</summary>
    /// <param name="GlyphPx">The glyph size.</param>
    /// <param name="EmPx">The em.</param>
    /// <param name="LinePx">The natural line box.</param>
    /// <param name="EmBox">Whether the size of the style measures the em.</param>
    /// <param name="LineHeight">The line height. <see cref="StyleUnit.Default"/> is the natural line height.</param>
    public readonly record struct Sizes(float GlyphPx, float EmPx, float LinePx, bool EmBox, StyleLength LineHeight)
    {
        /// <summary>Gets what the size of the style measures.</summary>
        public float Measure => this.EmBox ? this.EmPx : this.LinePx;

        /// <summary>Gets the push size.</summary>
        public float PushPx => StyleLength.Resolve(this.LineHeight, this.Measure, 1, this.LinePx);

        /// <summary>Gets the sizes scaled.</summary>
        /// <param name="k">The scale.</param>
        /// <returns>The scaled sizes.</returns>
        public Sizes Scale(float k) =>
            this with { GlyphPx = this.GlyphPx * k, EmPx = this.EmPx * k, LinePx = this.LinePx * k };
    }

    // The sizes of a level of the font stack, as Current found them.
    private readonly record struct Level(FontImpl? Impl, float PushPx, CompositeFont? Target, Sizes Sizes);
}
