using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface;
using Dalamud.Interface.Internal;

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
    /// <param name="uv0">The left top coordinates relative to the size of the source texture.</param>
    /// <param name="uv1">The right bottom coordinates relative to the size of the source texture.</param>
    /// <param name="dxgiFormat">The desired target format.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the copied texture on success. Dispose after use.</returns>
    /// <remarks>
    /// <para>Coordinates in <paramref name="uv0"/> and <paramref name="uv1"/> should be in range between 0 and 1.
    /// </para>
    /// <para>Supported values for <paramref name="dxgiFormat"/> may not necessarily match
    /// <see cref="IsDxgiFormatSupported"/>.</para>
    /// </remarks>
    Task<IDalamudTextureWrap> CreateFromExistingTextureAsync(
        IDalamudTextureWrap wrap,
        Vector2 uv0,
        Vector2 uv1,
        int dxgiFormat,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a texture from the given bytes, trying to interpret it as a .tex file or other well-known image
    /// files, such as .png.</summary>
    /// <param name="bytes">The bytes to load.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success. Dispose after use.</returns>
    Task<IDalamudTextureWrap> CreateFromImageAsync(
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a texture from the given stream, trying to interpret it as a .tex file or other well-known image
    /// files, such as .png.</summary>
    /// <param name="stream">The stream to load data from.</param>
    /// <param name="leaveOpen">Whether to leave the stream open once the task completes, sucessfully or not.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success. Dispose after use.</returns>
    /// <remarks><paramref name="stream"/> will be closed or not only according to <paramref name="leaveOpen"/>;
    /// <paramref name="cancellationToken"/> is irrelevant in closing the stream.</remarks>
    Task<IDalamudTextureWrap> CreateFromImageAsync(
        Stream stream,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a texture from the given bytes, interpreting it as a raw bitmap.</summary>
    /// <param name="specs">The specifications for the raw bitmap.</param>
    /// <param name="bytes">The bytes to load.</param>
    /// <returns>The texture loaded from the supplied raw bitmap. Dispose after use.</returns>
    IDalamudTextureWrap CreateFromRaw(
        RawImageSpecification specs,
        ReadOnlySpan<byte> bytes);

    /// <summary>Gets a texture from the given bytes, interpreting it as a raw bitmap.</summary>
    /// <param name="specs">The specifications for the raw bitmap.</param>
    /// <param name="bytes">The bytes to load.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success. Dispose after use.</returns>
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
    /// <remarks><paramref name="stream"/> will be closed or not only according to <paramref name="leaveOpen"/>;
    /// <paramref name="cancellationToken"/> is irrelevant in closing the stream.</remarks>
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
    IDalamudTextureWrap CreateFromTexFile(TexFile file);

    /// <summary>Get a texture handle for the specified Lumina <see cref="TexFile"/>.</summary>
    /// <param name="file">The texture to obtain a handle to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A texture wrap that can be used to render the texture. Dispose after use.</returns>
    Task<IDalamudTextureWrap> CreateFromTexFileAsync(
        TexFile file,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a shared texture corresponding to the given game resource icon specifier.</summary>
    /// <param name="lookup">A game icon specifier.</param>
    /// <returns>The shared texture that you may use to obtain the loaded texture wrap and load states.</returns>
    ISharedImmediateTexture GetFromGameIcon(in GameIconLookup lookup);

    /// <summary>Gets a shared texture corresponding to the given path to a game resource.</summary>
    /// <param name="path">A path to a game resource.</param>
    /// <returns>The shared texture that you may use to obtain the loaded texture wrap and load states.</returns>
    ISharedImmediateTexture GetFromGame(string path);

    /// <summary>Gets a shared texture corresponding to the given file on the filesystem.</summary>
    /// <param name="path">A path to a file on the filesystem.</param>
    /// <returns>The shared texture that you may use to obtain the loaded texture wrap and load states.</returns>
    ISharedImmediateTexture GetFromFile(string path);

    /// <summary>Gets a shared texture corresponding to the given file of the assembly manifest resources.</summary>
    /// <param name="assembly">The assembly containing manifest resources.</param>
    /// <param name="name">The case-sensitive name of the manifest resource being requested.</param>
    /// <returns>The shared texture that you may use to obtain the loaded texture wrap and load states.</returns>
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
    bool TryGetIconPath(in GameIconLookup lookup, [NotNullWhen(true)] out string? path);

    /// <summary>Gets the raw data of a texture wrap.</summary>
    /// <param name="wrap">The source texture wrap. The passed value may be disposed once this function returns,
    /// without having to wait for the completion of the returned <see cref="Task{TResult}"/>.</param>
    /// <param name="uv0">The left top coordinates relative to the size of the source texture.</param>
    /// <param name="uv1">The right bottom coordinates relative to the size of the source texture.</param>
    /// <param name="dxgiFormat">The desired target format.
    /// If 0 (unknown) is passed, then the format will not be converted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The raw data and its specifications.</returns>
    /// <remarks>
    /// <para>The length of the returned <c>RawData</c> may not match
    /// <see cref="RawImageSpecification.Height"/> * <see cref="RawImageSpecification.Pitch"/>.</para>
    /// <para>If <paramref name="uv0"/> is <see cref="Vector2.Zero"/>,
    /// <paramref name="uv1"/> is <see cref="Vector2.One"/>, and <paramref name="dxgiFormat"/> is <c>0</c>,
    /// then the source data will be returned.</para>
    /// <para>This function can fail.</para>
    /// </remarks>
    Task<(RawImageSpecification Specification, byte[] RawData)> GetRawDataAsync(
        IDalamudTextureWrap wrap,
        Vector2 uv0,
        Vector2 uv1,
        int dxgiFormat = 0,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the supported image file extensions.</summary>
    /// <returns>The supported extensions. Each <c>string[]</c> entry indicates that there can be multiple extensions
    /// that correspond to one container format.</returns>
    IEnumerable<string[]> GetSupportedImageExtensions();

    /// <summary>Saves a texture wrap to a stream in an image file format.</summary>
    /// <param name="wrap">The texture wrap to save.</param>
    /// <param name="extension">The extension of the file to deduce the file format with the leading dot.</param>
    /// <param name="stream">The stream to save to.</param>
    /// <param name="leaveOpen">Whether to leave <paramref name="stream"/> open.</param>
    /// <param name="props">Properties to pass to the encoder. See
    /// <a href="https://learn.microsoft.com/en-us/windows/win32/wic/-wic-creating-encoder#encoder-options">Microsoft
    /// Learn</a> for available parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the save process.</returns>
    /// <remarks>
    /// <para><paramref name="wrap"/> may be disposed as soon as this function returns.</para>
    /// <para>If no image container format corresponding to <paramref name="extension"/> is found, then the image will
    /// be saved in png format.</para>
    /// </remarks>
    [SuppressMessage(
        "StyleCop.CSharp.LayoutRules",
        "SA1519:Braces should not be omitted from multi-line child statement",
        Justification = "Multiple fixed blocks")]
    Task SaveAsImageFormatToStreamAsync(
        IDalamudTextureWrap wrap,
        string extension,
        Stream stream,
        bool leaveOpen = false,
        IReadOnlyDictionary<string, object>? props = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the system supports the given DXGI format.
    /// For use with <see cref="RawImageSpecification.DxgiFormat"/>.
    /// </summary>
    /// <param name="dxgiFormat">The DXGI format.</param>
    /// <returns><c>true</c> if supported.</returns>
    bool IsDxgiFormatSupported(int dxgiFormat);

    /// <summary>Determines whether the system supports the given DXGI format for use with
    /// <see cref="CreateFromExistingTextureAsync"/>.</summary>
    /// <param name="dxgiFormat">The DXGI format.</param>
    /// <returns><c>true</c> if supported.</returns>
    bool IsDxgiFormatSupportedForCreateFromExistingTextureAsync(int dxgiFormat);
}
