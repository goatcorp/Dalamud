using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Internal.Types;

using ImGuiNET;

namespace Dalamud.ImGuiScene;

/// <summary>
/// A simple shared public interface that all ImGui render implementations follows.
/// </summary>
internal interface IImGuiRenderer : IDisposable
{
    /// <summary>
    /// Callback to be invoked on rendering, added via <see cref="AddDrawCmdUserCallback"/>.
    /// </summary>
    /// <param name="drawData">The relevant draw data.</param>
    /// <param name="drawCmd">The relevant draw command.</param>
    public delegate void DrawCmdUserCallbackDelegate(ImDrawDataPtr drawData, ImDrawCmdPtr drawCmd);

    /// <summary>
    /// Notifies that the window is about to be resized.
    /// </summary>
    void OnPreResize();

    /// <summary>
    /// Notifies that the window has been resized.
    /// </summary>
    /// <param name="width">The new window width.</param>
    /// <param name="height">The new window height.</param>
    void OnPostResize(int width, int height);

    /// <summary>
    /// Marks the beginning of a new frame.
    /// </summary>
    void OnNewFrame();

    /// <summary>
    /// Renders the draw data.
    /// </summary>
    /// <param name="drawData">The draw data.</param>
    void RenderDrawData(ImDrawDataPtr drawData);

    /// <summary>
    /// Sets the texture pipeline. The pipeline must be created from the concrete implementation of this interface.<br />
    /// The references of <paramref name="texture"/> and <paramref name="pipeline"/> are copied.
    /// You may dispose <paramref name="pipeline"/> after the call.
    /// </summary>
    /// <param name="texture">The texture handle.</param>
    /// <param name="pipeline">The pipeline handle to set, or null to clear.</param>
    void SetTexturePipeline(IDalamudTextureWrap texture, ITexturePipelineWrap? pipeline);
    
    /// <summary>
    /// Creates a new reference of the pipeline registered for use with the given texture.<br />
    /// Dispose after use.
    /// </summary>
    /// <param name="texture">The texture handle.</param>
    /// <returns>The previous pixel shader handle, or null if none.</returns>
    ITexturePipelineWrap? GetTexturePipeline(IDalamudTextureWrap texture);

    /// <summary>
    /// Adds a user callback handler.
    /// </summary>
    /// <param name="delegate">The delegate.</param>
    /// <returns>The value to use with <see cref="ImDrawListPtr.AddCallback"/>.</returns>
    nint AddDrawCmdUserCallback(DrawCmdUserCallbackDelegate @delegate);

    /// <summary>
    /// Removes a user callback handler.
    /// </summary>
    /// <param name="delegate">The delegate.</param>
    void RemoveDrawCmdUserCallback(DrawCmdUserCallbackDelegate @delegate);

    /// <summary>
    /// Load an image from a span of bytes of specified format.
    /// </summary>
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

    /// <summary>Creates a texture from an ImGui viewport.</summary>
    /// <param name="args">The arguments for creating a texture.</param>
    /// <param name="ownerPlugin">The owner plugin.</param>
    /// <param name="debugName">Name for debug display purposes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The copied texture on success. Dispose after use.</returns>
    /// <remarks>
    /// <para>Use <c>ImGui.GetMainViewport().ID</c> to capture the game screen with Dalamud rendered.</para>
    /// <para>This function may throw an exception.</para>
    /// </remarks>
    IDalamudTextureWrap CreateTextureFromImGuiViewport(
        ImGuiViewportTextureArgs args,
        LocalPlugin? ownerPlugin,
        string? debugName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the specification of a texture.
    /// </summary>
    /// <param name="texture">The texture to obtain data about.</param>
    /// <returns>The specifications.</returns>
    RawImageSpecification GetTextureSpecification(IDalamudTextureWrap texture);

    /// <summary>
    /// Gets the raw texture data.
    /// </summary>
    /// <param name="texture">Texture to obtain its raw data.</param>
    /// <param name="specification">Raw image specifications.</param>
    /// <returns>Extracted data.</returns>
    byte[] GetTextureData(IDalamudTextureWrap texture, out RawImageSpecification specification);

    /// <summary>
    /// Gets the raw texture resource.
    /// </summary>
    /// <param name="texture">Texture to obtain its underlying resource.</param>
    /// <returns>The underlying resource.</returns>
    nint GetTextureResource(IDalamudTextureWrap texture);

    /// <summary>
    /// Draws a texture onto another texture.
    /// </summary>
    /// <param name="target">Target texture.</param>
    /// <param name="targetUv0">Relative coordinates of the left-top point of the rectangle in the target texture.</param>
    /// <param name="targetUv1">Relative coordinates of the right-bottom point of the rectangle in the target texture.</param>
    /// <param name="source">Source texture.</param>
    /// <param name="sourceUv0">Relative coordinates of the left-top point of the rectangle in the source texture.</param>
    /// <param name="sourceUv1">Relative coordinates of the right-bottom point of the rectangle in the source texture.</param>
    /// <param name="copyAlphaOnly">Whether to only copy alpha values.</param>
    void DrawTextureToTexture(
        IDalamudTextureWrap target,
        Vector2 targetUv0,
        Vector2 targetUv1,
        IDalamudTextureWrap source,
        Vector2 sourceUv0,
        Vector2 sourceUv1,
        bool copyAlphaOnly = false);
}
