using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Storage.Assets;

namespace Dalamud.Interface.Textures;

/// <summary>
/// Wraps a dalamud texture allowing interoperability with certain services. Only use this if you need to provide a texture that has been created or rented as a ISharedImmediateTexture.
/// </summary>
public class ForwardingSharedImmediateTexture : ISharedImmediateTexture
{
    private readonly IDalamudTextureWrap textureWrap;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForwardingSharedImmediateTexture"/> class.
    /// </summary>
    /// <param name="textureWrap">A textureWrap that has been created or provided by RentAsync.</param>
    public ForwardingSharedImmediateTexture(IDalamudTextureWrap textureWrap)
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
