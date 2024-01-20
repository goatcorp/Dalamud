using Dalamud.Utility;

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
    /// Gets or sets the relevant <see cref="IFontAtlasBuildToolkitPreBuild"/> for this.
    /// </summary>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    IFontAtlasBuildToolkitPreBuild? PreBuildToolkitForApi9Compat { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to create a new instance of <see cref="ImGuiNET.ImFontPtr"/> on first
    /// access, for compatibility with API 9.
    /// </summary>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    bool CreateFontOnAccess { get; set; }

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
    /// Called between <see cref="OnPreBuild"/> and <see cref="ImFontAtlasPtr.Build"/> calls.<br />
    /// Any further modification to <see cref="IFontAtlasBuildToolkit.Fonts"/> will result in undefined behavior.
    /// </summary>
    /// <param name="toolkitPreBuild">The toolkit.</param>
    void OnPreBuildCleanup(IFontAtlasBuildToolkitPreBuild toolkitPreBuild);

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
