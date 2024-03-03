using Dalamud.Interface.Internal;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>A texture wrap that ignores <see cref="IDisposable.Dispose"/> calls.</summary>
internal class DisposeSuppressingTextureWrap : ForwardingTextureWrap
{
    private readonly IDalamudTextureWrap innerWrap;

    /// <summary>Initializes a new instance of the <see cref="DisposeSuppressingTextureWrap"/> class.</summary>
    /// <param name="wrap">The inner wrap.</param>
    public DisposeSuppressingTextureWrap(IDalamudTextureWrap wrap) => this.innerWrap = wrap;

    /// <inheritdoc/>
    protected override bool TryGetWrap(out IDalamudTextureWrap? wrap)
    {
        wrap = this.innerWrap;
        return true;
    }
}
