using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;

namespace Dalamud.Storage.Assets;

/// <summary>
/// Wraps a dalamud asset texture allowing interoperability with certain services.
/// </summary>
internal class DalamudAssetTexture : ISharedImmediateTexture
{
    private readonly IDalamudTextureWrap textureWrap;

    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudAssetTexture"/> class.
    /// </summary>
    /// <param name="textureWrap">A textureWrap loaded by <see cref="DalamudAssetManager"/>.</param>
    internal DalamudAssetTexture(IDalamudTextureWrap textureWrap)
    {
        this.textureWrap = textureWrap;
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap GetWrapOrEmpty()
    {
        return this.textureWrap;
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap? GetWrapOrDefault(IDalamudTextureWrap? defaultWrap = null)
    {
        return this.textureWrap;
    }

    /// <inheritdoc/>
    public bool TryGetWrap(out IDalamudTextureWrap? texture, out Exception? exception)
    {
        texture = this.textureWrap;
        exception = null;
        return true;
    }

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> RentAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(this.textureWrap);
    }
}
