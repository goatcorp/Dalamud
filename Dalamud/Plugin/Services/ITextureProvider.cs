using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.Windows.Data.Widgets;
using Dalamud.Interface.Textures;

using Lumina.Data.Files;

namespace Dalamud.Plugin.Services;

/// <summary>Service that grants you access to textures you may render via ImGui.</summary>
/// <remarks>
/// <para>
/// <b>Create</b> functions will return a new texture, and the returned instance of <see cref="IDalamudTextureWrap"/>
/// must be disposed after use.
/// </para>
/// <para>
/// <b>Get</b> functions will return a shared texture, and the returnd instance of <see cref="ISharedImmediateTexture"/>
/// do not require calling <see cref="IDisposable.Dispose"/>, unless a new reference has been created by calling
/// <see cref="ISharedImmediateTexture.RentAsync"/>.<br />
/// Use <see cref="ISharedImmediateTexture.TryGetWrap"/> and alike to obtain a reference of
/// <see cref="IDalamudTextureWrap"/> that will stay valid for the rest of the frame.
/// </para>
/// <para>
/// <c>debugName</c> parameter can be used to name your textures, to aid debugging resource leaks using
/// <see cref="TexWidget"/>.
/// </para>
/// </remarks>
public interface ITextureProvider
{
    /// <summary>Creates an empty texture.</summary>
    /// <param name="specs">Texture specifications.</param>
    /// <param name="cpuRead">Whether to support reading from CPU, while disabling reading from GPU.</param>
    /// <param name="cpuWrite">Whether to support writing from CPU, while disabling writing from GPU.</param>
    /// <param name="debugName">Name for debug display purposes.</param>
    /// <returns>A new empty texture.</returns>
    IDalamudTextureWrap CreateEmpty(
        RawImageSpecification specs,
        bool cpuRead,
        bool cpuWrite,
        string? debugName = null);

