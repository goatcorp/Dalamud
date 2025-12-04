namespace Dalamud.Plugin.Ipc;

/// <summary>
/// The context associated for an IPC call. Reads from ThreadLocal.
/// </summary>
public class IpcContext
{
    /// <summary>
    /// Gets the plugin that initiated this IPC call.
    /// </summary>
    public IExposedPlugin? SourcePlugin { get; init; }

    /// <inheritdoc/>
    public override string ToString() => $"<IpcContext SourcePlugin={this.SourcePlugin?.Name ?? "null"}>";
}
