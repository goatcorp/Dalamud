namespace Dalamud.Plugin.Ipc.Internal;

/// <summary>
/// Stores the internal name and effective working ID of a plugin accessing datashare.
/// </summary>
/// <param name="InternalName">The internal name of the plugin.</param>
/// <param name="EffectiveWorkingId">The effective working ID of the plugin.</param>
public record DataCachePluginId(string InternalName, Guid EffectiveWorkingId);
