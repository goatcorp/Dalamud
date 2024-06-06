using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;

namespace Dalamud.Plugin.Services;

/// <summary>Service that grants you to read instances of <see cref="IDalamudTextureWrap"/>.</summary>
public interface ITextureReadbackProvider
{
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
    Task<(RawImageSpecification Specification, byte[] RawData)> GetRawImageAsync(
        IDalamudTextureWrap wrap,
        TextureModificationArgs args = default,
        bool leaveWrapOpen = false,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the supported bitmap encoders.</summary>
    /// <returns>The supported bitmap encoders.</returns>
    /// <remarks>
    /// The following functions support the files of the container types pointed by yielded values.
    /// <ul>
    /// <li><see cref="SaveToStreamAsync"/></li>
    /// <li><see cref="SaveToFileAsync"/></li>
    /// </ul>
    /// <para>This function may throw an exception.</para>
    /// </remarks>
    IEnumerable<IBitmapCodecInfo> GetSupportedImageEncoderInfos();

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
    /// <para>If the target file exists, it will be overwritten only if the save operation is successful.</para>
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
}
