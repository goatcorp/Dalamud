using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Internal.Types;

namespace Dalamud.ImGuiScene;

internal interface ISceneTextureManager
{
    /// <summary>Creates a texture.</summary>
    /// <param name="data">Optional data to initialize the texture with.</param>
    /// <param name="specs">Texture specifications.</param>
    /// <param name="cpuRead">Whether to support reading from CPU, while disabling reading from GPU.</param>
    /// <param name="cpuWrite">Whether to support writing from CPU, while disabling writing from GPU.</param>
    /// <param name="immutable">Whether the texture should be immutable.</param>
    /// <param name="debugName">Name for debug display purposes.</param>
    /// <returns>A new empty texture.</returns>
    IDalamudTextureWrap Create(
        ReadOnlySpan<byte> data,
        RawImageSpecification specs,
        bool cpuRead,
        bool cpuWrite,
        bool immutable,
        string? debugName = null);

    /// <summary>Creates a texture from the given existing texture, cropping and converting pixel format as needed.
    /// </summary>
    /// <param name="wrap">The source texture wrap. The passed value may be disposed once this function returns,
    ///     without having to wait for the completion of the returned <see cref="Task{TResult}"/>.</param>
    /// <param name="args">The texture modification arguments.</param>
    /// <param name="debugName">Name for debug display purposes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the copied texture on success. Dispose after use.</returns>
    /// <remarks><para>This function may throw an exception.</para></remarks>
    Task<IDalamudTextureWrap> CreateFromExistingTextureAsync(
        IDalamudTextureWrap wrap,
        TextureModificationArgs args = default,
        string? debugName = null,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a texture from an ImGui viewport.</summary>
    /// <param name="args">The arguments for creating a texture.</param>
    /// <param name="ownerPlugin">Plugin that called the function.</param>
    /// <param name="debugName">Name for debug display purposes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the copied texture on success. Dispose after use.</returns>
    /// <remarks>
    /// <para>Use <c>ImGui.GetMainViewport().ID</c> to capture the game screen with Dalamud rendered.</para>
    /// <para>This function may throw an exception.</para>
    /// </remarks>
    Task<IDalamudTextureWrap> CreateFromImGuiViewportAsync(
        ImGuiViewportTextureArgs args,
        LocalPlugin? ownerPlugin,
        string? debugName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the system supports the given DXGI format.
    /// For use with <see cref="RawImageSpecification.DxgiFormat"/>.
    /// </summary>
    /// <param name="dxgiFormat">The DXGI format.</param>
    /// <returns><c>true</c> if supported.</returns>
    /// <remarks><para>This function does not throw exceptions.</para></remarks>
    bool IsDxgiFormatSupported(int dxgiFormat);

    /// <summary>Determines whether the system supports the given DXGI format for use with
    /// <see cref="CreateFromExistingTextureAsync"/>.</summary>
    /// <param name="dxgiFormat">The DXGI format.</param>
    /// <returns><c>true</c> if supported.</returns>
    /// <remarks><para>This function does not throw exceptions.</para></remarks>
    bool IsDxgiFormatSupportedForCreateFromExistingTextureAsync(int dxgiFormat);

    /// <summary>Gets the raw data of a texture wrap.</summary>
    /// <param name="wrap">The source texture wrap.</param>
    /// <param name="args">The texture modification arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The raw data and its specifications.</returns>
    /// <remarks>
    /// <para>The length of the returned <c>RawData</c> may not match
    /// <see cref="RawImageSpecification.Height"/> * <see cref="RawImageSpecification.Pitch"/>.</para>
    /// <para>This function may throw an exception.</para>
    /// </remarks>
    Task<(RawImageSpecification Specification, byte[] RawData)> GetRawImageAsync(
        IDalamudTextureWrap wrap,
        TextureModificationArgs args = default,
        CancellationToken cancellationToken = default);
}
