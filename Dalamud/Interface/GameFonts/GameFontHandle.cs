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
        private readonly GameFontStyle fontStyle;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameFontHandle"/> class.
        /// </summary>
        /// <param name="manager">GameFontManager instance.</param>
        /// <param name="font">Font to use.</param>
        internal GameFontHandle(GameFontManager manager, GameFontStyle font)
        {
            this.manager = manager;
            this.fontStyle = font;
        }

        /// <summary>
        /// Gets the font style.
        /// </summary>
        public GameFontStyle Style => this.fontStyle;

        /// <summary>
        /// Gets a value indicating whether this font is ready for use.
        /// </summary>
        public bool Available => this.manager.GetFont(this.fontStyle) != null;

        /// <summary>
        /// Gets the font.
        /// </summary>
        public ImFontPtr ImFont => this.manager.GetFont(this.fontStyle).Value;

        /// <summary>
        /// Gets the FdtReader.
        /// </summary>
        public FdtReader FdtReader => this.manager.GetFdtReader(this.fontStyle.FamilyAndSize);

        /// <inheritdoc/>
        public void Dispose() => this.manager.DecreaseFontRef(this.fontStyle);
    }
}
