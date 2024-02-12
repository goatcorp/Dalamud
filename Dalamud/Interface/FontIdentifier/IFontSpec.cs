using Dalamud.Interface.ManagedFontAtlas;

using ImGuiNET;

namespace Dalamud.Interface.FontIdentifier;

/// <summary>
/// Represents a user's choice of font(s).
/// </summary>
public interface IFontSpec
{
    /// <summary>
    /// Gets the font size in pixels.
    /// </summary>
    public float SizePx { get; }

    /// <summary>
    /// Gets the font size in points.
    /// </summary>
    public float SizePt { get; }

    /// <summary>
    /// Gets the line height in pixels.
    /// </summary>
    public float LineHeightPx { get; }

    /// <summary>
    /// Creates a font handle corresponding to this font specification.
    /// </summary>
    /// <param name="atlas">The atlas to bind this font handle to.</param>
    /// <param name="callback">Optional callback to be called after creating the font handle.</param>
    /// <returns>The new font handle.</returns>
    public IFontHandle CreateFontHandle(IFontAtlas atlas, FontAtlasBuildStepDelegate? callback = null);

    /// <summary>
    /// Adds this font to the given font build toolkit.
    /// </summary>
    /// <param name="tk">The font build toolkit.</param>
    /// <param name="mergeFont">The font to merge to.</param>
    /// <returns>The added font.</returns>
    public ImFontPtr AddToBuildToolkit(IFontAtlasBuildToolkitPreBuild tk, ImFontPtr mergeFont = default);
}
