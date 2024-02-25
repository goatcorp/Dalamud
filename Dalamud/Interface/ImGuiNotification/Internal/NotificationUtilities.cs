using System.Numerics;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.Windows;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Storage.Assets;

using ImGuiNET;

namespace Dalamud.Interface.ImGuiNotification.Internal;

/// <summary>Utilities for implementing stuff under <see cref="ImGuiNotification"/>.</summary>
internal static class NotificationUtilities
{
    /// <summary>Draws the given texture, or the icon of the plugin if texture is <c>null</c>.</summary>
    /// <param name="texture">The texture.</param>
    /// <param name="minCoord">The coordinates of the top left of the icon area.</param>
    /// <param name="maxCoord">The coordinates of the bottom right of the icon area.</param>
    /// <param name="initiatorPlugin">The initiator plugin.</param>
    public static void DrawTexture(
        IDalamudTextureWrap? texture,
        Vector2 minCoord,
        Vector2 maxCoord,
        LocalPlugin? initiatorPlugin)
    {
        var handle = nint.Zero;
        var size = Vector2.Zero;
        if (texture is not null)
        {
            try
            {
                handle = texture.ImGuiHandle;
                size = texture.Size;
            }
            catch
            {
                // must have been disposed or something; ignore the texture
            }
        }

        if (handle == nint.Zero)
        {
            var dam = Service<DalamudAssetManager>.Get();
            if (initiatorPlugin is null)
            {
                texture = dam.GetDalamudTextureWrap(DalamudAsset.LogoSmall);
            }
            else
            {
                if (!Service<PluginImageCache>.Get().TryGetIcon(
                        initiatorPlugin,
                        initiatorPlugin.Manifest,
                        initiatorPlugin.IsThirdParty,
                        out texture) || texture is null)
                {
                    texture = initiatorPlugin switch
                    {
                        { IsDev: true } => dam.GetDalamudTextureWrap(DalamudAsset.DevPluginIcon),
                        { IsThirdParty: true } => dam.GetDalamudTextureWrap(DalamudAsset.ThirdInstalledIcon),
                        _ => dam.GetDalamudTextureWrap(DalamudAsset.InstalledIcon),
                    };
                }
            }

            handle = texture.ImGuiHandle;
            size = texture.Size;
        }

        if (size.X > maxCoord.X - minCoord.X)
            size *= (maxCoord.X - minCoord.X) / size.X;
        if (size.Y > maxCoord.Y - minCoord.Y)
            size *= (maxCoord.Y - minCoord.Y) / size.Y;
        ImGui.SetCursorPos(((minCoord + maxCoord) - size) / 2);
        ImGui.Image(handle, size);
    }
}
