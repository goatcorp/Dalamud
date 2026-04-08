using System;
using System.Drawing;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.CorePlugin.PluginInstallerV2.Controllers;
using Dalamud.CorePlugin.PluginInstallerV2.Enums;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Internal.Types.Manifest;

namespace Dalamud.CorePlugin.PluginInstallerV2.Drawing;

/// <summary>
/// Class responsible for laying out and drawing a AvailablePlugin.
/// </summary>
internal class AvailablePluginRenderer : PluginEntryRenderer
{
    /// <summary>
    /// Draws an available plugin entry from RemotePluginManifest.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    /// <param name="onEntryClicked">Callback when entry is clicked.</param>
    public void DrawAvailablePlugin(RemotePluginManifest manifest, Action<IPluginManifest>? onEntryClicked = null)
    {
        using var id = ImRaii.PushId(manifest.InternalName);

        var entrySize = new Vector2(ImGui.GetContentRegionMax().X, this.EntryInnerHeight * ImGuiHelpers.GlobalScale);

        using var pushColor = ImRaii.PushColor(ImGuiCol.ChildBg, KnownColor.Gray.Vector().Fade(0.75f), this.ParentWindow.PluginListManager.PluginListAvailable.IndexOf(manifest) % 2 == 0);
        using var entryChild = ImRaii.Child($"AvailablePluginEntry", entrySize, false, ImGuiWindowFlags.NoScrollbar);
        if (!entryChild.Success)
        {
            return;
        }

        pushColor.Pop();

        DrawBackgroundTexture(manifest);

        var startPosition = ImGui.GetCursorPos();
        if (ImGui.Selectable($"##{manifest.InternalName}", false, ImGuiSelectableFlags.None, ImGui.GetContentRegionMax()))
        {
            onEntryClicked?.Invoke(manifest);
        }

        ImGui.OpenPopupOnItemClick("AvailablePluginContextMenu");

        ImGui.SetCursorPos(startPosition);

        using (var imageChild = ImRaii.Child($"ImageChild", ImGuiHelpers.ScaledVector2(this.EntryInnerHeight, this.EntryInnerHeight), false, ImGuiWindowFlags.NoInputs))
        {
            if (imageChild.Success)
            {
                var imageModifier = this.GetImageModifier(manifest);

                var iconOverlayStart = ImGui.GetCursorPos();
                using (ImRaii.Disabled(imageModifier is not (PluginImageModifier.None or PluginImageModifier.Installed)))
                {
                    DrawPluginIcon(GetPluginIcon(manifest));
                }

                ImGui.SetCursorPos(iconOverlayStart);
                DrawPluginStatusTexture(imageModifier);
            }
        }

        ImGui.SameLine();

        using (var contentsChild = ImRaii.Child("ContentsChild", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.NoInputs))
        {
            if (contentsChild.Success)
            {
                var downloadSize = 150.0f * ImGuiHelpers.GlobalScale;
                var repositorySize = 125.0f * ImGuiHelpers.GlobalScale;

                using (var titleChild = ImRaii.Child(
                           $"TitleChild",
                           new Vector2(ImGui.GetContentRegionAvail().X - downloadSize - repositorySize - (ImGui.GetStyle().ItemSpacing.X * 2.0f), ImGui.GetContentRegionAvail().Y * (3.0f / 5.0f)),
                           false,
                           ImGuiWindowFlags.NoInputs))
                {
                    if (titleChild.Success)
                    {
                        this.DrawPluginTitle(manifest);
                    }
                }

                ImGui.SameLine();

                // Extras are Download Count, Source Repository, and maybe in the future some badge icons.
                using (var extrasChild = ImRaii.Child($"ExtrasChild", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y * (3.0f / 5.0f)), false, ImGuiWindowFlags.NoInputs))
                {
                    if (extrasChild.Success)
                    {
                        using (var topLineChild = ImRaii.Child("TopLineChild", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y / 2.0f), false, ImGuiWindowFlags.NoInputs))
                        {
                            if (topLineChild.Success)
                            {
                                using (var downloadsChild = ImRaii.Child("DownloadsChild", new Vector2(downloadSize, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.NoInputs))
                                {
                                    if (downloadsChild.Success)
                                    {
                                        DrawPluginDownloadCount(manifest);
                                    }
                                }

                                ImGui.SameLine();

                                using (var repoChild = ImRaii.Child("RepositoryChild", new Vector2(repositorySize, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                                {
                                    if (repoChild.Success)
                                    {
                                        this.DrawPluginSource(manifest);
                                    }
                                }
                            }
                        }

                        using (var bottomLineChild = ImRaii.Child("BottomLineChild", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.NoInputs))
                        {
                            if (bottomLineChild.Success)
                            {
                                // Placeholder for Badges like AI Generated, etc.
                            }
                        }
                    }
                }

                // Footer is Punchline and a toggle button to enable/disable.
                using (var footerChild = ImRaii.Child("FooterChild", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.NoInputs))
                {
                    if (footerChild.Success)
                    {
                        var installedPlugin = this.ParentWindow.PluginListManager.PluginListInstalled.FirstOrDefault(plugin => plugin.InternalName == manifest.InternalName);
                        var controlWidgetSize = installedPlugin is not null ? ImGui.GetFrameHeight() * 1.55f : 0.0f;
                        var punchlineSpacing = installedPlugin is not null ? ImGui.GetStyle().ItemSpacing.X : 0.0f;

                        using (var punchlineChild = ImRaii.Child(
                                   "PunchlineChild",
                                   new Vector2(ImGui.GetContentRegionAvail().X - controlWidgetSize - punchlineSpacing, ImGui.GetContentRegionAvail().Y),
                                   false,
                                   ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                        {
                            if (punchlineChild.Success)
                            {
                                DrawPunchline(manifest);
                            }
                        }

                        DrawInstalledPluginToggleWidget(installedPlugin);
                    }
                }
            }
        }

        this.DrawContextMenu(manifest);
    }

    private static void DrawBackgroundTexture(RemotePluginManifest manifest)
    {
        var configuration = Service<DalamudConfiguration>.Get();

        if (!configuration.DoPluginTest)
        {
            return;
        }

        if (manifest.IsTestingExclusive || (manifest.IsAvailableForTesting && configuration.PluginTestingOptIns.Any(plugin => plugin.InternalName == manifest.InternalName)))
        {
            var startCursor = ImGui.GetCursorPos();
            DrawCautionTape(startCursor + new Vector2(0.0f, 1.0f), ImGui.GetContentRegionAvail(), ImGuiHelpers.GlobalScale * 40);
            ImGui.SetCursorPos(startCursor);
        }
    }

    private static void DrawInstalledPluginToggleWidget(LocalPlugin? installedPlugin)
    {
        if (installedPlugin is not null)
        {
            ImGui.SameLine();

            using var widgetChild = ImRaii.Child("WidgetChild", ImGui.GetContentRegionAvail(), false);
            if (!widgetChild.Success)
            {
                return;
            }

            var isEnabled = installedPlugin.IsLoaded;
            if (ImGuiComponents.ToggleButton("ToggleButton", ref isEnabled))
            {
                if (installedPlugin.IsLoaded)
                {
                    PluginListManager.DisablePlugin(installedPlugin);
                }
                else
                {
                    PluginListManager.EnablePlugin(installedPlugin);
                }
            }
        }
    }

    private void DrawContextMenu(RemotePluginManifest manifest)
    {
        using var popupContextMenu = ImRaii.ContextPopup("AvailablePluginContextMenu");
        if (!popupContextMenu.Success)
        {
            return;
        }

        var configuration = Service<DalamudConfiguration>.Get();

        var hasTestingVersionAvailable = configuration.DoPluginTest && manifest.IsAvailableForTesting;
        if (hasTestingVersionAvailable)
        {
            // If we are opted in to get testing for this plugin
            var pluginTestingOptIn = configuration.PluginTestingOptIns.FirstOrDefault(plugin => plugin.InternalName == manifest.InternalName);
            if (pluginTestingOptIn is not null)
            {
                if (ImGui.MenuItem(PluginInstallerLocs.PluginContext_OptOutTestingVersion))
                {
                    configuration.PluginTestingOptIns.Remove(pluginTestingOptIn);
                    configuration.QueueSave();
                }
            }
            else
            {
                if (ImGui.MenuItem(PluginInstallerLocs.PluginContext_InstallTestingVersion))
                {
                    configuration.PluginTestingOptIns.Add(new PluginTestingOptIn(manifest.InternalName));
                    configuration.QueueSave();

                    this.ParentWindow.PluginListManager.StartInstall(manifest, true);
                }
            }

            ImGui.Separator();
        }

        if (ImGui.MenuItem(PluginInstallerLocs.PluginContext_MarkAllSeen))
        {
            configuration.SeenPluginInternalName.AddRange(this.ParentWindow.PluginListManager.PluginListAvailable.Select(x => x.InternalName));
            configuration.QueueSave();
        }

        var isHidden = configuration.HiddenPluginInternalName.Contains(manifest.InternalName);
        if (!isHidden && ImGui.MenuItem(PluginInstallerLocs.PluginContext_HidePlugin))
        {
            configuration.HiddenPluginInternalName.Add(manifest.InternalName);
            configuration.QueueSave();
        }

        if (isHidden && ImGui.MenuItem(PluginInstallerLocs.PluginContext_UnhidePlugin))
        {
            configuration.HiddenPluginInternalName.Remove(manifest.InternalName);
            configuration.QueueSave();
        }
    }
}
