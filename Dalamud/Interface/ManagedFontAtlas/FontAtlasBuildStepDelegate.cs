namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Delegate to be called when a font needs to be built.
/// </summary>
/// <param name="toolkit">A toolkit that may help you for font building steps.</param>
/// <remarks>
/// An implementation of <see cref="IFontAtlasBuildToolkit"/> may implement all of
/// <see cref="IFontAtlasBuildToolkitPreBuild"/>, <see cref="IFontAtlasBuildToolkitPostBuild"/>, and
/// <see cref="IFontAtlasBuildToolkitPostPromotion"/>.<br />
/// Either use <see cref="IFontAtlasBuildToolkit.BuildStep"/> to identify the build step, or use
/// <see cref="FontAtlasBuildToolkitUtilities.OnPreBuild"/>, <see cref="FontAtlasBuildToolkitUtilities.OnPostBuild"/>,
/// and <see cref="FontAtlasBuildToolkitUtilities.OnPostPromotion"/> for routing.
/// </remarks>
public delegate void FontAtlasBuildStepDelegate(IFontAtlasBuildToolkit toolkit);
