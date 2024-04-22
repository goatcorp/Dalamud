using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Convenience function for building fonts through <see cref="IFontAtlas"/>.
/// </summary>
public static class FontAtlasBuildToolkitUtilities
{
    /// <summary>Begins building a new array of <see cref="ushort"/> containing ImGui glyph ranges.</summary>
    /// <param name="chars">The chars.</param>
    /// <returns>A new range builder.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FluentGlyphRangeBuilder BeginGlyphRange(this IEnumerable<char> chars) =>
        default(FluentGlyphRangeBuilder).With(chars);

    /// <summary>Begins building a new array of <see cref="ushort"/> containing ImGui glyph ranges.</summary>
    /// <param name="chars">The chars.</param>
    /// <returns>A new range builder.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FluentGlyphRangeBuilder BeginGlyphRange(this ReadOnlySpan<char> chars) =>
        default(FluentGlyphRangeBuilder).With(chars);

    /// <summary>Begins building a new array of <see cref="ushort"/> containing ImGui glyph ranges.</summary>
    /// <param name="chars">The chars.</param>
    /// <returns>A new range builder.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FluentGlyphRangeBuilder BeginGlyphRange(this string chars) =>
        default(FluentGlyphRangeBuilder).With(chars);

    /// <summary>Begins building a new array of <see cref="ushort"/> containing ImGui glyph ranges.</summary>
    /// <param name="range">The unicode range.</param>
    /// <returns>A new range builder.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FluentGlyphRangeBuilder BeginGlyphRange(this UnicodeRange range) =>
        default(FluentGlyphRangeBuilder).With(range);

    /// <summary>
    /// Compiles given <see cref="char"/>s into an array of <see cref="ushort"/> containing ImGui glyph ranges. 
    /// </summary>
    /// <param name="enumerable">The chars.</param>
    /// <param name="addFallbackCodepoints">Add fallback codepoints to the range.</param>
    /// <param name="addEllipsisCodepoints">Add ellipsis codepoints to the range.</param>
    /// <returns>The compiled range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort[] ToGlyphRange(
        this IEnumerable<char> enumerable,
        bool addFallbackCodepoints = true,
        bool addEllipsisCodepoints = true) =>
        enumerable.BeginGlyphRange().Build(addFallbackCodepoints, addEllipsisCodepoints);

    /// <summary>
    /// Compiles given <see cref="char"/>s into an array of <see cref="ushort"/> containing ImGui glyph ranges. 
    /// </summary>
    /// <param name="span">The chars.</param>
    /// <param name="addFallbackCodepoints">Add fallback codepoints to the range.</param>
    /// <param name="addEllipsisCodepoints">Add ellipsis codepoints to the range.</param>
    /// <returns>The compiled range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort[] ToGlyphRange(
        this ReadOnlySpan<char> span,
        bool addFallbackCodepoints = true,
        bool addEllipsisCodepoints = true) =>
        span.BeginGlyphRange().Build(addFallbackCodepoints, addEllipsisCodepoints);

    /// <summary>
    /// Compiles given string into an array of <see cref="ushort"/> containing ImGui glyph ranges. 
    /// </summary>
    /// <param name="string">The string.</param>
    /// <param name="addFallbackCodepoints">Add fallback codepoints to the range.</param>
    /// <param name="addEllipsisCodepoints">Add ellipsis codepoints to the range.</param>
    /// <returns>The compiled range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort[] ToGlyphRange(
        this string @string,
        bool addFallbackCodepoints = true,
        bool addEllipsisCodepoints = true) =>
        @string.BeginGlyphRange().Build(addFallbackCodepoints, addEllipsisCodepoints);

    /// <summary>
    /// Finds the corresponding <see cref="ImFontConfigPtr"/> in
    /// <see cref="IFontAtlasBuildToolkit.NewImAtlas"/>.<see cref="ImFontAtlasPtr.ConfigData"/> that corresponds to the
    /// specified font <paramref name="fontPtr"/>.
    /// </summary>
    /// <param name="toolkit">The toolkit.</param>
    /// <param name="fontPtr">The font.</param>
    /// <returns>The relevant config pointer, or empty config pointer if not found.</returns>
    public static unsafe ImFontConfigPtr FindConfigPtr(this IFontAtlasBuildToolkit toolkit, ImFontPtr fontPtr)
    {
        foreach (ref var c in toolkit.NewImAtlas.ConfigDataWrapped().DataSpan)
        {
            if (c.DstFont == fontPtr.NativePtr)
                return new((nint)Unsafe.AsPointer(ref c));
        }

        return default;
    }

    /// <summary>
    /// Invokes <paramref name="action"/>
    /// if <see cref="IFontAtlasBuildToolkit.BuildStep"/> of <paramref name="toolkit"/>
    /// is <see cref="FontAtlasBuildStep.PreBuild"/>.
    /// </summary>
    /// <param name="toolkit">The toolkit.</param>
    /// <param name="action">The action.</param>
    /// <returns>This, for method chaining.</returns>
    public static IFontAtlasBuildToolkit OnPreBuild(
        this IFontAtlasBuildToolkit toolkit,
        Action<IFontAtlasBuildToolkitPreBuild> action)
    {
        if (toolkit.BuildStep is FontAtlasBuildStep.PreBuild)
            action.Invoke((IFontAtlasBuildToolkitPreBuild)toolkit);
        return toolkit;
    }

    /// <summary>
    /// Invokes <paramref name="action"/>
    /// if <see cref="IFontAtlasBuildToolkit.BuildStep"/> of <paramref name="toolkit"/>
    /// is <see cref="FontAtlasBuildStep.PostBuild"/>.
    /// </summary>
    /// <param name="toolkit">The toolkit.</param>
    /// <param name="action">The action.</param>
    /// <returns>toolkit, for method chaining.</returns>
    public static IFontAtlasBuildToolkit OnPostBuild(
        this IFontAtlasBuildToolkit toolkit,
        Action<IFontAtlasBuildToolkitPostBuild> action)
    {
        if (toolkit.BuildStep is FontAtlasBuildStep.PostBuild)
            action.Invoke((IFontAtlasBuildToolkitPostBuild)toolkit);
        return toolkit;
    }
}
