namespace Dalamud.Plugin.Ipc.Internal;

/// <summary>
/// Marks attribute-bound IPC wrappers so registrations can invalidate them on dispose.
/// </summary>
internal interface IIpcBoundCallable
{
    /// <summary>Marks this callable as disposed.</summary>
    void MarkDisposed();
}
