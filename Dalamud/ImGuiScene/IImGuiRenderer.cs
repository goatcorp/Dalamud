using System.Runtime.CompilerServices;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;

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

    /// <summary>Gets the texture manager.</summary>
    ISceneTextureManager TextureManager { get; }

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
}
