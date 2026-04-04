namespace Dalamud.CorePlugin.PluginInstallerV2.Enums.EnumExtensions;

/// <summary>
/// Extensions methods for PluginHeaderFlags.
/// </summary>
internal static class PluginHeaderFlagsExtensions
{
    /// <summary>
    /// Adds the specified flag to this flags value.
    /// </summary>
    /// <param name="flags">this.</param>
    /// <param name="newFlag">Flag to add.</param>
    public static void AddFlag(ref this PluginHeaderFlags flags, PluginHeaderFlags newFlag)
    {
        flags |= newFlag;
    }
}
