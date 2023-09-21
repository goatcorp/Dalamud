namespace Dalamud.Plugin.Internal.Types.Manifest;

/// <summary>
/// A fake enum representing "special" sources for plugins.
/// </summary>
public static class SpecialPluginSource 
{
    /// <summary>
    /// Indication that this plugin came from the official Dalamud repository. 
    /// </summary>
    public const string MainRepo = "OFFICIAL";

    /// <summary>
    /// Indication that this plugin is loaded as a dev plugin. See also <see cref="DalamudPluginInterface.IsDev"/>.
    /// </summary>
    public const string DevPlugin = "DEVPLUGIN";
}
