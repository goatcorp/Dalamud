using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Internal.Windows;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Storage.Assets;

namespace Dalamud.CorePlugin.PluginInstallerV2.DrawingExtensions;

/// <summary>
/// Class of extensions for drawing various parts of a LocalPlugin in a generic way.
/// This is intended to prevent code duplication when drawing things like Title, Download Count, etc.
/// </summary>
[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:Prefix local calls with this", Justification = "'this' is not valid within extension blocks.")]
internal static class LocalPluginExtensions
{
    extension(LocalPlugin localPlugin) {
        /// <summary>
        /// Draws Plugin Name.
        /// </summary>
        public void DrawPluginName()
        {
            ImGui.Text(localPlugin.Manifest.Name);
        }

        public void DrawIcon()
        {
            var imageCache = Service<PluginImageCache>.Get();
            var assetManager = Service<DalamudAssetManager>.Get();

            if (!imageCache.TryGetIcon(
                    localPlugin,
                    localPlugin.Manifest,
                    localPlugin.IsThirdParty,
                    out var texture,
                    out _) || texture is null)
            {
                texture = assetManager.GetDalamudTextureWrap(DalamudAsset.DefaultIcon);
            }

            ImGui.Image(texture.Handle, new Vector2(32.0f, 32.0f));
        }
    }
}
