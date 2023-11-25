namespace Dalamud.ImGuiScene.Helpers;

/// <summary>
/// Wraps an unmanaged type as a reference type (class).
/// </summary>
/// <typeparam name="T">The contained unmanaged type.</typeparam>
internal sealed class StructWrapper<T> : IDisposable
    where T : unmanaged
{
    private readonly T data;

    /// <summary>
    /// Initializes a new instance of the <see cref="StructWrapper{T}"/> class.
    /// </summary>
    /// <param name="obj">The parameter.</param>
    public StructWrapper(T obj) => this.data = obj;

    /// <summary>
    /// Finalizes an instance of the <see cref="StructWrapper{T}"/> class.
    /// </summary>
    ~StructWrapper() => this.ReleaseUnmanagedResources();

    /// <summary>
    /// Gets the struct.
    /// </summary>
    public ref readonly T O => ref this.data;

    public static implicit operator T(StructWrapper<T> t) => t.O;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources() => (this.data as IDisposable)?.Dispose();
}
