using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Spannables;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Interface.Spannables.Text;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Plugin.Services;

/// <summary>A custom text renderer.</summary>
/// <remarks>Everything, unless noted otherwise, can only be used from the <b>main thread</b> while <b>drawing</b>.</remarks>
public interface ISpannableRenderer
{
    /// <summary>Rents a builder.</summary>
    /// <returns>The rented builder.</returns>
    /// <remarks>
    /// <para>This function is safe to use from a non-main thread.</para>
    /// <para>Return using <see cref="ReturnBuilder"/>, but don't bother to wrap in <c>try { ... } finally { ... }</c>
    /// block, unless you already have one. The cost of throwing an exception is more significant enough that creating
    /// another instance of <see cref="TextSpannableBuilder"/> doesn't matter at that point.</para>
    /// </remarks>
    TextSpannableBuilder RentBuilder();

    /// <summary>Returns a builder borrowed using <see cref="RentBuilder"/>.</summary>
    /// <param name="builder">The rented builder using <see cref="RentBuilder"/>.</param>
    /// <remarks>
    /// <para>This function is safe to use from a non-main thread.</para>
    /// <para>Specifying <c>null</c> to <paramref name="builder"/> is a no-op.</para>
    /// </remarks>
    void ReturnBuilder(TextSpannableBuilder? builder);

    /// <summary>Rents a private draw list.</summary>
    /// <param name="template">Template for the first draw command.</param>
    /// <returns>The rented draw list.</returns>
    /// <remarks>
    /// <para>This function is safe to use from a non-main thread.</para>
    /// <para>Always return using <see cref="ReturnDrawList"/>. Not doing so will result in a memory leak.</para>
    /// </remarks>
    ImDrawListPtr RentDrawList(ImDrawListPtr template);

    /// <summary>Returns a draw list borrowed using <see cref="RentDrawList"/>.</summary>
    /// <param name="drawListPtr">The draw list borrowed using <see cref="RentDrawList"/>.</param>
    /// <remarks>
    /// <para>This function is safe to use from a non-main thread.</para>
    /// <para>Specifying <c>null</c> to <paramref name="drawListPtr"/> is a no-op.</para>
    /// </remarks>
    void ReturnDrawList(ImDrawListPtr drawListPtr);

    /// <summary>Draws the draw list to the texture, resizing as necessary.</summary>
    /// <param name="drawListPtr">The pointer to the draw list.</param>
    /// <param name="clipRect">The clip rect.</param>
    /// <param name="clearColor">The color to clear with.</param>
    /// <param name="scale">The texture size scaling.</param>
    /// <param name="clipRectUv">The UV for the clip rect.</param>
    /// <returns>An instance of <see cref="IDalamudTextureWrap"/> that is valid for the rest of the frame, with draw
    /// instructions from <paramref name="drawListPtr"/> applied, or <c>null</c> if there was nothing to draw.</returns>
    IDalamudTextureWrap? RentDrawListTexture(
        ImDrawListPtr drawListPtr,
        RectVector4 clipRect,
        Vector4 clearColor,
        Vector2 scale,
        out RectVector4 clipRectUv);

    /// <summary>Returns a texture borrowed from <see cref="RentDrawListTexture"/>.</summary>
    /// <param name="textureWrap">The texture list borrowed from <see cref="RentDrawListTexture"/>.</param>
    /// <remarks>Specifyling a null pointer is a no-op. A returned texture will become available for use after the
    /// drawing cycle is complete.</remarks>
    void ReturnDrawListTexture(IDalamudTextureWrap? textureWrap);

    /// <summary>Attempts to resolve the font data.</summary>
    /// <param name="renderScale">The render scale.</param>
    /// <param name="style">The style to resolve from.</param>
    /// <param name="fontData">The resolved font data.</param>
    /// <returns><c>true</c> if any font was available; <c>false</c> if the current ImGui font from
    /// <see cref="ImGui.GetFont"/> has been used instead.</returns>
    /// <remarks>Regardless of the return value, <paramref name="fontData"/> will contain valid values.</remarks>
    bool TryGetFontData(float renderScale, scoped in TextStyle style, out TextStyleFontData fontData);

    /// <summary>Attempts to get an icon by icon ID.</summary>
    /// <param name="iconType">The icon type.</param>
    /// <param name="iconId">The icon ID.</param>
    /// <param name="minDimensions">The minimum dimensions that this icon will be rendered.</param>
    /// <param name="textureWrap">The retrieved texture wrap.</param>
    /// <param name="uv0">The relative UV0 of the icon in <paramref name="textureWrap"/>.</param>
    /// <param name="uv1">The relative UV1 of the icon in <paramref name="textureWrap"/>.</param>
    /// <returns><c>true</c> if icon is retrieved.</returns>
    bool TryGetIcon(
        int iconType,
        uint iconId,
        Vector2 minDimensions,
        [NotNullWhen(true)] out IDalamudTextureWrap? textureWrap,
        out Vector2 uv0,
        out Vector2 uv1);

    /// <summary>Renders plain text.</summary>
    /// <param name="sequence">The UTF-16 character sequence.</param>
    /// <param name="renderContext">The render context.</param>
    /// <param name="textOptions">The text styling options.</param>
    /// <returns>The render results.</returns>
    RenderResult Render(
        ReadOnlySpan<char> sequence,
        in RenderContext renderContext = default,
        in TextState.Options textOptions = default);

    /// <summary>Renders a spannable.</summary>
    /// <param name="spannable">An instance of <see cref="ISpannable"/>.</param>
    /// <param name="renderContext">The render context.</param>
    /// <param name="textOptions">The initial text styling options.</param>
    /// <returns>The render results.</returns>
    RenderResult Render(
        ISpannable spannable,
        in RenderContext renderContext = default,
        in TextState.Options textOptions = default);
}
