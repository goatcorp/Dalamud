namespace Dalamud.Hooking;

/// <summary>
/// Interface describing a generic hook.
/// </summary>
public interface IDalamudHook : IDisposable
{
    /// <summary>
    /// Gets the address to hook.
    /// </summary>
    public IntPtr Address { get; }

    /// <summary>
    /// Gets a value indicating whether or not the hook is enabled.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether or not the hook is disposed.
    /// </summary>
    public bool IsDisposed { get; }

    /// <summary>
    /// Gets the name of the hooking backend used for the hook.
    /// </summary>
    public string BackendName { get; }
}
