using System.Runtime.CompilerServices;

using Dalamud.Interface.Internal;

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
    /// Check whether the current backend supports the given texture format.
    /// </summary>
    /// <param name="format">DXGI format to check.</param>
    /// <returns>Whether it is supported.</returns>
    public bool SupportsTextureFormat(int format);

    /// <summary>
    /// Loads an image from a file.
    /// </summary>
    /// <param name="path">The path to file.</param>
    /// <param name="debugName">The debug name.</param>
    /// <returns>The loaded image.</returns>
    IDalamudTextureWrap CreateTexture2DFromFile(string path, [CallerMemberName] string debugName = "");

    /// <summary>
    /// Loads an image from memory. The image must be in a contained format, such as .png, .jpg, and etc.
    /// </summary>
    /// <param name="data">The data of the image.</param>
    /// <param name="debugName">The debug name.</param>
    /// <returns>The loaded image.</returns>
    IDalamudTextureWrap CreateTexture2DFromBytes(ReadOnlySpan<byte> data, [CallerMemberName] string debugName = "");

    /// <summary>
    /// Load an image from a span of bytes of specified format.
    /// </summary>
    /// <param name="data">The data to load.</param>
    /// <param name="pitch">The pitch(stride) in bytes.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="format">Format of the texture.</param>
    /// <param name="debugName">The debug name.</param>
    /// <returns>A texture, ready to use in ImGui.</returns>
    IDalamudTextureWrap CreateTexture2DFromRaw(
        ReadOnlySpan<byte> data,
        int pitch,
        int width,
        int height,
        int format,
        [CallerMemberName] string debugName = "");

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
