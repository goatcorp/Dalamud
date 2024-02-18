using Dalamud.Interface.ManagedFontAtlas;

using ImGuiNET;

namespace Dalamud.Interface.FontIdentifier;

/// <summary>
/// Represents a user's choice of font(s).<br />
/// Not intended for plugins to implement.
/// </summary>
public interface IFontSpec
{
    /// <summary>
    /// Gets the font size in pixels.
    /// </summary>
    float SizePx { get; }

    /// <summary>
    /// Gets the font size in points.
    /// </summary>
    float SizePt { get; }

    /// <summary>
    /// Gets the line height in pixels.
    /// </summary>
    float LineHeightPx { get; }

    /// <summary>
    /// Creates a font handle corresponding to this font specification.
    /// </summary>
    /// <param name="atlas">The atlas to bind this font handle to.</param>
    /// <param name="callback">Optional callback to be called after creating the font handle.</param>
    /// <returns>The new font handle.</returns>
    /// <remarks><see cref="IFontAtlasBuildToolkit.Font"/> will be set when <paramref name="callback"/> is invoked.
    /// </remarks>
    IFontHandle CreateFontHandle(IFontAtlas atlas, FontAtlasBuildStepDelegate? callback = null);

    /// <summary>
    /// Adds this font to the given font build toolkit.
    /// </summary>
    /// <param name="tk">The font build toolkit.</param>
    /// <param name="mergeFont">The font to merge to.</param>
    /// <returns>The added font.</returns>
    ImFontPtr AddToBuildToolkit(IFontAtlasBuildToolkitPreBuild tk, ImFontPtr mergeFont = default);

    /// <summary>
    /// Represents this font specification, preferrably in the requested locale.
    /// </summary>
    /// <param name="localeCode">The locale code. Must be in lowercase(invariant).</param>
    /// <returns>The value.</returns>
    string ToLocalizedString(string localeCode);
}
