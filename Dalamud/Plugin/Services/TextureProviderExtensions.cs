using System.Diagnostics.CodeAnalysis;

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
        /// <param name="style"><inheritdoc cref="ITextureProvider.CreateTextureFromText"
        /// path="/param[@name='style']/node()"/></param>
        /// <param name="scale"><inheritdoc cref="ITextureProvider.CreateTextureFromText"
        /// path="/param[@name='scale']/node()"/></param>
        /// <param name="wrapWidth"><inheritdoc cref="ITextureProvider.CreateTextureFromText"
        /// path="/param[@name='wrapWidth']/node()"/></param>
        /// <param name="debugName">Name for debug display purposes.</param>
        /// <returns>The new texture.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><inheritdoc cref="ITextureProvider.CreateTextureFromText"
        /// path="/exception[@cref='ArgumentOutOfRangeException']/node()"/></exception>
        /// <remarks>As <see cref="ITextureProvider.CreateTextureFromText"/> with <see cref="TextMarkup.SeString"/>.
        /// </remarks>
        public IDalamudTextureWrap CreateTextureFromSeString(
            ReadOnlySpan<byte> text,
            scoped in TextStyle style = default,
            float scale = 0,
            float wrapWidth = float.MaxValue,
            string? debugName = null) =>
            provider.CreateTextureFromText(
                text,
                TextMarkup.SeString,
                style,
                scale,
                wrapWidth,
                debugName ?? nameof(CreateTextureFromSeString));
    }
}
