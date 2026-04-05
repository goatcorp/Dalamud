using System.Drawing;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.Windows;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal.Types.Manifest;

namespace Dalamud.CorePlugin.PluginInstallerV2.Drawing;

/// <summary>
/// Class containing all the standard parts of a Plugin Entry for display in the Plugin Installer.
/// </summary>
internal abstract class PluginEntryRenderer
{
    /// <summary>
    /// Gets the size of each plugin entries height.
    /// </summary>
    public virtual float EntryInnerHeight { get; init; } = 72.0f;

    /// <summary>
    /// Gets reference to parent plugin installer window.
    /// </summary>
    public required PluginInstallerWindow2 ParentWindow { get; init; }

    /// <summary>
    /// Gets plugin icon from Manifest.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    /// <returns>Default Icon or Loaded Texture.</returns>
    protected static IDalamudTextureWrap GetPluginIcon(RemotePluginManifest manifest)
    {
        var imageCache = Service<PluginImageCache>.Get();

        var iconTex = imageCache.DefaultIcon;
        var hasIcon = imageCache.TryGetIcon(null, manifest, manifest.SourceRepo.IsThirdParty, out var cachedIconTex, out _);

        if (hasIcon && cachedIconTex != null)
        {
            iconTex = cachedIconTex;
        }

        return iconTex;
    }

    /// <summary>
    /// Draws a Plugin Icon to fill container.
    /// </summary>
    /// <param name="iconImage">Icon Texture.</param>
    protected static void DrawPluginIcon(IDalamudTextureWrap iconImage)
    {
        var padding = ImGuiHelpers.ScaledVector2(2.0f, 2.0f);
        var size = ImGui.GetContentRegionMax() - (padding * 2.0f);

        ImGui.SetCursorPos(ImGui.GetCursorPos() + padding);
        ImGui.Image(iconImage.Handle, size);
    }

    /// <summary>
    /// Draws the current download count from manifest.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    protected static void DrawPluginDownloadCount(RemotePluginManifest manifest)
    {
        if (manifest.DownloadCount <= 0)
        {
            return;
        }

        ImGui.SameLine();

        var downloadCount = $"{manifest.DownloadCount:N0} {PluginInstallerLocs.Header_Downloads}";
        var textSize = ImGui.CalcTextSize(downloadCount);

        ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - textSize.X - ImGui.GetStyle().ItemSpacing.X);
        ImGui.TextColored(ImGuiColors.DalamudGrey, downloadCount);
    }

    /// <summary>
    /// Draws a plugin punchline. Centered vertically in the content area.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    protected static void DrawPunchline(RemotePluginManifest manifest)
    {
        var punchline = manifest.Punchline;
        var textSize = ImGui.CalcTextSize(punchline);

        // Center text vertically.
        ImGui.SetCursorPosY((ImGui.GetCursorPosY() + (ImGui.GetContentRegionAvail().Y / 2.0f)) - (textSize.Y / 2.0f));
        ImGui.Text(punchline);
    }

    /// <summary>
    /// Draws font awesome icon multiple times to outline it.
    /// </summary>
    /// <param name="icon">FontAwesome Icon.</param>
    /// <param name="outline">Outline Color.</param>
    /// <param name="iconColor">Icon Color.</param>
    protected static void DrawFontawesomeIconOutlined(FontAwesomeIcon icon, Vector4 outline, Vector4 iconColor)
    {
        var positionOffset = ImGuiHelpers.ScaledVector2(0.0f, 1.0f);
        var cursorStart = ImGui.GetCursorPos() + positionOffset;

        using var font = ImRaii.PushFont(InterfaceManager.IconFontFixedWidth);
        using (ImRaii.PushColor(ImGuiCol.Text, outline))
        {
            foreach (var x in Enumerable.Range(-1, 3))
            {
                foreach (var y in Enumerable.Range(-1, 3))
                {
                    if (x is 0 && y is 0) continue;

                    ImGui.SetCursorPos(cursorStart + new Vector2(x, y));
                    ImGui.Text(icon.ToIconString());
                }
            }
        }

        using (ImRaii.PushColor(ImGuiCol.Text, iconColor))
        {
            ImGui.SetCursorPos(cursorStart);
            ImGui.Text(icon.ToIconString());
        }

        ImGui.SetCursorPos(ImGui.GetCursorPos() - positionOffset);
    }

    /// <summary>
    /// Draws a line indicating whether the source repo is from the Dalamud Repo, or a Custom Repo.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    protected void DrawRepoSource(RemotePluginManifest manifest)
    {
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (2.0f * ImGuiHelpers.GlobalScale));
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (2.0f * ImGuiHelpers.GlobalScale));

        if (this.IsPluginInstalled(manifest) && this.IsDevPlugin(manifest))
        {
            DrawFontawesomeIconOutlined(FontAwesomeIcon.Wrench, KnownColor.White.Vector(), KnownColor.MediumOrchid.Vector());

            ImGui.SameLine();
            ImGuiHelpers.ScaledDummy(0.0f);
            ImGui.SameLine();

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (1.0f * ImGuiHelpers.GlobalScale));
            ImGui.TextColored(KnownColor.MediumOrchid.Vector(), PluginInstallerLocs.PluginTitleMod_DevPlugin);
        }
        else
        {
            if (!manifest.SourceRepo.IsThirdParty)
            {
                DrawFontawesomeIconOutlined(FontAwesomeIcon.CheckCircle, KnownColor.White.Vector(), KnownColor.RoyalBlue.Vector());

                ImGui.SameLine();
                ImGuiHelpers.ScaledDummy(0.0f);
                ImGui.SameLine();

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (1.0f * ImGuiHelpers.GlobalScale));
                ImGui.TextColored(KnownColor.CornflowerBlue.Vector(), PluginInstallerLocs.VerifiedCheckmark_DalamudApproved);
            }
            else
            {
                DrawFontawesomeIconOutlined(FontAwesomeIcon.ExclamationCircle, KnownColor.Black.Vector(), KnownColor.Orange.Vector());

                ImGui.SameLine();
                ImGuiHelpers.ScaledDummy(0.0f);
                ImGui.SameLine();

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (1.0f * ImGuiHelpers.GlobalScale));
                ImGui.TextColored(KnownColor.Orange.Vector(), PluginInstallerLocs.VerifiedCheckmark_CustomRepo);
            }
        }
    }

    /// <summary>
    /// Draw Plugin Name and Author.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    protected void DrawPluginTitle(RemotePluginManifest manifest)
    {
        using (this.ParentWindow.FontManager.LargerFontHandle.Value.Push())
        {
            ImGui.Text(manifest.Name);
        }

        if (manifest.Author is not null)
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (4.0f * ImGuiHelpers.GlobalScale));

            using (ImRaii.PushIndent())
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, PluginInstallerLocs.PluginBody_AuthorWithoutDownloadCount(manifest.Author));
            }
        }
    }

    /// <summary>
    /// Gets whether the provided manifest matches an installed plugin.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    /// <returns>If this represents an installed plugin.</returns>
    protected bool IsPluginInstalled(RemotePluginManifest manifest)
        => this.ParentWindow.PluginListManager.PluginListInstalled.Any(plugin => plugin.InternalName == manifest.InternalName);

    /// <summary>
    /// Gets whether the provided manifest matches a dev plugin.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    /// <returns>If this represents an installed dev plugin.</returns>
    protected bool IsDevPlugin(RemotePluginManifest manifest)
        => this.ParentWindow.PluginListManager.PluginListInstalled
               .FirstOrDefault(plugin => plugin.InternalName == manifest.InternalName)?.IsDev ?? false;
}
