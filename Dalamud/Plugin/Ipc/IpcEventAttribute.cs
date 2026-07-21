namespace Dalamud.Plugin.Ipc;

/// <summary>
/// Marks a method as an IPC event subscriber, or a field/property as an IPC event sender.
/// </summary>
/// <remarks>
/// Name templates support <c>%m</c>/<c>{member}</c> (member name) and <c>%p</c>/<c>{<see cref="IDalamudPluginInterface.InternalName"/>}</c>.
/// </remarks>
/// <param name="name">
/// IPC name or template. When null, the member name is used.
/// </param>
/// <param name="applyPrefix">
/// When true, the create-call prefix (or plugin internal name) is prepended.
/// </param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
public sealed class IpcEventAttribute(string? name = null, bool applyPrefix = true) : Attribute
{
    /// <summary>Gets the IPC name or template.</summary>
    public string? Name { get; } = name;

    /// <summary>Gets a value indicating whether to apply the prefix.</summary>
    public bool ApplyPrefix { get; } = applyPrefix;
}
