namespace Dalamud.Plugin.Ipc;

/// <summary>
/// Marks a method as an IPC provider, or a field/property as an IPC subscriber binding.
/// </summary>
/// <remarks>
/// When <paramref name="applyPrefix"/> is true, the tag is built from the create-call prefix, optional <see cref="IpcPrefixAttribute"/>, and <paramref name="name"/> (or the member name when null).
/// When <paramref name="applyPrefix"/> is false and <paramref name="name"/> is set, <paramref name="name"/> is used as the full IPC tag.
/// </remarks>
/// <param name="name">
/// IPC name segment or full tag. When null, the member name is used.
/// </param>
/// <param name="applyPrefix">
/// When true, the create-call prefix (or plugin internal name) and optional <see cref="IpcPrefixAttribute"/> are prepended.
/// When false with a name, the name is the full IPC tag.
/// </param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
public sealed class IpcAttribute(string? name = null, bool applyPrefix = true) : Attribute
{
    /// <summary>Gets the IPC name.</summary>
    public string? Name { get; } = name;

    /// <summary>Gets a value indicating whether to apply the prefix.</summary>
    public bool ApplyPrefix { get; } = applyPrefix;
}
