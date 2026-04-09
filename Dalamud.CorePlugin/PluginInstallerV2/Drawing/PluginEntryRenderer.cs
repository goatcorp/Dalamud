using System.Drawing;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.CorePlugin.PluginInstallerV2.Enums;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.Windows;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Utility;

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
    /// Draws a line the plugin source, Main Repo, Custom Repo, or Dev Plugin.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    protected internal static void DrawPluginSource(IPluginManifest manifest)
    {
        ImGui.SetCursorPos(ImGui.GetCursorPos() + ImGuiHelpers.ScaledVector2(2.0f, 2.0f));

        FontAwesomeIcon icon;
        Vector4 outlineColor;
        Vector4 color;
        string label;
        string tooltip;

        var plugin = ManifestHelpers.GetInstalledPluginFromManifest(manifest);

        if (plugin is { IsDev: true })
        {
            icon = FontAwesomeIcon.Wrench;
            outlineColor = KnownColor.White.Vector();
            color = KnownColor.MediumOrchid.Vector();
            label = PluginInstallerLocs.PluginTitleMod_DevPlugin;
            tooltip = PluginInstallerLocs.VerifiedCheckmark_DevPluginTooltip;
        }
        else
        {
            var sourceRepo = manifest is RemotePluginManifest remotePluginManifest ? remotePluginManifest.SourceRepo : null;
            if (sourceRepo is { IsThirdParty: true })
            {
                icon = FontAwesomeIcon.ExclamationCircle;
                outlineColor = KnownColor.Black.Vector();
                color = KnownColor.Orange.Vector();
                label = PluginInstallerLocs.VerifiedCheckmark_CustomRepo;
                tooltip = PluginInstallerLocs.VerifiedCheckmark_UnverifiedTooltip;
            }
            else
            {
                icon = FontAwesomeIcon.CheckCircle;
                outlineColor = KnownColor.White.Vector();
                color = KnownColor.RoyalBlue.Vector();
                label = PluginInstallerLocs.VerifiedCheckmark_DalamudApproved;
                tooltip = PluginInstallerLocs.VerifiedCheckmark_VerifiedTooltip;
            }
        }

        DrawFontawesomeIconOutlined(icon, outlineColor, color);
        var isHovered = ImGui.IsItemHovered();

        ImGui.SameLine();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (1.0f * ImGuiHelpers.GlobalScale));
        ImGui.TextColored(color, label);
        isHovered |= ImGui.IsItemHovered();

        if (isHovered)
        {
            ImGui.SetTooltip(tooltip);
        }
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
    protected static void DrawPluginDownloadCount(IPluginManifest manifest)
    {
        if (manifest.DownloadCount <= 0)
        {
            return;
        }

        var downloadCount = $"{manifest.DownloadCount:N0} {PluginInstallerLocs.Header_Downloads}";
        var textSize = ImGui.CalcTextSize(downloadCount);

        ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - textSize.X - ImGui.GetStyle().ItemSpacing.X);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (2.0f * ImGuiHelpers.GlobalScale));
        ImGui.TextColored(ImGuiColors.DalamudGrey, downloadCount);
    }

    /// <summary>
    /// Draws a plugin punchline. Centered vertically in the content area.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    protected static void DrawPunchline(IPluginManifest manifest)
    {
        var punchline = ManifestHelpers.GetPunchline(manifest);
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
    /// Draws caution tape background effect for testing plugins.
    /// </summary>
    /// <param name="position">Position to draw at.</param>
    /// <param name="size">Size to draw.</param>
    /// <param name="stripeWidth">Width of each stripe.</param>
    /// <param name="skewAmount">Tilt of each stripe.</param>
    protected static void DrawCautionTape(Vector2 position, Vector2 size, float stripeWidth, float skewAmount = 20.0f)
    {
        var drawList = ImGui.GetWindowDrawList();

        var windowPos = ImGui.GetWindowPos();
        var scroll = new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());

        var adjustedPosition = (windowPos + position) - scroll;

        var yellow = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.9f, 0.0f, 0.10f));
        var numStripes = (int)(size.X / stripeWidth) + (int)(size.Y / skewAmount) + 1;  // +1 to cover partial stripe

        for (var i = 0; i < numStripes; i++)
        {
            var x0 = adjustedPosition.X + (i * stripeWidth);
            var x1 = x0 + stripeWidth;
            var y0 = adjustedPosition.Y;
            var y1 = y0 + size.Y;

            var p0 = new Vector2(x0, y0);
            var p1 = new Vector2(x1, y0);
            var p2 = new Vector2(x1 - skewAmount, y1);
            var p3 = new Vector2(x0 - skewAmount, y1);

            if (i % 2 != 0)
            {
                continue;
            }

            drawList.AddQuadFilled(p0, p1, p2, p3, yellow);
        }
    }

    /// <summary>
    /// Draws the image that overlays the plugin icon indicating the plugins status.
    /// </summary>
    /// <param name="modifierImage">Modifier Image.</param>
    protected static void DrawPluginStatusTexture(PluginImageModifier modifierImage)
    {
        var iconCache = Service<PluginImageCache>.Get();

        var modifierTexture = modifierImage switch
        {
            PluginImageModifier.None => null,
            PluginImageModifier.Incompatible => iconCache.TroubleIcon,
            PluginImageModifier.Disabled => iconCache.DisabledIcon,
            PluginImageModifier.Updatable => iconCache.UpdateIcon,
            PluginImageModifier.Installed => iconCache.InstalledIcon,
            PluginImageModifier.Outdated => iconCache.OutdatedInstallableIcon,
            _ => null,
        };

        ImGui.Image(modifierTexture?.Handle ?? 0, ImGui.GetContentRegionAvail());
    }

    /// <summary>
    /// Gets a value indicating what kind of modifier to use for this manifest.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    /// <returns>What image modifier is needed to be rendered.</returns>
    protected PluginImageModifier GetImageModifier(IPluginManifest manifest)
    {
        var plugin = ManifestHelpers.GetInstalledPluginFromManifest(manifest);

        if (manifest.MinimumDalamudVersion != null &&
            manifest.MinimumDalamudVersion > Versioning.GetAssemblyVersionParsed())
        {
            return PluginImageModifier.Incompatible;
        }

        switch (plugin)
        {
            case { IsOutdated: true } or { IsBanned: true } or { IsDecommissioned: true, IsOrphaned: false }:
                return PluginImageModifier.Incompatible;

            case { IsWantedByAnyProfile: false }:
                return PluginImageModifier.Disabled;

            case not null when ManifestHelpers.IsAvailableForUpdate(plugin):
                return PluginImageModifier.Updatable;

            case { IsLoaded: true }:
                return PluginImageModifier.Installed;

            default:
                return PluginImageModifier.None;
        }
    }

    /// <summary>
    /// Draw Plugin Name and Author.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    protected void DrawPluginTitle(IPluginManifest manifest)
    {
        using (this.ParentWindow.FontManager.LargerFontHandle.Value.Push())
        {
            ImGui.Text(manifest.Name);
        }

        if (!manifest.Author.IsNullOrEmpty())
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (4.0f * ImGuiHelpers.GlobalScale));

            using (ImRaii.PushIndent())
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, PluginInstallerLocs.PluginBody_AuthorWithoutDownloadCount(manifest.Author));
            }
        }

        PrintPluginStatusText(manifest);
    }

    /// <summary>
    /// Draw plugin status text near the plugin title.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    private static void PrintPluginStatusText(IPluginManifest manifest)
    {
        var pluginManager = Service<PluginManager>.Get();
        // var configuration = Service<DalamudConfiguration>.Get();

        var canUseTesting = pluginManager.CanUseTesting(manifest);
        var useTesting = pluginManager.UseTesting(manifest);
        // var wasSeen = PluginListManager.WasPluginSeen(manifest.InternalName);

        var statusText = string.Empty;
        var color = KnownColor.White.Vector();
        // var plugin = ManifestHelpers.GetInstalledPluginFromManifest(manifest);

        if (useTesting)
        {
            statusText = PluginInstallerLocs.PluginTitleMod_TestingVersion;
            color = ImGuiColors.DalamudOrange;
        }
        else if (canUseTesting)
        {
            statusText = PluginInstallerLocs.PluginTitleMod_TestingAvailable;
            color = ImGuiColors.DalamudOrange;
        }

        // if (plugin is { IsTesting: true })
        // {
        //     statusText = PluginInstallerLocs.PluginTitleMod_TestingVersion;
        //     color = ImGuiColors.DalamudOrange;
        // }
        // else
        // {
        //     var testingEnabled = configuration.DoPluginTest;
        //     var canTesting = pluginManager.AvailablePlugins.Any(x => x.InternalName == manifest.InternalName && x.IsAvailableForTesting);
        //     var isOptedIn = configuration.PluginTestingOptIns.Any(x => x.InternalName == plugin?.Manifest.InternalName);
        //
        //     if (testingEnabled && canTesting && isOptedIn)
        //     {
        //         statusText = PluginInstallerLocs.PluginTitleMod_TestingAvailable;
        //         color = ImGuiColors.DalamudYellow;
        //     }
        // }

        // if (plugin is { IsWantedByAnyProfile: true })
        // {
        //     statusText = PluginInstallerLocs.PluginTitleMod_Installed;
        //     color = ImGuiColors.HealerGreen;
        // }
        // else if (!Service<DalamudConfiguration>.Get().SeenPluginInternalName.Contains(manifest.InternalName))
        // {
        //     statusText = PluginInstallerLocs.PluginTitleMod_New;
        //     color = ImGuiColors.TankBlue;
        // }

        var textSize = ImGui.CalcTextSize(statusText);

        ImGui.SetCursorPos(new Vector2(ImGui.GetContentRegionMax().X - textSize.X, 0.0f));
        ImGui.TextColored(color, statusText);
    }
}
