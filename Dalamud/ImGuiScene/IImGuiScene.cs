using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Internal.Types;

namespace Dalamud.ImGuiScene;

/// <summary>
/// Backend for ImGui.
/// </summary>
internal interface IImGuiScene : IDisposable
{
    /// <summary>
    /// Delegate to be called when ImGui should be used to layout now.
    /// </summary>
    public delegate void BuildUiDelegate();

    /// <summary>
    /// Delegate to be called on new input frame.
    /// </summary>
    public delegate void NewInputFrameDelegate();

    /// <summary>
    /// Delegaet to be called on new render frame.
    /// </summary>
    public delegate void NewRenderFrameDelegate();

    /// <summary>
    /// User methods invoked every ImGui frame to construct custom UIs.
    /// </summary>
    event BuildUiDelegate? BuildUi;

    /// <summary>
    /// User methods invoked every ImGui frame on handling inputs.
    /// </summary>
    event NewInputFrameDelegate? NewInputFrame;

    /// <summary>
    /// User methods invoked every ImGui frame on handling renders.
    /// </summary>
    event NewRenderFrameDelegate? NewRenderFrame;

    /// <summary>
    /// Gets or sets a value indicating whether or not the cursor should be overridden with the ImGui cursor.
    /// </summary>
    public bool UpdateCursor { get; set; }

    /// <summary>
    /// Gets or sets the path of ImGui configuration .ini file.
    /// </summary>
    public string? IniPath { get; set; }

    /// <summary>
    /// Gets the device handle.
    /// </summary>
    public nint DeviceHandle { get; }

    /// <summary>
    /// Perform a render cycle.
    /// </summary>
    void Render();

    /// <summary>
    /// Handle stuff before resizing happens.
    /// </summary>
    void OnPreResize();
    
    /// <summary>
    /// Handle stuff after resizing happens.
    /// </summary>
    /// <param name="newWidth">The new width.</param>
    /// <param name="newHeight">The new height.</param>
    void OnPostResize(int newWidth, int newHeight);

    /// <summary>
    /// Invalidate fonts immediately.
    /// </summary>
    /// <remarks>Call this while handling <see cref="NewRenderFrame"/>.</remarks>
    void InvalidateFonts();

    /// <summary>
    /// Check whether the current backend supports the given texture format for shader input.
    /// </summary>
    /// <param name="format">DXGI format to check.</param>
    /// <returns>Whether it is supported.</returns>
    public bool SupportsTextureFormat(int format);

    /// <summary>
    /// Check whether the current backend supports the given texture format for rendering to.
    /// </summary>
    /// <param name="format">DXGI format to check.</param>
    /// <returns>Whether it is supported.</returns>
    public bool SupportsTextureFormatForRenderTarget(int format);

    /// <inheritdoc cref="IImGuiRenderer.CreateTexture2D"/>
    IDalamudTextureWrap CreateTexture2D(
        ReadOnlySpan<byte> data,
        RawImageSpecification specs,
        bool cpuRead,
        bool cpuWrite,
        bool allowRenderTarget,
        [CallerMemberName] string debugName = "");

    /// <inheritdoc cref="IImGuiRenderer.CreateTextureFromImGuiViewport"/>
    IDalamudTextureWrap CreateTextureFromImGuiViewport(
        ImGuiViewportTextureArgs args,
        LocalPlugin? ownerPlugin,
        string? debugName = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="IImGuiRenderer.GetTextureSpecification"/>
    RawImageSpecification GetTextureSpecification(IDalamudTextureWrap texture);

    /// <inheritdoc cref="IImGuiRenderer.GetTextureData"/>
    byte[] GetTextureData(IDalamudTextureWrap texture, out RawImageSpecification specification);

    /// <inheritdoc cref="IImGuiRenderer.GetTextureResource"/>
    nint GetTextureResource(IDalamudTextureWrap texture);

    /// <inheritdoc cref="IImGuiRenderer.DrawTextureToTexture"/>
    void DrawTextureToTexture(
        IDalamudTextureWrap target,
        Vector2 targetUv0,
        Vector2 targetUv1,
        IDalamudTextureWrap source,
        Vector2 sourceUv0,
        Vector2 sourceUv1,
        bool copyAlphaOnly = false);

    /// <inheritdoc cref="IImGuiRenderer.SetTexturePipeline"/>
    void SetTexturePipeline(IDalamudTextureWrap textureHandle, ITexturePipelineWrap? pipelineHandle);
    
    /// <inheritdoc cref="IImGuiRenderer.GetTexturePipeline"/>
    ITexturePipelineWrap? GetTexturePipeline(IDalamudTextureWrap textureHandle);

    /// <summary>
    /// Determines if <paramref name="cursorHandle"/> is owned by this.
    /// </summary>
    /// <param name="cursorHandle">The cursor.</param>
    /// <returns>Whether it is the case.</returns>
    public bool IsImGuiCursor(nint cursorHandle);

    /// <summary>
    /// Determines if this instance of <see cref="IImGuiScene"/> is rendering to <paramref name="targetHandle"/>.
    /// </summary>
    /// <param name="targetHandle">The present target handle.</param>
    /// <returns>Whether it is the case.</returns>
    public bool IsAttachedToPresentationTarget(nint targetHandle);

    /// <summary>
    /// Determines if the main viewport is full screen.
    /// </summary>
    /// <returns>Whether it is the case.</returns>
    public bool IsMainViewportFullScreen();
}
