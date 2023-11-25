using System.Numerics;

namespace Dalamud.Interface.Internal;

/// <summary>
/// Base interface for all Dalamud-owned texture wraps.
/// </summary>
public interface IDalamudTextureWrap : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether this instance of <see cref="IDalamudTextureWrap"/> has been disposed.
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// Gets a texture handle suitable for direct use with ImGui functions.
    /// </summary>
    IntPtr ImGuiHandle { get; }

    /// <summary>
    /// Gets the width of the texture.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the height of the texture.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets the size vector of the texture using Width, Height.
    /// </summary>
    Vector2 Size => new(this.Width, this.Height);
}

/// <summary>
/// Safety harness for ImGuiScene textures that will defer destruction until
/// the end of the frame.
/// </summary>
public class DalamudTextureWrap : IDalamudTextureWrap
{
    private readonly IDalamudTextureWrap inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudTextureWrap"/> class.
    /// </summary>
    /// <param name="inner">The pointer to an instance of <see cref="IDalamudTextureWrap"/>.</param>
    internal DalamudTextureWrap(IDalamudTextureWrap inner) => this.inner = inner;

    /// <inheritdoc/>
    public bool IsDisposed => this.inner.IsDisposed;
    
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
        Service<InterfaceManager>.GetNullable()?.EnqueueDeferredDispose(this);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Actually dispose the wrapped texture.
    /// </summary>
    internal void RealDispose() => this.inner.Dispose();
}
