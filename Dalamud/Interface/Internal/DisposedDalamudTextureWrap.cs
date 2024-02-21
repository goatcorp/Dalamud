namespace Dalamud.Interface.Internal;

/// <summary>
/// A disposed texture wrap.
/// </summary>
internal sealed class DisposedDalamudTextureWrap : IDalamudTextureWrap
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static readonly DisposedDalamudTextureWrap Instance = new();

    private DisposedDalamudTextureWrap()
    {
    }

    /// <inheritdoc/>
    public IntPtr ImGuiHandle => throw new ObjectDisposedException(nameof(DisposedDalamudTextureWrap));

    /// <inheritdoc/>
    public int Width => throw new ObjectDisposedException(nameof(DisposedDalamudTextureWrap));

    /// <inheritdoc/>
    public int Height => throw new ObjectDisposedException(nameof(DisposedDalamudTextureWrap));

    /// <inheritdoc/>
    public void Dispose()
    {
        // suppressed
    }
}
