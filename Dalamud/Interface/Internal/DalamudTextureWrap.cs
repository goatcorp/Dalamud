using Dalamud.Utility;

namespace Dalamud.Interface.Internal;

/// <summary>
/// Safety harness for ImGuiScene textures that will defer destruction until
/// the end of the frame.
/// </summary>
public class DalamudTextureWrap : IDalamudTextureWrap, IDeferredDisposable
{
    private readonly IDalamudTextureWrap inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudTextureWrap"/> class.
    /// </summary>
    /// <param name="inner">The pointer to an instance of <see cref="IDalamudTextureWrap"/>.</param>
    internal DalamudTextureWrap(IDalamudTextureWrap inner) => this.inner = inner;

    /// <inheritdoc/>
    public nint ImGuiHandle => this.inner.ImGuiHandle;

    /// <inheritdoc/>
    public int Width => this.inner.Width;

    /// <inheritdoc/>
    public int Height => this.inner.Height;

    /// <summary>
    /// Queue the texture to be disposed once the frame ends.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Actually dispose the wrapped texture.
    /// </summary>
    void IDeferredDisposable.RealDispose()
    {
        this.inner.Dispose();
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            Service<InterfaceManager>.GetNullable()?.EnqueueDeferredDispose(this);
        }
    }
}
