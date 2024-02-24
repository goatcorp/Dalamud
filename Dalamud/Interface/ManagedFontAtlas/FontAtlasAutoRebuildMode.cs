namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// How to rebuild <see cref="IFontAtlas"/>.
/// </summary>
public enum FontAtlasAutoRebuildMode
{
    /// <summary>
    /// Do not rebuild.
    /// </summary>
    Disable,

    /// <summary>
    /// Rebuild on new frame.
    /// </summary>
    OnNewFrame,

    /// <summary>
    /// Rebuild asynchronously.
    /// </summary>
    Async,
}
