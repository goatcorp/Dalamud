using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Spannables;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Interface.Spannables.Text;

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

    /// <summary>Returns the rented builder.</summary>
    /// <param name="builder">The rented builder from <see cref="RentBuilder"/>.</param>
    /// <remarks>
    /// <para>This function is safe to use from a non-main thread.</para>
    /// <para>Specifying <c>null</c> to <paramref name="builder"/> is a no-op.</para>
    /// </remarks>
    void ReturnBuilder(TextSpannableBuilder? builder);

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
