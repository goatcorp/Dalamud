using Dalamud.Utility;

using ImGuiScene;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Internal;

/// <summary>Safety harness for ImGuiScene textures that will defer destruction until the end of the frame.</summary>
[Obsolete($"Use {nameof(IDalamudTextureWrap)}.")]
[Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
public class DalamudTextureWrap : IDalamudTextureWrap, IDeferredDisposable
{
    private readonly TextureWrap wrappedWrap;

    /// <summary>Initializes a new instance of the <see cref="DalamudTextureWrap"/> class.</summary>
    /// <param name="wrappingWrap">The texture wrap to wrap.</param>
    internal DalamudTextureWrap(TextureWrap wrappingWrap) => this.wrappedWrap = wrappingWrap;

    /// <summary>Finalizes an instance of the <see cref="DalamudTextureWrap"/> class.</summary>
    ~DalamudTextureWrap() => this.Dispose(false);

    /// <inheritdoc/>
    public IntPtr ImGuiHandle => this.wrappedWrap.ImGuiHandle;

    /// <inheritdoc/>
    public int Width => this.wrappedWrap.Width;

    /// <inheritdoc/>
    public int Height => this.wrappedWrap.Height;

    /// <summary>Queue the texture to be disposed once the frame ends.</summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Actually dispose the wrapped texture.</summary>
    void IDeferredDisposable.RealDispose() => this.wrappedWrap.Dispose();

    private void Dispose(bool disposing)
    {
        if (disposing)
            Service<InterfaceManager>.GetNullable()?.EnqueueDeferredDispose(this);
    }
}
