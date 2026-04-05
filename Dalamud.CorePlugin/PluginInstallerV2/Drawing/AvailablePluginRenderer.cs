using System;
using System.Drawing;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal.Types.Manifest;

namespace Dalamud.CorePlugin.PluginInstallerV2.Drawing;

/// <summary>
/// Class responsible for laying out and drawing a AvailablePlugin.
/// </summary>
internal class AvailablePluginRenderer : PluginEntryRenderer
{
    private int entryCounter;

    /// <summary>
    /// Tells the renderer that a new draw sequence is started to keep things like colored backgrounds in sync.
    /// </summary>
    public void ResetDraw()
    {
        this.entryCounter = 0;
    }

    /// <summary>
    /// Draws an available plugin entry from RemotePluginManifest.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    /// <param name="onEntryClicked">Callback when entry is clicked.</param>
    public void DrawAvailablePlugin(RemotePluginManifest manifest, Action<RemotePluginManifest>? onEntryClicked = null)
    {
        using var id = ImRaii.PushId(manifest.InternalName);

        var entrySize = new Vector2(ImGui.GetContentRegionMax().X, this.EntryInnerHeight * ImGuiHelpers.GlobalScale);

        using var pushColor = ImRaii.PushColor(ImGuiCol.ChildBg, KnownColor.Gray.Vector().Fade(0.75f), this.entryCounter++ % 2 == 0);
        using var entryChild = ImRaii.Child($"AvailablePluginEntry", entrySize, false, ImGuiWindowFlags.NoScrollbar);
        if (!entryChild.Success)
        {
            return;
        }

        pushColor.Pop();

        var startPosition = ImGui.GetCursorPos();
        if (ImGui.Selectable($"##{manifest.InternalName}", false, ImGuiSelectableFlags.None, ImGui.GetContentRegionMax()))
        {
            onEntryClicked?.Invoke(manifest);
        }

        ImGui.SetCursorPos(startPosition);

        using (var imageChild = ImRaii.Child($"ImageChild", ImGuiHelpers.ScaledVector2(this.EntryInnerHeight, this.EntryInnerHeight), false, ImGuiWindowFlags.NoInputs))
        {
            if (imageChild.Success)
            {
                DrawPluginIcon(GetPluginIcon(manifest));
            }
        }

        ImGui.SameLine();

        this.DrawContents(manifest);
    }

    private static void DrawExtras(RemotePluginManifest manifest, float downloadSize, float repositorySize)
    {
        using var extrasChild = ImRaii.Child($"ExtrasChild", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y * (3.0f / 5.0f)), false, ImGuiWindowFlags.NoInputs);
        if (!extrasChild.Success)
        {
            return;
        }

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

                using (var repoChild = ImRaii.Child("RepositoryChild", new Vector2(repositorySize, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.NoInputs))
                {
                    if (repoChild.Success)
                    {
                        DrawRepoSource(manifest);
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

    private static void DrawFooter(RemotePluginManifest manifest)
    {
        using var footerChild = ImRaii.Child("FooterChild", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.NoInputs);
        if (!footerChild.Success)
        {
            return;
        }

        var controlWidgetSize = 100.0f * ImGuiHelpers.GlobalScale;

        using (var punchlineChild = ImRaii.Child("PunchlineChild", new Vector2(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X - controlWidgetSize, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if (punchlineChild.Success)
            {
                DrawPunchline(manifest);
            }
        }

        ImGui.SameLine();

        using (var widgetChild = ImRaii.Child("WidgetChild", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.NoInputs))
        {
            if (widgetChild.Success)
            {
                // Placeholder for Enable/Disable Widget
            }
        }
    }

    /// <summary>
    /// Draws plugins main content body.
    /// </summary>
    /// <param name="manifest">Manifest.</param>
    private void DrawContents(RemotePluginManifest manifest)
    {
        using var contentsChild = ImRaii.Child("ContentsChild", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.NoInputs);
        if (!contentsChild.Success)
        {
            return;
        }

        var downloadSize = 150.0f * ImGuiHelpers.GlobalScale;
        var repositorySize = 150.0f * ImGuiHelpers.GlobalScale;

        using (var titleChild = ImRaii.Child($"TitleChild", new Vector2(ImGui.GetContentRegionAvail().X - downloadSize - repositorySize - (ImGui.GetStyle().ItemSpacing.X * 2.0f), ImGui.GetContentRegionAvail().Y * (3.0f / 5.0f)), false, ImGuiWindowFlags.NoInputs))
        {
            if (titleChild.Success)
            {
                this.DrawPluginTitle(manifest);
            }
        }

        ImGui.SameLine();

        // Extras are Download Count, Source Repository, and maybe in the future some badge icons.
        DrawExtras(manifest, downloadSize, repositorySize);

        // Footer is Punchline and a toggle button to enable/disable.
        DrawFooter(manifest);
    }
}
