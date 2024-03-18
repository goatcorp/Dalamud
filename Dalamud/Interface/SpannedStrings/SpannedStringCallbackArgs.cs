using System.Numerics;

using Dalamud.Interface.SpannedStrings.Internal;
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
    internal SpannedStringCallbackArgs(
        ImDrawList* drawListPtr,
        ImDrawListSplitter* splitterPtr,
        in RenderState renderState,
        Vector2 xy0,
        Vector2 xy1,
        SpanStyleFontData fontData,
        in SpanStyle style)
    {
        this.drawListPtr = drawListPtr;
        this.splitterPtr = splitterPtr;
        this.Xy0 = xy0;
        this.Xy1 = xy1;
        this.fontData = fontData;
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

    /// <summary>Switches to the background channel.</summary>
    public void SwitchToBackgroundChannel() => ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
        this.splitterPtr,
        this.drawListPtr,
        SpannedStringRenderer.BackChannel);

    /// <summary>Switches to the shadow channel.</summary>
    public void SwitchToShadowChannel() => ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
        this.splitterPtr,
        this.drawListPtr,
        SpannedStringRenderer.ShadowChannel);

    /// <summary>Switches to the border channel.</summary>
    public void SwitchToBorderChannel() => ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
        this.splitterPtr,
        this.drawListPtr,
        SpannedStringRenderer.BorderChannel);

    /// <summary>Switches to the text decoration channel for overline and underline.</summary>
    public void SwitchToTextDecorationOverUnderChannel() => ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
        this.splitterPtr,
        this.drawListPtr,
        SpannedStringRenderer.TextDecorationOverUnderChannel);

    /// <summary>Switches to the foreground channel.</summary>
    public void SwitchToForegroundChannel() => ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
        this.splitterPtr,
        this.drawListPtr,
        SpannedStringRenderer.ForeChannel);

    /// <summary>Switches to the text decoration channel for line through.</summary>
    public void SwitchToTextDecorationThroughChannel() => ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
        this.splitterPtr,
        this.drawListPtr,
        SpannedStringRenderer.TextDecorationThroughChannel);
}
