using System.Linq;

using Dalamud.Interface.Internal.Windows;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Internal.Types.Manifest;

namespace Dalamud.CorePlugin.PluginInstallerV2.Drawing;

/// <summary>
/// Class of helpers for getting information relevant to drawing plugin entries in the plugin installer.
/// </summary>
internal static class ManifestHelpers
{
    /// <summary>
    /// Gets plugin icon from Manifest.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    /// <returns>Default Icon or Loaded Texture.</returns>
    public static IDalamudTextureWrap GetPluginIcon(IPluginManifest manifest)
    {
        var imageCache = Service<PluginImageCache>.Get();

        var sourceRepo = manifest is RemotePluginManifest remotePluginManifest ? remotePluginManifest.SourceRepo : null;

        var iconTex = imageCache.DefaultIcon;
        var hasIcon = imageCache.TryGetIcon(null, manifest, sourceRepo is { IsThirdParty: true }, out var cachedIconTex, out _);

        if (hasIcon && cachedIconTex != null)
        {
            iconTex = cachedIconTex;
        }

        return iconTex;
    }

    /// <summary>
    /// Get the LocalPlugin with same internal name as this manifest or null if no plugin with this internal name is installed.
    /// </summary>
    /// <param name="manifest">Manifest to check.</param>
    /// <returns>LocalPlugin if found, null if not installed.</returns>
    public static LocalPlugin? GetInstalledPluginFromManifest(IPluginManifest manifest)
        => Service<PluginManager>.Get().InstalledPlugins.FirstOrDefault(plugin => plugin.InternalName == manifest.InternalName);

    /// <summary>
    /// Get if the manifest represents an updatable plugin.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    /// <returns>True if the manifest represents an updatable plugin.</returns>
    public static bool IsAvailableForUpdate(IPluginManifest manifest)
        => IsAvailableForUpdate(GetInstalledPluginFromManifest(manifest));

    /// <summary>
    /// Get if the manifest represents a custom repo plugin.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    /// <returns>True if manifest represents a custom repo plugin.</returns>
    public static bool IsThirdParty(IPluginManifest manifest)
        => manifest is RemotePluginManifest { SourceRepo.IsThirdParty: true };

    /// <summary>
    /// Get if the manifest represents an updatable plugin.
    /// </summary>
    /// <param name="plugin">LocalPlugin.</param>
    /// <returns>True if the manifest represents an updatable plugin.</returns>
    public static bool IsAvailableForUpdate(LocalPlugin? plugin) => plugin switch
    {
        null => false,
        { IsDev: true } => false,
        _ => Service<PluginManager>.Get().UpdatablePlugins.Any(updatablePlugin => updatablePlugin.InstalledPlugin == plugin),
    };

    /// <summary>
    /// Gets the formatted punchline for this manifest.
    /// </summary>
    /// <remarks>
    /// Strips newlines from punchline, it's meant to be a single line not a paragraph.
    /// </remarks>
    /// <param name="manifest">Manifest.</param>
    /// <returns>Formatted punchline, or string.Empty if no punchline exists.</returns>
    public static string GetPunchline(IPluginManifest manifest)
        => manifest.Punchline?.Replace("\n", " ") ?? string.Empty;
}
