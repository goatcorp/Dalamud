using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Interface.Fonts;
using Dalamud.Interface.Internal.Windows;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Storage.Assets;

namespace Dalamud.Interface.ImGuiNotification;

/// <summary>Utilities for implementing stuff under <see cref="ImGuiNotification"/>.</summary>
public static class NotificationUtilities
{
    /// <inheritdoc cref="INotificationIcon.From(SeIconChar)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INotificationIcon ToNotificationIcon(this SeIconChar iconChar) =>
        INotificationIcon.From(iconChar);

    /// <inheritdoc cref="INotificationIcon.From(FontAwesomeIcon)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INotificationIcon ToNotificationIcon(this FontAwesomeIcon iconChar) =>
        INotificationIcon.From(iconChar);

    /// <inheritdoc cref="INotificationIcon.FromFile(string)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INotificationIcon ToNotificationIcon(this FileInfo fileInfo) =>
        INotificationIcon.FromFile(fileInfo.FullName);

    /// <summary>Draws an icon from a font and a <see cref="char"/>.</summary>
    /// <param name="minCoord">The coordinates of the top left of the icon area.</param>
    /// <param name="maxCoord">The coordinates of the bottom right of the icon area.</param>
    /// <param name="c">The icon character.</param>
    /// <param name="iconFont">The font to use.</param>
    /// <param name="color">The foreground color.</param>
    /// <returns><c>true</c> if anything has been drawn.</returns>
    internal static bool DrawIconFrom(
        Vector2 minCoord,
        Vector2 maxCoord,
        char c,
        ImFontRef iconFont,
        Vector4 color)
    {
        if (c is '\0' or char.MaxValue)
            return false;

        using (iconFont.Push())
        {
            if (!ImGui.Font.IsGlyphInFont(c))
                return false;
            var windowPos = ImGui.GetWindowPos();
            return ImGui.GetWindowDrawList().AddTextFitted(
                minCoord + windowPos,
                maxCoord + windowPos,
                new ReadOnlySpan<char>(in c),
                ImGui.GetColorU32(color));
        }
    }

    /// <summary>Draws an icon from an instance of <see cref="ISharedImmediateTexture"/>.</summary>
    /// <param name="minCoord">The coordinates of the top left of the icon area.</param>
    /// <param name="maxCoord">The coordinates of the bottom right of the icon area.</param>
    /// <param name="texture">The texture.</param>
    /// <returns><c>true</c> if anything has been drawn.</returns>
    internal static bool DrawIconFrom(Vector2 minCoord, Vector2 maxCoord, ISharedImmediateTexture? texture)
    {
        if (texture is null)
            return false;

        return DrawIconFrom(minCoord, maxCoord, texture.GetWrapOrEmpty());
    }

    /// <summary>Draws an icon from an instance of <see cref="IDalamudTextureWrap"/>.</summary>
    /// <param name="minCoord">The coordinates of the top left of the icon area.</param>
    /// <param name="maxCoord">The coordinates of the bottom right of the icon area.</param>
    /// <param name="texture">The texture.</param>
    /// <returns><c>true</c> if anything has been drawn.</returns>
    internal static bool DrawIconFrom(Vector2 minCoord, Vector2 maxCoord, IDalamudTextureWrap? texture)
    {
        if (texture is null)
            return false;
        try
        {
            var handle = texture.Handle;
            var size = texture.Size;
            if (size.X > maxCoord.X - minCoord.X)
                size *= (maxCoord.X - minCoord.X) / size.X;
            if (size.Y > maxCoord.Y - minCoord.Y)
                size *= (maxCoord.Y - minCoord.Y) / size.Y;
            ImGui.SetCursorPos(((minCoord + maxCoord) - size) / 2);
            ImGui.Image(handle, size);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Draws an icon from an instance of <see cref="LocalPlugin"/>.</summary>
    /// <param name="minCoord">The coordinates of the top left of the icon area.</param>
    /// <param name="maxCoord">The coordinates of the bottom right of the icon area.</param>
    /// <param name="plugin">The plugin. Dalamud icon will be drawn if <c>null</c> is given.</param>
    /// <returns><c>true</c> if anything has been drawn.</returns>
    internal static bool DrawIconFrom(Vector2 minCoord, Vector2 maxCoord, LocalPlugin? plugin)
    {
        var dam = Service<DalamudAssetManager>.Get();
        if (plugin is null)
            return false;

        if (!Service<PluginImageCache>.Get().TryGetIcon(
                plugin,
                plugin.Manifest,
                plugin.IsThirdParty,
                out var texture, out _) || texture is null)
        {
            texture = dam.GetDalamudTextureWrap(DalamudAsset.DefaultIcon);
        }

        return DrawIconFrom(minCoord, maxCoord, texture);
    }

    /// <summary>Draws the Dalamud logo as an icon.</summary>
    /// <param name="minCoord">The coordinates of the top left of the icon area.</param>
    /// <param name="maxCoord">The coordinates of the bottom right of the icon area.</param>
    internal static void DrawIconFromDalamudLogo(Vector2 minCoord, Vector2 maxCoord)
    {
        var dam = Service<DalamudAssetManager>.Get();
        var texture = dam.GetDalamudTextureWrap(DalamudAsset.LogoSmall);
        DrawIconFrom(minCoord, maxCoord, texture);
    }
}
