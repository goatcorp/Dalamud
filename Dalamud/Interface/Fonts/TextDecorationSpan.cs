using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal.Layout;

namespace Dalamud.Interface.Fonts;

/// <summary>A span of a line of text to draw a decoration over.</summary>
/// <remarks>Positions are relative to <see cref="Origin"/>, in pixels, and before the transform of the text, if any.
/// Use the methods within this struct to draw to have transformations applied.</remarks>
/// <seealso cref="TextDecorationDrawer"/>
public readonly unsafe ref struct TextDecorationSpan
{
    private readonly ImTextTransform transform;
    private readonly uint alphaSource;

    /// <summary>Initializes a new instance of the <see cref="TextDecorationSpan"/> struct.</summary>
    /// <param name="drawList">The draw list.</param>
    /// <param name="origin">The origin of the text.</param>
    /// <param name="transform">The transform of the text around <paramref name="origin"/>, if any.</param>
    /// <param name="alphaSource">The color the text is drawn with, whose alpha multiplies colors.</param>
    internal TextDecorationSpan(ImDrawList* drawList, Vector2 origin, ImTextTransform? transform, uint alphaSource)
    {
        this.DrawListHandle = drawList;
        this.Origin = origin;
        this.transform = transform.GetValueOrDefault();
        this.IsTransformed = transform.HasValue;
        this.alphaSource = alphaSource;
    }

    /// <summary>Gets the layer this is drawn in.</summary>
    /// <seealso cref="TextDecorationFlags"/>
    public TextDecorationLayer Layer { get; internal init; }

    /// <summary>Gets the band this is drawn in, from 0. A decoration is drawn once per band in each layer.</summary>
    /// <seealso cref="IUiBuilder.RegisterTextDecoration"/>
    public int Band { get; internal init; }

    /// <summary>Gets a value indicating whether this is drawn in any layer under the text.</summary>
    public bool IsBelowText => this.Layer != TextDecorationLayer.OverText;

    /// <summary>Gets the context of the decoration.</summary>
    /// <seealso cref="TextDecoration.Context"/>
    public uint Context { get; internal init; }

    /// <summary>Gets the data of the decoration.</summary>
    /// <seealso cref="TextDecoration.Data"/>
    public ulong Data { get; internal init; }

    /// <summary>Gets the color of the text, with the alpha it is drawn with.</summary>
    public uint TextColor { get; internal init; }

    /// <summary>Gets the draw list.</summary>
    public ImDrawListPtr DrawList => this.DrawListHandle;

    /// <summary>Gets the origin of the text, in screen pixels.</summary>
    public Vector2 Origin { get; }

    /// <summary>Gets a value indicating whether the text is transformed (rotated or scaled).</summary>
    public bool IsTransformed { get; }

    /// <summary>Gets the left of the span.</summary>
    public float X0 { get; internal init; }

    /// <summary>Gets the right of the span.</summary>
    public float X1 { get; internal init; }

    /// <summary>Gets where the line starts.</summary>
    /// <remarks>Decorations may apply across spans.</remarks>
    public float LineStart { get; internal init; }

    /// <summary>Gets the top of the line.</summary>
    public float LineTop { get; internal init; }

    /// <summary>Gets the bottom of the line.</summary>
    public float LineBottom { get; internal init; }

    /// <summary>Gets the baseline of the text.</summary>
    public float Baseline { get; internal init; }

    /// <summary>Gets the style of the text at the start of the span.</summary>
    public TextStyle Style { get; internal init; }

    /// <summary>Gets the glyph size of the text at the start of the span, in pixels.</summary>
    public float FontSize { get; internal init; }

    /// <summary>Gets the em of the text at the start of the span, in pixels.</summary>
    public float EmSize { get; internal init; }

    /// <summary>Gets the ascent of the font, above the baseline.</summary>
    public float Ascent { get; internal init; }

    /// <summary>Gets the descent of the font, below the baseline.</summary>
    public float Descent { get; internal init; }

    /// <summary>Gets the top of the font's underline, below the baseline.</summary>
    public float UnderlinePosition { get; internal init; }

    /// <summary>Gets the thickness of the font's underline, at least 1.</summary>
    public float UnderlineThickness { get; internal init; }

    /// <summary>Gets the top of the font's strikethrough, above the baseline.</summary>
    public float StrikethroughPosition { get; internal init; }

    /// <summary>Gets the thickness of the font's strikethrough, at least 1.</summary>
    public float StrikethroughThickness { get; internal init; }

    /// <summary>Gets the layout the span is of, for <see cref="DrawGlyphs"/>.</summary>
    internal TextLayout? Layout { get; init; }

    /// <summary>Gets the device pixels per pixel.</summary>
    internal float Density { get; init; } = 1;

    /// <summary>Gets the first item of the span.</summary>
    internal int ItemBegin { get; init; }

    /// <summary>Gets the item after the last of the span.</summary>
    internal int ItemEnd { get; init; }

    /// <summary>Gets the draw list.</summary>
    internal ImDrawList* DrawListHandle { get; }

    /// <summary>Gets the transform of the text, if any.</summary>
    internal ImTextTransform? Transform => this.IsTransformed ? this.transform : null;

    /// <summary>Draws the glyphs of the span in a color, dilated and moved, as the edge and the shadow of text are.
    /// </summary>
    /// <param name="color">The color, whose alpha is multiplied as <see cref="ApplyAlpha"/> does.</param>
    /// <param name="width">How far to dilate the glyphs, in pixels, or 0 for their shapes as they are.</param>
    /// <param name="softness">How soft the dilated edge is, as <see cref="TextEdge.Softness"/>.</param>
    /// <param name="offset">How far to move the glyphs, in pixels.</param>
    /// <remarks><ul>
    /// <li>Glyphs are dilated by up to 8 texels of their bitmaps. Color glyphs are drawn as their shapes.</li>
    /// <li>Moved glyphs are taken from their fonts each time, rather than kept by the layout.</li>
    /// </ul></remarks>
    public void DrawGlyphs(ImTextColor color, float width = 0, float softness = 0, Vector2 offset = default) =>
        this.Layout?.DrawDecorationGlyphs(this, this.ApplyAlpha(color), width, softness, offset);

    /// <summary>Gets a color with its alpha multiplied by that of the color the text is drawn with.</summary>
    /// <param name="color">The color.</param>
    /// <returns>The color to draw with.</returns>
    public uint ApplyAlpha(ImTextColor color) =>
        (color & 0x00FFFFFFu) | ((((color >> 24) * (this.alphaSource >> 24)) / 255) << 24);

    /// <summary>Gets the color of a decoration whose context is a color.</summary>
    /// <returns><see cref="TextColor"/> for 0, or the color with <see cref="ApplyAlpha"/>.</returns>
    public uint ContextColor() => this.Context == 0 ? this.TextColor : this.ApplyAlpha(this.Context);

    /// <summary>Gets a point in screen pixels.</summary>
    /// <param name="p">The point, relative to <see cref="Origin"/>.</param>
    /// <returns>The point, transformed.</returns>
    public Vector2 ToScreen(Vector2 p) =>
        this.IsTransformed
            ? this.Origin + (this.transform.AxisX * p.X) + (this.transform.AxisY * p.Y)
            : this.Origin + p;

    /// <summary>Fills a rectangle.</summary>
    /// <param name="min">The top left.</param>
    /// <param name="max">The bottom right.</param>
    /// <param name="color">The color.</param>
    /// <param name="antiAliased">Whether to anti-alias the edges when not transformed. Lines on whole pixels look
    /// sharper without.</param>
    public void AddRectFilled(Vector2 min, Vector2 max, uint color, bool antiAliased = false)
    {
        if (this.IsTransformed)
        {
            ImGuiNative.AddQuadFilled(
                this.DrawListHandle,
                this.ToScreen(min),
                this.ToScreen(new(max.X, min.Y)),
                this.ToScreen(max),
                this.ToScreen(new(min.X, max.Y)),
                color);
            return;
        }

        var flags = this.DrawListHandle->Flags;
        if (!antiAliased)
            this.DrawListHandle->Flags &= ~ImDrawFlags.AaFill;
        ImGuiNative.AddRectFilled(
            this.DrawListHandle, this.Origin + min, this.Origin + max, color, 0, ImDrawFlags.None);
        this.DrawListHandle->Flags = flags;
    }

    /// <summary>Fills a quad.</summary>
    /// <param name="a">The first corner.</param>
    /// <param name="b">The second corner.</param>
    /// <param name="c">The third corner.</param>
    /// <param name="d">The fourth corner.</param>
    /// <param name="color">The color.</param>
    public void AddQuadFilled(Vector2 a, Vector2 b, Vector2 c, Vector2 d, uint color) =>
        ImGuiNative.AddQuadFilled(
            this.DrawListHandle, this.ToScreen(a), this.ToScreen(b), this.ToScreen(c), this.ToScreen(d), color);

    /// <summary>Draws connected line segments.</summary>
    /// <param name="points">The points.</param>
    /// <param name="color">The color.</param>
    /// <param name="thickness">The thickness, before the transform. A transform scales it by the square root of its
    /// area scale.</param>
    public void AddPolyline(ReadOnlySpan<Vector2> points, uint color, float thickness)
    {
        if (this.IsTransformed)
        {
            var (x, y) = (this.transform.AxisX, this.transform.AxisY);
            thickness *= MathF.Sqrt(MathF.Abs((x.X * y.Y) - (x.Y * y.X)));
        }

        Span<Vector2> buffer = stackalloc Vector2[64];
        while (points.Length > 1)
        {
            var n = Math.Min(points.Length, buffer.Length);
            for (var i = 0; i < n; i++)
                buffer[i] = this.ToScreen(points[i]);
            fixed (Vector2* p = buffer)
                ImGuiNative.AddPolyline(this.DrawListHandle, p, n, color, thickness, ImDrawFlags.None);

            // The next chunk starts where this one ended.
            points = points[(n - 1)..];
        }
    }
}
