using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Build step for <see cref="IFontAtlas"/>.
/// </summary>
public enum FontAtlasBuildStep
{
    // Note: leave 0 alone; make default(FontAtlasBuildStep) not have a valid value

    /// <summary>
    /// Called before calling <see cref="ImFontAtlasPtr.Build"/>.<br />
    /// Expect <see cref="IFontAtlasBuildToolkitPreBuild"/> to be passed.<br />
    /// When called from <see cref="IFontAtlas.BuildStepChange"/>, this will be called <b>before</b> the delegates
    /// passed to <see cref="IFontAtlas.NewDelegateFontHandle"/>.
    /// </summary>
    PreBuild = 1,

    /// <summary>
    /// Called after calling <see cref="ImFontAtlasPtr.Build"/>.<br />
    /// Expect <see cref="IFontAtlasBuildToolkitPostBuild"/> to be passed.<br />
    /// When called from <see cref="IFontAtlas.BuildStepChange"/>, this will be called <b>after</b> the delegates
    /// passed to <see cref="IFontAtlas.NewDelegateFontHandle"/>; you can do cross-font operations here.<br />
    /// <br />
    /// This callback is not guaranteed to happen after <see cref="PreBuild"/>,
    /// but it will never happen on its own.
    /// </summary>
    PostBuild = 2,
}
