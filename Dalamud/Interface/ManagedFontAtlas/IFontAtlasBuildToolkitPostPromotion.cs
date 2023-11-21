using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Toolkit for use when the build state is <see cref="FontAtlasBuildStep.PostPromotion"/>.
/// </summary>
public interface IFontAtlasBuildToolkitPostPromotion : IFontAtlasBuildToolkit
{
    /// <summary>
    /// Copies glyphs across fonts, in a safer way.<br />
    /// If the font does not belong to the current atlas, this function is a no-op.
    /// </summary>
    /// <param name="source">Source font.</param>
    /// <param name="target">Target font.</param>
    /// <param name="missingOnly">Whether to copy missing glyphs only.</param>
    /// <param name="rebuildLookupTable">Whether to call target.BuildLookupTable().</param>
    /// <param name="rangeLow">Low codepoint range to copy.</param>
    /// <param name="rangeHigh">High codepoing range to copy.</param>
    void CopyGlyphsAcrossFonts(
        ImFontPtr source,
        ImFontPtr target,
        bool missingOnly,
        bool rebuildLookupTable = true,
        char rangeLow = ' ',
        char rangeHigh = '\uFFFE');

    /// <summary>
    /// Calls <see cref="ImFontPtr.BuildLookupTable"/>, with some fixups.
    /// </summary>
    /// <param name="font">The font.</param>
    void BuildLookupTable(ImFontPtr font);
}
