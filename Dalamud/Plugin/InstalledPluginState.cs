using System;

namespace Dalamud.Plugin;

public record InstalledPluginState(string Name, string InternalName, bool IsLoaded, Version Version);
