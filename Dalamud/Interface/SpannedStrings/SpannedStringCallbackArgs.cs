using System.Numerics;

using Dalamud.Interface.SpannedStrings.Internal;
using Dalamud.Interface.SpannedStrings.Rendering;
using Dalamud.Interface.SpannedStrings.Rendering.Internal;
using Dalamud.Interface.SpannedStrings.Styles;

using ImGuiNET;

namespace Dalamud.Interface.SpannedStrings;

/// <summary>Arguments for <see cref="SpannedStringCallbackDelegate"/>.</summary>
public readonly unsafe ref struct SpannedStringCallbackArgs
{
    /// <summary>The render state so far.</summary>
    public readonly ref readonly RenderState RenderState;

    /// <summary>The current style.</summary>
    public readonly ref readonly SpanStyle Style;

    /// <summary>The transformation matrix in use.</summary>
    public readonly ref readonly Matrix4x4 Transformation;

    /// <summary>The left top screen coordinates for this span.</summary>
    public readonly Vector2 Xy0;

    /// <summary>The right bottom screen coordinates for this span.</summary>
    public readonly Vector2 Xy1;

    private readonly ImDrawList* drawListPtr;
    private readonly ImDrawListSplitter* splitterPtr;
    private readonly SpanStyleFontData fontData;

    /// <summary>Initializes a new instance of the <see cref="SpannedStringCallbackArgs"/> struct.</summary>
    /// <param name="drawListPtr">The parent draw list.</param>
    /// <param name="splitterPtr">The splitter.</param>
    /// <param name="renderState">The render state.</param>
    /// <param name="xy0">The left top screen coordinates for this span.</param>
    /// <param name="xy1">The right bottom screen coordinates for this span.</param>
    /// <param name="fontData">The current font data.</param>
    /// <param name="style">The current style.</param>
    /// <param name="transformation">The transformation matrix in use.</param>
    internal SpannedStringCallbackArgs(
        ImDrawList* drawListPtr,
        ImDrawListSplitter* splitterPtr,
        in RenderState renderState,
        Vector2 xy0,
        Vector2 xy1,
        SpanStyleFontData fontData,
        in SpanStyle style,
        in Matrix4x4 transformation)
    {
        this.drawListPtr = drawListPtr;
        this.splitterPtr = splitterPtr;
        this.Xy0 = xy0;
        this.Xy1 = xy1;
        this.fontData = fontData;
        this.Transformation = ref transformation;
        this.Style = ref style;
        this.RenderState = ref renderState;
    }

    /// <summary>Gets the draw list.</summary>
    public ImDrawListPtr DrawListPtr => this.drawListPtr;

    /// <summary>Gets the current render scale.</summary>
    public float RenderScale => this.fontData.Scale;

    /// <summary>Gets the current font.</summary>
    public ImFontPtr FontPtr => this.fontData.Font;

    /// <summary>Gets the current font size.</summary>
    public float FontSize => this.fontData.ScaledFontSize;

    public static implicit operator ImDrawListPtr(SpannedStringCallbackArgs a) => a.drawListPtr;

    public static implicit operator ImDrawList*(SpannedStringCallbackArgs a) => a.drawListPtr;

    /// <summary>Transforms the given coordinates w.r.t. <see cref="Xy0"/> by <see cref="Transformation"/>.</summary>
    /// <param name="coord">The screen coordinates.</param>
    /// <returns>The transformed screen coordinates.</returns>
    public Vector2 Transform(Vector2 coord) => this.Xy0 + Vector2.Transform(this.Xy1 - this.Xy0, this.Transformation);

    /// <summary>Switches to the background channel.</summary>
    public void SwitchToBackgroundChannel() => ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
        this.splitterPtr,
        this.drawListPtr,
        SpannableRenderer.BackChannel);

    /// <summary>Switches to the shadow channel.</summary>
    public void SwitchToShadowChannel() => ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
        this.splitterPtr,
        this.drawListPtr,
        SpannableRenderer.ShadowChannel);

    /// <summary>Switches to the border channel.</summary>
    public void SwitchToBorderChannel() => ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
        this.splitterPtr,
        this.drawListPtr,
        SpannableRenderer.BorderChannel);

    /// <summary>Switches to the text decoration channel for overline and underline.</summary>
    public void SwitchToTextDecorationOverUnderChannel() => ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
        this.splitterPtr,
        this.drawListPtr,
        SpannableRenderer.TextDecorationOverUnderChannel);

    /// <summary>Switches to the foreground channel.</summary>
    public void SwitchToForegroundChannel() => ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
        this.splitterPtr,
        this.drawListPtr,
        SpannableRenderer.ForeChannel);

    /// <summary>Switches to the text decoration channel for line through.</summary>
    public void SwitchToTextDecorationThroughChannel() => ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
        this.splitterPtr,
        this.drawListPtr,
        SpannableRenderer.TextDecorationThroughChannel);
}
