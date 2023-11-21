using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// Substance of a font.
/// </summary>
internal interface IFontHandleSubstance : IDisposable
{
    /// <summary>
    /// Gets the manager relevant to this instance of <see cref="IFontHandleSubstance"/>.
    /// </summary>
    IFontHandleManager Manager { get; }

    /// <summary>
    /// Gets the font.
    /// </summary>
    /// <param name="handle">The handle to get from.</param>
    /// <returns>Corresponding font or null.</returns>
    ImFontPtr GetFontPtr(IFontHandle handle);

    /// <summary>
    /// Gets the exception happened while loading for the font.
    /// </summary>
    /// <param name="handle">The handle to get from.</param>
    /// <returns>Corresponding font or null.</returns>
    Exception? GetBuildException(IFontHandle handle);

    /// <summary>
    /// Called before <see cref="ImFontAtlasPtr.Build"/> call.
    /// </summary>
    /// <param name="toolkitPreBuild">The toolkit.</param>
    void OnPreBuild(IFontAtlasBuildToolkitPreBuild toolkitPreBuild);

    /// <summary>
    /// Called after <see cref="ImFontAtlasPtr.Build"/> call.
    /// </summary>
    /// <param name="toolkitPostBuild">The toolkit.</param>
    void OnPostBuild(IFontAtlasBuildToolkitPostBuild toolkitPostBuild);

    /// <summary>
    /// Called on the specific thread depending on <see cref="IFontAtlasBuildToolkit.IsAsyncBuildOperation"/> after
    /// promoting the staging atlas to direct use with <see cref="IFontAtlas"/>.
    /// </summary>
    /// <param name="toolkitPostPromotion">The toolkit.</param>
    void OnPostPromotion(IFontAtlasBuildToolkitPostPromotion toolkitPostPromotion);
}
