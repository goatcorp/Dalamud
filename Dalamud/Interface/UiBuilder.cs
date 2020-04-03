using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using ImGuiScene;

namespace Dalamud.Interface
{
    /// <summary>
    /// This class represents the Dalamud UI that is drawn on top of the game.
    /// It can be used to draw custom windows and overlays.
    /// </summary>
    public class UiBuilder : IDisposable {
        private readonly string namespaceName;

        /// <summary>
        /// The delegate that gets called when Dalamud is ready to draw your windows or overlays.
        /// When it is called, you can use static ImGui calls.
        /// </summary>
        public event RawDX11Scene.BuildUIDelegate OnBuildUi;

        private readonly InterfaceManager interfaceManager;

        /// <summary>
        /// Create a new UiBuilder and register it. You do not have to call this manually.
        /// </summary>
        /// <param name="interfaceManager">The interface manager to register on.</param>
        /// <param name="namespaceName">The plugin namespace.</param>
        public UiBuilder(InterfaceManager interfaceManager, string namespaceName) {
            this.namespaceName = namespaceName;

            this.interfaceManager = interfaceManager;
            this.interfaceManager.OnDraw += OnDraw;
        }

        /// <summary>
        /// Unregister the UiBuilder. Do not call this in plugin code.
        /// </summary>
        public void Dispose() {
            this.interfaceManager.OnDraw -= OnDraw;
        }

        /// <summary>
        /// Loads an image from the specified file.
        /// </summary>
        /// <param name="filePath">The full filepath to the image.</param>
        /// <returns>A <see cref="TextureWrap"/> object wrapping the created image.  Use <see cref="TextureWrap.ImGuiHandle"/> inside ImGui.Image()</returns>
        public TextureWrap LoadImage(string filePath) =>
            this.interfaceManager.LoadImage(filePath);

        /// <summary>
        /// Loads an image from a byte stream, such as a png downloaded into memory.
        /// </summary>
        /// <param name="imageData">A byte array containing the raw image data.</param>
        /// <returns>A <see cref="TextureWrap"/> object wrapping the created image.  Use <see cref="TextureWrap.ImGuiHandle"/> inside ImGui.Image()</returns>
        public TextureWrap LoadImage(byte[] imageData) =>
            this.interfaceManager.LoadImage(imageData);

        /// <summary>
        /// Loads an image from raw unformatted pixel data, with no type or header information.  To load formatted data, use <see cref="LoadImage(byte[])"/>.
        /// </summary>
        /// <param name="imageData">A byte array containing the raw pixel data.</param>
        /// <param name="width">The width of the image contained in <paramref name="imageData"/>.</param>
        /// <param name="height">The height of the image contained in <paramref name="imageData"/>.</param>
        /// <param name="numChannels">The number of channels (bytes per pixel) of the image contained in <paramref name="imageData"/>.  This should usually be 4.</param>
        /// <returns>A <see cref="TextureWrap"/> object wrapping the created image.  Use <see cref="TextureWrap.ImGuiHandle"/> inside ImGui.Image()</returns>
        public TextureWrap LoadImageRaw(byte[] imageData, int width, int height, int numChannels) =>
            this.interfaceManager.LoadImageRaw(imageData, width, height, numChannels);

        /// <summary>
        /// Event that is fired when the plugin should open its configuration interface.
        /// </summary>
        public EventHandler OnOpenConfigUi;

        private void OnDraw() {
            ImGui.PushID(this.namespaceName);
            OnBuildUi?.Invoke();
            ImGui.PopID();
        }
    }
}
