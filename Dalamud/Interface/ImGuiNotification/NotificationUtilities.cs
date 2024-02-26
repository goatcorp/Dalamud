using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Game.Text;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.Windows;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Storage.Assets;

using ImGuiNET;

namespace Dalamud.Interface.ImGuiNotification;

/// <summary>Utilities for implementing stuff under <see cref="ImGuiNotification"/>.</summary>
public static class NotificationUtilities
{
    /// <inheritdoc cref="INotificationIconSource.From(SeIconChar)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INotificationIconSource ToIconSource(this SeIconChar iconChar) =>
        INotificationIconSource.From(iconChar);

    /// <inheritdoc cref="INotificationIconSource.From(FontAwesomeIcon)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INotificationIconSource ToIconSource(this FontAwesomeIcon iconChar) =>
        INotificationIconSource.From(iconChar);

    /// <inheritdoc cref="INotificationIconSource.From(IDalamudTextureWrap,bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INotificationIconSource ToIconSource(this IDalamudTextureWrap? wrap, bool takeOwnership = true) =>
        INotificationIconSource.From(wrap, takeOwnership);

    /// <inheritdoc cref="INotificationIconSource.FromFile(string)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INotificationIconSource ToIconSource(this FileInfo fileInfo) =>
        INotificationIconSource.FromFile(fileInfo.FullName);

    /// <summary>Draws an icon string.</summary>
    /// <param name="fontHandleLarge">The font handle to use.</param>
    /// <param name="c">The icon character.</param>
    /// <param name="minCoord">The coordinates of the top left of the icon area.</param>
    /// <param name="maxCoord">The coordinates of the bottom right of the icon area.</param>
    /// <param name="color">The foreground color.</param>
    internal static unsafe void DrawIconString(
        IFontHandle fontHandleLarge,
        char c,
        Vector2 minCoord,
        Vector2 maxCoord,
        Vector4 color)
    {
        var smallerDim = Math.Max(maxCoord.Y - minCoord.Y, maxCoord.X - minCoord.X);
        using (fontHandleLarge.Push())
        {
            var font = ImGui.GetFont();
            ref readonly var glyph = ref *(ImGuiHelpers.ImFontGlyphReal*)font.FindGlyph(c).NativePtr;
            var size = glyph.XY1 - glyph.XY0;
            var smallerSizeDim = Math.Min(size.X, size.Y);
            var scale = smallerSizeDim > smallerDim ? smallerDim / smallerSizeDim : 1f;
            size *= scale;
            var pos = ((minCoord + maxCoord) - size) / 2;
            pos += ImGui.GetWindowPos();
            ImGui.GetWindowDrawList().AddImage(
                font.ContainerAtlas.Textures[glyph.TextureIndex].TexID,
                pos,
                pos + size,
                glyph.UV0,
                glyph.UV1,
                ImGui.GetColorU32(color with { W = color.W * ImGui.GetStyle().Alpha }));
        }
    }

    /// <summary>Draws the given texture, or the icon of the plugin if texture is <c>null</c>.</summary>
    /// <param name="texture">The texture.</param>
    /// <param name="minCoord">The coordinates of the top left of the icon area.</param>
    /// <param name="maxCoord">The coordinates of the bottom right of the icon area.</param>
    /// <param name="initiatorPlugin">The initiator plugin.</param>
    internal static void DrawTexture(
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
