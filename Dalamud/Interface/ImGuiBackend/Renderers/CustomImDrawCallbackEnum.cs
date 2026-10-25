using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.ImGuiBackend.Renderers;

/// <summary>Preset draw-list callbacks handled directly by <see cref="Dx11Renderer"/>.</summary>
/// <remarks>Extends <see cref="ImDrawCallbackEnum"/>. Values must stay negative and not collide with it, as ImGui
/// treats positive values as function pointers.</remarks>
internal enum CustomImDrawCallbackEnum : long
{
    /// <summary>Blurs the region behind the command's clip rect.</summary>
    /// <remarks>It is a six-pass dual Kawase blur. The result is composited back with a smooth
    /// rounded-rectangle SDF mask. UserCallbackData must point to a <see cref="BlurCallbackData"/>.</remarks>
    Blur = -1,

    /// <summary>Binds the glyph texture arrays at <c>t1</c> and <c>t2</c> for the draws after it.</summary>
    /// <remarks>UserCallbackData is the handle of a large glyph image, or null for the arrays of the glyph
    /// atlas.</remarks>
    /// <seealso cref="Fonts.Internal.Glyphs.GlyphAtlas.BindTextures"/>
    BindGlyphTextures = -2,
}
