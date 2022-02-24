using System;

using ImGuiNET;

namespace Dalamud.Interface.GameFonts
{
    /// <summary>
    /// Prepare and keep game font loaded for use in OnDraw.
    /// </summary>
    public class GameFontHandle : IDisposable
    {
        private readonly GameFontManager manager;
        private readonly GameFont font;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameFontHandle"/> class.
        /// </summary>
        /// <param name="manager">GameFontManager instance.</param>
        /// <param name="font">Font to use.</param>
        internal GameFontHandle(GameFontManager manager, GameFont font)
        {
            this.manager = manager;
            this.font = font;
        }

        /// <summary>
        /// Gets the font.
        /// </summary>
        /// <returns>Corresponding font or null.</returns>
        public ImFontPtr? Get() => this.manager.GetFont(this.font);

        /// <inheritdoc/>
        public void Dispose() => this.manager.DecreaseFontRef(this.font);
    }
}
