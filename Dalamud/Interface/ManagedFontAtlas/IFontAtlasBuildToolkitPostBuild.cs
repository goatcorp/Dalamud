using Dalamud.Interface.Internal;

using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Toolkit for use when the build state is <see cref="FontAtlasBuildStep.PostBuild"/>.<br />
/// Not intended for plugins to implement.
/// </summary>
public interface IFontAtlasBuildToolkitPostBuild : IFontAtlasBuildToolkit
{
    /// <summary>
    /// Gets whether global scaling is ignored for the given font.
    /// </summary>
    /// <param name="fontPtr">The font.</param>
    /// <returns>True if ignored.</returns>
    bool IsGlobalScaleIgnored(ImFontPtr fontPtr);

    /// <summary>
    /// Stores a texture to be managed with the atlas.
    /// </summary>
    /// <param name="textureWrap">The texture wrap.</param>
    /// <param name="disposeOnError">Dispose the wrap on error.</param>
    /// <returns>The texture index.</returns>
    int StoreTexture(IDalamudTextureWrap textureWrap, bool disposeOnError);

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
