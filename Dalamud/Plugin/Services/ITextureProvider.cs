using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;

using Lumina.Data.Files;

namespace Dalamud.Plugin.Services;

/// <summary>Service that grants you access to textures you may render via ImGui.</summary>
/// <remarks>
/// <para>
/// <b>Get</b> functions will return a shared texture, and the returnd instance of <see cref="ISharedImmediateTexture"/>
/// do not require calling <see cref="IDisposable.Dispose"/>, unless a new reference has been created by calling
/// <see cref="ISharedImmediateTexture.RentAsync"/>.<br />
/// Use <see cref="ISharedImmediateTexture.TryGetWrap"/> and alike to obtain a reference of
/// <see cref="IDalamudTextureWrap"/> that will stay valid for the rest of the frame.
/// </para>
/// <para>
/// <b>Create</b> functions will return a new texture, and the returned instance of <see cref="IDalamudTextureWrap"/>
/// must be disposed after use.
/// </para>
/// </remarks>
public partial interface ITextureProvider
{
    /// <summary>Creates a texture from the given existing texture, cropping and converting pixel format as needed.
    /// </summary>
    /// <param name="wrap">The source texture wrap. The passed value may be disposed once this function returns,
    /// without having to wait for the completion of the returned <see cref="Task{TResult}"/>.</param>
    /// <param name="args">The texture modification arguments.</param>
    /// <param name="leaveWrapOpen">Whether to leave <paramref name="wrap"/> non-disposed when the returned
    /// <see cref="Task{TResult}"/> completes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the copied texture on success. Dispose after use.</returns>
    /// <remarks><para>This function may throw an exception.</para></remarks>
    Task<IDalamudTextureWrap> CreateFromExistingTextureAsync(
        IDalamudTextureWrap wrap,
        ExistingTextureModificationArgs args = default,
        bool leaveWrapOpen = false,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a texture from the game screen, before rendering Dalamud.</summary>
    /// <param name="autoUpdate">If <c>true</c>, automatically update the underlying texture.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the copied texture on success. Dispose after use.</returns>
    /// <remarks><para>This function may throw an exception.</para></remarks>
    Task<IDalamudTextureWrap> CreateFromGameScreen(
        bool autoUpdate = false,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a texture from the game screen, before rendering Dalamud.</summary>
    /// <param name="viewportId">The viewport ID.</param>
    /// <param name="autoUpdate">If <c>true</c>, automatically update the underlying texture.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the copied texture on success. Dispose after use.</returns>
    /// <remarks>
    /// <para>Use <c>ImGui.GetMainViewport().ID</c> to capture the game screen with Dalamud rendered.</para>
    /// <para>This function may throw an exception.</para>
    /// </remarks>
    Task<IDalamudTextureWrap> CreateFromImGuiViewport(
        uint viewportId,
        bool autoUpdate = false,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a texture from the given bytes, trying to interpret it as a .tex file or other well-known image
    /// files, such as .png.</summary>
    /// <param name="bytes">The bytes to load.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success. Dispose after use.</returns>
    /// <remarks><para>This function may throw an exception.</para></remarks>
    Task<IDalamudTextureWrap> CreateFromImageAsync(
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a texture from the given stream, trying to interpret it as a .tex file or other well-known image
    /// files, such as .png.</summary>
    /// <param name="stream">The stream to load data from.</param>
    /// <param name="leaveOpen">Whether to leave the stream open once the task completes, sucessfully or not.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success. Dispose after use.</returns>
    /// <remarks>
    /// <para><paramref name="stream"/> will be closed or not only according to <paramref name="leaveOpen"/>;
    /// <paramref name="cancellationToken"/> is irrelevant in closing the stream.</para>
    /// <para>This function may throw an exception.</para></remarks>
    Task<IDalamudTextureWrap> CreateFromImageAsync(
        Stream stream,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a texture from the given bytes, interpreting it as a raw bitmap.</summary>
    /// <param name="specs">The specifications for the raw bitmap.</param>
    /// <param name="bytes">The bytes to load.</param>
    /// <returns>The texture loaded from the supplied raw bitmap. Dispose after use.</returns>
    /// <remarks><para>This function may throw an exception.</para></remarks>
    IDalamudTextureWrap CreateFromRaw(
        RawImageSpecification specs,
        ReadOnlySpan<byte> bytes);

    /// <summary>Gets a texture from the given bytes, interpreting it as a raw bitmap.</summary>
    /// <param name="specs">The specifications for the raw bitmap.</param>
    /// <param name="bytes">The bytes to load.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success. Dispose after use.</returns>
    /// <remarks><para>This function may throw an exception.</para></remarks>
    Task<IDalamudTextureWrap> CreateFromRawAsync(
        RawImageSpecification specs,
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a texture from the given stream, interpreting the read data as a raw bitmap.</summary>
    /// <param name="specs">The specifications for the raw bitmap.</param>
    /// <param name="stream">The stream to load data from.</param>
    /// <param name="leaveOpen">Whether to leave the stream open once the task completes, sucessfully or not.</param>
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
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A texture wrap that can be used to render the texture. Dispose after use.</returns>
    /// <remarks><para>This function may throw an exception.</para></remarks>
    Task<IDalamudTextureWrap> CreateFromTexFileAsync(
        TexFile file,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the supported bitmap decoders.</summary>
    /// <returns>The supported bitmap decoders.</returns>
    /// <remarks>
    /// <para>The following functions support the files of the container types pointed by yielded values.</para>
    /// <ul>
    /// <li><see cref="GetFromFile"/></li>
    /// <li><see cref="GetFromManifestResource"/></li>
    /// <li><see cref="CreateFromImageAsync(ReadOnlyMemory{byte},CancellationToken)"/></li>
    /// <li><see cref="CreateFromImageAsync(Stream,bool,CancellationToken)"/></li>
    /// </ul>
    /// <para>This function may throw an exception.</para>
    /// </remarks>
    IEnumerable<IBitmapCodecInfo> GetSupportedImageDecoderInfos();

    /// <summary>Gets the supported bitmap encoders.</summary>
    /// <returns>The supported bitmap encoders.</returns>
    /// <remarks>
    /// The following function supports the files of the container types pointed by yielded values.
    /// <ul>
    /// <li><see cref="SaveToStreamAsync"/></li>
    /// </ul>
    /// <para>This function may throw an exception.</para>
    /// </remarks>
    IEnumerable<IBitmapCodecInfo> GetSupportedImageEncoderInfos();

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

    /// <summary>Gets the raw data of a texture wrap.</summary>
    /// <param name="wrap">The source texture wrap.</param>
    /// <param name="args">The texture modification arguments.</param>
    /// <param name="leaveWrapOpen">Whether to leave <paramref name="wrap"/> non-disposed when the returned
    /// <see cref="Task{TResult}"/> completes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The raw data and its specifications.</returns>
    /// <remarks>
    /// <para>The length of the returned <c>RawData</c> may not match
    /// <see cref="RawImageSpecification.Height"/> * <see cref="RawImageSpecification.Pitch"/>.</para>
    /// <para>This function may throw an exception.</para>
    /// </remarks>
    Task<(RawImageSpecification Specification, byte[] RawData)> GetRawDataFromExistingTextureAsync(
        IDalamudTextureWrap wrap,
        ExistingTextureModificationArgs args = default,
        bool leaveWrapOpen = false,
        CancellationToken cancellationToken = default);

    /// <summary>Saves a texture wrap to a stream in an image file format.</summary>
    /// <param name="wrap">The texture wrap to save.</param>
    /// <param name="containerGuid">The container GUID, obtained from <see cref="GetSupportedImageEncoderInfos"/>.</param>
    /// <param name="stream">The stream to save to.</param>
    /// <param name="props">Properties to pass to the encoder. See remarks for valid values.</param>
    /// <param name="leaveWrapOpen">Whether to leave <paramref name="wrap"/> non-disposed when the returned
    /// <see cref="Task{TResult}"/> completes.</param>
    /// <param name="leaveStreamOpen">Whether to leave <paramref name="stream"/> open when the returned
    /// <see cref="Task{TResult}"/> completes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the save process.</returns>
    /// <remarks>
    /// <para><paramref name="wrap"/> must not be disposed until the task finishes.</para>
    /// <para>See the following webpages for the valid values for <paramref name="props"/> per
    /// <paramref name="containerGuid"/>.</para>
    /// <ul>
    /// <li><a href="https://learn.microsoft.com/en-us/windows/win32/wic/native-wic-codecs">
    /// WIC Codecs from Microsoft</a></li>
    /// <li><a href="https://learn.microsoft.com/en-us/windows/win32/wic/-wic-creating-encoder#encoder-options">
    /// Image Encoding Overview: Encoder options</a></li>
    /// </ul>
    /// <para>This function may throw an exception.</para>
    /// </remarks>
    Task SaveToStreamAsync(
        IDalamudTextureWrap wrap,
        Guid containerGuid,
        Stream stream,
        IReadOnlyDictionary<string, object>? props = null,
        bool leaveWrapOpen = false,
        bool leaveStreamOpen = false,
        CancellationToken cancellationToken = default);
    
    /// <summary>Saves a texture wrap to a file as an image file.</summary>
    /// <param name="wrap">The texture wrap to save.</param>
    /// <param name="containerGuid">The container GUID, obtained from <see cref="GetSupportedImageEncoderInfos"/>.</param>
    /// <param name="path">The target file path. The target file will be overwritten if it exist.</param>
    /// <param name="props">Properties to pass to the encoder. See remarks for valid values.</param>
    /// <param name="leaveWrapOpen">Whether to leave <paramref name="wrap"/> non-disposed when the returned
    /// <see cref="Task{TResult}"/> completes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the save process.</returns>
    /// <remarks>
    /// <para><paramref name="wrap"/> must not be disposed until the task finishes.</para>
    /// <para>See the following webpages for the valid values for <paramref name="props"/> per
    /// <paramref name="containerGuid"/>.</para>
    /// <ul>
    /// <li><a href="https://learn.microsoft.com/en-us/windows/win32/wic/native-wic-codecs">
    /// WIC Codecs from Microsoft</a></li>
    /// <li><a href="https://learn.microsoft.com/en-us/windows/win32/wic/-wic-creating-encoder#encoder-options">
    /// Image Encoding Overview: Encoder options</a></li>
    /// </ul>
    /// <para>This function may throw an exception.</para>
    /// </remarks>
    Task SaveToFileAsync(
        IDalamudTextureWrap wrap,
        Guid containerGuid,
        string path,
        IReadOnlyDictionary<string, object>? props = null,
        bool leaveWrapOpen = false,
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
}
