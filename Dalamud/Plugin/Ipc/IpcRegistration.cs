namespace Dalamud.Plugin.Ipc;

/// <summary>
/// Lifetime handle for a batch of IPC bindings created by <see cref="IDalamudPluginInterface.CreateIpcSubscribers{T}(string?)"/> or <see cref="IDalamudPluginInterface.CreateIpcProviders{T}(string?)"/>.
/// </summary>
public interface IpcRegistration : IDisposable
{
    /// <summary>Gets a value indicating whether this registration has been disposed.</summary>
    bool IsDisposed { get; }
}

/// <summary>
/// Lifetime handle that also exposes the bound IPC instance.
/// </summary>
/// <typeparam name="T">The IPC type.</typeparam>
public interface IpcRegistration<out T> : IpcRegistration
{
    /// <summary>Gets the bound IPC instance.</summary>
    T Instance { get; }
}