    /// <summary>Creates a texture from the given existing texture, cropping and converting pixel format as needed.
    /// </summary>
    /// <param name="wrap">The source texture wrap. The passed value may be disposed once this function returns,
    /// without having to wait for the completion of the returned <see cref="Task{TResult}"/>.</param>
    /// <param name="args">The texture modification arguments.</param>
    /// <param name="leaveWrapOpen">Whether to leave <paramref name="wrap"/> non-disposed when the returned
    /// <see cref="Task{TResult}"/> completes.</param>
    /// <param name="debugName">Name for debug display purposes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the copied texture on success. Dispose after use.</returns>
    /// <remarks><para>This function may throw an exception.</para></remarks>
    Task<IDalamudTextureWrap> CreateFromExistingTextureAsync(
        IDalamudTextureWrap wrap,
        TextureModificationArgs args = default,
        bool leaveWrapOpen = false,
        string? debugName = null,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a texture from an ImGui viewport.</summary>
    /// <param name="args">The arguments for creating a texture.</param>
    /// <param name="debugName">Name for debug display purposes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the copied texture on success. Dispose after use.</returns>
    /// <remarks>
    /// <para>Use <c>ImGui.GetMainViewport().ID</c> to capture the game screen with Dalamud rendered.</para>
    /// <para>This function may throw an exception.</para>
    /// </remarks>
    Task<IDalamudTextureWrap> CreateFromImGuiViewportAsync(
        ImGuiViewportTextureArgs args,
        string? debugName = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a texture from the given bytes, trying to interpret it as a .tex file or other well-known image
    /// files, such as .png.</summary>
    /// <param name="bytes">The bytes to load.</param>
    /// <param name="debugName">Name for debug display purposes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success. Dispose after use.</returns>
    /// <remarks><para>This function may throw an exception.</para></remarks>
    Task<IDalamudTextureWrap> CreateFromImageAsync(
        ReadOnlyMemory<byte> bytes,
        string? debugName = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a texture from the given stream, trying to interpret it as a .tex file or other well-known image
    /// files, such as .png.</summary>
    /// <param name="stream">The stream to load data from.</param>
    /// <param name="leaveOpen">Whether to leave the stream open once the task completes, sucessfully or not.</param>
    /// <param name="debugName">Name for debug display purposes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success. Dispose after use.</returns>
    /// <remarks>
    /// <para><paramref name="stream"/> will be closed or not only according to <paramref name="leaveOpen"/>;
    /// <paramref name="cancellationToken"/> is irrelevant in closing the stream.</para>
    /// <para>This function may throw an exception.</para></remarks>
    Task<IDalamudTextureWrap> CreateFromImageAsync(
        Stream stream,
        bool leaveOpen = false,
        string? debugName = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a texture from the given bytes, interpreting it as a raw bitmap.</summary>
    /// <param name="specs">The specifications for the raw bitmap.</param>
    /// <param name="bytes">The bytes to load.</param>
    /// <param name="debugName">Name for debug display purposes.</param>
    /// <returns>The texture loaded from the supplied raw bitmap. Dispose after use.</returns>
    /// <remarks><para>This function may throw an exception.</para></remarks>
    IDalamudTextureWrap CreateFromRaw(
        RawImageSpecification specs,
        ReadOnlySpan<byte> bytes,
        string? debugName = null);

    /// <summary>Gets a texture from the given bytes, interpreting it as a raw bitmap.</summary>
    /// <param name="specs">The specifications for the raw bitmap.</param>
    /// <param name="bytes">The bytes to load.</param>
    /// <param name="debugName">Name for debug display purposes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success. Dispose after use.</returns>
    /// <remarks><para>This function may throw an exception.</para></remarks>
    Task<IDalamudTextureWrap> CreateFromRawAsync(
        RawImageSpecification specs,
        ReadOnlyMemory<byte> bytes,
        string? debugName = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a texture from the given stream, interpreting the read data as a raw bitmap.</summary>
    /// <param name="specs">The specifications for the raw bitmap.</param>
    /// <param name="stream">The stream to load data from.</param>
    /// <param name="leaveOpen">Whether to leave the stream open once the task completes, sucessfully or not.</param>
    /// <param name="debugName">Name for debug display purposes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success. Dispose after use.</returns>
    /// <remarks>
    /// <para><paramref name="stream"/> will be closed or not only according to <paramref name="leaveOpen"/>;
    /// <paramref name="cancellationToken"/> is irrelevant in closing the stream.</para>
    /// <para>This function may throw an exception.</para>
    /// </remarks>
    Task<IDalamudTextureWrap> CreateFromRawAsync(
        RawImageSpecification specs,
        Stream stream,
        bool leaveOpen = false,
        string? debugName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a texture handle for the specified Lumina <see cref="TexFile"/>.
    /// Alias for fetching <see cref="Task{TResult}.Result"/> from <see cref="CreateFromTexFileAsync"/>.
    /// </summary>
    /// <param name="file">The texture to obtain a handle to.</param>
    /// <returns>A texture wrap that can be used to render the texture. Dispose after use.</returns>
    /// <remarks><para>This function may throw an exception.</para></remarks>
    IDalamudTextureWrap CreateFromTexFile(TexFile file);

    /// <summary>Get a texture handle for the specified Lumina <see cref="TexFile"/>.</summary>
    /// <param name="file">The texture to obtain a handle to.</param>
    /// <param name="debugName">Name for debug display purposes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A texture wrap that can be used to render the texture. Dispose after use.</returns>
    /// <remarks><para>This function may throw an exception.</para></remarks>
    Task<IDalamudTextureWrap> CreateFromTexFileAsync(
        TexFile file,
        string? debugName = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the supported bitmap decoders.</summary>
    /// <returns>The supported bitmap decoders.</returns>
    /// <remarks>
    /// <para>The following functions support the files of the container types pointed by yielded values.</para>
    /// <ul>
    /// <li><see cref="GetFromFile"/></li>
    /// <li><see cref="GetFromManifestResource"/></li>
    /// <li><see cref="CreateFromImageAsync(ReadOnlyMemory{byte},string?,CancellationToken)"/></li>
    /// <li><see cref="CreateFromImageAsync(Stream,bool,string?,CancellationToken)"/></li>
    /// </ul>
    /// <para>This function may throw an exception.</para>
    /// </remarks>
    IEnumerable<IBitmapCodecInfo> GetSupportedImageDecoderInfos();

    /// <summary>Gets a shared texture corresponding to the given game resource icon specifier.</summary>
    /// <param name="lookup">A game icon specifier.</param>
    /// <returns>The shared texture that you may use to obtain the loaded texture wrap and load states.</returns>
    /// <remarks>
    /// <para>This function is under the effect of <see cref="ITextureSubstitutionProvider.GetSubstitutedPath"/>.</para>
    /// <para>This function does not throw exceptions.</para>
    /// </remarks>
    ISharedImmediateTexture GetFromGameIcon(in GameIconLookup lookup);

    /// <summary>Gets a shared texture corresponding to the given path to a game resource.</summary>
    /// <param name="path">A path to a game resource.</param>
    /// <returns>The shared texture that you may use to obtain the loaded texture wrap and load states.</returns>
    /// <remarks>
    /// <para>This function is under the effect of <see cref="ITextureSubstitutionProvider.GetSubstitutedPath"/>.</para>
    /// <para>This function does not throw exceptions.</para>
    /// </remarks>
    ISharedImmediateTexture GetFromGame(string path);

    /// <summary>Gets a shared texture corresponding to the given file on the filesystem.</summary>
    /// <param name="path">A path to a file on the filesystem.</param>
    /// <returns>The shared texture that you may use to obtain the loaded texture wrap and load states.</returns>
    /// <remarks><para>This function does not throw exceptions.</para></remarks>
    ISharedImmediateTexture GetFromFile(string path);

    /// <summary>Gets a shared texture corresponding to the given file of the assembly manifest resources.</summary>
    /// <param name="assembly">The assembly containing manifest resources.</param>
    /// <param name="name">The case-sensitive name of the manifest resource being requested.</param>
    /// <returns>The shared texture that you may use to obtain the loaded texture wrap and load states.</returns>
    /// <remarks><para>This function does not throw exceptions.</para></remarks>
    ISharedImmediateTexture GetFromManifestResource(Assembly assembly, string name);

    /// <summary>Get a path for a specific icon's .tex file.</summary>
    /// <param name="lookup">The icon lookup.</param>
    /// <returns>The path to the icon.</returns>
    /// <exception cref="FileNotFoundException">If a corresponding file could not be found.</exception>
    string GetIconPath(in GameIconLookup lookup);

    /// <summary>
    /// Gets the path of an icon.
    /// </summary>
    /// <param name="lookup">The icon lookup.</param>
    /// <param name="path">The resolved path.</param>
    /// <returns><c>true</c> if the corresponding file exists and <paramref name="path"/> has been set.</returns>
    /// <remarks><para>This function does not throw exceptions.</para></remarks>
    bool TryGetIconPath(in GameIconLookup lookup, [NotNullWhen(true)] out string? path);

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
}
