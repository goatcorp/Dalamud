using System.Threading;

using Dalamud.Utility;

using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Internal;

/// <summary>
/// A texture wrap that is created by cloning the underlying <see cref="IDalamudTextureWrap.ImGuiHandle"/>.
/// </summary>
internal sealed unsafe class UnknownTextureWrap : IDalamudTextureWrap, IDeferredDisposable
{
    private IntPtr imGuiHandle;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownTextureWrap"/> class.
    /// </summary>
    /// <param name="unknown">The pointer to <see cref="IUnknown"/> that is suitable for use with
    /// <see cref="IDalamudTextureWrap.ImGuiHandle"/>.</param>
    /// <param name="width">The width of the texture.</param>
    /// <param name="height">The height of the texture.</param>
    /// <param name="callAddRef">If <c>true</c>, call <see cref="IUnknown.AddRef"/>.</param>
    public UnknownTextureWrap(IUnknown* unknown, int width, int height, bool callAddRef)
    {
        ObjectDisposedException.ThrowIf(unknown is null, typeof(IUnknown));
        this.imGuiHandle = (nint)unknown;
        this.Width = width;
        this.Height = height;
        if (callAddRef)
            unknown->AddRef();
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="UnknownTextureWrap"/> class.
    /// </summary>
    ~UnknownTextureWrap() => this.Dispose(false);

    /// <inheritdoc/>
    public nint ImGuiHandle =>
        this.imGuiHandle == nint.Zero
            ? throw new ObjectDisposedException(nameof(UnknownTextureWrap))
            : this.imGuiHandle;

    /// <inheritdoc/>
    public int Width { get; }

    /// <inheritdoc/>
    public int Height { get; }

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
        var handle = Interlocked.Exchange(ref this.imGuiHandle, nint.Zero);
        if (handle != nint.Zero)
            ((IUnknown*)handle)->Release();
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
            Service<InterfaceManager>.GetNullable()?.EnqueueDeferredDispose(this);
        else
            ((IDeferredDisposable)this).RealDispose();
    }
}
