using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using ImGuiScene;

namespace Dalamud.Interface
{
    public class UiBuilder : IDisposable {
        private readonly string namespaceName;

        public event RawDX11Scene.BuildUIDelegate OnBuildUi;

        private InterfaceManager interfaceManager;

        public UiBuilder(InterfaceManager interfaceManager, string namespaceName) {
            this.namespaceName = namespaceName;

            this.interfaceManager = interfaceManager;
            this.interfaceManager.OnDraw += OnDraw;
        }

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

        private void OnDraw() {
            ImGui.PushID(this.namespaceName);
            OnBuildUi?.Invoke();
            ImGui.PopID();
        }
    }
}
