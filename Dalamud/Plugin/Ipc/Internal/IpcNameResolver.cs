namespace Dalamud.Plugin.Ipc.Internal;

/// <summary>
/// Resolves IPC tag names from attributes and member metadata.
/// </summary>
internal static class IpcNameResolver
{
    /// <summary>
    /// Resolves the full IPC tag.
    /// </summary>
    /// <param name="attributeName">Name from <see cref="IpcAttribute"/> or <see cref="IpcEventAttribute"/>.</param>
    /// <param name="applyPrefix">Whether to apply <paramref name="createPrefix"/> and <paramref name="typePrefix"/>.</param>
    /// <param name="memberName">The member name.</param>
    /// <param name="typePrefix">Optional type-level prefix from <see cref="IpcPrefixAttribute"/>.</param>
    /// <param name="createPrefix">Prefix from CreateIpc* (or plugin internal name).</param>
    /// <returns>The full IPC tag.</returns>
    public static string Resolve(string? attributeName, bool applyPrefix, string memberName, string? typePrefix, string createPrefix)
    {
        if (!applyPrefix)
        {
            if (!string.IsNullOrEmpty(attributeName))
                return attributeName;

            return string.IsNullOrEmpty(typePrefix) ? memberName : $"{typePrefix}.{memberName}";
        }

        var leaf = string.IsNullOrEmpty(attributeName) ? memberName : attributeName;
        var name = string.IsNullOrEmpty(typePrefix) ? leaf : $"{typePrefix}.{leaf}";

        if (string.IsNullOrEmpty(createPrefix))
            return name;

        return $"{createPrefix}.{name}";
    }
}
