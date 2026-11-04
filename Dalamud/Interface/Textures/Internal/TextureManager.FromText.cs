using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts;
using Dalamud.Interface.Fonts.Internal;
using Dalamud.Interface.Fonts.Internal.Layout;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Utility;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
internal sealed partial class TextureManager
{
    /// <inheritdoc/>
    public unsafe IDalamudTextureWrap CreateTextureFromText(
        ImU8String text,
        TextMarkup markup,
        ImFontPtr font = default,
        float fontSizePx = 0,
        float wrapWidth = float.MaxValue,
        string? debugName = null)
    {
        try
        {
            ThreadSafety.AssertMainThread();
            if (font.IsNull)
                font = ImGui.Font;
            if (FontImpl.FromNative(font) is not { } impl)
                throw new ArgumentException("The font is not a Dalamud font.", nameof(font));
            // The size as a push takes it, and lines as ImGui spaces them, so that the text is as it is in a window.
            var scale = FontSizes.GlobalScale;
            var em = fontSizePx > 0 && float.IsFinite(fontSizePx)
                         ? fontSizePx * scale
                         : ImGui.GetFontSize() / impl.LineRatio;
            var desc = new TextLayoutDesc(
                em * impl.LineRatio,
                em,
                scale,
                WrapWidth: wrapWidth >= float.MaxValue ? 0 : wrapWidth);
            using var dd = TextDrawData.Create(
                impl.WithMarkup(markup), desc, text.Span);
            var texture = this.CreateDrawListTexture(debugName ?? nameof(this.CreateTextureFromText));
            try
            {
                texture.Size = dd.Data.DisplaySize;
                texture.Draw(dd.DataPtr);
                return texture;
            }
            catch
            {
                texture.Dispose();
                throw;
            }
        }
        finally
        {
            text.Recycle();
        }
    }
}
