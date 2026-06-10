using System.Runtime.CompilerServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;

namespace Dalamud.Interface.ImGuiBackend.Renderers;

/// <summary>A simple shared public interface that all ImGui render implementations follow.</summary>
internal interface IImGuiRenderer : IDisposable
{
    /// <summary>Load an image from a span of bytes of specified format.</summary>
    /// <param name="data">The data to load.</param>
    /// <param name="specs">Texture specifications.</param>
    /// <param name="cpuRead">Whether to support reading from CPU, while disabling reading from GPU.</param>
    /// <param name="cpuWrite">Whether to support writing from CPU, while disabling writing from GPU.</param>
    /// <param name="allowRenderTarget">Whether to allow rendering to this texture.</param>
    /// <param name="debugName">Name for debugging.</param>
    /// <returns>A texture, ready to use in ImGui.</returns>
    IDalamudTextureWrap CreateTexture2D(
        ReadOnlySpan<byte> data,
        RawImageSpecification specs,
        bool cpuRead,
        bool cpuWrite,
        bool allowRenderTarget,
        [CallerMemberName] string debugName = "");

    /// <summary>Notifies that the window is about to be resized.</summary>
    void OnPreResize();

    /// <summary>Notifies that the window has been resized.</summary>
    /// <param name="width">The new window width.</param>
    /// <param name="height">The new window height.</param>
    void OnPostResize(int width, int height);

    /// <summary>Marks the beginning of a new frame.</summary>
    void OnNewFrame();

    /// <summary>Renders the draw data.</summary>
    /// <param name="drawData">The draw data.</param>
    void RenderDrawData(ImDrawDataPtr drawData);

    /// <summary>
    /// Draws and presents a single secondary (multi-viewport) window from a snapshot of its draw data.
    /// </summary>
    /// <remarks>
    /// This mirrors what <c>ImGui.RenderPlatformWindowsDefault()</c> would do per secondary viewport
    /// (render then swap buffers), but using Dalamud-owned snapshotted draw data and a renderer-private
    /// viewport handle captured under the backend's draw-data lock. It must not be called for the main viewport.
    /// </remarks>
    /// <param name="rendererUserData">The viewport's <c>RendererUserData</c> handle.</param>
    /// <param name="drawData">The snapshotted draw data for the viewport.</param>
    void RenderViewportSnapshot(nint rendererUserData, ImDrawDataPtr drawData);
}
