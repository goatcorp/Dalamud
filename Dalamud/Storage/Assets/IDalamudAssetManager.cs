using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;

namespace Dalamud.Storage.Assets;

/// <summary>
/// Holds Dalamud Assets' handles hostage, so that they do not get closed while Dalamud is running.<br />
/// Also, attempts to load optional assets.<br />
/// <br />
/// <strong>Note on <see cref="PureAttribute"/></strong><br />
/// It will help you get notified if you discard the result of functions, mostly likely because of a mistake.
/// Think of C++ [[nodiscard]]. Also, like the intended meaning of the attribute, such methods will not have
/// externally visible state changes.
/// </summary>
public interface IDalamudAssetManager
{
    /// <summary>
    /// Gets the shared texture wrap for <see cref="DalamudAsset.Empty4X4"/>.
    /// </summary>
    IDalamudTextureWrap Empty4X4 { get; }

    /// <summary>
    /// Gets whether the stream for the asset is instantly available.
    /// </summary>
    /// <param name="asset">The asset.</param>
    /// <returns>Whether the stream of an asset is immediately available.</returns>
    [Pure]
    bool IsStreamImmediatelyAvailable(DalamudAsset asset);

    /// <summary>
    /// Creates a stream backed by the specified asset, waiting as necessary.<br />
    /// <strong>Call <see cref="IDisposable.Dispose"/> after use.</strong>
    /// </summary>
    /// <param name="asset">The asset.</param>
    /// <returns>The stream.</returns>
    [Pure]
    Stream CreateStream(DalamudAsset asset);

    /// <summary>
    /// Creates a stream backed by the specified asset.<br />
    /// <strong>Call <see cref="IDisposable.Dispose"/> after use.</strong>
    /// </summary>
    /// <param name="asset">The asset.</param>
    /// <returns>The stream, wrapped inside a <see cref="Stream"/>.</returns>
    [Pure]
    Task<Stream> CreateStreamAsync(DalamudAsset asset);

    /// <summary>
    /// Gets a shared instance of <see cref="IDalamudTextureWrap"/>, after waiting as necessary.<br />
    /// Calls to <see cref="IDisposable.Dispose"/> is unnecessary; they will be ignored.
    /// </summary>
    /// <param name="asset">The texture asset.</param>
    /// <returns>The texture wrap.</returns>
    [Pure]
    IDalamudTextureWrap GetDalamudTextureWrap(DalamudAsset asset);

    /// <summary>
    /// Gets a shared instance of <see cref="IDalamudTextureWrap"/> if it is available instantly;
    /// if it is not ready, returns <paramref name="defaultWrap"/>.<br />
    /// Calls to <see cref="IDisposable.Dispose"/> is unnecessary; they will be ignored.
    /// </summary>
    /// <param name="asset">The texture asset.</param>
    /// <param name="defaultWrap">The default return value, if the asset is not ready for whatever reason.</param>
    /// <returns>The texture wrap. Can be <c>null</c> only if <paramref name="defaultWrap"/> is <c>null</c>.</returns>
    [Pure]
    [return: NotNullIfNotNull(nameof(defaultWrap))]
    IDalamudTextureWrap? GetDalamudTextureWrap(DalamudAsset asset, IDalamudTextureWrap? defaultWrap);

    /// <summary>
    /// Gets a shared instance of <see cref="IDalamudTextureWrap"/> in a <see cref="Task{T}"/>.<br />
    /// Calls to <see cref="IDisposable.Dispose"/> is unnecessary; they will be ignored.
    /// </summary>
    /// <param name="asset">The texture asset.</param>
    /// <returns>The new texture wrap, wrapped inside a <see cref="Task{T}"/>.</returns>
    [Pure]
    Task<IDalamudTextureWrap> GetDalamudTextureWrapAsync(DalamudAsset asset);
}
