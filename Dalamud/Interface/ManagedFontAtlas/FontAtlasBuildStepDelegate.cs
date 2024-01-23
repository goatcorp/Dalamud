namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Delegate to be called when a font needs to be built.
/// </summary>
/// <param name="toolkit">A toolkit that may help you for font building steps.</param>
/// <remarks>
/// An implementation of <see cref="IFontAtlasBuildToolkit"/> may implement all of
/// <see cref="IFontAtlasBuildToolkitPreBuild"/> and <see cref="IFontAtlasBuildToolkitPostBuild"/>.<br />
/// Either use <see cref="IFontAtlasBuildToolkit.BuildStep"/> to identify the build step, or use
/// <see cref="FontAtlasBuildToolkitUtilities.OnPreBuild"/> and <see cref="FontAtlasBuildToolkitUtilities.OnPostBuild"/>
/// for routing.
/// </remarks>
public delegate void FontAtlasBuildStepDelegate(IFontAtlasBuildToolkit toolkit);
