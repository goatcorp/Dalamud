using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Specifies how should global font scale affect a font.
/// </summary>
public enum FontScaleMode
{
    /// <summary>
    /// Do the default handling. Dalamud will load the sufficienty large font that will accomodate the global scale,
    /// and stretch the loaded glyphs so that they look pixel-perfect after applying global scale on drawing.
    /// Note that bitmap fonts and game fonts will always look blurry if they're not in their original sizes.
    /// </summary>
    Default,
    
    /// <summary>
    /// Do nothing with the font. Dalamud will load the font with the size that is exactly as specified.
    /// On drawing, the font will look blurry due to stretching.
    /// Intended for use with custom scale handling.
    /// </summary>
    SkipHandling,
    
    /// <summary>
    /// Stretch the glyphs of the loaded font by the inverse of the global scale.
    /// On drawing, the font will always render exactly as the requested size without blurring, as long as
    /// <see cref="ImGuiHelpers.GlobalScale"/> and <see cref="ImGui.SetWindowFontScale"/> do not affect the scale any
    /// further. Note that bitmap fonts and game fonts will always look blurry if they're not in their original sizes.
    /// </summary>
    UndoGlobalScale,
}
