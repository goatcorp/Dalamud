using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;

using Lumina.Data.Files;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Service that grants you access to textures you may render via ImGui.
/// </summary>
public partial interface ITextureProvider
{
    /// <summary>Gets the corresponding game icon for use with the current frame.</summary>
    /// <param name="lookup">The icon specifier.</param>
    /// <returns>An instance of <see cref="IDalamudTextureWrap"/> that is guaranteed to be available for the current
    /// frame being drawn.</returns>
    /// <remarks><see cref="IDisposable.Dispose"/> will be ignored.<br />
    /// If the file is unavailable, then the returned instance of <see cref="IDalamudTextureWrap"/> will point to an
    /// empty texture instead.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when called outside the UI thread.</exception>
    public IDalamudTextureWrap ImmediateGetFromGameIcon(in GameIconLookup lookup);

    /// <summary>Gets a texture from a file shipped as a part of the game resources for use with the current frame.
    /// </summary>
    /// <param name="path">The game-internal path to a .tex, .atex, or an image file such as .png.</param>
    /// <returns>An instance of <see cref="IDalamudTextureWrap"/> that is guaranteed to be available for the current
    /// frame being drawn.</returns>
    /// <remarks><see cref="IDisposable.Dispose"/> will be ignored.<br />
    /// If the file is unavailable, then the returned instance of <see cref="IDalamudTextureWrap"/> will point to an
    /// empty texture instead.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when called outside the UI thread.</exception>
    public IDalamudTextureWrap ImmediateGetFromGame(string path);

    /// <summary>Gets a texture from a file on the filesystem for use with the current frame.</summary>
    /// <param name="file">The filesystem path to a .tex, .atex, or an image file such as .png.</param>
    /// <returns>An instance of <see cref="IDalamudTextureWrap"/> that is guaranteed to be available for the current
    /// frame being drawn.</returns>
    /// <remarks><see cref="IDisposable.Dispose"/> will be ignored.<br />
    /// If the file is unavailable, then the returned instance of <see cref="IDalamudTextureWrap"/> will point to an
    /// empty texture instead.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when called outside the UI thread.</exception>
    public IDalamudTextureWrap ImmediateGetFromFile(string file);

    /// <summary>Gets the corresponding game icon for use with the current frame.</summary>
    /// <param name="lookup">The icon specifier.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success. Dispose after use.</returns>
    public Task<IDalamudTextureWrap> GetFromGameIconAsync(in GameIconLookup lookup);

    /// <summary>Gets a texture from a file shipped as a part of the game resources.</summary>
    /// <param name="path">The game-internal path to a .tex, .atex, or an image file such as .png.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success. Dispose after use.</returns>
    public Task<IDalamudTextureWrap> GetFromGameAsync(string path);

    /// <summary>Gets a texture from a file on the filesystem.</summary>
    /// <param name="file">The filesystem path to a .tex, .atex, or an image file such as .png.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success. Dispose after use.</returns>
    public Task<IDalamudTextureWrap> GetFromFileAsync(string file);

    /// <summary>Gets a texture from the given bytes, trying to interpret it as a .tex file or other well-known image
    /// files, such as .png.</summary>
    /// <param name="bytes">The bytes to load.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success. Dispose after use.</returns>
    public Task<IDalamudTextureWrap> GetFromImageAsync(ReadOnlyMemory<byte> bytes);

    /// <summary>Gets a texture from the given stream, trying to interpret it as a .tex file or other well-known image
    /// files, such as .png.</summary>
    /// <param name="stream">The stream to load data from.</param>
    /// <param name="leaveOpen">Whether to leave the stream open once the task completes, sucessfully or not.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success. Dispose after use.</returns>
    public Task<IDalamudTextureWrap> GetFromImageAsync(Stream stream, bool leaveOpen = false);

    /// <summary>Gets a texture from the given bytes, interpreting it as a raw bitmap.</summary>
    /// <param name="specs">The specifications for the raw bitmap.</param>
    /// <param name="bytes">The bytes to load.</param>
    /// <returns>The texture loaded from the supplied raw bitmap. Dispose after use.</returns>
    public IDalamudTextureWrap GetFromRaw(RawImageSpecification specs, ReadOnlySpan<byte> bytes);

    /// <summary>Gets a texture from the given bytes, interpreting it as a raw bitmap.</summary>
    /// <param name="specs">The specifications for the raw bitmap.</param>
    /// <param name="bytes">The bytes to load.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success. Dispose after use.</returns>
    public Task<IDalamudTextureWrap> GetFromRawAsync(RawImageSpecification specs, ReadOnlyMemory<byte> bytes);

    /// <summary>Gets a texture from the given stream, interpreting the read data as a raw bitmap.</summary>
    /// <param name="specs">The specifications for the raw bitmap.</param>
    /// <param name="stream">The stream to load data from.</param>
    /// <param name="leaveOpen">Whether to leave the stream open once the task completes, sucessfully or not.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success. Dispose after use.</returns>
    public Task<IDalamudTextureWrap> GetFromRawAsync(
        RawImageSpecification specs,
        Stream stream,
        bool leaveOpen = false);

    /// <summary>
    /// Get a path for a specific icon's .tex file.
    /// </summary>
    /// <param name="lookup">The icon lookup.</param>
    /// <returns>The path to the icon.</returns>
    /// <exception cref="FileNotFoundException">If a corresponding file could not be found.</exception>
    public string GetIconPath(in GameIconLookup lookup);

    /// <summary>
    /// Gets the path of an icon.
    /// </summary>
    /// <param name="lookup">The icon lookup.</param>
    /// <param name="path">The resolved path.</param>
    /// <returns><c>true</c> if the corresponding file exists and <paramref name="path"/> has been set.</returns>
    public bool TryGetIconPath(in GameIconLookup lookup, [NotNullWhen(true)] out string? path);
    
    /// <summary>
    /// Get a texture handle for the specified Lumina <see cref="TexFile"/>.
    /// </summary>
    /// <param name="file">The texture to obtain a handle to.</param>
    /// <returns>A texture wrap that can be used to render the texture.</returns>
    public IDalamudTextureWrap GetTexture(TexFile file);
}
