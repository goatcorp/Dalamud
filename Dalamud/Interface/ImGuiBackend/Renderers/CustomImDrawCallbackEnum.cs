using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.ImGuiBackend.Renderers;

/// <summary>
/// Extends <see cref="ImDrawCallbackEnum"/> with Dalamud-specific preset draw-list callbacks
/// that are handled directly inside <see cref="Dx11Renderer"/>.
/// Values must not collide with <see cref="ImDrawCallbackEnum"/> and must remain negative
/// (positive values are treated as real function pointers by ImGui).
/// </summary>
internal enum CustomImDrawCallbackEnum : long
{
    /// <summary>
    /// Performs a two-pass separable Gaussian blur-behind effect for the region described by
    /// the command's clip rect, then composites the result back with a smooth rounded-rectangle
    /// SDF mask.<br />
    /// UserCallbackData must be a pointer to an instance of <see cref="BlurCallbackData"/>.
    /// </summary>
    Blur = -1,

    /// <summary>
    /// Binds the glyph texture arrays at <c>t1</c> and <c>t2</c> for the draws after it.<br />
    /// UserCallbackData is a pointer to the two views, or null for the glyph atlas's.
    /// </summary>
    /// <seealso cref="Fonts.Internal.Glyphs.GlyphAtlasTextResources.BindTextures"/>
    BindGlyphTextures = -2,
}
