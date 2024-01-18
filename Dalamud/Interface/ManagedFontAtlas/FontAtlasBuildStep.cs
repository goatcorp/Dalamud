using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Build step for <see cref="IFontAtlas"/>.
/// </summary>
public enum FontAtlasBuildStep
{
    /// <summary>
    /// An invalid value. This should never be passed through event callbacks.
    /// </summary>
    Invalid,

    /// <summary>
    /// Called before calling <see cref="ImFontAtlasPtr.Build"/>.<br />
    /// Expect <see cref="IFontAtlasBuildToolkitPreBuild"/> to be passed.
    /// </summary>
    PreBuild,

    /// <summary>
    /// Called after calling <see cref="ImFontAtlasPtr.Build"/>.<br />
    /// Expect <see cref="IFontAtlasBuildToolkitPostBuild"/> to be passed.<br />
    /// <br />
    /// This callback is not guaranteed to happen after <see cref="PreBuild"/>,
    /// but it will never happen on its own.
    /// </summary>
    PostBuild,

    /// <summary>
    /// Called after promoting staging font atlas to the actual atlas for <see cref="IFontAtlas"/>.<br />
    /// Expect <see cref="PostBuild"/> to be passed.<br />
    /// <br />
    /// This callback is not guaranteed to happen after <see cref="IFontAtlasBuildToolkitPostPromotion"/>,
    /// but it will never happen on its own.
    /// </summary>
    PostPromotion,
}
