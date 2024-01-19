using Dalamud.Interface.Internal;

using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Toolkit for use when the build state is <see cref="FontAtlasBuildStep.PostBuild"/>.
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
}
