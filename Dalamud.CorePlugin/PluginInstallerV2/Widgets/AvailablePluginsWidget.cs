using System;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.CorePlugin.PluginInstallerV2.Drawing;
using Dalamud.CorePlugin.PluginInstallerV2.Interfaces;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.Windows;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Utility;

namespace Dalamud.CorePlugin.PluginInstallerV2.Widgets;

/// <summary>
/// Class responsible for drawing the AvailablePlugins List, and selected plugin information pane.
/// </summary>
internal class AvailablePluginsWidget : IPluginInstallerWidget
{
    // Initialized via required ParentWindow property.
    private readonly AvailablePluginRenderer pluginEntryRenderer = null!;

    /// <inheritdoc/>
    public required PluginInstallerWindow2 ParentWindow
    {
        get;
        init
        {
            field = value;
            this.pluginEntryRenderer = new AvailablePluginRenderer { ParentWindow = value };
        }
    }

    private IPluginManifest? SelectedPlugin { get; set; }

    private int CurrentImageIndex { get; set; }

    /// <inheritdoc/>
    public void Draw()
    {
        if (this.SelectedPlugin is null)
        {
            if (this.ParentWindow.PluginListManager.PluginListAvailable.Count is not 0)
            {
                ImGuiClip.ClippedDraw(this.ParentWindow.PluginListManager.PluginListAvailable, this.DrawPluginEntry, this.pluginEntryRenderer.EntryInnerHeight * ImGuiHelpers.GlobalScale);
            }
            else
            {
                ImGuiHelpers.ScaledDummy(15.0f);

                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudOrange))
                {
                    ImGuiHelpers.CenteredText(PluginInstallerLocs.TabBody_SearchNoCompatible);
                }
            }
        }
        else
        {
            this.DrawSelectedPlugin();
        }
    }

    private void DrawPluginEntry(RemotePluginManifest manifest)
    {
        this.pluginEntryRenderer.DrawAvailablePlugin(manifest, manifestEntry =>
        {
            this.SelectedPlugin = manifestEntry;
        });
    }

    private void DrawSelectedPlugin()
    {
        if (this.SelectedPlugin is null)
        {
            return;
        }

        using (ImRaii.PushColor(ImGuiCol.Button, Vector4.Zero))
        using (this.ParentWindow.FontManager.LargerIconFontHandle.Value.Push())
        {
            if (ImGui.Button(FontAwesomeIcon.ChevronCircleLeft.ToIconString(), new Vector2(64.0f * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionMax().Y)))
            {
                this.SelectedPlugin = null;
                this.CurrentImageIndex = 0;
                return; // May cause flickering? Be sure to test later -midorikami
            }
        }

        ImGui.SameLine();

        this.DrawPluginInformation(this.SelectedPlugin);
    }

    /// <summary>
    /// Draws the lage pae version of this plugin's information.
    /// </summary>
    /// <param name="manifest">Plugin Manifest.</param>
    private void DrawPluginInformation(IPluginManifest manifest)
    {
        using var id = ImRaii.PushId(manifest.InternalName);

        // Changelog
        // Controls (footer)

        var startPosition = ImGui.GetCursorPos();
        var icon = ManifestHelpers.GetPluginIcon(manifest);
        var iconScale = 0.9f;
        var iconSize = ImGui.GetContentRegionMax() * iconScale;
        var squareSize = new Vector2(Math.Min(iconSize.X, iconSize.Y));

        ImGui.SetCursorPos((ImGui.GetContentRegionMax() / 2.0f) - (squareSize / 2.0f));
        ImGui.Image(icon.Handle, squareSize, Vector2.Zero, Vector2.One, new Vector4(1.0f, 1.0f, 1.0f, 0.10f));

        ImGui.SetCursorPos(startPosition);

        using var pluginInfoChild = ImRaii.Child("InfoChild", ImGui.GetContentRegionAvail());
        if (!pluginInfoChild.Success)
        {
            return;
        }

        var sourceChildSize = ImGuiHelpers.ScaledVector2(125.0f, 40.0f);
        this.DrawTitleElements(manifest);

        var endPosition = ImGui.GetCursorPos();
        ImGui.SetCursorPos(new Vector2((ImGui.GetContentRegionMax().X - sourceChildSize.X) + ImGui.GetStyle().ItemSpacing.X, 2.0f * ImGuiHelpers.GlobalScale));
        using (var sourceChild = ImRaii.Child("SourceChild", sourceChildSize))
        {
            if (sourceChild.Success)
            {
                PluginEntryRenderer.DrawPluginSource(manifest);
            }
        }

        ImGui.SetCursorPos(endPosition);

        ImGui.Separator();

        if (manifest.Tags?.Count > 0)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, $"Tags: {string.Join(", ", manifest.Tags ?? ["No Tags"])}");
        }

        if (manifest.DownloadCount > 0)
        {
            ImGui.SameLine();

            var downloads = $"{manifest.DownloadCount:N0} {PluginInstallerLocs.Header_Downloads}";
            var textSize = ImGui.CalcTextSize(downloads);

            ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - textSize.X);
            ImGui.TextColored(ImGuiColors.DalamudGrey, downloads);
        }

        ImGuiHelpers.ScaledDummy(5.0f);

        var footerHeight = 35.0f * ImGuiHelpers.GlobalScale;

        using (var contentsChild = ImRaii.Child("ContentSChild", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - footerHeight)))
        {
            if (contentsChild.Success)
            {
                this.DrawImages(manifest);

                ImGuiHelpers.ScaledDummy(5.0f);

                ImGui.TextWrapped(manifest.Description);
            }
        }

        ImGui.Separator();

        using (var footerChild = ImRaii.Child("Footer", ImGui.GetContentRegionAvail()))
        {
            if (footerChild.Success)
            {
                var isInstalled = ManifestHelpers.GetInstalledPluginFromManifest(manifest) is not null;

                if (!isInstalled)
                {
                    using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.HealerGreen.Fade(0.60f)))
                    {
                        if (ImGui.Button(PluginInstallerLocs.PluginButton_InstallVersion(manifest.AssemblyVersion.ToString()), new Vector2(300.0f * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().Y)))
                        {
                        }
                    }
                }
                else
                {
                    using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.DalamudRed.Fade(0.60f)))
                    {
                        if (ImGui.Button(PluginInstallerLocs.PluginButton_Unload, new Vector2(300.0f * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().Y)))
                        {
                        }
                    }
                }
            }
        }
    }

    private void DrawImages(IPluginManifest manifest)
    {
        var imageAreaHeight = ImGui.GetWindowSize().X / 3.5f;
        if (manifest.ImageUrls?.Count is not 0)
        {
            var plugin = ManifestHelpers.GetInstalledPluginFromManifest(manifest);
            if (Service<PluginImageCache>.Get().TryGetImages(plugin, manifest, plugin?.IsThirdParty ?? false, out var imageTextures) && !imageTextures.All(texture => texture is null))
            {
                using var imageChild = ImRaii.Child("Image", new Vector2(ImGui.GetContentRegionAvail().X, imageAreaHeight));
                if (imageChild.Success)
                {
                    var region = ImGui.GetContentRegionAvail();
                    var halfSize = region / 2.0f;
                    var buttonSize = new Vector2(48.0f, 48.0f);
                    var buttonPadding = 10.0f * ImGuiHelpers.GlobalScale;

                    var image = imageTextures[this.CurrentImageIndex];

                    // todo: better scaling, if the image is very wide it doesn't work great.
                    var imageSize = new Vector2(image?.Width ?? halfSize.X, image?.Height ?? region.Y);
                    var widthRatio = imageSize.X / halfSize.X;
                    var heightRatio = imageSize.Y / region.Y;
                    var ratio = Math.Min(widthRatio, heightRatio);

                    imageSize /= ratio;

                    ImGui.SetCursorPos(new Vector2(halfSize.X - (imageSize.X / 2.0f) - buttonSize.X - buttonPadding, halfSize.Y - (buttonSize.Y / 2.0f)));
                    using (ImRaii.Disabled(this.CurrentImageIndex is 0))
                    {
                        using (ImRaii.PushColor(ImGuiCol.Button, Vector4.Zero))
                        using (this.ParentWindow.FontManager.LargerIconFontHandle.Value.Push())
                        {
                            if (ImGui.Button(FontAwesomeIcon.ChevronLeft.ToIconString(), buttonSize))
                            {
                                this.CurrentImageIndex--;
                            }
                        }
                    }

                    ImGui.SameLine();
                    ImGui.SetCursorPos(new Vector2(halfSize.X - (imageSize.X / 2.0f), 0.0f));

                    ImGui.Image(image?.Handle ?? 0, imageSize);

                    if (image is not null)
                    {
                        ImGui.OpenPopupOnItemClick("ImageButBigger");

                        using var bigImagePopup = ImRaii.Popup("ImageButBigger");
                        if (bigImagePopup.Success)
                        {
                            ImGui.Image(image.Handle, image.Size);
                        }
                    }

                    ImGui.SameLine();

                    ImGui.SetCursorPos(new Vector2(halfSize.X + (imageSize.X / 2.0f) + buttonPadding, halfSize.Y - (buttonSize.Y / 2.0f)));
                    using (ImRaii.Disabled(this.CurrentImageIndex >= imageTextures.IndexOf(texture => texture is null) - 1))
                    {
                        using (ImRaii.PushColor(ImGuiCol.Button, Vector4.Zero))
                        using (this.ParentWindow.FontManager.LargerIconFontHandle.Value.Push())
                        {
                            if (ImGui.Button(FontAwesomeIcon.ChevronRight.ToIconString(), buttonSize))
                            {
                                this.CurrentImageIndex++;
                            }
                        }
                    }
                }
            }
        }
    }

    private void DrawTitleElements(IPluginManifest manifest)
    {
        using (this.ParentWindow.FontManager.LargerFontHandle.Value.Push())
        {
            ImGuiHelpers.CenteredText(manifest.Name);
        }

        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
        {
            ImGuiHelpers.CenteredText(PluginInstallerLocs.PluginBody_AuthorWithoutDownloadCount(manifest.Author));
        }

        if (ManifestHelpers.IsThirdParty(manifest))
        {
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
            {
                ImGuiHelpers.CenteredText(manifest.RepoUrl);
            }
        }

        var punchlineString = ManifestHelpers.GetPunchline(manifest);

        // If the punchline fits in one line, then center it to make it look nice.
        if (ImGui.CalcTextSize(punchlineString).X < ImGui.GetContentRegionAvail().X)
        {
            ImGuiHelpers.CenteredText(punchlineString);
        }
        else
        {
            ImGui.TextWrapped(punchlineString);
        }
    }
}
