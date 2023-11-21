using System.Collections.Generic;

using Dalamud.Interface.Utility;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Convenience function for building fonts through <see cref="IFontAtlas"/>.
/// </summary>
public static class FontAtlasBuildToolkitUtilities
{
    /// <summary>
    /// Compiles given <see cref="char"/>s into an array of <see cref="ushort"/> containing ImGui glyph ranges. 
    /// </summary>
    /// <param name="enumerable">The chars.</param>
    /// <param name="addFallbackCodepoints">Add fallback codepoints to the range.</param>
    /// <param name="addEllipsisCodepoints">Add ellipsis codepoints to the range.</param>
    /// <returns>The compiled range.</returns>
    public static ushort[] ToGlyphRange(
        this IEnumerable<char> enumerable,
        bool addFallbackCodepoints = true,
        bool addEllipsisCodepoints = true)
    {
        using var builderScoped = ImGuiHelpers.NewFontGlyphRangeBuilderPtrScoped(out var builder);
        foreach (var c in enumerable)
            builder.AddChar(c);
        return builder.BuildRangesToArray(addFallbackCodepoints, addEllipsisCodepoints);
    }

    /// <summary>
    /// Compiles given <see cref="char"/>s into an array of <see cref="ushort"/> containing ImGui glyph ranges. 
    /// </summary>
    /// <param name="span">The chars.</param>
    /// <param name="addFallbackCodepoints">Add fallback codepoints to the range.</param>
    /// <param name="addEllipsisCodepoints">Add ellipsis codepoints to the range.</param>
    /// <returns>The compiled range.</returns>
    public static ushort[] ToGlyphRange(
        this ReadOnlySpan<char> span,
        bool addFallbackCodepoints = true,
        bool addEllipsisCodepoints = true)
    {
        using var builderScoped = ImGuiHelpers.NewFontGlyphRangeBuilderPtrScoped(out var builder);
        foreach (var c in span)
            builder.AddChar(c);
        return builder.BuildRangesToArray(addFallbackCodepoints, addEllipsisCodepoints);
    }

    /// <summary>
    /// Compiles given string into an array of <see cref="ushort"/> containing ImGui glyph ranges. 
    /// </summary>
    /// <param name="string">The string.</param>
    /// <param name="addFallbackCodepoints">Add fallback codepoints to the range.</param>
    /// <param name="addEllipsisCodepoints">Add ellipsis codepoints to the range.</param>
    /// <returns>The compiled range.</returns>
    public static ushort[] ToGlyphRange(
        this string @string,
        bool addFallbackCodepoints = true,
        bool addEllipsisCodepoints = true) =>
        @string.AsSpan().ToGlyphRange(addFallbackCodepoints, addEllipsisCodepoints);

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

    /// <summary>
    /// Invokes <paramref name="action"/>
    /// if <see cref="IFontAtlasBuildToolkit.BuildStep"/> of <paramref name="toolkit"/>
    /// is <see cref="FontAtlasBuildStep.PostPromotion"/>.
    /// </summary>
    /// <param name="toolkit">The toolkit.</param>
    /// <param name="action">The action.</param>
    /// <returns>toolkit, for method chaining.</returns>
    public static IFontAtlasBuildToolkit OnPostPromotion(
        this IFontAtlasBuildToolkit toolkit,
        Action<IFontAtlasBuildToolkitPostPromotion> action)
    {
        if (toolkit.BuildStep is FontAtlasBuildStep.PostPromotion)
            action.Invoke((IFontAtlasBuildToolkitPostPromotion)toolkit);
        return toolkit;
    }
}
