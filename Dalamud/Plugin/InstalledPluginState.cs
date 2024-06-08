using Dalamud.Utility;

namespace Dalamud.Plugin;

/// <summary>
/// State of an installed plugin.
/// </summary>
/// <param name="Name">The name of the plugin.</param>
/// <param name="InternalName">The internal name of the plugin.</param>
/// <param name="IsLoaded">Whether or not the plugin is loaded.</param>
/// <param name="Version">The version of the plugin.</param>
[Api10ToDo("Refactor into an interface, add wrappers for OpenMainUI and OpenConfigUI")]
public record InstalledPluginState(string Name, string InternalName, bool IsLoaded, Version Version);
