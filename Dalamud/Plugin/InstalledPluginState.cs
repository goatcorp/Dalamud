using Dalamud.Utility;

namespace Dalamud.Plugin;

[Api10ToDo("Refactor into an interface, add wrappers for OpenMainUI and OpenConfigUI")]
public record InstalledPluginState(string Name, string InternalName, bool IsLoaded, Version Version);
