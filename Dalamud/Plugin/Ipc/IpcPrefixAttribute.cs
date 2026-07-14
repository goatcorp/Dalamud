namespace Dalamud.Plugin.Ipc;

/// <summary>
/// Optional type-level name segment prepended to member names when the member attribute does not specify a name.
/// </summary>
/// <remarks>
/// For example, <c>[IpcPrefix("PluginState")]</c> on a type with a member <c>IsBusy</c> resolves to <c>PluginState.IsBusy</c> before the create-call prefix is applied.
/// </remarks>
/// <param name="prefix">The name segment to prepend to default member names.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class IpcPrefixAttribute(string prefix) : Attribute
{
    /// <summary>Gets the type-level name segment.</summary>
    public string Prefix { get; } = prefix ?? throw new ArgumentNullException(nameof(prefix));
}
