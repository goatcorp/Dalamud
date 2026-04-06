using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.CorePlugin.PluginInstallerV2.Controllers;
using Dalamud.CorePlugin.PluginInstallerV2.Drawing;
using Dalamud.CorePlugin.PluginInstallerV2.Interfaces;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal.Types.Manifest;

namespace Dalamud.CorePlugin.PluginInstallerV2.Widgets;

/// <summary>
/// Class responsible for drawing the AvailablePlugins List.
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
            this.pluginEntryRenderer = new AvailablePluginRenderer
            {
                ParentWindow = value,
            };
        }
    }

    private RemotePluginManifest? SelectedPlugin { get; set; }

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
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudOrange))
                {
                    // todo: display a localized error here.
                    ImGuiHelpers.CenteredText("Unable to load any Available Plugins.");
                }
            }
        }
        else
        {
            this.DrawSelectedPlugin();
        }
    }

    /// <inheritdoc/>
    public void OnSearchUpdated(SearchController searchInfo)
    {
    }

    private void DrawPluginEntry(RemotePluginManifest manifest)
    {
        this.pluginEntryRenderer.DrawAvailablePlugin(manifest, OnEntryClicked);
        return;

        void OnEntryClicked(RemotePluginManifest clickedManifest)
        {
            this.SelectedPlugin = clickedManifest;
        }
    }

    private void DrawSelectedPlugin()
    {
        using (ImRaii.PushColor(ImGuiCol.Button, Vector4.Zero))
        using (this.ParentWindow.FontManager.LargerIconFontHandle.Value.Push())
        {
            if (ImGui.Button(FontAwesomeIcon.ChevronCircleLeft.ToIconString(), new Vector2(64.0f * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionMax().Y)))
            {
                this.SelectedPlugin = null;
            }
        }
    }
}
