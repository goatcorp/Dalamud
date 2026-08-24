namespace Dalamud.Plugin.Ipc.Internal;

/// <summary>
/// Resolves IPC tag names from attributes and member metadata.
/// </summary>
internal static class IpcNameResolver
{
    /// <summary>
    /// Resolves the full IPC tag.
    /// </summary>
    /// <param name="attributeName">
    /// Name from <see cref="IpcAttribute"/> or <see cref="IpcEventAttribute"/>.
    /// When set, this must be the full IPC tag and <paramref name="applyPrefix"/> must be <see langword="false"/>.
    /// </param>
    /// <param name="applyPrefix">Whether to apply <paramref name="createPrefix"/>.</param>
    /// <param name="memberName">The member name.</param>
    /// <param name="typePrefix">Optional type-level prefix from <see cref="IpcPrefixAttribute"/>.</param>
    /// <param name="createPrefix">Prefix from CreateIpc* (or plugin internal name).</param>
    /// <returns>The full IPC tag.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="attributeName"/> is set and <paramref name="applyPrefix"/> is <see langword="true"/>.
    /// </exception>
    public static string Resolve(string? attributeName, bool applyPrefix, string memberName, string? typePrefix, string createPrefix)
    {
        if (!string.IsNullOrEmpty(attributeName))
        {
            if (applyPrefix)
            {
                throw new ArgumentException("When specifying an IPC name override, applyPrefix must be false.");
            }

            return ApplyMemberTemplate(attributeName, memberName);
        }

        var name = string.IsNullOrEmpty(typePrefix) ? memberName : $"{typePrefix}.{memberName}";

        if (!applyPrefix || string.IsNullOrEmpty(createPrefix))
            return name;

        return $"{createPrefix}.{name}";
    }

    /// <summary>
    /// Replaces <c>%m</c> with the member name.
    /// </summary>
    /// <param name="name">The name or template.</param>
    /// <param name="memberName">The member name for <c>%m</c>.</param>
    /// <returns>The name with <c>%m</c> applied.</returns>
    public static string ApplyMemberTemplate(string name, string memberName)
        => name.Replace("%m", memberName, StringComparison.Ordinal);
}
