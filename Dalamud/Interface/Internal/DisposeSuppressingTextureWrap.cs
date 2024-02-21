namespace Dalamud.Interface.Internal;

/// <summary>
/// A texture wrap that ignores <see cref="IDisposable.Dispose"/> calls.
/// </summary>
internal sealed class DisposeSuppressingTextureWrap : IDalamudTextureWrap
{
    private readonly IDalamudTextureWrap innerWrap;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposeSuppressingTextureWrap"/> class.
    /// </summary>
    /// <param name="wrap">The inner wrap.</param>
    public DisposeSuppressingTextureWrap(IDalamudTextureWrap wrap) => this.innerWrap = wrap;

    /// <inheritdoc/>
    public IntPtr ImGuiHandle => this.innerWrap.ImGuiHandle;

    /// <inheritdoc/>
    public int Width => this.innerWrap.Width;

    /// <inheritdoc/>
    public int Height => this.innerWrap.Height;

    /// <inheritdoc/>
    public void Dispose()
    {
        // suppressed
    }
}
