namespace Dalamud.Plugin.Ipc;

/// <summary>
/// Marks a method as an IPC provider, or a field/property as an IPC subscriber binding.
/// </summary>
/// <remarks>
/// When <paramref name="name"/> is null, the tag is built from the create-call prefix, optional <see cref="IpcPrefixAttribute"/>, and the member name.
/// When <paramref name="name"/> is set, it must be the full IPC tag and <paramref name="applyPrefix"/> must be <see langword="false"/>.
/// </remarks>
/// <param name="name">
/// IPC name or template. When null, the member name is used.
/// </param>
/// <param name="applyPrefix">
/// When true, the create-call prefix (or plugin internal name) is prepended. Must be false when <paramref name="name"/> is specified.
/// </param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
public sealed class IpcAttribute(string? name = null, bool applyPrefix = true) : Attribute
{
    /// <summary>Gets the IPC name or template.</summary>
    public string? Name { get; } = name;

    /// <summary>Gets a value indicating whether to apply the prefix.</summary>
    public bool ApplyPrefix { get; } = applyPrefix;
}
