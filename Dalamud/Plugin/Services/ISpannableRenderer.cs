using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Spannables;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Strings;
using Dalamud.Interface.Spannables.Styles;

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
    /// another instance of <see cref="SpannedStringBuilder"/> doesn't matter at that point.</para>
    /// </remarks>
    SpannedStringBuilder RentBuilder();

    /// <summary>Returns the rented builder.</summary>
    /// <param name="builder">The rented builder from <see cref="RentBuilder"/>.</param>
    /// <remarks>
    /// <para>This function is safe to use from a non-main thread.</para>
    /// <para>Specifying <c>null</c> to <paramref name="builder"/> is a no-op.</para>
    /// </remarks>
    void ReturnBuilder(SpannedStringBuilder? builder);

    /// <summary>Attempts to resolve the font data.</summary>
    /// <param name="renderScale">The render scale.</param>
    /// <param name="style">The style to resolve from.</param>
    /// <param name="fontData">The resolved font data.</param>
    /// <returns><c>true</c> if any font was available; <c>false</c> if the current ImGui font from
    /// <see cref="ImGui.GetFont"/> has been used instead.</returns>
    /// <remarks>Regardless of the return value, <paramref name="fontData"/> will contain valid values.</remarks>
    bool TryGetFontData(float renderScale, scoped in SpanStyle style, out SpanStyleFontData fontData);

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

    /// <summary>Renders a spannable.</summary>
    /// <param name="sequence">The char sequence.</param>
    /// <param name="renderState">The final render state.</param>
    /// <remarks>Use <see cref="Render(ReadOnlySpan{char}, ref RenderState)"/> if you want to retrieve the state after
    /// rendering.</remarks>
    void Render(ReadOnlySpan<char> sequence, RenderState renderState);

    /// <summary>Renders a spannable.</summary>
    /// <param name="sequence">The char sequence.</param>
    /// <param name="renderState">The final render state.</param>
    void Render(ReadOnlySpan<char> sequence, ref RenderState renderState);

    /// <summary>Renders a spannable.</summary>
    /// <param name="spannable">The spannable.</param>
    /// <param name="renderState">The final render state.</param>
    /// <remarks>Use <see cref="Render(ISpannable, ref RenderState)"/> if you want to retrieve the state after
    /// rendering.</remarks>
    void Render(ISpannable spannable, RenderState renderState);

    /// <summary>Renders a spannable.</summary>
    /// <param name="spannable">The spannable.</param>
    /// <param name="renderState">The final render state.</param>
    void Render(ISpannable spannable, ref RenderState renderState);

    /// <summary>Renders an interactive spannable.</summary>
    /// <param name="spannable">The spannable.</param>
    /// <param name="renderState">The final render state.</param>
    /// <param name="hoveredLink">The payload being hovered, if any.</param>
    /// <returns><c>true</c> if any payload is currently being hovered.</returns>
    /// <remarks><paramref name="hoveredLink"/> is only valid until next render.</remarks>
    /// <remarks>Use <see cref="Render(ISpannable, ref RenderState, out ReadOnlySpan{byte})"/> if you want to
    /// retrieve the state after rendering.</remarks>
    bool Render(ISpannable spannable, RenderState renderState, out ReadOnlySpan<byte> hoveredLink);

    /// <summary>Renders a spannable.</summary>
    /// <param name="spannable">The spannable.</param>
    /// <param name="renderState">The final render state.</param>
    /// <param name="hoveredLink">The payload being hovered, if any.</param>
    /// <returns><c>true</c> if any payload is currently being hovered.</returns>
    /// <remarks><paramref name="hoveredLink"/> is only valid until next render.</remarks>
    bool Render(ISpannable spannable, ref RenderState renderState, out ReadOnlySpan<byte> hoveredLink);
}
