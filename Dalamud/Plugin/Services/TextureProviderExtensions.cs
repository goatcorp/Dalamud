using System.Diagnostics.CodeAnalysis;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts;
using Dalamud.Interface.Textures.TextureWraps;

namespace Dalamud.Plugin.Services;

/// <summary>Extensions for <see cref="ITextureProvider"/>.</summary>
[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101", Justification = "Extension receivers are not members")]
public static class TextureProviderExtensions
{
    /// <summary>Extensions for texture providers.</summary>
    /// <param name="provider">The texture provider.</param>
    extension(ITextureProvider provider)
    {
        /// <summary>Creates a texture by drawing a SeString onto it.</summary>
        /// <param name="text">The SeString.</param>
        /// <param name="font">The font, whose style the text starts with, or default for the current font.</param>
        /// <param name="fontSizePx"><inheritdoc cref="ITextureProvider.CreateTextureFromText"
        /// path="/param[@name='fontSizePx']/node()"/></param>
        /// <param name="wrapWidth">The width to wrap text at.</param>
        /// <param name="debugName">Name for debug display purposes.</param>
        /// <returns>The new texture.</returns>
        /// <remarks>As <see cref="ITextureProvider.CreateTextureFromText"/> with <see cref="TextMarkup.SeString"/>.
        /// </remarks>
        public IDalamudTextureWrap CreateTextureFromSeString(
            ReadOnlySpan<byte> text,
            ImFontPtr font = default,
            float fontSizePx = 0,
            float wrapWidth = float.MaxValue,
            string? debugName = null) =>
            provider.CreateTextureFromText(
                text,
                TextMarkup.SeString,
                font,
                fontSizePx,
                wrapWidth,
                debugName ?? nameof(CreateTextureFromSeString));
    }
}
