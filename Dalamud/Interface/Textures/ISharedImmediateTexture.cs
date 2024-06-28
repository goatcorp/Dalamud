using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Utility;

namespace Dalamud.Interface.Textures;

/// <summary>A texture with a backing instance of <see cref="IDalamudTextureWrap"/> that is shared across multiple
/// requesters.</summary>
/// <remarks>
/// <para>Calling <see cref="IDisposable.Dispose"/> on this interface is a no-op.</para>
/// <para><see cref="GetWrapOrEmpty"/> and <see cref="TryGetWrap"/> may stop returning the intended texture at any point.
/// Use <see cref="RentAsync"/> to lock the texture for use in any thread for any duration.</para>
/// </remarks>
public interface ISharedImmediateTexture
{
    /// <summary>Gets the texture for use with the current frame, or an empty texture if unavailable.</summary>
    /// <returns>An instance of <see cref="IDalamudTextureWrap"/> that is guaranteed to be available for the current
    /// frame being drawn.</returns>
    /// <remarks>
    /// <para>Do not cache the result of this function across draw calls for different frames.
    /// <see cref="ISharedImmediateTexture"/>s may be cached, but the performance benefit will be minimal.</para>
    /// <para>Calling outside the main thread will fail.</para>
    /// <para>This function does not throw.</para>
    /// <para><see cref="IDisposable.Dispose"/> will be ignored.</para>
    /// <para>If the texture is unavailable for any reason, then the returned instance of
    /// <see cref="IDalamudTextureWrap"/> will point to an empty texture instead.</para>
    /// </remarks>
    IDalamudTextureWrap GetWrapOrEmpty();

    /// <summary>Gets the texture for use with the current frame, or a default value specified via
    /// <paramref name="defaultWrap"/> if unavailable.</summary>
    /// <param name="defaultWrap">The default wrap to return if the requested texture was not immediately available.
    /// </param>
    /// <returns>An instance of <see cref="IDalamudTextureWrap"/> that is guaranteed to be available for the current
    /// frame being drawn.</returns>
    /// <remarks>
    /// <para>Do not cache the result of this function across draw calls for different frames.
    /// <see cref="ISharedImmediateTexture"/>s may be cached, but the performance benefit will be minimal.</para>
    /// <para>Calling outside the main thread will fail.</para>
    /// <para>This function does not throw.</para>
    /// <para><see cref="IDisposable.Dispose"/> will be ignored.</para>
    /// <para>If the texture is unavailable for any reason, then <paramref name="defaultWrap"/> will be returned.</para>
    /// </remarks>
    [return: NotNullIfNotNull(nameof(defaultWrap))]
    IDalamudTextureWrap? GetWrapOrDefault(IDalamudTextureWrap? defaultWrap = null);

    /// <summary>Attempts to get the texture for use with the current frame.</summary>
    /// <param name="texture">An instance of <see cref="IDalamudTextureWrap"/> that is guaranteed to be available for
    /// the current frame being drawn, or <c>null</c> if texture is not loaded (yet).</param>
    /// <param name="exception">The load exception, if any.</param>
    /// <returns><c>true</c> if <paramref name="texture"/> points to the loaded texture; <c>false</c> if the texture is
    /// still being loaded, or the load has failed.</returns>
    /// <remarks>
    /// <para>Do not cache the result of this function across draw calls for different frames.
    /// <see cref="ISharedImmediateTexture"/>s may be cached, but the performance benefit will be minimal.</para>
    /// <para>Calling outside the main thread will fail.</para>
    /// <para>This function does not throw.</para>
    /// <para><see cref="IDisposable.Dispose"/> on the returned <paramref name="texture"/> will be ignored.</para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when called outside the UI thread.</exception>
    bool TryGetWrap([NotNullWhen(true)] out IDalamudTextureWrap? texture, out Exception? exception);

    /// <summary>Creates a new instance of <see cref="IDalamudTextureWrap"/> holding a new reference to this texture.
    /// The returned texture is guaranteed to be available until <see cref="IDisposable.Dispose"/> is called.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the loaded texture on success.</returns>
    /// <remarks>
    /// <see cref="IDisposable.Dispose"/> must be called on the resulting instance of <see cref="IDalamudTextureWrap"/>
    /// from the returned <see cref="Task{TResult}"/> after use. Consider using
    /// <see cref="DisposeSafety.ToContentDisposedTask{T}"/> to dispose the result automatically according to the state
    /// of the task.</remarks>
    Task<IDalamudTextureWrap> RentAsync(CancellationToken cancellationToken = default);
}
