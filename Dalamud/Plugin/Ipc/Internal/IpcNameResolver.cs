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
    /// <param name="applyPrefix">Whether to apply <paramref name="createPrefix"/>.</param>
    /// <param name="memberName">The member name.</param>
    /// <param name="typePrefix">Optional type-level prefix from <see cref="IpcPrefixAttribute"/>.</param>
    /// <param name="createPrefix">Prefix from CreateIpc* (or plugin internal name).</param>
    /// <param name="pluginInternalName">Calling plugin internal name for %p / {plugin}.</param>
    /// <returns>The full IPC tag.</returns>
    public static string Resolve(string? attributeName, bool applyPrefix, string memberName, string? typePrefix, string createPrefix, string pluginInternalName)
    {
        var name = attributeName;
        if (string.IsNullOrEmpty(name))
        {
            name = string.IsNullOrEmpty(typePrefix) ? memberName : $"{typePrefix}.{memberName}";
        }

        name = ApplyTemplates(name, memberName, pluginInternalName);

        if (!applyPrefix)
            return name;

        if (string.IsNullOrEmpty(createPrefix))
            return name;

        return $"{createPrefix}.{name}";
    }

    /// <summary>
    /// Applies <c>%m</c>/<c>{member}</c> and <c>%p</c>/<c>{plugin}</c> templates.
    /// </summary>
    /// <param name="name">The name or template.</param>
    /// <param name="memberName">The member name for <c>%m</c>/<c>{member}</c>.</param>
    /// <param name="pluginInternalName">The plugin internal name for <c>%p</c>/<c>{plugin}</c>.</param>
    /// <returns>The name with templates applied.</returns>
    public static string ApplyTemplates(string name, string memberName, string pluginInternalName)
    {
        return name
            .Replace("%m", memberName, StringComparison.Ordinal)
            .Replace("{member}", memberName, StringComparison.Ordinal)
            .Replace("%p", pluginInternalName, StringComparison.Ordinal)
            .Replace("{plugin}", pluginInternalName, StringComparison.Ordinal);
    }
}
