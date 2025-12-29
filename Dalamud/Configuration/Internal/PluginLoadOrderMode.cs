namespace Dalamud.Configuration.Internal;

/// <summary>
/// Override for plugin load flavor in the load order list.
/// </summary>
internal enum PluginLoadOrderMode
{
    /// <summary>
    /// Use the plugin's default load flavor.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Force synchronous load.
    /// </summary>
    ForceSync = 1,

    /// <summary>
    /// Force asynchronous load.
    /// </summary>
    ForceAsync = 2,
}
