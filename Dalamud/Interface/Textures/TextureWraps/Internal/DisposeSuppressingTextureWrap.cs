namespace Dalamud.Interface.Textures.TextureWraps.Internal;

/// <summary>A texture wrap that ignores <see cref="IDisposable.Dispose"/> calls.</summary>
/// <param name="innerWrap">The inner wrap.</param>
internal class DisposeSuppressingTextureWrap(IDalamudTextureWrap innerWrap) : ForwardingTextureWrap
{
    /// <inheritdoc/>
    protected override bool TryGetWrap(out IDalamudTextureWrap? wrap)
    {
        wrap = innerWrap;
        return true;
    }
}
